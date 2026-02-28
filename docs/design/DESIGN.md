# Design

In this project, I organize UI design with shared layout, reusable components, and typed contracts.

## UI architecture

Global app structure is handled in:
- `app/layout.tsx`

Reusable UI components live in:
- `components/`

Admin-specific screens and components are under:
- `components/admin/`

Pages are composed through App Router routes in `app/*`.

## Design system status

Styling is implemented with:
- global CSS
- CSS modules
- Tailwind config

## UI data contracts

Typed contracts:
- Auth: `lib/types/auth.ts`
- Business: `lib/types/business.ts`
- User: `lib/types/user.ts`
- Comment: `lib/types/comment.ts`

Validation schemas:
- `lib/validation/`

Forms and API payloads are handled with these types and validation schemas.
