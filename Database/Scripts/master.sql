-- MASTER DB (Merkezi Veri Tabanı)

-- 1. SaaS Şirketleri (Tenants)
CREATE TABLE company (
    id INT GENERATED ALWAYS AS IDENTITY,
    public_id UUID NOT NULL DEFAULT gen_random_uuid() UNIQUE,
    name VARCHAR(255) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    create_date TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_company PRIMARY KEY (id)
);

-- 2. Abonelik Planları ve Servis Yetkileri (YENİ)
CREATE TABLE subscription_plan (
    id INT GENERATED ALWAYS AS IDENTITY,
    name VARCHAR(255) NOT NULL, -- Örn: "Sadece Bütçe", "Tam Paket (Bütçe+İK)"
    has_budget_access BOOLEAN NOT NULL DEFAULT FALSE,
    has_hr_access BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT pk_subscription_plan PRIMARY KEY (id)
);

CREATE TABLE company_subscription (
    id INT GENERATED ALWAYS AS IDENTITY,
    company_id INT NOT NULL,
    plan_id INT NOT NULL,
    expires_at TIMESTAMPTZ NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT pk_company_subscription PRIMARY KEY (id),
    CONSTRAINT fk_company_to_subscription FOREIGN KEY (company_id) REFERENCES company(id),
    CONSTRAINT fk_plan_to_subscription FOREIGN KEY (plan_id) REFERENCES subscription_plan(id)
);

-- 3. Roller ve Kullanıcılar
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
    distributor_id INT, -- MANTIKSAL BAĞLANTI: FK yok, Bütçe DB'deki distributor id'sini tutar. Company Admin için NULL olabilir.
    name VARCHAR(255) NOT NULL,
    email VARCHAR(255) NOT NULL UNIQUE,
    password_hash TEXT NOT NULL, 
    recovery_key_hash TEXT, 
    is_verified BOOLEAN NOT NULL DEFAULT FALSE,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    create_user INT,
    create_date TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    update_user INT,
    update_date TIMESTAMPTZ,
    delete_user INT,
    delete_date TIMESTAMPTZ,
    CONSTRAINT pk_app_user PRIMARY KEY (id),
    CONSTRAINT fk_company_to_app_user FOREIGN KEY (company_id) REFERENCES company(id),
    CONSTRAINT fk_role_to_app_user FOREIGN KEY (role_id) REFERENCES app_role(id)
);

-- 4. Doğrulama ve Kurtarma Tokenları
CREATE TABLE user_token(
    id INT GENERATED ALWAYS AS IDENTITY,
    user_id INT NOT NULL,
    token_hash VARCHAR(512) NOT NULL,
    token_type VARCHAR(50) NOT NULL,
    expires_at TIMESTAMPTZ NOT NULL,
    used BOOLEAN NOT NULL DEFAULT FALSE,
    create_date TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_user_token PRIMARY KEY (id),
    CONSTRAINT fk_user_to_token FOREIGN KEY (user_id) REFERENCES app_user(id)
);

CREATE INDEX IX_UserToken_TokenHash ON user_token(token_hash);
CREATE INDEX IX_UserToken_ExpiresAt ON user_token(expires_at);