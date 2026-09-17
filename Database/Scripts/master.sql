DROP TABLE IF EXISTS company, module, 
    subscription_plan, plan_module, company_subscription,
    app_role, app_user, user_token, refresh_token;

-- ============================================================
-- MASTER DB (Merkezi Veri Tabanı)
-- ============================================================

-- 1. SaaS Şirketleri (Tenants)
CREATE TABLE company (
    id INT GENERATED ALWAYS AS IDENTITY,
    public_id UUID NOT NULL DEFAULT gen_random_uuid() UNIQUE,
    name VARCHAR(255) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    create_date TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_company PRIMARY KEY (id)
);
 
-- 2. Modüller (uygulama katmanındaki "Service" ile karışmaması için "module")
CREATE TABLE module (
    id INT GENERATED ALWAYS AS IDENTITY,
    module_key VARCHAR(50) NOT NULL UNIQUE,    -- route'ta kullanılacak: 'cost-management'
    name VARCHAR(255) NOT NULL,                -- kullanıcıya gösterilen: 'Masraf Yönetimi'
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT pk_module PRIMARY KEY (id)
);

-- 3. Abonelik Planları ve Modül Yetkileri
CREATE TABLE subscription_plan (
    id INT GENERATED ALWAYS AS IDENTITY,
    name VARCHAR(255) NOT NULL,
    price DECIMAL(18,2) NOT NULL DEFAULT 0,
    description VARCHAR(500) NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT pk_subscription_plan PRIMARY KEY (id)
);

CREATE TABLE plan_module (
    plan_id INT NOT NULL,
    module_id INT NOT NULL,
    CONSTRAINT pk_plan_module PRIMARY KEY (plan_id, module_id),
    CONSTRAINT fk_plan_to_plan_module FOREIGN KEY (plan_id) REFERENCES subscription_plan(id),
    CONSTRAINT fk_module_to_plan_module FOREIGN KEY (module_id) REFERENCES module(id)
);

CREATE TABLE company_subscription (
    id INT GENERATED ALWAYS AS IDENTITY,
    company_id INT NOT NULL,
    plan_id INT NOT NULL,
    expires_at TIMESTAMPTZ NOT NULL,
    auto_renew BOOLEAN NOT NULL DEFAULT TRUE,
    renewal_failed_at TIMESTAMPTZ NULL,
    renewal_attempt_count INT NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,   -- "en güncel kayıt" demek, "geçerli" demek değil
    create_date TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_company_subscription PRIMARY KEY (id),
    CONSTRAINT fk_company_to_subscription FOREIGN KEY (company_id) REFERENCES company(id),
    CONSTRAINT fk_plan_to_subscription FOREIGN KEY (plan_id) REFERENCES subscription_plan(id)
);

-- 4. Roller ve Kullanıcılar
CREATE TABLE app_role (
    id INT GENERATED ALWAYS AS IDENTITY,
    name VARCHAR(255) NOT NULL,
    CONSTRAINT pk_app_role PRIMARY KEY (id)
);

CREATE TABLE app_user (
    id INT GENERATED ALWAYS AS IDENTITY,
    public_id UUID NOT NULL DEFAULT gen_random_uuid() UNIQUE,
    company_id INT NOT NULL,
    role_id INT NOT NULL,
    org_unit_id UUID,   -- MANTIKSAL BAĞLANTI: FK yok, CMS DB'deki org_unit.public_id
    name VARCHAR(255) NOT NULL,
    email VARCHAR(255) NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,
    recovery_key_hash TEXT,
    is_verified BOOLEAN NOT NULL DEFAULT FALSE,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    password_changed_at TIMESTAMPTZ,
    create_user UUID,
    create_date TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    update_user UUID,
    update_date TIMESTAMPTZ,
    delete_user UUID,
    delete_date TIMESTAMPTZ,
    CONSTRAINT pk_app_user PRIMARY KEY (id),
    CONSTRAINT fk_company_to_app_user FOREIGN KEY (company_id) REFERENCES company(id),
    CONSTRAINT fk_role_to_app_user FOREIGN KEY (role_id) REFERENCES app_role(id)
);

