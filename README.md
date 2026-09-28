## Mimari ve Veri Modeli

> Uygulama tek bir ASP.NET Core API içinde modüler monolit olarak çalışır; Next.js tarafındaki API route'ları BFF/proxy görevi görür.

### 1. Sistem Bileşenleri

![Mimari](docs/diagrams/1-mimari.png)

### 2. İstek Akışı (Kimlik doğrulama → abonelik/modül kontrolü → tenant filtreli sorgu)

![İstek akışı](docs/diagrams/2-istek-akisi.png)

### 3. Master DB (Kimlik, şirket, abonelik, modül kataloğu)

![Master DB ER şeması](docs/diagrams/3-master-er.png)

### 4. CMS DB (Bütçe ve masraf yönetimi)

![CMS DB ER şeması](docs/diagrams/4-cms-er.png)

> Not: Master ve CMS ayrı PostgreSQL veritabanlarıdır. CMS tarafındaki `company_id` ve `user_id`, Master'daki kaydın public UUID değerini taşıyan mantıksal referanslardır (veritabanları arası FK yoktur).
