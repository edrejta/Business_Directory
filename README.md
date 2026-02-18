# Business_Directory

## Build (Dotnet 8)

This repo is pinned to .NET 8 via `global.json`. If your environment sets `MSBuildSDKsPath` to another SDK, use the wrapper script to force the .NET 8 SDK during builds.

Examples:

```bash
./scripts/dotnet8.sh build BusinessDirectory.sln
./scripts/clean.sh
```

## Notes

- Admin seeding (informal): `docs/ADMIN_SEED_INFORMAL.md`
