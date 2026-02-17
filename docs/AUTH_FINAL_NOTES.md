# Business Directory – Auth: Backend & Frontend (Final)

---

## PËRMARRJE

Ky dokument përmbledh **krejt** punën e bërë për Signup dhe Login – backend-in e implementuar dhe çfarë duhet bërë në frontend.

---

# PART 1: BACKEND (E BËRË 100%)

## 1.1 Çfarë u implementua

### API Endpointet
| Metodë | URL | Përshkrim |
|--------|-----|-----------|
| POST | `/api/auth/register` | Signup – krijon përdorues të ri |
| POST | `/api/auth/login` | Login – verifikon kredencialet, kthen JWT |

### Body për Register
**Inputet vijnë nga klienti (formularët e përdoruesit), jo nga databaza.**
```json
{
  "username": "emri",
  "email": "email@example.com",
  "password": "Fjalekalim123!",
  "role": 0
}
```
**Role:** 0 = User, 1 = BusinessOwner. **Admin (2) NUK lejohet** – vetëm User dhe BusinessOwner mund të zgjidhen gjatë regjistrimit.

### Body për Login
**Inputet vijnë nga klienti (formularët e përdoruesit), jo nga databaza.**
```json
{
  "email": "email@example.com",
  "password": "Fjalekalim123!"
}
```

### Përgjigja (të dy endpointet)
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "id": "guid",
  "username": "emri",
  "email": "email@example.com",
  "role": 0
}
```

---

## 1.2 Skedarë të krijuar (Backend)

| Skedar | Përshkrim |
|--------|-----------|
| `Application/Dtos/LoginDto.cs` | DTO për Login |
| `Application/Dtos/AuthResponseDto.cs` | DTO për përgjigjen (token + user) |
| `Application/Options/JwtSettings.cs` | Konfigurim JWT |
| `Application/Interfaces/IAuthService.cs` | Ndërfaqe AuthService |
| `Infrastructure/Services/AuthService.cs` | Implementimi – Register, Login, BCrypt, JWT |
| `BusinessDirectory/Controllers/AuthController.cs` | POST register, POST login |
| `Infrastructure/Migrations/20260215120000_AddUniqueEmailIndex.cs` | Migrim indeks unik Email |

## 1.3 Skedarë të ndryshuar (Backend)

| Skedar | Ndryshimi |
|--------|-----------|
| `Application/Dtos/UserCreateDto.cs` | Shtuar `Role` |
| `Infrastructure/ApplicationDbContext.cs` | Indeks unik për Email |
| `BusinessDirectory/Program.cs` | JWT, AuthService, UseAuthentication, SQLite/Server |
| `BusinessDirectory/appsettings.json` | JWT settings |
| `BusinessDirectory/appsettings.Development.json` | UseSqliteForDev, ConnectionStrings |
| `BusinessDirectory/BusinessDirectory.API.csproj` | BCrypt, JwtBearer |
| `Infrastructure/BusinessDirectory.Infrastructure.csproj` | BCrypt, JWT packages, Sqlite |
| `Core/BusinessDirectory.Domain.csproj` | net10.0 |
| `global.json` | rollForward latestMajor |

---

## 1.4 Siguria (Backend)

- Fjalëkalimet hash-hen me **BCrypt**
- JWT me claims: Id, Email, Username, Role
- Indeks unik mbi Email (pa përsëritje)
- Inputet vijnë nga request body (frontend), jo nga databaza

---

## 1.5 Si të ekzekutosh backend-in

```bash
dotnet run --project BusinessDirectory
```

- API: `http://localhost:5003`
- Swagger: `http://localhost:5003/swagger`

---

# PART 2: FRONTEND (ÇFARË DUHET BËRË)

**Në këtë repo nuk ka frontend të implementuar.** Projekti është vetëm backend API.

## 2.1 Çfarë duhet krijuar në frontend

### A) Faqja Signup (Register)
- Formë me: Username, Email, Password, Zgjedhje roli (User / Business Owner / Admin)
- Request: `POST {API_URL}/api/auth/register`
- Body: `{ username, email, password, role }`
- Më sukses: ruaj token (localStorage/sessionStorage), redirect sipas rolit
- Më gabim: shfaq mesazh (p.sh. "Email ekziston tashmë")

### B) Faqja Login (Signin)
- Formë me: Email, Password
- Request: `POST {API_URL}/api/auth/login`
- Body: `{ email, password }`
- Më sukses: ruaj token, redirect sipas rolit
- Më gabim: shfaq "Email ose fjalëkalim i gabuar"

### C) Redirect sipas rolit
- Role 0 (User) → `/dashboard-user`
- Role 1 (BusinessOwner / pronar biznesi) → `/dashboard-business`
- Role 2 (Admin) → `/dashboard-admin`

### D) Ruajtja e token-it
- Pas Register/Login të suksesshëm: ruaj `token` (dhe mundesisht `role`) në localStorage ose cookie
- Për requestet e mëvonshme: shto header `Authorization: Bearer <token>`

### E) Shembull fetch (JavaScript)

```javascript
// Register
const res = await fetch(`${API_URL}/api/auth/register`, {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ username, email, password, role: 0 })
});
const data = await res.json();
if (res.ok) {
  localStorage.setItem('token', data.token);
  if (data.role === 0) window.location = '/dashboard-user';
  else if (data.role === 1) window.location = '/dashboard-business';
  else if (data.role === 2) window.location = '/dashboard-admin';
}

// Login
const res = await fetch(`${API_URL}/api/auth/login`, {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ email, password })
});
const data = await res.json();
if (res.ok) {
  localStorage.setItem('token', data.token);
  // redirect sipas data.role (0, 1, 2)
}
```

---

## 2.2 Konfigurim frontend

- Vendos `API_URL` (p.sh. `http://localhost:5003` për dev)
- Backend duhet të jetë në ekzekutim kur teston frontend-in

---

# PART 3: GIT WORKFLOW

```bash
git checkout dev
git pull
git checkout -b feature/emri-feature

# ... puno ...

git add .
git commit -m "feat: përshkrimi"
git push -u origin feature/emri-feature
```

Pastaj krijo Pull Request: `feature/emri-feature` → `dev`

### .gitignore
```
*.db
*.db-shm
*.db-wal
```

---

# PËRMBLEDHJE

| Pjesa | Status |
|-------|--------|
| Backend – Register | ✅ E bërë |
| Backend – Login | ✅ E bërë |
| Backend – JWT, BCrypt | ✅ E bërë |
| Frontend – Signup form | ⏳ Duhet bërë |
| Frontend – Login form | ⏳ Duhet bërë |
| Frontend – Ruajtje token | ⏳ Duhet bërë |
| Frontend – Redirect sipas rolit | ⏳ Duhet bërë |
| Dashboards (user, owner, admin) | ⏳ Duhet bërë |
