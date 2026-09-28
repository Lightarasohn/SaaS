# Multi-Tenant SaaS

Şirketlerin bütçe ve masraf süreçlerini organizasyon birimleri üzerinden yönetebildiği, abonelik tabanlı modül erişimi olan çok kiracılı SaaS uygulaması. Backend ASP.NET Core Web API, frontend Next.js ile geliştirilmiştir. Master verileri ve CMS iş verileri PostgreSQL'de ayrı veritabanlarında tutulur.

> Uygulama şu an tek bir ASP.NET Core API içinde modüler monolit olarak çalışır. `Microservices/CMS` klasör adı bağımsız dağıtılan mikroservis olduğu anlamına gelmez. Gerçek işlevi bulunan modül CMS'dir; HR modülü yalnızca plan/modül kataloğu ve authorization policy düzeyinde tanımlıdır.

## Özellikler

- Tenant/company kaydı, davet koduyla şirkete katılım, e-posta doğrulama ve şirket kullanıcı yönetimi.
- JWT access token ve refresh token rotasyonu/iptali; parola ve e-posta kurtarma akışları.
- Şirket rolü ve organizasyon birimi rolüne göre erişim kontrolü.
- Abonelik planı, plan-modül erişimi, bitiş tarihi ve otomatik yenileme yönetimi.
- Hiyerarşik organizasyon birimleri, dönemsel bütçeler ve şirket bazlı masraf kategorileri.
- Tekli/toplu masraf oluşturma ve birim kapsamlı onay/ret akışları.
- SMTP e-posta bildirimleri ve operasyonel dashboard/widget görünümleri.

## Ekran Görüntüleri

Arayüz ekran görüntüleri frontend deposundaki `docs/screenshots/` klasöründe tutulur. Görselleri oraya eklediğinizde burada da görüntülenir.

### Landing Page

