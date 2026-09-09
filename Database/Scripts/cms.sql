DROP TABLE IF EXISTS budget, org_unit,
            expense, expense_category,
            expense_status;

-- ============================================================
-- CMS DB (Masraf Yönetimi Modülü Veri Tabanı)
--
-- NOT: Master DB'ye giden tüm referanslar UUID'dir (public_id).
-- Ayrı veri tabanları olduğu için FK kurulamaz; internal int id
-- kullanmak veri taşıma/birleştirme durumunda çakışma riski taşır.
-- ============================================================

-- 1. Modül Sabitleri
CREATE TABLE expense_status (
    id INT GENERATED ALWAYS AS IDENTITY,
    name VARCHAR(255) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT pk_expense_status PRIMARY KEY (id)
);

-- 2. Şirkete Özel Kategoriler ve Organizasyon Birimleri
CREATE TABLE expense_category (
    id INT GENERATED ALWAYS AS IDENTITY,
    public_id UUID NOT NULL DEFAULT gen_random_uuid() UNIQUE,
    company_id UUID NOT NULL,               -- MANTIKSAL BAĞLANTI: Master DB'deki company.public_id
    name VARCHAR(255) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT pk_expense_category PRIMARY KEY (id)
);

-- org_unit: şirketin bölümlendirme birimi — bayi, bölge, departman, ekip.
-- Kendine referans verir; hiyerarşi materialized path ile tutulur.
CREATE TABLE org_unit (
    id INT GENERATED ALWAYS AS IDENTITY,
    public_id UUID NOT NULL DEFAULT gen_random_uuid() UNIQUE,
    company_id UUID NOT NULL,               -- MANTIKSAL BAĞLANTI: Master DB'deki company.public_id
    parent_id INT,                          -- NULL ise kök birim
    path VARCHAR(255) NOT NULL DEFAULT '',  -- '/1/4/12/' — kökten kendine id zinciri
    name VARCHAR(255) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT pk_org_unit PRIMARY KEY (id),
    CONSTRAINT fk_parent_to_org_unit FOREIGN KEY (parent_id) REFERENCES org_unit(id)
);

-- 3. Bütçe ve Masraf (Kalp Tablolar)
CREATE TABLE budget (
    id INT GENERATED ALWAYS AS IDENTITY,
    public_id UUID NOT NULL DEFAULT gen_random_uuid() UNIQUE,
    company_id UUID NOT NULL,               -- tenant filtresi için denormalize
    org_unit_id INT NOT NULL,
    month INT NOT NULL,
    year INT NOT NULL,
    total_amount NUMERIC(18,2) NOT NULL,
    used_amount NUMERIC(18,2) NOT NULL DEFAULT 0,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    create_user UUID,                       -- MANTIKSAL BAĞLANTI: app_user.public_id
    create_date TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    update_user UUID,
    update_date TIMESTAMPTZ,
    delete_user UUID,
    delete_date TIMESTAMPTZ,
    CONSTRAINT pk_budget PRIMARY KEY (id),
    CONSTRAINT fk_org_unit_to_budget FOREIGN KEY (org_unit_id) REFERENCES org_unit(id),
    CONSTRAINT ck_budget_month CHECK (month BETWEEN 1 AND 12)
);

CREATE TABLE expense (
    id INT GENERATED ALWAYS AS IDENTITY,
    public_id UUID NOT NULL DEFAULT gen_random_uuid() UNIQUE,
    company_id UUID NOT NULL,               -- tenant filtresi için denormalize
    user_id UUID NOT NULL,                  -- MANTIKSAL BAĞLANTI: app_user.public_id (masrafın sahibi)
    budget_id INT NOT NULL,
    expense_category_id INT NOT NULL,
    amount NUMERIC(18,2) NOT NULL,
    description VARCHAR(512),
    status_id INT NOT NULL,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    create_user UUID,
    create_date TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    update_user UUID,
    update_date TIMESTAMPTZ,
    delete_user UUID,
    delete_date TIMESTAMPTZ,
    CONSTRAINT pk_expense PRIMARY KEY (id),
    CONSTRAINT fk_budget_to_expense FOREIGN KEY (budget_id) REFERENCES budget(id),
    CONSTRAINT fk_category_to_expense FOREIGN KEY (expense_category_id) REFERENCES expense_category(id),
    CONSTRAINT fk_status_to_expense FOREIGN KEY (status_id) REFERENCES expense_status(id),
    CONSTRAINT ck_expense_amount CHECK (amount > 0)
);

-- ============================================================
-- INDEX'LER
-- ============================================================

-- Tenant filtresi her sorguda çalışıyor
CREATE INDEX IX_ExpenseCategory_CompanyId ON expense_category(company_id);
CREATE INDEX IX_OrgUnit_CompanyId ON org_unit(company_id);
CREATE INDEX IX_Budget_CompanyId ON budget(company_id);
CREATE INDEX IX_Expense_CompanyId ON expense(company_id);

-- Organizasyon hiyerarşisi
CREATE INDEX IX_OrgUnit_ParentId ON org_unit(parent_id);
-- varchar_pattern_ops: LIKE 'önek%' sorgularının endeksi kullanabilmesi için
CREATE INDEX IX_OrgUnit_Path ON org_unit(path varchar_pattern_ops);

-- Sık kullanılan filtreler
CREATE INDEX IX_Budget_OrgUnitId ON budget(org_unit_id);
CREATE INDEX IX_Expense_BudgetId ON expense(budget_id);
CREATE INDEX IX_Expense_UserId ON expense(user_id);
-- Onay bekleyen masraf listesi
CREATE INDEX IX_Expense_CompanyId_StatusId ON expense(company_id, status_id) WHERE is_deleted = FALSE;

-- Bir birimin aynı ay/yıl için tek bütçesi olabilir
CREATE UNIQUE INDEX UX_Budget_OrgUnit_Period
    ON budget(org_unit_id, year, month) WHERE is_deleted = FALSE;

-- ============================================================
-- BAŞLANGIÇ VERİSİ
-- ============================================================

-- ExpenseStatusTypes enum'u ile eşleşmeli: Pending=1, Approved=2, Rejected=3
INSERT INTO expense_status (name) VALUES
    ('Beklemede'), ('Onaylandı'), ('Reddedildi');

ALTER TABLE expense ADD COLUMN reject_reason VARCHAR(512);