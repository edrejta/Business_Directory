# TDD Tasks – Zgjedhja e Rolit gjatë Signup

Kjo dokument përmban taskat finale për të implementuar **zgjedhjen e rolit** (User / Business Owner / Admin) në formularin e regjistrimit, duke përdorur **Test-Driven Development**.

**Për secilin task:** shkruaj fillimisht testin (që dështon), pastaj implementimin (që e kalon testin).

---

## Para kërkesave – Backend

Backend-i pranon `role` në signup:

- **UserCreateDto** ka fushën `Role` (UserRole: 0=User, 1=BusinessOwner, 2=Admin)
- **POST** `/api/auth/register` pret body: `{ username, email, password, role }`
- **Roli Admin (2) nuk lejohet gjatë signup** – vetëm një admin ekzistues mund ta caktojë. Nëse klienti dërgon `role: 2`, backend e vendos përdoruesin si **User (0)**.
- Rolet e lejuara për signup: **0** (User), **1** (BusinessOwner)

---

## Task 1: Types – RegisterInput me role

| # | Task | Testi (shkruaj fillimisht) | Implementimi |
|---|------|----------------------------|--------------|
| 1.1 | **RegisterInput type** | Nuk ka test të veçantë – thjesht përditëso interface | Shto `role: number` në `RegisterInput` (ose krijo nëse nuk ekziston). Vlerat e vlefshme: 0, 1, 2. |

**Skedar:** `src/types/auth.ts` (ose ekuivalenti në strukturën tënde)

```typescript
export interface RegisterInput {
  username: string;
  email: string;
  password: string;
  role: number; // 0=User, 1=BusinessOwner (Admin nuk lejohet në signup)
}
```

---

## Task 2: Validation – registerSchema me role

| # | Task | Testi | Implementimi |
|---|------|-------|--------------|
| 2.1 | **registerSchema pranon role** | `registerSchema.parse({ username: 'a', email: 'a@b.com', password: '12345678', role: 0 })` → success. Po kështu për role 1. **Admin (2) refuzohet** – vetëm 0 dhe 1 lejohen. | Shto `.refine(r => [0,1].includes(r))` për `role` – vetëm User dhe BusinessOwner. |
| 2.2 | **registerSchema refuzon role invalid** | `registerSchema.safeParse({ ..., role: 5 })` → error. `role: 2` (Admin) → error. `role: -1` → error. | Refino schema që të pranojë vetëm 0 dhe 1. |
| 2.3 | **registerSchema – role default** | (Opsional) Nëse nuk jepet role, default 0. | `.default(0)` në schema. |

**Skedar:** `src/validation/auth.ts` (ose `lib/validation/auth.ts`)

---

## Task 3: API – authApi.register() dërgon role

| # | Task | Testi | Implementimi |
|---|------|-------|--------------|
| 3.1 | **register dërgon role** | Mock `fetch`. Thirr `register({ username, email, password, role: 1 })`. Verifiko që `fetch` thirret me body që përmban `role: 1`. | Shto `role` në body të `register()`. |
| 3.2 | **register me role 0, 2** | Test që `role: 0` dhe `role: 2` dërgohen siç duhet. | Body: `JSON.stringify({ username, email, password, role })`. |

**Skedar:** `src/api/auth.ts` ose `lib/api/auth.ts`

---

## Task 4: RegisterForm – UI për zgjedhjen e rolit

| # | Task | Testi | Implementimi |
|---|------|-------|--------------|
| 4.1 | **Render – Role selector** | Komponenti renderon një `<select>` ose radio buttons me opsione: "Përdorues" (0), "Pronar Biznesi" (1). **Pa Admin** – vetëm këto dy opsione. | Shto fushën `role` në formë – dropdown ose radio me vetëm User dhe BusinessOwner. |
| 4.2 | **Render – label** | Ka label "Roli" ose "Zgjidhni rolin". | Label i qartë për fushen. |
| 4.3 | **Submit – role përfshihet** | Kur përdoruesi zgjedh "Pronar Biznesi" (1) dhe shtyp submit, `onSubmit` thirret me `{ username, email, password, role: 1 }`. | `handleSubmit` i kalon `role` nga state në `onSubmit`. |
| 4.4 | **Default value** | Kur forma hapet, role i parazgjedhur është 0 (User). | `useState(0)` ose `defaultValue={0}`. |

**Skedar:** `src/components/RegisterForm.tsx` ose `components/RegisterForm.tsx`

**Opsionet për UI:**
- `<select>` me `<option value="0">Përdorues</option>`, `value="1">Pronar Biznesi`, `value="2">Administrator`
- Ose radio buttons për UX më të qartë.

---

## Task 5: AuthContext / useAuth – register kalon role

