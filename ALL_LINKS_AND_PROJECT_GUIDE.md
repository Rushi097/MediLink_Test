# MediLink — Links, Working Guide, and Run Instructions

## Local links

| Area | Link | Purpose |
| --- | --- | --- |
| Customer website | [http://localhost:5173](http://localhost:5173) | Medicine search, customer registration, cart, checkout, and orders. |
| Customer registration | [http://localhost:5173/register](http://localhost:5173/register) | Create a customer account. |
| Store registration | [http://localhost:8081/register](http://localhost:8081/register) | Create a store-owner account, owner profile, and store. |
| Store login | [http://localhost:8081/login](http://localhost:8081/login) | Store-owner inventory and order portal. |
| Store dashboard | [http://localhost:8081/dashboard](http://localhost:8081/dashboard) | Inventory and low-stock overview. |
| API / Swagger | [http://localhost:5140/swagger](http://localhost:5140/swagger) | API documentation and development testing. |
| API health | [http://localhost:5140/health](http://localhost:5140/health) | API-to-MySQL health check. |

## Project working

```text
Customer browser -> React/Vite (5173) -> ASP.NET Core API (5140) -> MySQL
Store owner browser -> Spring Boot portal (8081) ----------------> MySQL
```

- `src/MediLink.Web`: customer-facing React/Vite app.
- `src/MediLink.Api`: ASP.NET Core API for customers, JWT login, carts, orders, medicines, and dashboards.
- `src/MediLink.Core`: shared entities, DTOs, interfaces, and enums.
- `src/MediLink.Infrastructure`: EF Core MySQL data access, migrations, repositories, and JWT service.
- `src/MediLink.Store.Java`: Spring Boot portal for store registration, inventory, images, and order handling.
- `tests/MediLink.Tests`: automated .NET tests; they do not use your real MySQL database.

Both backends share the `MediLink` MySQL database. A store owner has rows in `Users`, `StoreOwnerProfiles`, and `Stores`. Products are stored in `Medicines` and linked to their store through `StoreInventories`. Medicine images are saved in `src/MediLink.Store.Java/uploads` and served as `/uploads/...`.

## First-time setup

Install MySQL 8, .NET SDK 10, Node.js 20+, Java 21+, Maven, and Git Bash/WSL/Linux/macOS Bash. Start MySQL, then create the database once:

```bash
mysql -u root -p -e "CREATE DATABASE IF NOT EXISTS MediLink;"
```

## Start every application in Windows PowerShell

You are already in the MediLink folder, so do not run the Git Bash `cd /d/...` command. Run these PowerShell commands:

```powershell
$env:MEDILINK_DB_PASSWORD = 'root'
$env:MEDILINK_JWT_SECRET = 'medilink-development-secret-must-be-32-characters'
.\run.ps1 start
```

Use these commands later:

```powershell
.\run.ps1 status
.\run.ps1 stop
```

If PowerShell blocks the script, run this once in the same terminal, then retry:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
```

## Start every application in Git Bash, WSL, Linux, or macOS

From the MediLink root:

```bash
export MEDILINK_DB_PASSWORD='your-mysql-password'
export MEDILINK_JWT_SECRET='use-a-long-random-development-secret-of-at-least-32-characters'
./run.sh start
```

Windows Git Bash example:

```bash
cd /d/cdac/Project/from-ubuntu-for-zip/6/MediLink
export MEDILINK_DB_PASSWORD='root'
export MEDILINK_JWT_SECRET='medilink-development-secret-must-be-32-characters'
bash run.sh start
```

Use `./run.sh status` to check processes and `./run.sh stop` to stop them. Runtime logs are saved in `.medilink-run/`.

## Normal workflow

1. Start MySQL and run `./run.sh start`.
2. Create a store owner at the store-registration link and sign in.
3. Add medicines and product images from the store portal.
4. Open the customer website, register a customer, search available medicines, add them to the cart, and place an order.
5. Return to the store portal to view and claim orders.

## Tests

Run these checks before submitting the project:

```bash
dotnet build
dotnet test
cd src/MediLink.Web && npm run lint && npm run build
cd ../MediLink.Store.Java && mvn compile
```
