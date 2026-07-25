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

These examples use `hrms.example.com` for the frontend and `api.hrms.example.com` for the API -
substitute your own domain throughout.

## 1. MySQL + API on Coolify (one resource, Docker Compose)

`docker-compose.prod.yml` (repo root) defines both services - **not** the plain
`docker-compose.yml` at the repo root, which is local-dev-only (mysql + adminer, no api service).
Every value either service needs comes from a `${VAR}` in that file, supplied as an Environment
Variable on the Coolify resource - nothing sensitive is hardcoded in git.

1. **+ New Resource -> Application**, connect this git repository, **Build Pack: Docker Compose**.
   - **Base Directory**: `/`
   - **Docker Compose Location**: `/docker-compose.prod.yml` (not the default `/docker-compose.yml`)
2. **Environment Variables** tab -> add these (Production environment):

   | Variable | Value |
   |---|---|
   | `MYSQL_ROOT_PASSWORD` | A strong generated password |
   | `MYSQL_DATABASE` | `tsthrms_prod` |
   | `MYSQL_USER` | `tsthrms_app` |
   | `MYSQL_PASSWORD` | A strong generated password (different from root's) |
   | `JWT_KEY` | A random 64+ character secret (`openssl rand -base64 64`) - unique to this deployment, never the dev key from `appsettings.Development.json` |
   | `FRONTEND_ORIGIN` | `https://hrms.example.com` (exact scheme + host, no trailing slash - this becomes the API's CORS-allowed origin) |
   | `SEED_ADMIN_EMAIL` / `SEED_ADMIN_PASSWORD` | Set for the **first** deploy only - creates the initial HR Admin. The seed step is a no-op once any tenant exists, so leaving these set afterward is harmless but unnecessary. |

3. **Persistent Storage** tab -> add a volume mounted at `/data` **on the `api` service**.
   Uploaded documents (`/data/uploads`) and log files (`/data/logs`) both need to survive a
   redeploy - without this volume, every deploy silently wipes every uploaded file, since the
   rest of the container filesystem is ephemeral. Don't add a volume for `mysql_data` here -
   that one's already a named Docker volume declared in the compose file itself.
4. Save, then go back to **General** - after Coolify re-reads the compose file (click
   **Reload Compose File** if the field doesn't appear yet) you should see a **"Domains for api"**
   field (the same way "Domains for adminer" showed up for the local-dev compose file - Coolify
   offers a domain field per service that has a `ports:` entry). Set it to
   `api.hrms.example.com` and enable Coolify's automatic Let's Encrypt certificate. There should
   be no domain field for `mysql` - it has no `ports:` entry on purpose, reachable only
   internally by the `api` service at hostname `mysql`.
5. **DNS**: point `api.hrms.example.com` at your Coolify server (A record to its IP, or whatever
   record type Coolify's domain instructions specify).
6. **Deploy**. Migrations and the initial seed run automatically on the `api` container's startup
   (`Program.cs`, guarded by `Database:MigrateOnStartup`, default `true` - idempotent, so later
   redeploys just no-op past already-applied migrations).

## 2. Frontend on Vercel

1. Import this repository into Vercel as a new project.
2. **Root Directory**: `frontend` (this is a monorepo - Vercel needs to know the SPA doesn't
   live at the repo root).
3. **Framework Preset**: Vite (auto-detected once Root Directory is set correctly).
4. **Environment Variable**: `VITE_API_URL` = `https://api.hrms.example.com/api`. Without this,
   the built SPA falls back to a relative `/api` path, which only resolves correctly in a
   same-origin deployment - not this one.
5. Deploy, then **Settings -> Domains -> Add** `hrms.example.com`, and follow Vercel's DNS
   instructions (a CNAME, typically). This must match `FRONTEND_ORIGIN` on the API (step 1.2)
   exactly, or CORS will reject every request from the deployed frontend.

## 3. First-deploy checklist

- [ ] Coolify resource deployed; both `mysql` and `api` containers show as running (Logs tab).
- [ ] `GET https://api.hrms.example.com/health` returns `200`.
- [ ] `FRONTEND_ORIGIN` exactly matches the domain attached in Vercel (scheme + host, no path,
      no trailing slash) - a mismatch here fails silently as a browser CORS error, not a
      server-side one, so check the browser console first if login doesn't work.
- [ ] `VITE_API_URL` on Vercel points at `https://api.hrms.example.com/api` (note the `/api` suffix).
- [ ] Log in from `https://hrms.example.com` using the `SEED_ADMIN_EMAIL`/`SEED_ADMIN_PASSWORD`
      credentials, then reload the page - a successful silent session restore on reload proves
      the cross-site refresh cookie is actually being accepted by the browser (the part most
      likely to be subtly misconfigured).
- [ ] Upload a test document, trigger a redeploy of the Coolify resource, confirm the document is
      still downloadable afterward - proves the `/data` volume is mounted on the `api` service
      correctly, not just present in the Coolify config.

## 4. Ongoing deploys

- **API**: push to the deployed branch (Coolify's git webhook auto-builds if enabled, or trigger
  a deploy manually from the Coolify UI). Migrations apply automatically on the new container's
  startup, same as the first deploy.
- **Frontend**: push to the deployed branch - Vercel's git integration builds and deploys
  automatically, no manual step needed.
- **Logs**: Coolify's UI shows live container logs; Serilog's file sink also writes to
  `/data/logs/tsthrms-*.log` on the mounted volume for anything that needs to be pulled after
  the fact.
