# InventoryPro

A medium-level, production-style **Inventory & Stock Management System** built with
**C# / ASP.NET Core Web API, Entity Framework Core, SQL Server, JWT auth, and a plain
HTML/CSS/JavaScript frontend**.

It is designed as a resume project and interview-preparation portfolio for a
**Junior .NET Software Engineer** role. If you come from Java/Spring Boot, this README
calls out the equivalent concepts along the way.

---

## 1. What the application does

InventoryPro lets a small business track products, categories, suppliers, purchases
(stock in), sales (stock out) and the resulting inventory movements.

| Area | Feature |
| --- | --- |
| Identity | Register, login, JWT access tokens, role-based authorization |
| Catalog | Product, Category and Supplier CRUD |
| Stock in | Purchase orders that automatically increase product stock |
| Stock out | Sales that automatically decrease product stock (with oversell protection) |
| Inventory | Transaction history, manual stock adjustments, low-stock alerts |
| Discovery | Search, filter, sort and pagination on list endpoints |
| Insights | Admin and Staff dashboards, valuation/sales/purchase reports |
| Quality | DTO validation, global exception handling, standard response envelope, xUnit tests |
| Docs | Swagger / OpenAPI with a bearer-token padlock |

### Roles

- **Admin** - manages users, products, categories, suppliers; sees all reports and the admin dashboard.
- **Staff** - manages purchases (stock in), sales (stock out) and inventory adjustments; sees the staff dashboard.

---

## 2. Tech stack

| Concern | Technology | Spring Boot equivalent |
| --- | --- | --- |
| Language | C# 12 | Java |
| Web framework | ASP.NET Core 8 Web API | Spring Web (MVC) |
| ORM | Entity Framework Core 8 | Hibernate / JPA |
| Database | SQL Server (SQLite fallback for dev) | PostgreSQL / MySQL |
| Auth | JWT Bearer + `[Authorize(Roles=...)]` | Spring Security + JWT filter |
| Validation | DataAnnotations + model binding | Bean Validation (`@Valid`) |
| Docs | Swashbuckle / OpenAPI | springdoc-openapi |
| Tests | xUnit + EF Core SQLite in-memory | JUnit 5 |
| DI | Built-in `IServiceCollection` | Spring ApplicationContext |
| Config | `appsettings.json` + env vars | `application.yml` + profiles |
| Frontend | HTML, CSS, vanilla JS `fetch()` | static resources / Thymeleaf |
| Container | Docker multi-stage + docker compose | Dockerfile + compose |

---

## 3. Architecture

Classic layered flow, kept beginner-friendly:

```
Browser (HTML/CSS/JS)
        |  fetch() + Bearer JWT
        v
Controllers  ->  Services  ->  EF Core (AppDbContext)  ->  SQL Server
   (HTTP)        (business)        (data access)            (storage)
```

- **Controllers** only translate HTTP <-> DTOs. No business logic.
- **Services** hold business rules (stock math, uniqueness checks, oversell protection).
- **Repositories**: intentionally *not* added as a separate abstraction on top of EF Core.
  `DbSet<T>` plus LINQ already is a repository/unit-of-work. A hand-written repository
  layer here would be ceremony without value (a common interview discussion point).
- **DTOs** keep EF entities out of the API surface (prevents over-posting and circular JSON).

### Folder layout

```
InventoryPro.sln
src/InventoryPro.Api/
  Program.cs                     # composition root: DI, middleware, pipeline
  appsettings.json               # configuration
  Common/                        # ApiResponse envelope, paging, exceptions, roles
  Configuration/                 # JwtSettings
  Controllers/                   # 11 REST controllers
  DTOs/                          # request/response models per feature
  Data/
    AppDbContext.cs              # DbSets + model configuration
    Configurations/              # IEntityTypeConfiguration per entity (keys, indexes)
    DbSeeder.cs                  # baseline users/categories/suppliers/products
  Entities/                      # EF Core entities
  Extensions/                    # AddDatabase / AddApplicationServices / AddJwtAuthentication
  Mapping/                       # entity -> DTO projections
  Middleware/                    # ExceptionHandlingMiddleware
  Migrations/                    # EF Core migrations (SQL Server)
  Services/
    Interfaces/                  # contracts
    Implementations/             # business logic
  wwwroot/                       # the frontend (pages, css, js)
tests/InventoryPro.Api.Tests/    # xUnit tests
scripts/                         # local run + smoke test helpers
Dockerfile                       # multi-stage production image
docker-compose.yml               # SQL Server + API local stack
```

