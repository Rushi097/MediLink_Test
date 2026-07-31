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
$env:MEDILINK_DB_USERNAME = 'root'
$env:MEDILINK_DB_PASSWORD = 'your-mysql-password'
$env:MEDILINK_JWT_SECRET = 'medilink-development-secret-must-be-32-characters'
.\run.cmd start
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
export MEDILINK_DB_USERNAME='root'
export MEDILINK_DB_PASSWORD='your-mysql-password'
export MEDILINK_JWT_SECRET='use-a-long-random-development-secret-of-at-least-32-characters'
bash run.sh start
```

## Ubuntu setup

On Ubuntu or WSL, clone the repository in the Linux filesystem, not under
`/mnt/d`. This prevents Linux from trying to use Windows Node.js binaries:

```bash
mkdir -p ~/projects
cd ~/projects
git clone --branch rushikesh https://github.com/Rushi097/MediLink_Test.git
cd MediLink_Test
```

Install MySQL 8, .NET SDK 10, Node.js 20 or later, Java 21, Maven, and Git.
For a new Ubuntu MySQL installation, create a dedicated application account
instead of using the socket-authenticated `root` account:

```bash
sudo apt update
sudo apt install -y mysql-server mysql-client
sudo systemctl enable --now mysql
sudo mysql -e "CREATE DATABASE IF NOT EXISTS MediLink; CREATE USER IF NOT EXISTS 'medilink'@'localhost' IDENTIFIED BY 'choose-a-strong-password'; GRANT ALL PRIVILEGES ON MediLink.* TO 'medilink'@'localhost'; FLUSH PRIVILEGES;"
```

Install the React dependencies once after cloning:

```bash
cd src/MediLink.Web
npm ci
cd ../..
```

Set the MySQL username, password, and JWT secret, then run the application:

```bash
export MEDILINK_DB_USERNAME='medilink'
export MEDILINK_DB_PASSWORD='choose-a-strong-password'
export MEDILINK_JWT_SECRET='use-a-long-random-development-secret-of-at-least-32-characters'
bash run.sh start
```

Use `bash run.sh status` and `bash run.sh stop` to manage the services. On WSL,
the script writes logs to `~/.local/state/medilink`.

## Use Windows MySQL from WSL

You do not need to install a second MySQL server in WSL when MySQL already runs
on Windows. Install only the MySQL client in WSL so the launcher can verify the
connection:

```bash
sudo apt update
sudo apt install -y mysql-client
```

First test the Windows database through WSL. Modern WSL normally forwards the
Windows MySQL port to `localhost`:

```bash
mysql --protocol=TCP -h localhost -P 3306 -u medilink -p
```

If `localhost` is not reachable, use the Windows host gateway address instead:

```bash
WINDOWS_MYSQL_HOST="$(ip route show default | awk '{print $3}')"
mysql --protocol=TCP -h "$WINDOWS_MYSQL_HOST" -P 3306 -u medilink -p
```

When the test succeeds, use the same values to start MediLink in WSL:

```bash
export MEDILINK_DB_USERNAME='medilink'
export MEDILINK_DB_PASSWORD='your-windows-mysql-password'
export MEDILINK_DB_HOST='localhost' # replace with $WINDOWS_MYSQL_HOST only if needed
export MEDILINK_DB_PORT='3306'
export MEDILINK_JWT_SECRET='use-a-long-random-development-secret-of-at-least-32-characters'
bash run.sh start
```

For Windows PowerShell, use the same MySQL account with
`MEDILINK_DB_HOST='localhost'`. If WSL access is denied, create a development
account in Windows MySQL that is allowed to connect from WSL, then ensure the
Windows firewall permits TCP port 3306.

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
