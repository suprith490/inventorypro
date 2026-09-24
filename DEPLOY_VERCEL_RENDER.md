# Deploying InventoryPro: Vercel (frontend) + Render (backend)

This guide deploys the project as two pieces:

```
   Browser
      |
      v
  Vercel  (static HTML/CSS/JS from wwwroot)
      |  /api/*  rewritten to the backend
      v
  Render  (ASP.NET Core 8 API in Docker, SQLite database)
```

Vercel cannot run .NET, so the frontend is hosted as a static site and the API
runs on Render. Vercel rewrites `/api/*` to Render, so the browser talks to one
origin and no CORS configuration is required.

---

## 0. Prerequisites

- Code pushed to GitHub (already done: `https://github.com/suprith490/inventorypro`).
- A free [Render](https://render.com) account.
- A free [Vercel](https://vercel.com) account.

---

## 1. Deploy the backend to Render

### Option A - Blueprint (recommended)

The repository already contains `render.yaml`.

1. Render Dashboard -> **New +** -> **Blueprint**.
2. Connect the `suprith490/inventorypro` repository.
3. Render reads `render.yaml` and shows a service named **inventorypro-api**.
4. It will prompt for `Cors__AllowedOrigins__0` because it is marked `sync: false`.
   Enter your Vercel URL if you already have it, or a placeholder such as
   `https://example.vercel.app` and update it later.
5. Click **Apply** / **Create**.

### Option B - Manual Docker web service

1. Render Dashboard -> **New +** -> **Web Service**.
2. Connect the repository.
3. Settings:
   - **Runtime**: Docker
   - **Dockerfile Path**: `./Dockerfile`
   - **Docker Context**: `.`
   - **Plan**: Free
   - **Health Check Path**: `/health`
4. Add environment variables:

   | Key | Value |
   | --- | --- |
   | `ASPNETCORE_ENVIRONMENT` | `Production` |
   | `Database__Provider` | `Sqlite` |
   | `Database__ApplyMigrationsOnStartup` | `true` |
   | `Http__UseHttpsRedirection` | `false` |
   | `Jwt__Key` | a random string of 32+ characters |
   | `Jwt__Issuer` | `InventoryPro` |
   | `Jwt__Audience` | `InventoryProClient` |
   | `Jwt__ExpiryMinutes` | `60` |

5. Click **Create Web Service**.

Render builds the Docker image, starts the container, and assigns a URL such as:

```
https://inventorypro-api.onrender.com
```

> Free instances sleep after ~15 minutes of inactivity. The first request after
> sleeping takes 30-60 seconds (cold start). This is normal on the free plan.

### Test the backend

```bash
curl https://inventorypro-api.onrender.com/health
# Healthy

curl -X POST https://inventorypro-api.onrender.com/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"admin@inventorypro.local","password":"Admin@123"}'
# {"success":true,...,"data":{"token":"..."}}
```

---

## 2. Point the frontend at the backend

Open `src/InventoryPro.Api/wwwroot/vercel.json` and replace both placeholders with
your real Render URL:

```json
{
  "rewrites": [
    {
      "source": "/api/:path*",
      "destination": "https://inventorypro-api.onrender.com/api/:path*"
    },
    {
      "source": "/health",
      "destination": "https://inventorypro-api.onrender.com/health"
    }
  ]
}
```

Then commit and push:

```bash
git add src/InventoryPro.Api/wwwroot/vercel.json
git commit -m "chore: point Vercel rewrite at Render backend"
git push
```

---

## 3. Deploy the frontend to Vercel

1. Vercel Dashboard -> **Add New...** -> **Project**.
2. Import the `suprith490/inventorypro` repository.
3. Configure:
   - **Framework Preset**: `Other`
   - **Root Directory**: `src/InventoryPro.Api/wwwroot`  (click Edit to set it)
   - **Build Command**: leave empty / override to empty
   - **Output Directory**: leave empty (the root directory *is* the output)
   - **Install Command**: leave empty (there is no `package.json`)
4. Click **Deploy**.

Vercel gives you a URL such as `https://inventorypro.vercel.app`.

---

## 4. (Optional) Allow direct API calls via CORS

The Vercel rewrite means the browser never calls Render directly, so CORS is not
strictly needed. If you also want to call the Render API from another origin,
set on the Render service:

```
Cors__AllowedOrigins__0 = https://inventorypro.vercel.app
```

You can add more origins with `Cors__AllowedOrigins__1`, `Cors__AllowedOrigins__2`, etc.

---

## 5. Verify the deployment

Open your Vercel URL:

1. `https://<your-app>.vercel.app/` redirects to the login page.
2. Log in with `admin@inventorypro.local` / `Admin@123`.
3. The dashboard loads data (first load may take ~30s if Render was asleep).
4. Create a product, record a purchase and a sale, and confirm stock updates.
5. Check the low-stock and reports pages.
6. `https://<your-app>.vercel.app/health` should return `Healthy`.

Swagger is intentionally disabled in Production (`ASPNETCORE_ENVIRONMENT=Production`).
To inspect the API, run it locally or temporarily set `ASPNETCORE_ENVIRONMENT=Development`
on Render.

---

## 6. Data persistence

The deploy uses the **SQLite** provider. On Render's free plan the filesystem is
ephemeral: the database file is recreated and re-seeded on every restart or
redeploy. This is fine for a demo/portfolio.

For durable data choose one of:

- Attach a **Render persistent disk** (paid plan):

  | Setting | Value |
  | --- | --- |
  | Mount Path | `/data` |
  | Size | 1 GB |

  and set `ConnectionStrings__SqliteConnection` to `Data Source=/data/inventorypro.db`.

- Use a managed **SQL Server** and set:

  ```
  Database__Provider=SqlServer
  ConnectionStrings__DefaultConnection=Server=<host>,1433;Database=InventoryProDb;User Id=<user>;Password=<pass>;TrustServerCertificate=True
  ```

  The SQL Server EF Core migrations are already in the repository and applied
  automatically at startup.

---

## 7. Troubleshooting

| Symptom | Cause | Fix |
| --- | --- | --- |
| Render build fails on `dotnet restore` | Wrong Dockerfile path | Set Dockerfile Path `./Dockerfile`, Context `.` |
| Service starts then health check fails | App not listening on Render's `PORT` | The Dockerfile already honors `$PORT`; redeploy the latest commit |
| Login works in curl but not in the browser | CORS or wrong rewrite | Confirm `vercel.json` points to the correct Render URL and redeploy Vercel |
| Redirect loop / `ERR_TOO_MANY_REDIRECTS` | HTTPS redirection behind the TLS proxy | Set `Http__UseHttpsRedirection=false` on Render |
| First request very slow | Free instance cold start | Wait ~60s and retry, or upgrade the plan |
| Data disappears after a while | Ephemeral SQLite on free plan | Use a persistent disk or SQL Server (section 6) |
| `401` after a while | JWT expired (60 min) or `Jwt__Key` changed | Log in again; keep `Jwt__Key` stable |
| `403` on admin pages | Logged in as Staff | Use the admin account |

---

## 8. Environment variables on Render (reference)

| Variable | Required | Notes |
| --- | --- | --- |
| `ASPNETCORE_ENVIRONMENT` | yes | `Production` |
| `Database__Provider` | yes | `Sqlite` or `SqlServer` |
| `Database__ApplyMigrationsOnStartup` | yes | `true` |
| `Http__UseHttpsRedirection` | yes | `false` behind a proxy |
| `Jwt__Key` | yes | 32+ random characters (use `generateValue`) |
| `Jwt__Issuer` | yes | `InventoryPro` |
| `Jwt__Audience` | yes | `InventoryProClient` |
| `Jwt__ExpiryMinutes` | no | default `60` |
| `ConnectionStrings__DefaultConnection` | only for SQL Server | full connection string |
| `ConnectionStrings__SqliteConnection` | no | defaults to `Data Source=inventorypro.db` |
| `Cors__AllowedOrigins__0` | optional | Vercel URL for direct API access |

---

## 9. Alternative: full stack on Render only

If you prefer one platform, skip Vercel and open the Render URL directly. The API
already serves the frontend from `wwwroot`, so `https://inventorypro-api.onrender.com/`
works as a complete app. The `render.yaml` and manual instructions above are enough.
