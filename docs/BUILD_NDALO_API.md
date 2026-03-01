# Build dështon: "The file is locked by: BusinessDirectory.API"

## Çfarë ndodh?

Kur ekzekuton `dotnet build`, MSBuild përpiqet të kopjojë skedarët e kompilarë (`.dll`) nga folderat e projekteve (Core, Application, Infrastructure) në folderin e API (BusinessDirectory/bin/Debug/...). Nëse **API-ja është duke u ekzekutuar** (p.sh. nga `dotnet run` në një terminal tjetër), Windows nuk lejon të mbishkruhen ato skedarë – prandaj "The process cannot access the file ... because it is being used by another process".

- **BusinessDirectory.API (5160)** = emri i procesit dhe ID-ja e tij (PID). Numri (5160) ndryshon çdo herë që nis API-n.

## Si ta rregullosh

### 1. Ndalo API-n nga terminali ku e nise

Nëse e nise me `dotnet run --project BusinessDirectory` (ose F5 në Visual Studio/Cursor), shko në atë terminal dhe shtyp **Ctrl+C** që ta ndalosh. Pastaj ekzekuto përsëri:

```powershell
dotnet build BusinessDirectory/BusinessDirectory.API.csproj
```

### 2. Nëse nuk e gjen terminalin – ndalo procesin me komandë

Në PowerShell (Administrator ose normal):

```powershell
# Ndalojë procesin që mban .dll (përdor PID nga mesazhi i gabimit, p.sh. 5160)
taskkill /PID 5160 /F
```

Zëvendëso **5160** me numrin që të tregon gabimi (p.sh. "locked by: BusinessDirectory.API (12345)" → përdor 12345).

Ose ndalo të gjitha proceset dotnet që ekzekutojnë API-n (më brutale – mbyll edhe projekte të tjera dotnet nëse janë hapur):

```powershell
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Stop-Process -Force
```

Pas kësaj bëj përsëri `dotnet build`.

---

## Çfarë është çfarë (përmbledhje)

| Term / Gabim | Çfarë do të thotë |
|--------------|-------------------|
| **MSB3026 / MSB3027** | MSBuild nuk mund të kopjojë një skedar (retry 1..10). |
| **MSB3021** | "Unable to copy file" – e njëjta gjë, pas 10 përpjekjesh. |
| **locked by: BusinessDirectory.API (5160)** | Procesi me emrin BusinessDirectory.API dhe PID 5160 e ka skedarin të hapur. |
| **BusinessDirectory.Domain.dll** | Biblioteka e projektit Core (entitetet: User, Business, Comment). |
| **BusinessDirectory.Application.dll** | Biblioteka e projektit Application (DTOs, interfaces, options). |
| **BusinessDirectory.Infrastructure.dll** | Biblioteka e projektit Infrastructure (DbContext, AuthService, migrimet). |

Këto tre .dll duhen kopjuar në `BusinessDirectory/bin/Debug/net10.0/` që API të nisë me versionin e fundit. Ndersa API është duke u ekzekutuar, Windows nuk lejon t’i zëvendësosh ato skedarë – prandaj duhet ta ndalosh API-n, pastaj të bësh build (dhe nëse do, ta nisësh përsëri).