-- 5. Doğrulama ve Kurtarma Tokenları
CREATE TABLE user_token (
    id INT GENERATED ALWAYS AS IDENTITY,
    user_id INT NOT NULL,
    token_hash CHAR(64) NOT NULL, -- SHA-256 hex: her zaman tam 64 karakter
    token_type VARCHAR(50) NOT NULL,
    expires_at TIMESTAMPTZ NOT NULL,
    used BOOLEAN NOT NULL DEFAULT FALSE,
    create_date TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_user_token PRIMARY KEY (id),
    CONSTRAINT fk_user_to_token FOREIGN KEY (user_id) REFERENCES app_user(id)
);

-- 6. Refresh Tokenları (Oturum Yönetimi)
CREATE TABLE refresh_token (
    id INT GENERATED ALWAYS AS IDENTITY,
    user_id INT NOT NULL,
    token_hash CHAR(64) NOT NULL,
    expires_at TIMESTAMPTZ NOT NULL,
    revoked_at TIMESTAMPTZ,
    replaced_by_token_id INT, -- Rotasyon zinciri
    created_by_ip VARCHAR(45),
    user_agent VARCHAR(512),
    create_date TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_refresh_token PRIMARY KEY (id),
    CONSTRAINT fk_user_to_refresh_token FOREIGN KEY (user_id) REFERENCES app_user(id),
    CONSTRAINT fk_replaced_by_to_refresh_token FOREIGN KEY (replaced_by_token_id) REFERENCES refresh_token(id)
);

-- ============================================================
-- INDEX'LER
-- ============================================================

-- Bir şirketin aynı anda yalnızca bir aktif abonelik kaydı olabilir.
-- is_active kolon olarak da yer alıyor: aksi halde EF Core bunu 1-1 ilişki
-- sanıp Company.CompanySubscription'ı tekil üretiyor.
CREATE UNIQUE INDEX UX_CompanySubscription_ActiveCompany
    ON company_subscription(company_id, is_active) WHERE is_active = TRUE;

-- user_token: VerifyAccount / ChangePassword tekil lookup yapıyor
CREATE UNIQUE INDEX UX_UserToken_TokenHash ON user_token(token_hash);
-- Login kontrolü ve InvalidateActiveTokensAsync bu üçlüyü filtreliyor
CREATE INDEX IX_UserToken_UserId_Type_Active ON user_token(user_id, token_type) WHERE used = FALSE;
-- Temizlik servisi için
CREATE INDEX IX_UserToken_ExpiresAt ON user_token(expires_at);

-- refresh_token: RefreshAsync tekil lookup yapıyor
CREATE UNIQUE INDEX UX_RefreshToken_TokenHash ON refresh_token(token_hash);
-- RevokeAllUserTokensAsync bu filtreyi kullanıyor
CREATE INDEX IX_RefreshToken_UserId_Active ON refresh_token(user_id) WHERE revoked_at IS NULL;
-- Temizlik servisi için
CREATE INDEX IX_RefreshToken_ExpiresAt ON refresh_token(expires_at);

-- Kullanıcıyı organizasyon birimine göre listeleme
CREATE INDEX IX_AppUser_OrgUnitId ON app_user(org_unit_id) WHERE org_unit_id IS NOT NULL;

-- ============================================================
-- BAŞLANGIÇ VERİSİ
-- ============================================================

-- RoleTypes enum'u ile eşleşmeli: User=1, Admin=2, SuperAdmin=3
INSERT INTO app_role (name) VALUES ('User'), ('Admin'), ('SuperAdmin');

INSERT INTO module (module_key, name) VALUES
    ('cost-management', 'Masraf Yönetimi'),
    ('hr', 'İnsan Kaynakları');

INSERT INTO subscription_plan (name, price, description) VALUES
    ('free', 0, 'Temel özellikler, deneme sürümü'),
    ('cost-management', 500, 'Sadece Masraf Yönetimi'),
    ('hr', 750, 'Sadece İnsan Kaynakları'),
    ('full', 1000, 'Tüm Modüller');

-- FREE hiçbir modüle bağlı değil, o yüzden satırı yok
INSERT INTO plan_module (plan_id, module_id) VALUES
    (2, 1),           -- CMS  → cost-management
    (3, 2),           -- HR   → hr
    (4, 1), (4, 2);   -- FULL → ikisi