---

## 4. Prerequisites

- .NET SDK **8.0** (`dotnet --version`)
- SQL Server 2019+ **or** use the built-in **SQLite fallback** for a zero-install first run
- (Optional) Docker + Docker Compose for the production-like stack

---

## 5. Running the project

### 5.1 Quick start with SQLite (no database server required)

```bash
dotnet restore InventoryPro.sln
dotnet run --project src/InventoryPro.Api/InventoryPro.Api.csproj
```

Or use the helper script:

```bash
./scripts/run-local-sqlite.sh
```

The app serves the API **and** the frontend on `http://localhost:5231`:

- Frontend: http://localhost:5231/
- Swagger: http://localhost:5231/swagger

### 5.2 Run with SQL Server

1. Make sure SQL Server is reachable (default `localhost,1433`) and update
   `ConnectionStrings:DefaultConnection` in `src/InventoryPro.Api/appsettings.json`
   or set the environment variable.
2. Run:

```bash
./scripts/run-local-sqlserver.sh
```

### 5.3 Run the full production-like stack with Docker

```bash
cp .env.example .env
# edit .env and set a strong MSSQL_SA_PASSWORD and a random JWT_KEY
docker compose up --build
```

Then open http://localhost:8080 (Swagger at `/swagger`).

### 5.4 Seeded accounts

| Role | Email | Password |
| --- | --- | --- |
| Admin | `admin@inventorypro.local` | `Admin@123` |
| Staff | `staff@inventorypro.local` | `Staff@123` |

> Change these before any real deployment.

---

## 6. Configuration

