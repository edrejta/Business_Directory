# Task List – Frontend Login & Register (TDD)

Lista e plotë e taskave për ndërtimin e frontend-it të Login dhe Register. Për secilin task, shkruaj fillimisht testin, pastaj implementimin.

---

## Faza 0: Setup

| # | Task | Përshkrim |
|---|------|-----------|
| 0.1 | Krijo projekt | React + Vite + TypeScript në folder `client/` |
| 0.2 | Instalo varësitë | `axios`, `react-router-dom`, `zod` |
| 0.3 | Instalo test | `vitest`, `@testing-library/react`, `@testing-library/jest-dom`, `@testing-library/user-event`, `jsdom` |
| 0.4 | Konfiguro Vitest | `vite.config.ts` – shto `test: { globals, environment: 'jsdom', setupFiles }` |
| 0.5 | Konfiguro env | Krijo `.env` me `VITE_API_URL=http://localhost:5003` |
| 0.6 | Shto script test | `package.json`: `"test": "vitest"` |

---

## Faza 1: Auth API Layer

| # | Task (TDD) | Testi që shkruan fillimisht | Implementimi |
|---|------------|-----------------------------|--------------|
| 1.1 | **authApi.register()** | Mock `fetch`, thirre `register({ username, email, password })`, verifiko që POST shkon te `/api/auth/register` dhe kthen `{ token, id, username, email, role }` | Krijo `src/api/auth.ts` – funksion `register()` me axios |
| 1.2 | **authApi.login()** | Mock `fetch`, thirre `login({ email, password })`, verifiko POST te `/api/auth/login`, kthen të dhënat e përdoruesit | Funksion `login()` në `src/api/auth.ts` |
| 1.3 | **authApi – error 400** | Kur API kthen 400, hedh error me mesazh `message` nga response (p.sh. "Një përdorues me këtë email ekziston tashmë.") | Error handling në `register()` |
| 1.4 | **authApi – error 401** | Kur API kthen 401, hedh error me mesazh "Email ose fjalëkalim i gabuar." | Error handling në `login()` |

**Skedarë:** `src/api/auth.ts`, `src/types/auth.ts` (interfaces për RegisterInput, LoginInput, AuthResponse)

---

## Faza 2: Auth Storage & Redirect

| # | Task (TDD) | Testi | Implementimi |
|---|------------|-------|--------------|
| 2.1 | **saveToken(token)** | Test që `saveToken('xyz')` e ruaj `xyz` në `localStorage` nën çelësin `auth_token` | `src/auth/storage.ts` |
| 2.2 | **getToken()** | Test që `getToken()` kthen vlerën nga localStorage, ose `null` nëse nuk ekziston | Funksion `getToken()` |
| 2.3 | **clearAuth()** | Test që `clearAuth()` heq token nga localStorage | Funksion `clearAuth()` |
| 2.4 | **getRedirectPath(role)** | Test: `role 0` → `/dashboard-user`, `role 1` → `/dashboard-business`, `role 2` → `/dashboard-admin` | Funksion `getRedirectPath(role)` në `src/auth/redirect.ts` |

---

## Faza 3: Validimi (Zod)

| # | Task (TDD) | Testi | Implementimi |
|---|------------|-------|--------------|
| 3.1 | **registerSchema** | Test: schema e pranon `{ username, email, password }` valide; refuzon username bosh, email pa @, password < 8 | `src/validation/auth.ts` – schema Zod për register |
| 3.2 | **loginSchema** | Test: schema e pranon `{ email, password }` valide; refuzon email invalid, password bosh | Schema Zod për login |

---

## Faza 4: Komponentët e Formës

| # | Task (TDD) | Testi | Implementimi |
|---|------------|-------|--------------|
| 4.1 | **LoginForm – render** | Renderon input Email, Password (type="password"), butonin "Hyr" | `src/components/LoginForm.tsx` |
| 4.2 | **LoginForm – submit** | Kur përdoruesi plotëson dhe shtyp submit, thirret `onSubmit` me `{ email, password }` | `handleSubmit` që thirr `onSubmit` |
| 4.3 | **LoginForm – error** | Nëse `error="Email ose fjalëkalim i gabuar."`, shfaqet mesazhi i kuq | Prop `error`, shfaqja e tij |
| 4.4 | **LoginForm – loading** | Kur `isLoading=true`, butoni është disabled dhe shfaq "Duke u ngarkuar..." | Prop `isLoading` |
| 4.5 | **RegisterForm – render** | Renderon Username, Email, Password, butonin "Regjistrohu" | `src/components/RegisterForm.tsx` |
| 4.6 | **RegisterForm – submit** | Kur submit, thirret `onSubmit` me `{ username, email, password }` | `handleSubmit` |
| 4.7 | **RegisterForm – error** | Nëse `error="Një përdorues me këtë email ekziston tashmë."`, shfaqet mesazhi | Prop `error` |
| 4.8 | **RegisterForm – loading** | Kur `isLoading=true`, butoni disabled | Prop `isLoading` |

---

## Faza 5: Faqet dhe Hook useAuth

