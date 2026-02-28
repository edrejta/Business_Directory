# Shënime – Backend i Signup & Login (Auth)
Signup (Register)
Kërkesa	Status
Emri (Username)	✅
Email	✅
Fjalëkalim	✅
Zgjedhja e rolit (User, BusinessOwner, Admin)	✅
Hash i fjalëkalimit (BCrypt)	✅
Kontroll që email nuk është i përsëritur	✅
JWT me Id, Username, Email, Role	✅
Login
Kërkesa	Status
Input: Email + fjalëkalim	✅
Verifikimi i kredencialesh	✅
Verifikimi i hash-it të fjalëkalimit (BCrypt.Verify)	✅
Gjenerimi i JWT me të dhënat e përdoruesit dhe rolin	✅
Dërgimi i token-it tek frontend-i	✅
JWT
Kërkesa	Status
Claims: Id, Email, Username, Role	✅
Përdorur për autorizim sipas rolit	✅ (ClaimTypes.Role)
Konfigurim (Secret, Issuer, Audience, Expiration)	✅
 
 
## 1. Skedarë të krijuar (të rinj)



`Application/Dtos/LoginDto.cs` | DTO për login – Email, Password 
`Application/Dtos/AuthResponseDto.cs` | DTO për përgjigjen – Token, Id, Username, Email, Role 
`Application/Options/JwtSettings.cs` | Konfigurim JWT – Secret, Issuer, Audience, ExpirationInMinutes 
`Application/Interfaces/IAuthService.cs` | Ndërfaqe për AuthService 
`Infrastructure/Services/AuthService.cs` | Implementimi – Register, Login, hash fjalëkalim, gjenerim JWT 
`BusinessDirectory/Controllers/AuthController.cs` | API – POST register, POST login 
`Infrastructure/Migrations/20260215120000_AddUniqueEmailIndex.cs` | Migrim – indeks unik për Email 
`Infrastructure/Migrations/20260215120000_AddUniqueEmailIndex.Designer.cs` | Designer i migrimit 

---

## 2. Skedarë të ndryshuar (të modifikuar)

`Application/Dtos/UserCreateDto.cs` | Shtuar `Role` (UserRole) – për zgjedhjen e rolit gjatë signup 
`Infrastructure/ApplicationDbContext.cs` | Shtuar indeks unik për `User.Email` 
`Infrastructure/Migrations/ApplicationDbContextModelSnapshot.cs` | Përditësuar me indeksin unik të Email 
`BusinessDirectory/Program.cs` | Shtuar: konfigurim JWT, IAuthService, Authentication (JwtBearer), `UseAuthentication()` 
`BusinessDirectory/appsettings.json` | Shtuar seksion `Jwt` (Secret, Issuer, Audience, ExpirationInMinutes) 
`BusinessDirectory/BusinessDirectory.API.csproj` | Shtuar paketa BCrypt.Net-Next, Microsoft.AspNetCore.Authentication.JwtBearer; hequr Folder Controllers 
`Infrastructure/BusinessDirectory.Infrastructure.csproj` | Shtuar paketa: BCrypt.Net-Next, Microsoft.Extensions.Options.ConfigurationExtensions, Microsoft.IdentityModel.Tokens, System.IdentityModel.Tokens.Jwt 
`global.json` | Ndryshuar version SDK në 10.0.102 me rollForward latestPatch 

---

## 3. Paketa të instaluara


`BCrypt.Net-Next` (4.0.3) | API, Infrastructure 
`Microsoft.AspNetCore.Authentication.JwtBearer` (10.0.3) | API 
`Microsoft.IdentityModel.Tokens` (8.2.1) | Infrastructure 
`System.IdentityModel.Tokens.Jwt` (8.2.1) | Infrastructure 
`Microsoft.Extensions.Options.ConfigurationExtensions` (10.0.0) | Infrastructure 

---

## 4. API endpointet


| POST | `/api/auth/register` | Signup – krijon përdorues të ri |
| POST | `/api/auth/login` | Login – verifikon kredencialet dhe kthen JWT |

