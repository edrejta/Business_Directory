# Admin Seed (Informal Notes)

## What this is
This is a small startup helper that creates **one Admin user** automatically, but only when enabled in config.

It is mainly for local/dev setup so you are not blocked without an admin account.

## What was added
- Config model: `BusinessDirectory/Options/AdminSeedOptions.cs`
- Seeder service: `BusinessDirectory/Services/AdminSeeder.cs`
- Startup wiring: `BusinessDirectory/Program.cs` (marked with `ADMIN_SEED`)
- Config sections:
  - `BusinessDirectory/appsettings.Development.json` (enabled)
  - `BusinessDirectory/appsettings.json` (disabled by default)

## How it works
On app startup, `Program.cs` resolves `AdminSeeder` and calls `SeedAsync()`.

Inside `SeedAsync()`:
1. Checks `AdminSeed.Enabled`
2. Reads email/username/password from config
3. Validates values are not empty
4. Checks DB if admin already exists (by role or configured email)
5. If admin exists -> do nothing
6. If not -> creates admin with:
   - `Role = Admin`
   - normalized email
   - **hashed password** (`BCrypt`)

So it is idempotent (no duplicates on restart).

## Config example
```json
"AdminSeed": {
  "Enabled": true,
  "Email": "admin@businessdirectory.local",
  "Username": "admin",
  "Password": "ChangeThisAdminPassword123!"
}
```

## Dev vs Production
- **Development**: keep enabled if useful.
- **Production/Deployment**: keep disabled, or use it only one-time bootstrap with secrets/env vars, then disable.

## Quick verify (dev)
After first startup:
- one user exists with `Role = Admin`
- password in DB is hashed, not plain text
- restarting app does not create another admin

## Important
If `Enabled = true` and Email/Username/Password are empty, app throws an explicit startup error on purpose.
