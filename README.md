# Business_Directory

## Build (Dotnet 10)

This repo is pinned to .NET 10 via `global.json`. If your environment sets `MSBuildSDKsPath` to another SDK, use the wrapper script to force the .NET 10 SDK during builds.

Examples:

```bash
./scripts/dotnet8.sh build BusinessDirectory.sln
./scripts/clean.sh
```

## Notes

- Admin seeding: `docs/ADMIN_SEED_INFORMAL.md`