### Body për Register (JSON)
**Inputet vijnë nga formularët e frontend-it (përdoruesi i plotëson), jo nga databaza.**

```json
{
  "username": "emri_i_perdoruesit",
  "email": "email@example.com",
  "password": "Fjalekalim123!",
  "role": 0
}
```
**Role:** 0 = User, 1 = BusinessOwner, 2 = Admin

### Body për Login (JSON)
**Inputet vijnë nga formularët e frontend-it (përdoruesi i plotëson), jo nga databaza.**

```json
{
  "email": "email@example.com",
  "password": "Fjalekalim123!"
}
```

### Përgjigja (Token + user info)
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "id": "guid-i-perdoruesit",
  "username": "emri_i_perdoruesit",
  "email": "email@example.com",
  "role": 0
}
```

---

## 5. Çfarë duhet bërë në frontend për të kompletuar Signup & Signin

### 5.1 Signup (Regjistrim)

1. **Formë signup** me fusha:
   - Username
   - Email
   - Password (me fshehje)
   - Zgjedhje roli: User / Business Owner / Admin (0, 1, 2)

2. **Request:**
   - Metodë: `POST`
   - URL: `${API_BASE_URL}/api/auth/register`
   - Headers: `Content-Type: application/json`
   - Body: `{ username, email, password, role }`

3. **Mbyllja e suksesit:**
   - Ruaje `token` në localStorage/sessionStorage ose cookie.
   - Ruaje `role` (ose lexo nga token).
   - Bëje redirect sipas rolit:
     - `role 0` (User / përdorues) → `/dashboard-user`
     - `role 1` (BusinessOwner / pronar biznesi) → `/dashboard-business`
     - `role 2` (Admin) → `/dashboard-admin`

4. **Mbyllja e gabimit:**
   - Nëse API kthen 400 (email ekziston tashmë), shfaq mesazhin e gabimit.

### 5.2 Signin (Login)

1. **Formë login** me:
   - Email
   - Password (me fshehje)

2. **Request:**
   - Metodë: `POST`
   - URL: `${API_BASE_URL}/api/auth/login`
   - Headers: `Content-Type: application/json`
   - Body: `{ email, password }`

3. **Mbyllja e suksesit:**
   - Ruaje `token` në localStorage/sessionStorage ose cookie.
   - Bëje redirect sipas `role`:
     - `role 0` → `/dashboard-user`
     - `role 1` → `/dashboard-business`
     - `role 2` → `/dashboard-admin`

4. **Mbyllja e gabimit:**
   - Nëse API kthen 401, trego: "Email ose fjalëkalim i gabuar."

### 5.3 Leximi i rolit nga JWT (opsional)

- Nëse nuk ruhet `role` në frontend, mund ta lexosh nga token (decode JWT dhe merre claim-in `role` ose `ClaimTypes.Role`).

### 5.4 Autorizim për API të tjera

- Për endpointet e mbrojtura, shto header:
  - `Authorization: Bearer <token>`
- Përdoruesi duhet të jetë i autentifikuar (token i vlefshëm) dhe, sipas nevojës, me rol të përshtatshëm.

### 5.5 Shembull i përdorimit (fetch / axios)

```javascript
// Signup
const response = await fetch(`${API_URL}/api/auth/register`, {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    username: 'testuser',
    email: 'test@example.com',
    password: 'Password123!',
    role: 0
  })
});
const data = await response.json();
// Nëse OK: ruaj data.token, bëje redirect sipas data.role

