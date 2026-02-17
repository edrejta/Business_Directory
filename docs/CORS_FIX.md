# Rregullimi i CORS – Login "Failed to fetch" dhe preflight (OPTIONS)

## Çfarë ndodhi

Gjatë login-it nga faqja **http://localhost:3000/login**, frontendi dërgonte një kërkesë **POST** te API-ja e backend-it (p.sh. **http://localhost:5003/api/auth/login**). Shfaqej gabimi **"Failed to fetch"** dhe në skedën **Network** (Chrome DevTools) dy kërkesa shfaqeshin si të dështuara:

- **fetch** te `login` → status `(failed) net::ERR_FAILED`
- **preflight** te `login` → status `(failed) net::ERR_FAILED`

Kjo tregonte një problem **CORS** (Cross-Origin Resource Sharing): shfletuesi bën një kërkesë **OPTIONS** (preflight) para kërkesës reale POST. Nëse backend-i nuk i përgjigjet saktë kësaj kërkesë (status 2xx dhe header-at e duhur), shfletuesi e bllokon kërkesën dhe frontendi merr "Failed to fetch".

---

## Shkaku

1. **Mungesa e konfigurimit CORS**  
   Backend-i (ASP.NET Core) nuk ishte konfiguruar të lejonte kërkesa nga origjina e frontend-it (`http://localhost:3000`). Pa këtë, përgjigja nuk përmbante header-at:
   - `Access-Control-Allow-Origin`
   - `Access-Control-Allow-Methods`
   - `Access-Control-Allow-Headers`
   dhe preflight dështon.

2. **Rendi i middleware-it**  
   Kur **UseHttpsRedirection()** ekzekutohej **para** **UseCors()**, kërkesa OPTIONS (zakonisht në HTTP) mund të ridrejtohej në HTTPS para se CORS të shtonte header-at. Përgjigja e ridrejtimit nuk përmban header-at CORS, ndaj shfletuesi e konsideron preflight-in të dështuar → **net::ERR_FAILED**.

---

## Çfarë u bë (rregullimet)

### 1. Aktivizimi i CORS në backend

Në **`BusinessDirectory/Program.cs`** u shtua:

- **AddCors** me një politikë me emër `"AllowFrontend"`:
  - **Origjina e lejuar:** `http://localhost:3000`
  - **Metodat:** të gjitha (`AllowAnyMethod()`), përfshirë OPTIONS dhe POST
  - **Header-at:** të gjitha (`AllowAnyHeader()`), përfshirë `Content-Type` dhe `Authorization`
  - **Exposed headers:** `Content-Disposition` (opsional, për shkarkime)

Kështu API-ja tani përgjigjet me header-at e nevojshëm për CORS dhe lejon kërkesa nga frontendi.

### 2. Rendi i saktë i middleware-it

- **UseCors("AllowFrontend")** u vendos **para** **UseHttpsRedirection()**.

Kështu kërkesa OPTIONS (preflight) përputhet me politikën CORS dhe merr përgjigje **200/204** me header-at `Access-Control-Allow-Origin`, `Access-Control-Allow-Methods`, `Access-Control-Allow-Headers` para se të ekzekutohet ridrejtimi. Shfletuesi e pranon preflight-in dhe e lejon kërkesën e vërtetë POST.

---

## Skedarët e ndryshuar

| Skedar | Ndryshimi |
|--------|-----------|
| `BusinessDirectory/Program.cs` | Shtuar `AddCors` me politikën `AllowFrontend`; shtuar `UseCors("AllowFrontend")` **para** `UseHttpsRedirection()` |

---

## Si të testosh

1. Nis backend-in (p.sh. `dotnet run --project BusinessDirectory` ose profili **http** me portën 5003).
2. Nis frontend-in në http://localhost:3000.
3. Hap http://localhost:3000/login, plotëso email dhe fjalëkalim, kliko "Hyr".
4. Në Network (DevTools) duhet të shikosh:
   - Kërkesa **OPTIONS** te `/api/auth/login` me status **200** (ose 204) dhe header-at CORS.
   - Kërkesa **POST** te `/api/auth/login` me status 200 (sukses) ose 401 (kredenciale të gabuara).

Nëse ende merr "Failed to fetch", kontrollo që:

- Backend-i të jetë duke u ekzekutuar (p.sh. në portën 5003).
- Frontendi të përdorë URL të plotë të API-t (p.sh. `VITE_API_URL=http://localhost:5003` dhe thirrje te `${API_URL}/api/auth/login`).
- Të mos ketë gabime në konsolën e backend-it gjatë kërkesës.

---

## `net::ERR_CONNECTION_REFUSED` – Backend-i nuk është duke u ekzekutuar

Kur shikon **`POST http://localhost:5003/api/auth/login net::ERR_CONNECTION_REFUSED`** (ose të njëjtën për `/api/auth/register`), shkaku **nuk** është CORS. Kjo do të thotë që **asnjë server nuk dëgjon në portën 5003** – backend-i ASP.NET Core nuk është nisur ose nuk xhiron në atë portë.

### Çfarë të bësh

1. **Nis API-n** nga rrënja e projektit:
   ```bash
   dotnet run --project BusinessDirectory --launch-profile http
   ```
   Profili `http` në `launchSettings.json` e vendos API-n në **http://localhost:5003**.

2. Prit derisa të shfaqet në konsol diçka si: `Now listening on: http://localhost:5003`

3. Pastaj provo përsëri login/register nga frontendi (http://localhost:3000). Nëse backend-i xhiron, kërkesat do të arrijnë dhe CORS do të aplikohet siç duhet.
