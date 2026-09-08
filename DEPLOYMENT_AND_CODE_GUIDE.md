# MediLink — Deployment and Code Guide

MediLink is a hyperlocal medicine-ordering marketplace: searchable catalogue, verified pharmacy fulfilment, prescription-aware ordering, cart checkout, delivery status, and role-specific workspaces. It is inspired by the practical user flows of Indian e-pharmacies, but is not affiliated with any third-party pharmacy brand.

## Implemented modules

- **Customer:** medicine search/category browsing, registration/login, cart, checkout, order history and order cancellation.
- **Medical store:** store-owner registration, secured inventory endpoints and a low-stock/inventory-value portal.
- **Admin:** secured platform metrics for customers, pharmacy owners, medicines, pending orders and delivered revenue.
- **.NET concepts:** ASP.NET Core Web API, DTO validation, EF Core code-first/migrations, repository pattern, dependency injection, middleware, JWT, role-based authorization, LINQ, pagination/filtering, Swagger and health checks.
- **Safe stock flow:** checkout checks and decrements stock; a placed-order cancellation restores it.

Prescription approval, payment capture, store approval and delivery-partner assignment are integration boundaries. A real pharmacy deployment must add pharmacist review/auditing, compliant payment integration and local regulatory approval before handling prescription sales.

## Architecture

```text
React + Vite browser application
            │ HTTPS / JSON + JWT
ASP.NET Core API (controllers, middleware, DI)
            │ EF Core / LINQ / migrations
             MySQL 8
```

`MediLink.Core` holds entities, DTOs, interfaces and enums. `MediLink.Infrastructure` owns EF Core, MySQL, repositories and JWT. `MediLink.Api` provides the secured HTTP layer. `MediLink.Web` is the React/Vite interface.

## Run locally with MySQL root / root

Install .NET SDK 10, Node.js 20+ and MySQL 8. Create an empty `MediLink` database, then run:

```powershell
& "C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe" -u root -proot -e "CREATE DATABASE IF NOT EXISTS MediLink;"
cd D:\cdac\Project\from-ubuntu-for-zip\3\MediLink
dotnet user-secrets --project src\MediLink.Api set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;Database=MediLink;User ID=root;Password=root;"
dotnet user-secrets --project src\MediLink.Api set "JwtSettings:Secret" "replace-this-with-a-long-random-development-secret-2026"
dotnet run --project src\MediLink.Api
```

The API normally starts at `http://localhost:5140`; Swagger is at `/swagger` and `/health` checks API/database availability. In a second terminal:

```powershell
cd D:\cdac\Project\from-ubuntu-for-zip\3\MediLink\src\MediLink.Web
npm install
npm run dev
```

Open the printed Vite URL (normally `http://localhost:5173`). The password and JWT secret are intentionally not saved in `appsettings.json`; user secrets stay only on your machine.

If PowerShell says `mysql` is not recognized, MySQL may already be installed but its `bin` directory is not in `PATH`. Use the full command above, or add `C:\Program Files\MySQL\MySQL Server 8.0\bin` to your User `PATH` and open a new terminal.

## How to use

1. Register as a customer, add products, give a delivery address and place an order.
2. Open **My portal** to view order status; a placed order can be cancelled and stock is returned.
3. Register a store owner using `POST /api/auth/register/store-owner` in Swagger, then use secured medicine create/update/archive APIs.
4. Provision an admin in a secure deployment process and assign the `Admin` role to use `/api/dashboard/admin`.

For local development only, the first API startup creates `admin@medilink.local` with password `Admin@123`. Change or remove this account before any deployment.

## Production checklist

- Use managed MySQL with backups, TLS and a least-privilege application user; execute migrations in CI/CD.
- Set `ConnectionStrings__DefaultConnection` and `JwtSettings__Secret` as deployment secrets—never publish root credentials.
- Serve API and static Vite build behind HTTPS, allow only the production web origin in CORS, and rotate JWT keys.
- Add payment webhooks, scanned prescription object storage, pharmacist audit trails, rate limits, observability and privacy/retention controls.
- Before release run `dotnet test`, `dotnet build`, `npm run lint`, and `npm run build`.

## Key routes

| Area | Routes |
| --- | --- |
| Auth | `POST /api/auth/login`, customer/store-owner registration |
| Catalogue | `GET /api/medicines`, categories, secured create/update/archive |
| Orders | secured `/api/cart`, `POST /api/orders`, history and cancellation |
| Portals | `/api/dashboard/customer`, `/store`, `/admin` |
