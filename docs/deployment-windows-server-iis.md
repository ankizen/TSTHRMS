# Deploying to Windows Server (IIS)

TSTHRMS deploys as a single IIS site: the ASP.NET Core API serves both the JSON API (`/api/*`)
and the built React SPA (everything else) from one origin, one app pool, one process. This
avoids CORS entirely and keeps the ops surface small.

## 1. One-time server setup

1. **Windows Server** 2019 or later, with IIS installed (Server Manager -> Add Roles and
   Features -> Web Server (IIS)). Ensure these IIS role services are enabled:
   - Static Content
   - Default Document
   - Request Filtering
2. **Install the .NET 10 Hosting Bundle** (not just the SDK/runtime - the Hosting Bundle
   includes the ASP.NET Core Module v2, which IIS needs to proxy requests to Kestrel):
   https://dotnet.microsoft.com/download/dotnet/10.0 -> ASP.NET Core Runtime -> Hosting Bundle.
   After installing, run `iisreset` so IIS picks up the new module.
3. **Install MySQL 8.4** as a native Windows service (MySQL Installer for Windows, "Server only"
   setup type). Create the production database and a dedicated app user:
   ```sql
   CREATE DATABASE tsthrms_prod CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
   CREATE USER 'tsthrms_app'@'localhost' IDENTIFIED BY '<strong-generated-password>';
   GRANT ALL PRIVILEGES ON tsthrms_prod.* TO 'tsthrms_app'@'localhost';
   FLUSH PRIVILEGES;
   ```
4. **Create the IIS site**:
   - Application Pools -> Add Application Pool -> name `TSTHRMS`, .NET CLR version:
     **No Managed Code** (required - the app runs its own CLR via ANCM, IIS must not also
     try to load it).
   - Sites -> Add Website -> name `TSTHRMS`, physical path e.g. `C:\inetpub\tsthrms`,
     application pool `TSTHRMS`, binding on 443 with your TLS certificate (or 80 while
     testing, then add HTTPS once a certificate is in place - Let's Encrypt via
     [win-acme](https://www.win-acme.com/) works well on Windows Server).
   - If hosting multiple sites on the box, give this one its own dedicated app pool (already
     done above) so an app pool recycle never affects other sites.

## 2. Configuration (never commit these values)

On the server, under the site's physical path, create `appsettings.Production.json`
(this file is never checked into git - it's created directly on the server) or set the
equivalent values as **environment variables** on the Application Pool
(IIS Manager -> Application Pools -> TSTHRMS -> Advanced Settings, or via
`appcmd set config` / the `<environmentVariables>` element in `web.config`):

| Setting | Example |
|---|---|
| `ConnectionStrings__Default` | `Server=localhost;Port=3306;Database=tsthrms_prod;User=tsthrms_app;Password=<strong-generated-password>;` |
| `Jwt__Key` | A random 64+ character secret (`openssl rand -base64 64`) - **different from dev** |
| `Jwt__Issuer` / `Jwt__Audience` | Keep as `TSTHRMS` / `TSTHRMS.Client` unless you have a reason to change them |
| `FileStorage__RootPath` | An **absolute path outside the site's physical path**, e.g. `D:\tsthrms-data\documents` - see note below |

Uploaded files (education certificates, and more document types in later phases) are stored
on disk at `FileStorage:RootPath`, which defaults to a relative `storage/uploads` folder. Point
it at a location outside `C:\inetpub\tsthrms` in production - Step 5 (redeploying) overwrites
the site's physical path, and a relative/in-place path would silently delete every uploaded
file on the next release.

Do not set `SeedAdmin__Email`/`SeedAdmin__Password` in production - the dev-only auto-seed
block in `Program.cs` only runs `if (app.Environment.IsDevelopment())`. Create the first
production HR Admin user directly against the database (or add a one-time admin-provisioning
endpoint/script before go-live) instead of relying on a seeded default password.

Set `ASPNETCORE_ENVIRONMENT=Production` on the app pool so the dev-only migrate/seed/Scalar/CORS
block never runs on the server.

## 3. Publish and deploy

From a dev machine with the .NET 10 SDK and Node.js installed:

```bash
cd backend
dotnet publish src/TSTHRMS.Api -c Release -o ./publish
```

This single command also builds the React app and copies it into `wwwroot` (see the
`BuildAndCopyFrontend` MSBuild target in `TSTHRMS.Api.csproj`) - `./publish` ends up containing
everything needed: the API, its dependencies, and the compiled SPA.

Copy the contents of `./publish` to the server's site path (`C:\inetpub\tsthrms`), for example
via `robocopy`, a CI/CD artifact drop, or a zip deploy. Then:

```powershell
# Apply any pending EF Core migrations against the production database
cd C:\inetpub\tsthrms
dotnet TSTHRMS.Api.dll --apply-migrations   # see note below
```

> Note: this project does not auto-migrate on startup outside Development (by design - schema
> changes in production should be an explicit, reviewed step, not something that fires silently
> whenever the app pool recycles). Run `dotnet ef database update` from a machine with network
> access to the production database and the `Default` connection string configured, using the
> same command as local dev (see root `README.md`), before swapping in a new deployment.

Recycle the app pool (or `iisreset`) after copying new files.

## 4. Verify

- `https://<your-domain>/` should load the SPA shell (falls back to `index.html` via
  `app.MapFallbackToFile` for any non-API route).
- `https://<your-domain>/api/auth/login` (POST) should respond (401 with a bad password is
  expected and correct - it means the API pipeline is alive).
- Check `logs/tsthrms-*.log` under the site path (Serilog file sink) if anything looks wrong;
  IIS's own logs live under `%SystemDrive%\inetpub\logs\LogFiles`.

## 5. Updates

Repeat step 3 for each release: publish, copy files, run any new migrations, recycle the app
pool. Because the app pool is dedicated to this site, recycling it doesn't affect anything else
running on the server.