// Login
const response = await fetch(`${API_URL}/api/auth/login`, {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    email: 'test@example.com',
    password: 'Password123!'
  })
});
const data = await response.json();
// Nëse OK: ruaj data.token, bëje redirect sipas data.role
```

---

## 6. Lista e plotë e skedarëve të projektit

### Core (Domain)
| Skedar | Përshkrim |
|--------|-----------|
| `Core/BusinessDirectory.Domain.csproj` | Projekti Domain |
| `Core/Entities/User.cs` | Entiteti User |
| `Core/Entities/Business.cs` | Entiteti Business |
| `Core/Entities/Comment.cs` | Entiteti Comment |
| `Core/Enums/UserRole.cs` | Enum User, BusinessOwner, Admin |
| `Core/Enums/BusinessType.cs` | Enum për llojet e biznesit |
| `Core/Enums/BusinessStatus.cs` | Enum për statusin e biznesit |

### Application
| Skedar | Përshkrim |
|--------|-----------|
| `Application/BusinessDirectory.Application.csproj` | Projekti Application |
| `Application/Interfaces/IAuthService.cs` | Ndërfaqe AuthService |
| `Application/Options/JwtSettings.cs` | Konfigurim JWT |
| `Application/Dtos/UserDto.cs` | DTO për User |
| `Application/Dtos/UserCreateDto.cs` | DTO për krijim përdoruesi |
| `Application/Dtos/UserUpdateDto.cs` | DTO për përditësim përdoruesi |
| `Application/Dtos/LoginDto.cs` | DTO për Login |
| `Application/Dtos/AuthResponseDto.cs` | DTO për përgjigjen Auth |
| `Application/Dtos/BusinessDto.cs` | DTO për Business |
| `Application/Dtos/BusinessCreateDto.cs` | DTO për krijim biznesi |
| `Application/Dtos/BusinessUpdateDto.cs` | DTO për përditësim biznesi |
| `Application/Dtos/CommentDto.cs` | DTO për Comment |
| `Application/Dtos/CommentCreateDto.cs` | DTO për krijim komenti |
| `Application/Dtos/CommentUpdateDto.cs` | DTO për përditësim komenti |

### Infrastructure
| Skedar | Përshkrim |
|--------|-----------|
| `Infrastructure/BusinessDirectory.Infrastructure.csproj` | Projekti Infrastructure |
| `Infrastructure/ApplicationDbContext.cs` | DbContext – lidhja me databazën |
| `Infrastructure/Services/AuthService.cs` | Implementimi i AuthService |
| `Infrastructure/Migrations/ApplicationDbContextModelSnapshot.cs` | Snapshot i modelit |
| `Infrastructure/Migrations/20260215012057_InitialCreate.cs` | Migrim fillestar |
| `Infrastructure/Migrations/20260215012057_InitialCreate.Designer.cs` | Designer migrimi fillestar |
| `Infrastructure/Migrations/20260215120000_AddUniqueEmailIndex.cs` | Migrim indeks unik Email |
| `Infrastructure/Migrations/20260215120000_AddUniqueEmailIndex.Designer.cs` | Designer migrimi |

### API (BusinessDirectory)
| Skedar | Përshkrim |
|--------|-----------|
| `BusinessDirectory/BusinessDirectory.API.csproj` | Projekti API |
| `BusinessDirectory/Program.cs` | Pika e hyrjes – konfigurim, DI, middleware |
| `BusinessDirectory/appsettings.json` | Konfigurim – ConnectionString, JWT |
| `BusinessDirectory/appsettings.Development.json` | Konfigurim për Development |
| `BusinessDirectory/Properties/launchSettings.json` | Konfigurim për debug/launch |
| `BusinessDirectory/Controllers/AuthController.cs` | Kontrollues Auth – Register, Login |

### Të tjerë
| Skedar | Përshkrim |
|--------|-----------|
| `global.json` | Version i .NET SDK |
| `README.md` | Dokumentacion i projektit |
| `docs/AUTH_BACKEND_NOTES.md` | Këto shënime |

---

## 7. Si të lidhësh databazën lokale me projektin

### Hapi 1: Konfiguro ConnectionString

Hap `BusinessDirectory/appsettings.json` ose `BusinessDirectory/appsettings.Development.json` dhe vendos `DefaultConnection`:

#### Për SQL Server (Windows Authentication – nëse përdor vetë Windows për hyrje)
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=BusinessDirectory;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

#### Për SQL Server (SQL Authentication – përdorues + fjalëkalim)
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=BusinessDirectory;User Id=emri_i_perdoruesit;Password=fjalekalimi;TrustServerCertificate=True;"
}
```