| # | Task (TDD) | Testi | Implementimi |
|---|------------|-------|--------------|
| 5.1 | **useAuth hook** | Test (me mock): `login` thirret me kredencialet, kur sukses: `saveToken`, `navigate` te path sipas rolit | `src/hooks/useAuth.ts` ose logjika brenda AuthContext |
| 5.2 | **useAuth register** | Test: `register` thirret, kur sukses: `saveToken`, `navigate` | E njëjta |
| 5.3 | **AuthContext** | Context që ekspozon `user`, `token`, `login`, `register`, `logout`, `isAuthenticated` | `src/context/AuthContext.tsx` |
| 5.4 | **LoginPage** | Renderon `LoginForm`, lidhet me `useAuth().login`, shfaq error nga API | `src/pages/LoginPage.tsx` |
| 5.5 | **RegisterPage** | Renderon `RegisterForm`, lidhet me `useAuth().register`, shfaq error | `src/pages/RegisterPage.tsx` |
| 5.6 | **Link ndërmjet faqeve** | Në Login ka link "Nuk ke llogari? Regjistrohu" → `/register`; në Register "Ke llogari? Hyr" → `/login` | Link në secilën faqe |

---

## Faza 6: Routing & ProtectedRoute

| # | Task (TDD) | Testi | Implementimi |
|---|------------|-------|--------------|
| 6.1 | **Routing** | Routes: `/login`, `/register`, `/dashboard-user`, `/dashboard-business`, `/dashboard-admin`, `/` (home) | `src/App.tsx` me `BrowserRouter`, `Routes`, `Route` |
| 6.2 | **ProtectedRoute** | Përdoruesi pa token që hap `/dashboard-user` ridrejtohet në `/login` | `src/components/ProtectedRoute.tsx` |
| 6.3 | **Public routes** | Përdoruesi me token që hap `/login` ridrejtohet në dashboard sipas rolit | Redirect në LoginPage/RegisterPage |

---

## Faza 7: Dashboards & Logout

| # | Task (TDD) | Testi | Implementimi |
|---|------------|-------|--------------|
| 7.1 | **DashboardUser** | Faqe e thjeshtë: "Mirë se erdhe, {username}" dhe buton "Dil" | `src/pages/DashboardUser.tsx` |
| 7.2 | **DashboardOwner** | Placeholder: "Dashboard Business Owner" | `src/pages/DashboardOwner.tsx` |
| 7.3 | **DashboardAdmin** | Placeholder: "Dashboard Admin" | `src/pages/DashboardAdmin.tsx` |
| 7.4 | **Logout** | Thirret `clearAuth()`, ridrejton në `/login` | Funksioni `logout` në AuthContext |

---

## Faza 8: Stilizim & UX

| # | Task | Përshkrim |
|---|------|-----------|
| 8.1 | CSS/Tailwind | Stilizo formulat, inputet, butonat – dukje e pastër |
| 8.2 | Password visibility toggle | Buton për të shfaqur/fshehur fjalëkalimin |
| 8.3 | Error inline | Gabimet e validimit shfaqen pranë çdo fushe |
| 8.4 | Loading state | Spinner ose tekst "Duke u ngarkuar..." gjatë request |

---

## API Reference (Backend)

### Register
- **POST** `{API_URL}/api/auth/register`
- **Body:** `{ username, email, password }` (pa `role` – backend vendos gjithmonë User)
- **201:** `{ token, id, username, email, role }`
- **400:** `{ message: "Një përdorues me këtë email ekziston tashmë." }`

### Login
- **POST** `{API_URL}/api/auth/login`
- **Body:** `{ email, password }`
- **200:** `{ token, id, username, email, role }`
- **401:** `{ message: "Email ose fjalëkalim i gabuar." }`

### Role → Redirect
- `0` (User) → `/dashboard-user`
- `1` (BusinessOwner / pronar biznesi) → `/dashboard-business`
- `2` (Admin) → `/dashboard-admin`

---

## Struktura e Skedarëve

```
client/
├── src/
│   ├── api/
│   │   └── auth.ts
│   ├── auth/
│   │   ├── storage.ts
│   │   └── redirect.ts
│   ├── components/
│   │   ├── LoginForm.tsx
│   │   ├── RegisterForm.tsx
│   │   └── ProtectedRoute.tsx
│   ├── context/
│   │   └── AuthContext.tsx
│   ├── hooks/
│   │   └── useAuth.ts (opsional – mund të jetë në context)
│   ├── pages/
│   │   ├── LoginPage.tsx
│   │   ├── RegisterPage.tsx
│   │   ├── DashboardUser.tsx
│   │   ├── DashboardOwner.tsx
│   │   └── DashboardAdmin.tsx
│   ├── types/
│   │   └── auth.ts
│   ├── validation/
│   │   └── auth.ts
│   ├── App.tsx
│   └── main.tsx
├── .env
├── package.json
└── vite.config.ts
```

---

## Renditja e Rekomanduar

1. Faza 0 (Setup)
2. Faza 1 (API)
3. Faza 2 (Storage)
4. Faza 3 (Validimi)
5. Faza 4 (Forma)
6. Faza 5 (Pages, AuthContext)
7. Faza 6 (Routing)
8. Faza 7 (Dashboards)
9. Faza 8 (Stilizim)
