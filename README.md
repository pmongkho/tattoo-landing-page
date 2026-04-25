# tattoo-landing-page

## Connect Angular to the .NET API

This repository currently includes the backend API under `dotnet-server`.

### 1) Start the API

```bash
cd dotnet-server
dotnet run
```

The default development URLs are configured in `dotnet-server/Properties/launchSettings.json`:

- `https://localhost:7132`
- `http://localhost:5264`

### 2) CORS is enabled for Angular dev server

`Program.cs` defines an `AngularDev` CORS policy that allows requests from:

- `http://localhost:4200`

If your Angular app runs on a different origin, update the origin in `Program.cs`.

### 3) Call the API from Angular

Use this base URL in Angular service code:

- `https://localhost:7132/api`

Example request path:

- `GET https://localhost:7132/api/weatherforecast`

### 4) Optional (recommended): Angular proxy for local development

Create `proxy.conf.json` in your Angular app:

```json
{
  "/api": {
    "target": "https://localhost:7132",
    "secure": false,
    "changeOrigin": true
  }
}
```

Start Angular with:

```bash
ng serve --proxy-config proxy.conf.json
```

Then call relative paths from Angular:

- `/api/weatherforecast`

### 5) If HTTPS certificate is not trusted

Run:

```bash
dotnet dev-certs https --trust
```