#### Për SQL Server Express
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.\\SQLEXPRESS;Database=BusinessDirectory;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

#### Parametrat kryesorë
| Parametër | Përshkrim |
|-----------|-----------|
| `Server` | `localhost`, `(localdb)\mssqllocaldb`, `.\SQLEXPRESS`, ose emri i serverit |
| `Database` | Emri i databazës (p.sh. `BusinessDirectory`) |
| `Trusted_Connection=True` | Përdor autentikim të Windows-it |
| `User Id` / `Password` | Për SQL Authentication |
| `TrustServerCertificate=True` | Shpesh e nevojshme për development (localhost) |

### Hapi 2: Krijo databazën me migrimet

1. Hap terminal në folderin e projektit.
2. Ekzekuto:

```bash
dotnet ef database update --project Infrastructure --startup-project BusinessDirectory
```

Kjo krijon databazën `BusinessDirectory` (nëse nuk ekziston) dhe ekzekuton të gjitha migrimet.

### Hapi 3: Kontrollo lidhjen

1. Nis API-n:
```bash
dotnet run --project BusinessDirectory
```

2. Testo regjistrimin përmes Swagger (`https://localhost:PORT/swagger`) ose Postman.
3. Nëse nuk ka gabime, databaza është e lidhur siç duhet.

### Nëse nuk ke SQL Server të instaluar

- **SQL Server Express** (falas): https://www.microsoft.com/sql-server/sql-server-downloads  
- **LocalDB** (vjen me Visual Studio / .NET): zakonisht është i instaluar – përdor `(localdb)\mssqllocaldb`  
- **SQL Server Management Studio (SSMS)** për menaxhim: https://learn.microsoft.com/sql/ssms/download-sql-server-management-studio-ssms

---

## 8. Çfarë duhet bërë përpara se të testosh

- [ ] Vendose `ConnectionString` në `appsettings.json` ose `appsettings.Development.json` për databazën lokale
- [ ] Vendose `Jwt:Secret` të sigurt në prod (min 32 karaktere)
- [ ] Ekzekuto migrimet: `dotnet ef database update --project Infrastructure --startup-project BusinessDirectory`
- [ ] Vendose `API_BASE_URL` në frontend (p.sh. `https://localhost:7xxx` për development)



## 9. Feature: Businesses API + Swagger polish

Ky feature e ben backend-in me te lehte per testim dhe integrim me frontend.

- Shtuam `BusinessesController` me endpoint-at kryesore:
  - `GET /api/businesses/public` (search, city, type) dhe kthen vetem bizneset `Approved`
  - `GET /api/businesses/{id}`
  - `POST /api/businesses` (vetem user i kyqur)
  - `PUT /api/businesses/{id}` (vetem owner-i dhe vetem kur statusi eshte `Pending` ose `Rejected`)
- Swagger tash ka auth me `Bearer JWT`, qe me testu endpoint-at e mbrojtur direkt ne UI.
- U shtuan shembuj request/response ne Swagger per register, login dhe create business.
- Refactor i modeleve/DTO:
  - `Business.Id` dhe `Comment.BusinessId` kaluan ne `int`
  - `UserId` u riemru ne `OwnerId` te biznesi per me qene me e qarte
- Projektet u unifikuan ne `.NET 8` dhe paketat EF Core u pershtaten me kete.

### Auth update (already included in this workstream)

- Signup tash gjithmone krijon rol `User` (client role injorohet).
- Email/username/password trimohen para save; edhe login trimon input-et.
- Ka validime bazike (required, email format, minimum 8 karaktere per password).
- Duplicate email check eshte normalized + DB ka unique index ne email.
- JWT auth service + endpoint-at jane te lidhura.
