# Admin login and Identity tables

Admin login now uses ASP.NET Core Identity backed by the same PostgreSQL database as the API. The `AdminIdentity` migration adds `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`, `AspNetRoleClaims`, `AspNetUserLogins`, and `AspNetUserTokens`. Existing consultation and Instagram tables are retained. Identity hashes passwords; there is no plaintext password column and no public registration endpoint.

## First production deployment (Render)

Open **Render → your backend API service → Environment** and set these values securely:

| Variable | Value |
| --- | --- |
| `ConnectionStrings__DefaultConnection` | Your existing PostgreSQL connection string; keep using the intended database |
| `JWT__Key` | A new random signing secret, at least 32 bytes; generate with a password manager or `openssl rand -base64 48` |
| `JWT__Issuer` | `tattoo-api` |
| `JWT__Audience` | `tattoo-admin` |
| `JWT__AccessTokenMinutes` | `120` |
| `Database__ApplyMigrations` | `true` for the deployment that creates/updates the tables |
| `AdminBootstrap__Enabled` | `true` for the first admin creation |
| `ADMIN_EMAIL` | The email address you want to use to sign in |
| `ADMIN_PASSWORD` | A unique password with at least 12 characters, uppercase, lowercase, a number, and a symbol |

Save the settings and redeploy the backend. Migrations run before bootstrap and before the web server accepts requests. Use a single starting instance for first-admin bootstrap. In database tools, inspect the same database/schema used by `ConnectionStrings__DefaultConnection`.

Once the service starts successfully:

1. Confirm the `AspNetUsers` table has your user and `AspNetUserRoles` assigns it the `Admin` role.
2. Open your frontend's `/admin/login` page and use `ADMIN_EMAIL` and `ADMIN_PASSWORD`.
3. Set `AdminBootstrap__Enabled=false` and remove `ADMIN_PASSWORD` from the deployment settings. Keep your password in your password manager.
4. Set `Database__ApplyMigrations=false` if your deployment process applies migrations separately. For future schema changes, apply the migrations again before running the new application code.

Keep `JWT__Key` stable between deployments. Rotating it invalidates existing login tokens. Do not reuse an Instagram/Square/Quo token as this key or commit credentials into application settings. Bootstrap will not reset an existing admin password or silently promote an existing non-admin user. It can be used to create a different new admin email, but it is not a password-reset feature. If existing-user role assignment or password recovery is needed, use a separate controlled administrative operation.

## Local development

Set the database connection and choose your admin credentials in the backend process environment. You can enter secrets without putting them in shell history:

```bash
read -rp 'Admin email: ' ADMIN_EMAIL
export ADMIN_EMAIL
read -rsp 'Admin password: ' ADMIN_PASSWORD; echo
export ADMIN_PASSWORD
export AdminBootstrap__Enabled=true
export Database__ApplyMigrations=true
export ASPNETCORE_ENVIRONMENT=Development
cd dotnet-server
dotnet run
```

In Development only, an unset JWT key generates a temporary signing key for that process; log in again after restarting. In Production a signing key is required to enable admin login. If it is missing, login returns HTTP 503 and protected endpoints remain locked; public endpoints stay available. Set `JWT__Key` securely in development too if you want tokens to survive restarts.

On the prepared cloud machine, first source `/workspace/.setup-tools/env.sh`, then set the variables above. Build with external artifacts and launch the resulting application:

```bash
cd /workspace/tattoo-landing-page
dotnet build dotnet-server/dotnet-server.csproj --artifacts-path /workspace/.setup-tools/artifacts
cd dotnet-server
ASPNETCORE_URLS=http://127.0.0.1:5264 dotnet /workspace/.setup-tools/artifacts/bin/dotnet-server/debug/dotnet-server.dll
```

Stop only the API process you own before relaunching it on an occupied port. The exported variables must be present in the process that starts the API.

Alternatively, apply schema changes explicitly without enabling startup migration:

```bash
dotnet ef database update --project dotnet-server
```

On the prepared cloud machine use `--no-build --msbuildprojectextensionspath /workspace/.setup-tools/artifacts/obj/dotnet-server` after the external-artifacts build. The EF design-time factory reads `ConnectionStrings__DefaultConnection` and does not require JWT credentials.

## Authentication behavior

- Angular posts `{ email, password }` to `POST /api/auth/login`; only users in `Admin` receive an expiring JWT.
- The Angular interceptor sends bearer tokens only to this backend's protected admin/media endpoints, clears expired sessions, and redirects unauthorized sessions to login.
- Consultation management, Instagram management/sync, and the Quo test-message endpoint require the `Admin` role. Public consultation submission and the public Reels feed remain accessible.
- `/admin/media` uses the same admin login; the previous `Instagram__AdminKey` setting is no longer used and can be removed. Keep `Instagram__AccessToken` on the server for Instagram syncing.
- Five failed password attempts lock an account for 15 minutes. Login responses do not disclose whether an email exists or a user lacks the admin role.
- Tokens expire after the configured lifetime (default two hours); there is no refresh-token or password-reset UI in this change. Password/role changes do not invalidate already-issued JWTs before expiry; rotate the signing key if you need to invalidate all tokens immediately.
