# Deploying to Coolify (API + MySQL) and Vercel (Frontend)

Unlike the single-origin IIS setup (`docs/deployment-windows-server-iis.md`), this is a **split
deployment**: the API + MySQL run in Coolify, the built React SPA runs on Vercel, on a different
origin. That has two real consequences already handled in code, not just config:

- **CORS**: the API only accepts requests from origins listed in `Cors:AllowedOrigins` (env var
  `Cors__AllowedOrigins__0`, `__1`, ...) once `ASPNETCORE_ENVIRONMENT` isn't `Development`
  (`Program.cs`). Point this at your Vercel URL(s) - there is nothing else to change in code.
- **The refresh cookie**: cross-origin requests never send a `SameSite=Strict` or `Lax` cookie at
  all, so `AuthController.SetRefreshCookie` uses `SameSite=None` outside Development - which
  itself requires `Secure=true` (also already the case outside Development). Both origins must be
  HTTPS for this to work; Vercel always is, and Coolify's domains get one automatically (below).

## 1. MySQL on Coolify

1. Coolify dashboard -> your project -> **+ New Resource -> Database -> MySQL** (8.x).
2. Give it a name (e.g. `tsthrms-mysql`) and deploy it. Coolify creates the database, a root
   user, and puts the container on the project's private Docker network.
3. **Do not expose the database's port publicly** - leave it reachable only on the internal
   network. The API resource (step 2 below) reaches it by the internal hostname Coolify assigns
   (visible on the database resource's page, usually the resource/container name), not a public
   IP or port.
4. Create the application database and a dedicated app user (Coolify's MySQL "Terminal" tab, or
   any MySQL client pointed at the internal host from another container on the same network):
   ```sql
   CREATE DATABASE tsthrms_prod CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
   CREATE USER 'tsthrms_app'@'%' IDENTIFIED BY '<strong-generated-password>';
   GRANT ALL PRIVILEGES ON tsthrms_prod.* TO 'tsthrms_app'@'%';
   FLUSH PRIVILEGES;
   ```
5. Note the connection details: internal host, port (usually `3306`), database name, and the
   `tsthrms_app` user/password - needed for `ConnectionStrings__Default` in step 2.

## 2. API on Coolify (Docker deployment)

1. **+ New Resource -> Application**, connect this git repository.
2. **Build Pack: Dockerfile**.
   - **Base Directory**: `backend`
   - **Dockerfile Location**: `Dockerfile` (i.e. `backend/Dockerfile`)
   - **Port**: `8080` (matches `EXPOSE 8080` / `ASPNETCORE_URLS` baked into the Dockerfile)
3. **Persistent storage**: add a volume mounted at `/data`. Uploaded documents
   (`FileStorage__RootPath=/data/uploads`, set in the Dockerfile) and log files
   (`/data/logs`) both need to survive a redeploy - without this volume, every deploy silently
   wipes every uploaded file, since the rest of the container filesystem is ephemeral.
4. **Environment variables**:

   | Variable | Value |
   |---|---|
   | `ConnectionStrings__Default` | `Server=<mysql-internal-host>;Port=3306;Database=tsthrms_prod;User=tsthrms_app;Password=<strong-generated-password>;` |
   | `Jwt__Key` | A random 64+ character secret (`openssl rand -base64 64`) - unique to this deployment, never the dev key from `appsettings.Development.json` |
   | `Jwt__Issuer` / `Jwt__Audience` | Keep as `TSTHRMS` / `TSTHRMS.Client` unless you have a reason to change them |
   | `Cors__AllowedOrigins__0` | Your Vercel URL, e.g. `https://tsthrms.vercel.app` (exact scheme + host, no trailing slash; add `__1`, `__2`, ... for additional origins - a custom domain and its `www.` variant, for example) |
   | `SeedAdmin__Email` / `SeedAdmin__Password` | Set for the **first** deploy only - creates the initial HR Admin. The seed step is a no-op once any tenant exists, so leaving these set afterward is harmless but unnecessary. |

   `ASPNETCORE_ENVIRONMENT=Production` and `ASPNETCORE_URLS=http://+:8080` are already set in the
   Dockerfile; override only if you have a specific reason to.
5. **Health check**: path `/health`, port `8080` (a plain ASP.NET Core health check endpoint -
   no database dependency, so it stays green even mid-migration).
6. **Domain**: attach a domain (a custom subdomain, e.g. `api.yourdomain.com` via a CNAME/A
   record, or Coolify's own generated domain) and enable Coolify's automatic Let's Encrypt
   certificate. The API must be HTTPS for the cross-origin cookie to work at all.
7. Deploy. Migrations and the initial seed run automatically on container startup
   (`Program.cs`, guarded by `Database:MigrateOnStartup`, default `true` - idempotent, so later
   redeploys just no-op past already-applied migrations). Set
   `Database__MigrateOnStartup=false` if you'd rather run `dotnet ef database update` as a
   separate, explicitly-reviewed step instead.

## 3. Frontend on Vercel

1. Import this repository into Vercel as a new project.
2. **Root Directory**: `frontend` (this is a monorepo - Vercel needs to know the SPA doesn't
   live at the repo root).
3. **Framework Preset**: Vite (auto-detected once Root Directory is set correctly).
4. **Environment Variable**: `VITE_API_URL` = your Coolify API's public HTTPS URL plus `/api`,
   e.g. `https://api.yourdomain.com/api`. Without this, the built SPA falls back to a relative
   `/api` path, which only resolves correctly in a same-origin deployment - not this one.
5. Deploy. Optionally attach a custom domain in Vercel's project settings afterward; if you do,
   add that domain to `Cors__AllowedOrigins` on the API too (step 2.4).

## 4. First-deploy checklist

- [ ] MySQL resource running; `tsthrms_app` user created with access to `tsthrms_prod`.
- [ ] API deployed; `GET https://<api-domain>/health` returns `200`.
- [ ] `Cors__AllowedOrigins__0` exactly matches the deployed Vercel URL (scheme + host, no path,
      no trailing slash) - a mismatch here fails silently as a browser CORS error, not a
      server-side one, so check the browser console first if login doesn't work.
- [ ] `VITE_API_URL` on Vercel points at `https://<api-domain>/api` (note the `/api` suffix).
- [ ] Log in from the deployed frontend using the `SeedAdmin` credentials, then reload the page -
      a successful silent session restore on reload proves the cross-site refresh cookie is
      actually being accepted by the browser (the part most likely to be subtly misconfigured).
- [ ] Upload a test document, trigger a redeploy of the API, confirm the document is still
      downloadable afterward - proves the `/data` volume is mounted correctly, not just present
      in the Coolify config.

## 5. Ongoing deploys

- **API**: push to the deployed branch (Coolify's git webhook auto-builds if enabled, or trigger
  a deploy manually from the Coolify UI). Migrations apply automatically on the new container's
  startup, same as the first deploy.
- **Frontend**: push to the deployed branch - Vercel's git integration builds and deploys
  automatically, no manual step needed.
- **Logs**: Coolify's UI shows live container logs; Serilog's file sink also writes to
  `/data/logs/tsthrms-*.log` on the mounted volume for anything that needs to be pulled after
  the fact.
