# Auth – Backend i plotë & çfarë duhet në frontend

## Status: 39 dhe 42 – 100% të përfunduara

| Story | Përshkrim | Status backend |
|-------|-----------|----------------|
| **39** | Authenticated user can access protected endpoints (JWT në header → 401 pa/me token të pavlefshëm) | ✅ E përfunduar |
| **42** | Admin role is enforced (endpoint-et Admin → 403 për jo-admin; JWT me Role; policy "AdminOnly") | ✅ E përfunduar |

---

## 1. Çfarë është bërë në backend (komplet)

### 1.1 Autentifikim (Login / Register)
- **POST /api/auth/register** – Regjistrim (username, email, password, role). Kthen 201 + JWT. Email unik, BCrypt, Admin nuk lejohet në signup.
- **POST /api/auth/login** – Login (email, password). Kthen 200 + JWT ose 401 për kredenciale të gabuara.
- JWT përmban: `Id`, `Username`, `Email`, **Role** (ClaimTypes.Role). Konfigurim në `appsettings.json` (Jwt).

### 1.2 Mbrojtja e endpoint-eve (Story 39)
- **Autorizim global:** Të gjitha endpoint-et kërkojnë përdorues të autentifikuar (JWT).
  - Pa token ose token i pavlefshëm → **401 Unauthorized**.
- **Për publik:** Vetëm **Register** dhe **Login** kanë `[AllowAnonymous]` – pa token funksionojnë.
- Çdo endpoint i ri (kontroller/action) automatikisht kërkon JWT, përveç nëse shtoni `[AllowAnonymous]`.

### 1.3 Roli Admin (Story 42)
- **Policy "AdminOnly":** Në `Program.cs` – `RequireRole("Admin")`.
- **AdminController:** `[Authorize(Roles = "Admin")]` në të gjithë kontrollerin.
  - **GET /api/admin** – shembull: pa token → 401; me token User/BusinessOwner → **403 Forbidden**; me token Admin → **200**.
- Për endpoint-e të rinj vetëm për admin: shtoni `[Authorize(Roles = "Admin")]` ose `[Authorize(Policy = "AdminOnly")]` në controller/action.

### 1.4 Të tjera
- CORS për `http://localhost:3000` (frontend).
- SQLite për dev (`UseSqliteForDev`: true), migrimet / EnsureCreated.
- Indeks unik në `User.Email`.

---

## 2. Si funksionon tani (përmbledhje)

| Kërkesa | Rezultat |
|---------|----------|
| POST /api/auth/register (body: username, email, password, role) | 201 + { token, id, username, email, role } |
| POST /api/auth/login (body: email, password) | 200 + { token, id, username, email, role } ose 401 |
| Çdo endpoint tjetër **pa** header `Authorization: Bearer <token>` | 401 |
| GET /api/admin **me** token User ose BusinessOwner | 403 |
| GET /api/admin **me** token Admin | 200 |

---

## 3. Çfarë duhet të bësh ti në frontend

### 3.1 Tashmë (nëse ke formë register/login)
- Formë **Register**: fushat username, email, password, (confirm password), zgjedhje roli (User / Business Owner).
- Formë **Login**: email, password.
- Thirrje API:
  - Register: `POST ${API_BASE}/api/auth/register` me body `{ username, email, password, role }`.
  - Login: `POST ${API_BASE}/api/auth/login` me body `{ email, password }`.
- **Pas suksesit:** Ruaje `token` (dhe mundësisht `role`, `id`) në localStorage/sessionStorage ose cookie.
- **Redirect sipas rolit:**  
  `role === 0` → dashboard user, `role === 1` → dashboard business, `role === 2` → dashboard admin.
- **Gabime:** 400 (mesazh nga backend, p.sh. email i dyfishtë), 401 (email/fjalëkalim i gabuar).

### 3.2 Obligative për endpoint-et e mbrojtura
- Për **çdo** kërkesë ndaj API-së (përveç register/login), shto header:
  - `Authorization: Bearer <token>`
- Nëse API kthen **401**: token mungon, i pavlefshëm ose i skaduar → çlogout dhe ridrejto në faqen e login.
- Nëse API kthen **403**: përdoruesi nuk ka të drejtë (p.sh. u përpoq të hapë faqen/admin pa qenë Admin) → trego mesazh “Nuk ke të drejtë” ose ridrejto në dashboard-in e tij.

### 3.3 Opsionale (rekomandime)
- **Axios / fetch interceptor:** Në çdo request shto automatikisht `Authorization: Bearer <token>` nëse ka token; në 401 bëj logout + redirect në login.
- **Rrugët e mbrojtura (routes):** Nëse nuk ka token, mos lejo hyrje në faqe private; ridrejto në login.
- **Faqet vetëm Admin:** Në frontend kontrollo `role === 2` (ose ekuivalenti); nëse jo, mos shfaq ose ridrejto (backend gjithsesi kthen 403 në API-t admin).
- **Swagger:** Për testim, në http://localhost:5003/swagger mund të përdorësh “Authorize” dhe të vendosësh `Bearer <token>`.

---

## 4. URL dhe body (referencë e shpejtë)

- **Base URL (dev):** `http://localhost:5003` (ose porta që përdor projekti).
- **Register:** `POST /api/auth/register`  
  Body: `{ "username": "...", "email": "...", "password": "...", "role": 0 }`  
  role: 0 = User, 1 = BusinessOwner (Admin nuk jepet nga signup).
- **Login:** `POST /api/auth/login`  
  Body: `{ "email": "...", "password": "..." }`.
- **Përgjigja e auth:** `{ "token": "...", "id": "...", "username": "...", "email": "...", "role": 0 }`.
- **Endpoint admin (shembull):** `GET /api/admin` me header `Authorization: Bearer <token>`.

---

## 5. Përmbledhje

- **Backend:** 39 (protected endpoints → 401 pa JWT) dhe 42 (Admin → 403 për jo-admin, policy + `[Authorize(Roles = "Admin")]`) janë **100% të implementuara**.
- **Frontend:** Duhet të dërgosh tokenin në header për të gjitha kërkesat e mbrojtura, të trajtosh 401 (logout/redirect) dhe 403 (mesazh ose redirect), dhe të ridrejtosh sipas rolit pas login/register. Pjesa tjetër (interceptor, route guard, faqe vetëm Admin) është organizim dhe UX që ti e shtin sipas nevojës.

---

## 6. Session Update (What I implemented now)

In this session, I upgraded admin actions so they are tracked and safer:

- I added audit logging support (`AuditLogs`) to track important admin actions with actor, target, old/new value, optional reason, timestamp, ip, and user-agent.
- I added role-change auditing and reason support:
  - `PATCH /api/admin/users/{id}/role`
  - Optional body field: `reason`
  - Safety checks: no self-demotion, no demotion of last admin.
- I added audit log listing endpoint:
  - `GET /api/admin/audit-logs?take=100`
- I added admin delete-user endpoint with audit logging:
  - `DELETE /api/admin/users/{id}?reason=...`
  - Safety checks: no self-delete, no delete of last admin, and no delete when user still owns businesses/comments.
- I added business suspension reason support:
  - `PATCH /api/admin/businesses/{id}/suspend`
  - Optional body: `{ "reason": "..." }`
  - Saves `SuspensionReason` on business and writes audit log action `BUSINESS_SUSPENDED`.

Database changes I introduced:
- `AuditLogs` table
- `Businesses.SuspensionReason` column