| # | Task | Testi | Implementimi |
|---|------|-------|--------------|
| 5.1 | **register thirret me role** | Mock `authApi.register`. Kur `register({ username, email, password, role: 1 })` thirret, `authApi.register` merr të njëjtat argumente përfshirë `role`. | `register` në context/hook i kalon `role` te `authApi.register()`. |
| 5.2 | **Redirect sipas role** | Pas suksesit, `navigate(getRedirectPath(response.role))` – role vjen nga API. | (Zakonisht tashmë implementuar – API kthen `role` në response.) |

**Skedar:** `src/context/AuthContext.tsx` ose `AuthContext.tsx`

---

## Task 6: RegisterPage – lidhja e plotë

| # | Task | Testi | Implementimi |
|---|------|-------|--------------|
| 6.1 | **handleSubmit kalon role** | Kur `RegisterForm` submit-on me `{ username, email, password, role: 1 }`, `register` nga context thirret me këto të dhëna. | `handleSubmit` i RegisterPage i kalon objektin e plotë (përfshirë role) te `register()`. |

**Skedar:** `src/pages/RegisterPage.tsx` ose `app/register/page.tsx`

---

## Renditja e rekomanduar (TDD)

1. **Task 1** – Types
2. **Task 2** – Validation (test → implement)
3. **Task 3** – API (test → implement)
4. **Task 4** – RegisterForm (test → implement)
5. **Task 5** – AuthContext (test → implement)
6. **Task 6** – RegisterPage (test → implement)

---

## Konstante për rolet (opsional)

Krijo një skedar `src/constants/roles.ts` (ose ekuivalent). **Vetëm rolet e lejuara për signup** – pa Admin:

```typescript
/** Rolet që përdoruesi mund të zgjedhë gjatë signup. Admin nuk lejohet. */
export const SIGNUP_ROLES = [
  { value: 0, label: 'Përdorues' },
  { value: 1, label: 'Pronar Biznesi' },
] as const;
```

Përdore në `RegisterForm` për të gjeneruar opsionet e dropdown-it.

---

## Shembull Test (Vitest) – Task 2.1

```typescript
// validation/auth.test.ts
import { describe, it, expect } from 'vitest';
import { registerSchema } from './auth';

describe('registerSchema', () => {
  const validBase = { username: 'user1', email: 'a@b.com', password: '12345678' };

  it('pranon role 0 dhe 1 (User, BusinessOwner)', () => {
    expect(registerSchema.parse({ ...validBase, role: 0 })).toBeDefined();
    expect(registerSchema.parse({ ...validBase, role: 1 })).toBeDefined();
  });

  it('refuzon Admin (2) dhe vlera të tjera', () => {
    expect(registerSchema.safeParse({ ...validBase, role: 2 }).success).toBe(false); // Admin nuk lejohet
    expect(registerSchema.safeParse({ ...validBase, role: 5 }).success).toBe(false);
    expect(registerSchema.safeParse({ ...validBase, role: -1 }).success).toBe(false);
  });
});
```

---

## Shembull Test – Task 4.3 (RegisterForm submit me role)

```typescript
// RegisterForm.test.tsx
import { render, screen, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { RegisterForm } from './RegisterForm';

describe('RegisterForm', () => {
  it('dërgon role kur përdoruesi zgjedh Pronar Biznesi', async () => {
    const onSubmit = vi.fn();
    render(<RegisterForm onSubmit={onSubmit} />);

    await userEvent.type(screen.getByLabelText(/username/i), 'john');
    await userEvent.type(screen.getByLabelText(/email/i), 'john@test.com');
    await userEvent.type(screen.getByLabelText(/password/i), 'password123');
    await userEvent.selectOptions(screen.getByLabelText(/roli/i), '1');
    fireEvent.submit(screen.getByRole('button', { name: /regjistrohu/i }));

    expect(onSubmit).toHaveBeenCalledWith(
      expect.objectContaining({ username: 'john', email: 'john@test.com', password: 'password123', role: 1 })
    );
  });
});
```

---

## Përmbledhje

| Faza | Skedarë | Taskat |
|------|---------|--------|
| Types | `types/auth.ts` | RegisterInput me `role` |
| Validation | `validation/auth.ts` | registerSchema me role 0,1,2 |
| API | `api/auth.ts` | register() dërgon role në body |
| Form | `RegisterForm.tsx` | Dropdown/radio për rol, submit me role |
| Context | `AuthContext.tsx` | register() kalon role te API |
| Page | `RegisterPage.tsx` | Lidhja e plotë |

Pas përfundimit të të gjitha taskave, përdoruesi mund të zgjedhë rolin (User, Business Owner, Admin) gjatë regjistrimit dhe të ridrejtohet në dashboard-in e duhur pas signup.
