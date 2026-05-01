# Tattoo Artist Landing Page (Angular + ASP.NET Core)

## What this project now includes

The Angular UI lives in `angular-client/`.

- Angular frontend with Tailwind landing page, public consultation form, discounted tattoo ideas section, and admin pages.
- ASP.NET Core Web API with:
  - ASP.NET Core Identity + EF Core + PostgreSQL.
  - Admin-only JWT login.
  - Public consultation submission endpoint.
  - Public tattoo deals endpoints.
  - Admin consultations/tattoo-deals management endpoints.
  - Development-only admin seed flow using environment variables.

## Folder structure (backend)

- `_Controllers`
- `_Services`
- `_Models`
- `_Data`
- `_Dtos`

## Local development

### 1) Run PostgreSQL

Run PostgreSQL locally (or use Render PostgreSQL for integration testing).

### 2) Backend setup (ASP.NET Core)

From `dotnet-server` (all connection/auth values come from environment variables):

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=tattoo_landing;Username=postgres;Password=postgres"
export JWT__Key="<long-random-string-min-32-chars>"
export JWT__Issuer="dotnet-server"
export JWT__Audience="tattoo-frontend"
export JWT__AccessTokenMinutes="120"
export FRONTEND_ORIGIN="http://localhost:4200"
export ADMIN_EMAIL="admin@example.com"
export ADMIN_PASSWORD="ChangeMe123!"

dotnet ef database update
dotnet run
```

If you are using a Render/Postgres URL format (`postgresql://...`), export it directly and the app will normalize it:

```bash
export ConnectionStrings__DefaultConnection="postgresql://user:password@host:5432/database"
```

### 3) Frontend setup (Angular)

From `angular-client`:

```bash
npm install
npm start
```

Default dev API base URL is configured in `src/environments/environment.ts`.

## Environment variables

No connection string is committed to appsettings files.

### Backend required (production)

- `ConnectionStrings__DefaultConnection` (Render PostgreSQL connection string)
- `JWT__Key`
- `JWT__Issuer`
- `JWT__Audience`
- `JWT__AccessTokenMinutes`
- `FRONTEND_ORIGIN` (Vercel frontend origin)

### Backend optional (development admin seeding only)

- `ADMIN_EMAIL`
- `ADMIN_PASSWORD`

> Admin seeding runs in development only and creates/assigns the `Admin` role.

### Frontend (Vercel)

- `API_BASE_URL` for your production backend URL.

## EF migrations

Initial migration files are under `dotnet-server/Migrations`.

Use:

```bash
cd dotnet-server
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

If `dotnet ef database update` fails with `The ConnectionString property has not been initialized`, verify the variable exists in your current shell:

```bash
echo "$ConnectionStrings__DefaultConnection"
```

You can also set `DATABASE_URL` instead; the EF design-time factory supports both `ConnectionStrings__DefaultConnection` and `DATABASE_URL`.

## Security notes

- No real secrets are committed.
- `appsettings.json` keeps an empty placeholder for `DefaultConnection`.
- Public registration endpoint is not exposed.
- Only users already in the `Admin` role can log in successfully.
