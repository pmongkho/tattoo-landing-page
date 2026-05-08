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

## Endpoint testing cheatsheet

Use `dotnet-server/dotnet-server.http` with VS Code REST Client or Rider HTTP Client to run ready-made scenarios for:

- Consultation creation.
- Re-submitting the same client data.
- Validation failures.
- Square webhook deposit events and duplicate event idempotency checks.

Before webhook tests, set these backend variables so signature validation succeeds:

```bash
export Square__WebhookSignatureKey="<your-square-webhook-signature-key>"
export Square__WebhookNotificationUrl="<the-exact-webhook-url-configured-in-square>"
```

Then generate the `x-square-hmacsha256-signature` for each raw JSON payload as:

`base64(hmac_sha256(WebhookNotificationUrl + rawBody, WebhookSignatureKey))`


## Swagger endpoint quick tests

When the backend runs in Development, open Swagger UI (usually `http://localhost:5264/swagger`) and use these endpoints.

### 1) Create consultation

- **Method/Path:** `POST /api/consultations`
- **Swagger body:**

```json
{
  "name": "Jane Tester",
  "phoneNumber": "(702) 555-1188",
  "timeline": "2-4 weeks"
}
```

Expected: `201 Created` with the saved consultation payload.

### 2) Duplicate consultation submission (same person)

- **Method/Path:** `POST /api/consultations`
- **Swagger body:**

```json
{
  "name": "Jane Tester",
  "phoneNumber": "+1 702-555-1188",
  "timeline": "flexible"
}
```

Expected (current behavior): another `201 Created` and another Square sync attempt.

### 3) Validation check (bad full name)

- **Method/Path:** `POST /api/consultations`
- **Swagger body:**

```json
{
  "name": "Jane",
  "phoneNumber": "7025551188",
  "timeline": "next month"
}
```

Expected: `400` validation error (`Please provide your first and last name.`).

### 4) Validation check (bad phone)

- **Method/Path:** `POST /api/consultations`
- **Swagger body:**

```json
{
  "name": "Jane Tester",
  "phoneNumber": "123",
  "timeline": "next month"
}
```

Expected: `400` validation error (`Please provide a valid US phone number.`).

### 5) Square webhook tests from Swagger

You can run `POST /api/square/webhooks` in Swagger too, but add header:

- `x-square-hmacsha256-signature: <valid signature>`

and send a body like:

```json
{
  "event_id": "test-booking-created-001",
  "type": "booking.created",
  "data": {
    "id": "booking-created-test"
  }
}
```

Use a new `event_id` for each first-time test, and re-send the same `event_id` to verify duplicate-event idempotency.