![Landing Page](https://raw.githubusercontent.com/Lightarasohn/saas-front-next/master/docs/screenshots/landing-page.png)

### Dashboard

![Dashboard ve widget'lar](https://raw.githubusercontent.com/Lightarasohn/saas-front-next/master/docs/screenshots/dashboard.png)

### Abonelik

![Abonelik sayfası](https://raw.githubusercontent.com/Lightarasohn/saas-front-next/master/docs/screenshots/subscription.png)

### Profil

![Profil sayfası](https://raw.githubusercontent.com/Lightarasohn/saas-front-next/master/docs/screenshots/profile.png)

### Masraflar

![Masraflar sayfası](https://raw.githubusercontent.com/Lightarasohn/saas-front-next/master/docs/screenshots/expenses.png)

### Bütçeler

![Bütçeler sayfası](https://raw.githubusercontent.com/Lightarasohn/saas-front-next/master/docs/screenshots/budgets.png)

### Birimler

![Organizasyon birimleri sayfası](https://raw.githubusercontent.com/Lightarasohn/saas-front-next/master/docs/screenshots/org-units.png)

## Teknoloji Yığını

| Alan | Teknolojiler |
| --- | --- |
| Backend | .NET 10, ASP.NET Core Web API, Entity Framework Core, FluentValidation |
| Kimlik doğrulama | JWT Bearer, BCrypt, refresh token rotasyonu |
| Veritabanı | PostgreSQL, Npgsql; ayrı `MasterContext` ve `CMSContext` |
| Frontend | Next.js 16, React 19, Tailwind CSS 4 |
| E-posta ve API belgeleri | MailKit/SMTP, OpenAPI, Swagger |

## Mimari ve Veri Modeli

Next.js API route'ları BFF/proxy katmanı olarak backend'e istek iletir. API, kimlik doğrulama ve abonelik/modül izinlerini doğruladıktan sonra CMS sorgularına tenant filtresi uygular.

### Sistem bileşenleri

![Mimari](docs/diagrams/1-mimari.png)

### İstek akışı: kimlik doğrulama, modül yetkisi ve tenant filtresi

![İstek akışı](docs/diagrams/2-istek-akisi.png)

### Master DB: kullanıcılar, şirketler, abonelikler ve modüller

![Master DB ER şeması](docs/diagrams/3-master-er.png)

Master veritabanının ana tabloları:

| Tablo | Sorumluluk |
| --- | --- |
| `company`, `app_user`, `app_role` | Tenant, kullanıcı ve şirket seviyesi roller. |
| `subscription_plan`, `module`, `plan_module` | Plan kataloğu ve plana dahil modüller. |
| `company_subscription` | Şirketin abonelik geçmişi, aktif planı, bitiş ve yenileme durumu. |
| `user_token`, `refresh_token` | Tek kullanımlık doğrulama/kurtarma token'ları ve yenilenebilir oturumlar. |

### CMS DB: bütçe ve masraf yönetimi

![CMS DB ER şeması](docs/diagrams/4-cms-er.png)

CMS veritabanının ana tabloları:

| Tablo | Sorumluluk |
| --- | --- |
| `org_unit` | Tenant'a ait hiyerarşik organizasyon birimleri; parent ilişkisi ve path alanı. |
| `org_unit_role`, `org_unit_user_role` | Birim rolleri ve kullanıcılara verilmiş birim kapsamlı yetkiler. |
| `budget` | Birim ve ay/yıl bazında toplam ve kullanılan bütçe. |
| `expense` | Bütçeye bağlı masraf, sahibi, kategori, durum, ret nedeni ve denetim alanları. |
| `expense_category`, `expense_status` | Tenant'a özel masraf kategorileri ve durum sözlüğü. |

**Veritabanı sınırı:** Master ve CMS ayrı PostgreSQL veritabanlarıdır. CMS tarafındaki `company_id` ve `user_id`, Master'daki kayıtların public UUID değerlerini tutan mantıksal referanslardır; iki veritabanı arasında foreign key kurulmaz. CMS tablolarındaki global EF Core query filter'ları tenant kapsamını uygular; bütçe ve masraflarda silinmiş kayıtlar da varsayılan sorgulardan hariç tutulur.

### Yetkilendirme ve iş kuralları

1. Backend JWT içindeki kullanıcı, rol ve `company_id` claim'lerini doğrular.
2. CMS endpoint'leri `cost-management` modül policy'sini gerektirir. Erişim, şirketin aktif ve süresi dolmamış aboneliğinin planındaki modüle göre belirlenir.
3. CMS context'i claim'deki şirket kimliğiyle kurulur; EF Core query filter'ları şirket kapsamını sorgulara uygular.
4. Servisler şirket rolüne ek olarak ilgili organizasyon birimindeki Manager/Approver/User rollerini denetler.
5. Masraf onayı CMS veritabanı transaction'ı içinde kaydedilir; onaylanan tutar bütçenin `used_amount` alanına eklenir.

### Dashboard analitiği

Dashboard widget'ları bütçeleri birimlere göre, masrafları kategorilere göre, kullanıcı masraflarını ve bekleyen onayları görünür kılar. Temel finansal göstergeler:

| Gösterge | Hesaplama |
| --- | --- |
| Kullanılan bütçe | `budget.used_amount` |
| Kalan bütçe | `budget.total_amount - budget.used_amount` |
| Bütçe kullanım oranı | `used_amount / total_amount * 100` (toplam bütçe sıfır değilse) |
| Masraf iş yükü | Masrafların durum, kategori, dönem ve organizasyon birimine göre sayısı/tutarı |

Bu, uygulama içi operasyonel analitiktir; ayrı bir veri ambarı veya BI pipeline'ı değildir. Masrafın maliyet gerçekleşmesi olarak raporlanıp raporlanmayacağına göre onaylı ve bekleyen tutarlar ayrı gösterilmelidir.

## Gereksinimler

- .NET 10 SDK
- PostgreSQL
- Node.js 20.9 veya üzeri ve npm

## Yerelde Çalıştırma

### 1. Depoları klonlayın

Backend ve frontend ayrı Git depolarıdır:

```bash
git clone https://github.com/Lightarasohn/SaaS.git
git clone https://github.com/Lightarasohn/saas-front-next.git
```

### 2. PostgreSQL veritabanlarını hazırlayın

İki boş veritabanı oluşturun. Örnek adlar `saas_master` ve `saas_cms`:

```bash
createdb saas_master
createdb saas_cms
```

PowerShell'de veya `createdb` komutunun PATH'te olmadığı kurulumlarda veritabanlarını PostgreSQL aracıyla oluşturabilirsiniz. Sonra, repo kökünden aşağıdaki betikleri ilgili veritabanlarına uygulayın:

```bash
psql -h localhost -U YOUR_DB_USER -d saas_master -f Database/Scripts/master.sql
psql -h localhost -U YOUR_DB_USER -d saas_cms -f Database/Scripts/cms.sql
```

> **Önemli:** `master.sql` ve `cms.sql` başında tabloları silip yeniden oluşturan `DROP TABLE` komutları vardır. Bu betikleri yalnızca boş/atılabilir geliştirme veritabanında çalıştırın; mevcut veritabanına uygulamayın ve önemli veriler için yedek almadan çalıştırmayın. `master_alter.sql` yeni kurulum betiği değildir; bazı sütun/index'ler `master.sql` içinde zaten bulunduğu için mevcut veritabanında ayrıca çalıştırılması çakışma oluşturabilir.

### 3. Backend ayarlarını güvenli biçimde tanımlayın

Gerçek connection string, SMTP parolası veya JWT signing key'i `appsettings.json` içine yazmayın ve Git'e göndermeyin. Yerel geliştirme için backend klasöründe .NET User Secrets kullanın. İlk komut `SaaS.csproj` için bir User Secrets kimliği oluşturur:

```bash
cd SaaS
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:MasterConnection" "Host=localhost;Port=5432;Database=saas_master;Username=YOUR_DB_USER;Password=YOUR_DB_PASSWORD"
dotnet user-secrets set "ConnectionStrings:CMSConnection" "Host=localhost;Port=5432;Database=saas_cms;Username=YOUR_DB_USER;Password=YOUR_DB_PASSWORD"
dotnet user-secrets set "FrontendBaseUrl" "http://localhost:3000"
dotnet user-secrets set "JwtSettings:Issuer" "FullSaaS"
dotnet user-secrets set "JwtSettings:Audience" "FullSaaS.Frontend"
dotnet user-secrets set "JwtSettings:SigningKey" "REPLACE_WITH_RANDOM_SECRET_AT_LEAST_32_BYTES"
dotnet user-secrets set "JwtSettings:AccessTokenMinutes" "15"
dotnet user-secrets set "JwtSettings:RefreshTokenDays" "7"
```

`YOUR_DB_USER`, `YOUR_DB_PASSWORD` ve JWT signing key örneklerini kendi yerel değerlerinle değiştir. Uygulama signing key'in en az 32 byte olmasını bekler. User Secrets yalnızca geliştirme amaçlıdır ve şifreli bir secret store değildir; production/deployment ortamında secret manager veya korumalı ortam değişkenleri kullanın.

E-posta doğrulama ve parola kurtarma e-postalarını kullanacaksanız SMTP bilgilerini de secret olarak ekleyin. Gmail kullanılıyorsa hesap parolası yerine sağlayıcının uygulama parolası gerekir:

```bash
dotnet user-secrets set "EmailSettings:SystemName" "YOUR_SENDER_NAME"
dotnet user-secrets set "EmailSettings:SystemEmail" "YOUR_SENDER_EMAIL"
dotnet user-secrets set "EmailSettings:AppPassword" "YOUR_SMTP_APP_PASSWORD"
dotnet user-secrets set "EmailSettings:SmtpServer" "smtp.gmail.com"
dotnet user-secrets set "EmailSettings:SmtpPort" "587"
```

### 4. Backend'i başlatın

```bash
dotnet restore
dotnet build
dotnet run --launch-profile http
```

API varsayılan olarak `http://localhost:5012` adresinde başlar. Development ortamında Swagger UI: [http://localhost:5012/swagger](http://localhost:5012/swagger).

### 5. Frontend'i başlatın

Diğer terminalde frontend klasörüne geçin. `.template.env.local` dosyasını `.env.local` olarak kopyalayın ve sunucu tarafında kullanılan backend adresini tanımlayın:

```dotenv
API_URL=http://localhost:5012
```

Ardından:

```bash
cd saas-front-next
npm ci
npm run dev
```

Frontend [http://localhost:3000](http://localhost:3000) adresinde açılır. `API_URL`, Next.js API route'ları ve middleware tarafından sunucu tarafında kullanılır. Backend'in geliştirme CORS ayarı `http://localhost:3000` origin'ine izin verir.

## API Alanları

- `/api/auth/*`: kayıt, giriş/çıkış, token yenileme, hesap doğrulama ve parola/e-posta işlemleri.
- `/api/subscription/*`: plan listesi, mevcut abonelik, plana geçiş/yenileme ve otomatik yenileme.
- `/api/modules`: tenant için etkin modüller.
- CMS controller'ları: bütçe, masraf, masraf kategorisi ve organizasyon birimi işlemleri.
- `/api/UserManagemet`: şirket kullanıcılarını listeleme ve rol güncelleme.

İstek/yanıt örnekleri backend deposundaki `SaaS.*.http` dosyalarında bulunur. Swagger yalnızca Development ortamında etkinleştirilir.

## Mevcut Sınırlar ve Üretim Notları

- Gerçek ödeme kuruluşu entegrasyonu yoktur. `AlwaysSucceedPaymentProcessor` tahsilat yapmadan başarılı dönen geçici bir uygulamadır; ücretli aboneliklerin gerçek tahsilatını sağlamaz.
- HR modülü katalog ve policy seviyesinde yer alır; HR iş süreçleri uygulanmış değildir.
- E-posta kuyruğu uygulama belleğinde tutulan sınırlı bir `Channel` kuyruğudur; servis yeniden başlarsa bekleyen e-postalar kalıcı olarak saklanmaz.
- CMS ile Master arasındaki UUID referansları iki veritabanı arasında foreign key oluşturmaz; çapraz veritabanı tutarlılığı uygulama katmanında korunur.
- Global query filter'lara ek olarak servis yetkilendirmeleri bulunur. Yeni tenant tablolarında tenant alanı, filtre, indeks ve tenant izolasyonu testleri birlikte ele alınmalıdır.
