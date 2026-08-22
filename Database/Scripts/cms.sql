-- BUDGET DB (Sadece Bütçe Servisi Veri Tabanı)

-- 1. Bütçe Modülü Sabitleri
CREATE TABLE expense_status (
    id INT GENERATED ALWAYS AS IDENTITY,
    name VARCHAR(255) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT pk_expense_status PRIMARY KEY (id)
);

-- 2. Şirkete Özel Kategoriler ve Şubeler
CREATE TABLE expense_category (
    id INT GENERATED ALWAYS AS IDENTITY,
    company_id INT NOT NULL, -- MANTIKSAL BAĞLANTI: Master DB'deki company.id
    name VARCHAR(255) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT pk_expense_category PRIMARY KEY (id)
);

CREATE TABLE distributor (
    id INT GENERATED ALWAYS AS IDENTITY,
    company_id INT NOT NULL, -- MANTIKSAL BAĞLANTI: Master DB'deki company.id
    public_id UUID NOT NULL DEFAULT gen_random_uuid() UNIQUE,
    region VARCHAR(255) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT pk_distributor PRIMARY KEY (id)
);

-- 3. Bütçe ve Masraf (Kalp Tablolar)
CREATE TABLE budget (
    id INT GENERATED ALWAYS AS IDENTITY,
    public_id UUID NOT NULL DEFAULT gen_random_uuid() UNIQUE,
    distributor_id INT NOT NULL,
    month INT NOT NULL,
    year INT NOT NULL,
    total_amount NUMERIC(18,2) NOT NULL, 
    used_amount NUMERIC(18,2) NOT NULL DEFAULT 0, 
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    create_user INT, -- MANTIKSAL BAĞLANTI: Master DB'deki app_user.id
    create_date TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    update_user INT,
    update_date TIMESTAMPTZ,
    delete_user INT,
    delete_date TIMESTAMPTZ,
    CONSTRAINT pk_budget PRIMARY KEY (id),
    CONSTRAINT fk_distributor_to_budget FOREIGN KEY (distributor_id) REFERENCES distributor(id)
);

CREATE TABLE expense (
    id INT GENERATED ALWAYS AS IDENTITY,
    public_id UUID NOT NULL DEFAULT gen_random_uuid() UNIQUE,
    user_id INT NOT NULL, -- MANTIKSAL BAĞLANTI: Master DB'deki app_user.id
    budget_id INT NOT NULL,
    expense_category_id INT NOT NULL,
    amount NUMERIC(18,2) NOT NULL, 
    description VARCHAR(512),
    status_id INT NOT NULL,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    create_user INT,
    create_date TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    update_user INT,
    update_date TIMESTAMPTZ,
    delete_user INT,
    delete_date TIMESTAMPTZ,
    CONSTRAINT pk_expense PRIMARY KEY (id),
    CONSTRAINT fk_budget_to_expense FOREIGN KEY (budget_id) REFERENCES budget(id),
    CONSTRAINT fk_category_to_expense FOREIGN KEY (expense_category_id) REFERENCES expense_category(id),
    CONSTRAINT fk_status_to_expense FOREIGN KEY (status_id) REFERENCES expense_status(id)
);

-- Hızlı Tenant (Şirket) sorguları için Index'ler
CREATE INDEX IX_ExpenseCategory_CompanyId ON expense_category(company_id);
CREATE INDEX IX_Distributor_CompanyId ON distributor(company_id);
CREATE INDEX IX_Expense_UserId ON expense(user_id);