`appsettings.json` holds non-secret defaults. Secrets should come from environment
variables (they win over JSON, like Spring's relaxed binding / env overrides).

| Key | Purpose | Env var |
| --- | --- | --- |
| `Database:Provider` | `SqlServer` or `Sqlite` | `Database__Provider` |
| `Database:ApplyMigrationsOnStartup` | Migrate + seed at boot | `Database__ApplyMigrationsOnStartup` |
| `ConnectionStrings:DefaultConnection` | SQL Server connection | `ConnectionStrings__DefaultConnection` |
| `Jwt:Key` | HMAC signing key (32+ chars) | `Jwt__Key` |
| `Jwt:Issuer` / `Jwt:Audience` | Token scope | `Jwt__Issuer` / `Jwt__Audience` |
| `Jwt:ExpiryMinutes` | Token lifetime | `Jwt__ExpiryMinutes` |
| `Cors:AllowedOrigins` | Allowed browser origins | `Cors__AllowedOrigins__0` ... |

.NET uses `__` (double underscore) to express `:` in environment variable names.

---

## 7. Database design

Nine tables with proper keys, foreign keys, constraints and indexes.

```
Users                 (1) ─── (0..*) Purchases
Users                 (1) ─── (0..*) Sales
Users                 (1) ─── (0..*) InventoryTransactions

Categories            (1) ─── (0..*) Products
Suppliers             (1) ─── (0..*) Products

Purchases             (1) ─── (1..*) PurchaseItems
Products              (1) ─── (0..*) PurchaseItems

Sales                 (1) ─── (1..*) SaleItems
Products              (1) ─── (0..*) SaleItems

Products              (1) ─── (0..*) InventoryTransactions
Purchases             (0..1) ── (0..*) InventoryTransactions
Sales                 (0..1) ── (0..*) InventoryTransactions
```

Highlights:

- `Users.Email` and `Products.SKU` have **unique indexes**.
- `Products` has indexes on `CategoryId`, `SupplierId` and `Name` for filtering.
- `InventoryTransactions` links each movement back to its source purchase or sale.
- Delete behavior is deliberately restrictive where history must be preserved.

Entities live in `src/InventoryPro.Api/Entities/`; fluent configuration is in
`src/InventoryPro.Api/Data/Configurations/`.

---

## 8. API reference

All responses use the envelope:

```json
{
  "success": true,
  "message": "Request successful",
  "statusCode": 200,
  "data": { },
  "errors": null,
  "timestampUtc": "2026-01-01T00:00:00Z"
}
```

### Auth (`/api/auth`)

| Method | Route | Access | Description |
| --- | --- | --- | --- |
| POST | `/api/auth/register` | Anonymous | Create a Staff account |
| POST | `/api/auth/login` | Anonymous | Returns a JWT + profile |
| GET | `/api/auth/me` | Authenticated | Current user profile |
| GET | `/api/auth/admin-only` | Admin | Example role-guarded probe |

### Catalog

| Method | Route | Access |
| --- | --- | --- |
| GET | `/api/products?search=&categoryId=&supplierId=&lowStockOnly=&sortBy=&sortDescending=&page=&pageSize=` | Any role |
| GET | `/api/products/{id}` | Any role |
| POST / PUT / DELETE | `/api/products` | Admin |
| GET | `/api/categories` | Any role |
| POST / PUT / DELETE | `/api/categories` | Admin |
| GET | `/api/suppliers` | Any role |
| POST / PUT / DELETE | `/api/suppliers` | Admin |

### Stock movement

| Method | Route | Access | Description |
| --- | --- | --- | --- |
| GET | `/api/purchases` | Admin, Staff | Purchase history (paged) |
| GET | `/api/purchases/{id}` | Admin, Staff | One purchase with line items |
| POST | `/api/purchases` | Admin, Staff | Stock in: increases product quantity |
| GET | `/api/sales` | Admin, Staff | Sales history (paged) |
| GET | `/api/sales/{id}` | Admin, Staff | One sale with line items |
| POST | `/api/sales` | Admin, Staff | Stock out: decreases product quantity |
| GET | `/api/inventory/transactions` | Any role | Inventory transaction ledger |
| GET | `/api/inventory/low-stock` | Any role | Products at/below reorder level |
| POST | `/api/inventory/adjust` | Admin, Staff | Manual stock correction |

### Users, dashboards and reports

| Method | Route | Access |
| --- | --- | --- |
| GET | `/api/users` | Admin |
| PUT | `/api/users/{id}/role` | Admin |
| PUT | `/api/users/{id}/status` | Admin |
| GET | `/api/dashboard/admin` | Admin |
| GET | `/api/dashboard/staff` | Admin, Staff |
| GET | `/api/reports/inventory-valuation` | Admin |
| GET | `/api/reports/sales` | Admin, Staff |
| GET | `/api/reports/purchases` | Admin, Staff |

Health check (anonymous): `GET /health` -> `Healthy`.

---

## 9. Testing

```bash
dotnet test InventoryPro.sln
```

- 39 xUnit tests cover services, auth, password hashing and inventory math.
- Tests run against **EF Core SQLite** so they need no SQL Server.
- `TestDatabase.cs` builds an isolated context per test.

End-to-end smoke test against a running server:

```bash
./scripts/run-local-sqlite.sh &          # start the API
BASE_URL=http://localhost:5231 ./scripts/smoke-test.sh
```

The smoke test checks login, 401/403 authorization, purchase stock increase,
sale stock decrease, 409 oversell protection and 400 validation.

---

## 10. Frontend

The static site in `src/InventoryPro.Api/wwwroot/` is served from the same origin as
the API, so there is no CORS friction in production and relative `/api/...` URLs work.

Pages: `login`, `register`, `dashboard`, `products`, `categories`, `suppliers`,
`purchases`, `sales`, `inventory`, `reports`, `profile`, `users`.

- `js/api.js` wraps `fetch()`: attaches the JWT, unwraps the envelope, redirects on 401.
- `js/auth.js` stores the token/profile and guards pages.
- `js/ui.js` has shared rendering helpers (tables, toasts, pagination).
- `js/config.js` switches the API base URL if the frontend is hosted on another origin
  (for example VS Code Live Server on `:5500`).

To host the frontend separately, point `API_BASE_URL` at the API and make sure the
origin is listed in `Cors:AllowedOrigins`.

---

## 11. Common errors and fixes

| Symptom | Cause | Fix |
| --- | --- | --- |
| `Couldn't find a valid ICU package` (Linux) | Missing ICU libs | `apt-get install -y libicu-dev` or set `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` |
| Cannot connect to SQL Server | Server not running / wrong connection string | Start SQL Server, verify `ConnectionStrings__DefaultConnection` |
| `A network-related error...` with SQLite | Stale `inventorypro.db` | Delete `src/InventoryPro.Api/inventorypro.db` and restart |
| `401` immediately after login | Expired/invalid JWT, wrong `Jwt:Key` | Use the same key across restarts; re-login |
| `403` for an admin screen as Staff | Role guard working as designed | Use the admin account |
| `409 Conflict` on a sale | Selling more than stock on hand | Reduce quantity or buy more stock first |
| Swagger UI empty | `GenerateDocumentationFile` XML missing | Rebuild; XML comments are copied to output |
| CORS error in browser | Frontend origin not allowed | Add origin to `Cors:AllowedOrigins` |

---

## 12. AWS-ready deployment

The project ships with a multi-stage `Dockerfile` and a `docker-compose.yml`.

- The image listens on `http://+:8080` (ASP.NET Core 8 container convention).
- `ASPNETCORE_ENVIRONMENT=Production` enables HTTPS redirection and hides Swagger.
- Secrets are injected as environment variables (`Jwt__Key`, connection string).

Typical AWS options:

- **AWS App Runner** or **ECS Fargate** for the container.
- **Amazon RDS for SQL Server** as the database; set
  `ConnectionStrings__DefaultConnection` from Secrets Manager.
- Store `Jwt__Key` in **AWS Secrets Manager / SSM Parameter Store**.
- Front the service with an **ALB** and terminate TLS there.

Minimal ECS/App Runner environment variables:

```
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
Database__Provider=SqlServer
ConnectionStrings__DefaultConnection=Server=<rds-endpoint>,1433;Database=InventoryProDb;User Id=<user>;Password=<pass>;TrustServerCertificate=True
Jwt__Key=<32+ char secret>
Jwt__Issuer=InventoryPro
Jwt__Audience=InventoryProClient
```

---

## 13. Git and GitHub

```bash
git init
git add .
git commit -m "feat: InventoryPro full-stack inventory management system"
git branch -M main
git remote add origin https://github.com/<you>/inventorypro.git
git push -u origin main
```

Suggested commit style: `feat:`, `fix:`, `chore:`, `refactor:`, `test:`, `docs:`.

---

## 14. Resume bullets

Copy/paste and adapt (quantify where you can):

- Built a full-stack **Inventory & Stock Management System** using **C# / ASP.NET Core 8 Web API**,
  **Entity Framework Core**, **SQL Server** and a responsive **HTML/CSS/JavaScript** frontend.
- Designed a **normalized 9-table relational schema** (Users, Categories, Products, Suppliers,
  Purchases, PurchaseItems, Sales, SaleItems, InventoryTransactions) with foreign keys,
  unique constraints and performance indexes using EF Core Fluent API.
- Implemented **JWT authentication and role-based authorization** (Admin/Staff) with password
  hashing and `[Authorize(Roles=...)]` policies.
- Wrote **business logic for automatic stock updates** on purchases/sales, oversell prevention
  (`409 Conflict`), low-stock alerts and a full inventory transaction ledger.
- Exposed **RESTful APIs with search, filtering, sorting and pagination**, a consistent response
  envelope, **DTO validation** and **global exception handling** with correct HTTP status codes.
- Documented the API with **Swagger/OpenAPI**, including bearer-token auth for interactive testing.
- Achieved **39 passing xUnit tests** using EF Core SQLite in-memory databases, plus an
  end-to-end smoke test script.
- Containerized the API with a **multi-stage Dockerfile** and **docker-compose**, and prepared
  it for **AWS** deployment (ECS/App Runner + RDS SQL Server + Secrets Manager).

Short version for a one-page resume:

> **InventoryPro** - Full-stack inventory management system (ASP.NET Core 8, EF Core, SQL Server,
> JWT, vanilla JS). Built REST APIs with role-based auth, automatic stock accounting, reporting,
> Swagger docs, Docker deployment, and 39 xUnit tests.

---

## 15. Interview explanations and likely questions

### 15.1 Walk me through the architecture

> "Controllers receive HTTP requests and bind them to DTOs. Controllers call services, which
> contain the business rules and use EF Core's `AppDbContext` to read and write SQL Server.
> I return a consistent `ApiResponse` envelope and use a global exception middleware to map
> domain exceptions to proper status codes. It's the same layered idea as Spring Boot's
> `@RestController -> @Service -> @Repository`, except I let EF Core's `DbSet` be the repository
> and register dependencies explicitly in `Program.cs`."

### 15.2 Controllers vs. Services - why split them?

> "To keep HTTP concerns separate from business rules. Controllers stay thin and testable-free;
> services are unit-tested directly without spinning up a web server. In Spring terms it's
> `@Controller` vs `@Service`."

### 15.3 Why no repository pattern over EF Core?

> "`DbContext` already implements unit of work and `DbSet<T>` already implements repository.
> Wrapping them adds indirection with little payoff at this size. If I later needed to support
> a second data store or complex query composition, I would introduce an interface then."

### 15.4 How does the stock update stay consistent?

> "Creating a purchase/sale and adjusting product quantity and writing the inventory transaction
> happen inside one service call and one `SaveChangesAsync`, which EF Core wraps in a database
> transaction. If anything fails, the whole operation rolls back. Overselling is checked before
> committing and returns `409 Conflict`."

### 15.5 How does JWT auth work here?

> "On login the service verifies the password hash and issues a signed JWT containing the user id
> and role claims. The client stores it and sends `Authorization: Bearer <token>`. The JWT
> bearer middleware validates signature, issuer, audience and expiry, then `[Authorize]` and
> `[Authorize(Roles=...)]` enforce access. This mirrors Spring Security with a JWT filter."

### 15.6 Why DTOs instead of returning entities?

> "To avoid over-posting (a client setting `IsActive` or a foreign key it shouldn't), to shape
> responses for the UI, and to prevent serialization cycles from navigation properties.
> Equivalent to using Java records / request-response classes."

### 15.7 How is validation handled?

> "DataAnnotations on DTOs are evaluated automatically by `[ApiController]`. I customized
> `InvalidModelStateResponseFactory` so validation errors come back in the same envelope as
> every other error, with `400 Bad Request`. Business-rule violations throw typed exceptions
> mapped by the middleware."

### 15.8 How do you do pagination?

> "A reusable `PagedResult<T>` plus `PaginationQueryParameters`; services apply `Skip`/`Take`
> and return `Items`, `Page`, `PageSize`, `TotalCount` and `TotalPages`. The frontend renders
> page controls from that metadata."

### 15.9 What did you test and how?

> "Service-level xUnit tests against an EF Core SQLite in-memory database: auth, password
> hashing, product/category logic, purchase/sale stock math and inventory. SQLite keeps tests
> fast and removes the SQL Server dependency. I also have a bash smoke test for the running API."

### 15.10 Common follow-ups

- **Async/await**: why `async` all the way? -> frees the thread pool during I/O, scales better under load; never block with `.Result`.
- **`SaveChangesAsync` vs `SaveChanges`**: async I/O version of the same unit-of-work commit.
- **Migrations**: `dotnet ef migrations add Name` then `dotnet ef database update`; akin to Flyway/Liquibase versioned scripts.
- **Indexes**: unique on `Email`/`SKU`, non-clustered on foreign keys and search columns.
- **Status codes**: 200/201/204 success, 400 validation, 401 unauthenticated, 403 forbidden, 404 missing, 409 conflict.
- **Soft delete**: products/suppliers/categories are deactivated rather than physically deleted to protect history.
- **Scaling**: stateless API + JWT means horizontal scaling behind a load balancer is straightforward; move the DB to RDS and cache reads if needed.
- **What would you add next?**: refresh tokens, refresh-token rotation, audit logging, SignalR low-stock notifications, integration tests with `WebApplicationFactory`, CI with GitHub Actions.

---

## 16. License

Provided as a learning/portfolio project. Use and adapt freely.
