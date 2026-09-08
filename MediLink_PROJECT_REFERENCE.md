# MediLink --- Project Reference for AI Agents

> Generated from the uploaded `MediLink.zip` repository. This document
> is a repository map and behavioral reference for future agents. It
> describes the code that is actually present in the archive, not an
> idealized architecture.

## 1. Project identity

**MediLink** is a hyperlocal online medicine-ordering marketplace. The
customer side is a React/Vite web application. The primary customer
backend is ASP.NET Core Web API. The shared .NET domain/data libraries
are `MediLink.Core` and `MediLink.Infrastructure`. A separate Spring
Boot application provides the store-owner portal. The repository also
contains separate ASP.NET projects named `MediLink.Auth`,
`MediLink.Inventory`, and `MediLink.Order` that expose overlapping
service endpoints; the current Windows launcher starts the main
`MediLink.Api`, React web app, and Spring Boot store portal, not those
three standalone .NET services.

### High-level runtime flow

``` text
Customer Browser
    |
    v
MediLink.Web (React + Vite) :5173
    |
    | Axios / REST
    v
MediLink.Api (ASP.NET Core) :5140
    |
    +--> MediLink.Core (entities, DTOs, interfaces, enums)
    |
    +--> MediLink.Infrastructure (EF Core, MySQL DbContext, repository, JWT)
    |
    v
MySQL database: MediLink

Store Owner Browser
    |
    v
MediLink.Store.Java (Spring Boot + Thymeleaf) :8081
    |
    | Spring Data JPA / Hibernate
    v
Same MySQL database: MediLink
```

## 2. Repository structure

``` text
MediLink/
├── .agent.md
├── ALL_LINKS_AND_PROJECT_GUIDE.md
├── DEPLOYMENT_AND_CODE_GUIDE.md
├── PROJECT_CONTINUATION_NOTES.md
├── MediLink.slnx
├── run.cmd
├── run.ps1
├── run.sh
├── src/
│   ├── MediLink.Web/              # React/Vite customer frontend
│   ├── MediLink.Api/              # Main ASP.NET Core API currently launched
│   ├── MediLink.Core/             # Shared domain/DTO/interface library
│   ├── MediLink.Infrastructure/   # EF Core/MySQL/repository/JWT implementation
│   ├── MediLink.Auth/             # Separate ASP.NET Auth service project
│   ├── MediLink.Inventory/        # Separate ASP.NET Inventory service project
│   ├── MediLink.Order/             # Separate ASP.NET Order service project
│   └── MediLink.Store.Java/       # Spring Boot store-owner portal
└── tests/
    └── MediLink.Tests/            # .NET tests
```

## 3. Actual project references

`MediLink.Api` references: - `MediLink.Core` - `MediLink.Infrastructure`

`MediLink.Infrastructure` references: - `MediLink.Core`

`MediLink.Auth`, `MediLink.Inventory`, and `MediLink.Order` each
reference: - `MediLink.Core` - `MediLink.Infrastructure`

`MediLink.Store.Java` is independent of the .NET project references and
is a Maven/Spring Boot application.

The solution file `MediLink.slnx` includes the six .NET projects plus
the test project; the Java and React projects are present in the
repository but are not .NET solution projects.

## 4. Technology stack found in the repository

### Customer frontend

-   React 19
-   Vite 8
-   JavaScript/JSX
-   Axios
-   React Router DOM
-   Bootstrap / React Bootstrap
-   React Hook Form
-   React Icons
-   React Toastify
-   jwt-decode

### Main backend

-   ASP.NET Core / .NET 10
-   Entity Framework Core
-   Pomelo Entity Framework Core MySQL provider
-   JWT Bearer authentication
-   BCrypt.Net-Next password hashing
-   Swashbuckle / Swagger

### Store portal

-   Java 25 configured in Maven `pom.xml`
-   Spring Boot 3.5.16
-   Spring MVC
-   Thymeleaf
-   Spring Data JPA / Hibernate
-   Spring Security
-   MySQL Connector/J

### Database

-   MySQL, expected server version 8.0.x; main API configuration uses
    8.0.36.

## 5. Main runtime ports and URLs

  ---------------------------------------------------------------------------------
  Component               URL                               Role
  ----------------------- --------------------------------- -----------------------
  React customer web      `http://localhost:5173`           Customer UI

  ASP.NET Core main API   `http://localhost:5140`           Customer REST API

  Swagger                 `http://localhost:5140/swagger`   Main API
                                                            documentation/testing

  API health              `http://localhost:5140/health`    Main API -\> MySQL
                                                            health check

  Spring Boot store       `http://localhost:8081`           Store-owner UI
  portal                                                    
  ---------------------------------------------------------------------------------

The current `run.ps1` starts three runtime processes: 1. `MediLink.Api`
2. `MediLink.Web` 3. `MediLink.Store.Java`

It does **not** start `MediLink.Auth`, `MediLink.Inventory`, or
`MediLink.Order`.

## 6. Main React application

### Entry point

`src/MediLink.Web/src/main.jsx`

It renders `App.jsx` using React StrictMode and imports Bootstrap and
global CSS.

### React routes

`src/MediLink.Web/src/App.jsx`

Current page routes:

  ----------------------------------------------------------------------------------------
  Route                   Component                                Purpose
  ----------------------- ---------------------------------------- -----------------------
  `/`                     `pages/customer/HomePage.jsx`            Customer medicine
                                                                   browsing/search

  `/cart`                 `pages/customer/CartPage.jsx`            Customer cart

  `/portal`               `pages/admin/PortalPage.jsx`             Portal page

  `/medical-store`        `pages/seller/MedicalStoreCatalog.jsx`   Store catalogue view

  `/admin`                `PortalPage` with `requiredRole="Admin"` Admin portal

  `/admin-login`          `pages/auth/AdminLogin.jsx`              Admin login

  `/login`                `pages/auth/Login.jsx`                   User login

  `/register`             `pages/auth/Register.jsx`                Customer registration

  `/register-store`       `pages/auth/StoreRegister.jsx`           Store-owner
                                                                   registration

  `*`                     redirect to `/`                          Fallback
  ----------------------------------------------------------------------------------------

These are **frontend navigation routes**, not database routes.

### Axios configuration

`src/MediLink.Web/src/services/api.js`

-   Imports Axios.
-   API base URL is `VITE_API_URL` if supplied, otherwise
    `http://localhost:5140/api`.
-   `client()` creates an Axios instance and sends
    `Authorization: Bearer <token>` using
    `localStorage['medilink-token']`.
-   `user()` reads `localStorage['medilink-user']`.

`src/MediLink.Web/src/services/medicalStoreService.js`

-   `getRegisteredStores()` -\> `GET /api/portal/stores`
-   `getStoreInventory(storeId)` -\>
    `GET /api/portal/stores/{storeId}/inventory`

`App.jsx` also uses `client().post('/cart/items', ...)` when a logged-in
user adds a medicine to the cart.

## 7. Main ASP.NET API controllers and endpoints

The primary active API is `src/MediLink.Api`.

### AuthController

File: `src/MediLink.Api/Controllers/AuthController.cs` Base route:
`/api/auth`

  --------------------------------------------------------------------------------------------
  Method            Endpoint                           Auth              Purpose
  ----------------- ---------------------------------- ----------------- ---------------------
  POST              `/api/auth/login`                  Public            Verify email/password
                                                                         and return JWT

  POST              `/api/auth/register/customer`      Public            Create customer
                                                                         user/profile and
                                                                         return JWT

  POST              `/api/auth/register/store-owner`   Public            Create store
                                                                         owner/profile/store
                                                                         and return JWT
  --------------------------------------------------------------------------------------------

Password behavior: - Registration hashes passwords with BCrypt. - Login
verifies the submitted password against `User.PasswordHash` using
BCrypt. - The plaintext password is not stored.

### MedicinesController

File: `src/MediLink.Api/Controllers/MedicinesController.cs` Base route:
`/api/medicines`

  ------------------------------------------------------------------------------------
  Method            Endpoint                      Auth              Purpose
  ----------------- ----------------------------- ----------------- ------------------
  GET               `/api/medicines`              Public            Search/list active
                                                                    medicines with
                                                                    category and
                                                                    pagination

  GET               `/api/medicines/{id}`         Public            Get one active
                                                                    medicine

  GET               `/api/medicines/categories`   Public            List distinct
                                                                    active categories

  POST              `/api/medicines`              Admin, StoreOwner Create medicine

  PUT               `/api/medicines/{id}`         Admin, StoreOwner Update medicine

  DELETE            `/api/medicines/{id}`         Admin, StoreOwner Archive medicine
                                                                    by setting
                                                                    `IsActive=false`
  ------------------------------------------------------------------------------------

The GET list supports `search`, `category`, `page`, and `pageSize` query
parameters. Page size is clamped to 1..50.

### CartController

File: `src/MediLink.Api/Controllers/CartController.cs` Base route:
`/api/cart` Authorization: `Customer`

  Method   Endpoint                 Purpose
  -------- ------------------------ ---------------------------------
  GET      `/api/cart`              Get the current customer's cart
  POST     `/api/cart/items`        Add medicine/quantity to cart
  PUT      `/api/cart/items/{id}`   Change cart item quantity
  DELETE   `/api/cart/items/{id}`   Remove cart item

The controller directly injects `MediLinkDbContext` and uses EF Core
queries.

### OrdersController

File: `src/MediLink.Api/Controllers/OrdersController.cs` Base route:
`/api/orders` Authorization: `Customer`

  ---------------------------------------------------------------------------
  Method                  Endpoint                    Purpose
  ----------------------- --------------------------- -----------------------
  GET                     `/api/orders`               Get current customer's
                                                      orders

  POST                    `/api/orders`               Checkout current cart
                                                      and create order

  PUT                     `/api/orders/{id}/cancel`   Cancel a placed order
                                                      and restore stock
  ---------------------------------------------------------------------------

Checkout behavior: 1. Loads current customer's cart and medicines. 2.
Rejects empty cart. 3. Rejects inactive/out-of-stock items. 4. Creates
`Order` and `OrderItem` records. 5. Decreases `Medicine.StockQuantity`.
6. Removes cart items. 7. Calls `SaveChangesAsync()`.

### DashboardController

File: `src/MediLink.Api/Controllers/DashboardController.cs` Base route:
`/api/dashboard` Authorization required.

  -------------------------------------------------------------------------------------
  Method            Endpoint                    Role              Purpose
  ----------------- --------------------------- ----------------- ---------------------
  GET               `/api/dashboard/customer`   Customer          Customer order
                                                                  count/recent/active
                                                                  orders

  GET               `/api/dashboard/store`      StoreOwner        Active products, low
                                                                  stock, inventory
                                                                  value

  GET               `/api/dashboard/admin`      Admin             Counts for customers,
                                                                  owners, medicines,
                                                                  orders and revenue
  -------------------------------------------------------------------------------------

### PortalController

File: `src/MediLink.Api/Controllers/PortalController.cs` Base route:
`/api/portal`

  ----------------------------------------------------------------------------------------------------------
  Method            Endpoint                                   Role                        Current behavior
  ----------------- ------------------------------------------ --------------------------- -----------------
  GET               `/api/portal/stores`                       Public                      Reads stores from
                                                                                           DB

  GET               `/api/portal/stores/{storeId}/inventory`   Public                      Reads
                                                                                           store-linked
                                                                                           medicines from DB

  GET               `/api/portal/admin/overview`               Admin                       Returns
                                                                                           hard-coded sample
                                                                                           metrics (`1500`,
                                                                                           `45`)

  GET               `/api/portal/store-owner/inventory`        StoreOwner/Admin            Returns
                                                                                           hard-coded sample
                                                                                           strings

  GET               `/api/portal/customer/orders`              Customer/StoreOwner/Admin   Returns
                                                                                           hard-coded sample
                                                                                           strings
  ----------------------------------------------------------------------------------------------------------

Important: the last three Portal endpoints are currently
placeholders/sample responses, not real database-backed implementations.

### Main API endpoint count

The active `MediLink.Api` source contains **24 HTTP endpoints** across
six controllers: - Auth: 3 - Medicines: 6 - Cart: 4 - Orders: 3 -
Dashboard: 3 - Portal: 5

## 8. Database architecture

### Main DbContext

File: `src/MediLink.Infrastructure/Data/MediLinkDbContext.cs`

`MediLinkDbContext` exposes: - `Users` - `CustomerProfiles` -
`StoreOwnerProfiles` - `Stores` - `StoreInventories` - `Medicines` -
`Carts` - `CartItems` - `Orders` - `OrderItems`

Important relationships configured: - User -\> CustomerProfile:
one-to-one, cascade delete - User -\> StoreOwnerProfile: one-to-one,
cascade delete - StoreOwnerProfile -\> Stores: one-to-many - User -\>
Cart: one-to-one, cascade delete - Cart -\> CartItems: one-to-many,
cascade delete - CartItem -\> Medicine: many-to-one, restrict delete -
User -\> Orders: one-to-many, cascade delete - Order -\> OrderItems:
one-to-many, cascade delete - Store -\> StoreInventory: one-to-many-like
relationship via FK - Medicine -\> StoreInventory: FK relationship -
Store + Medicine combination in StoreInventory is unique - User email is
unique - Decimal precision: Medicine.Price, Order.TotalAmount,
OrderItem.UnitPrice = `(10,2)`

### Additional service DbContexts

File: `src/MediLink.Infrastructure/Data/ServiceDbContexts.cs`

There are three additional contexts:

#### AuthDbContext

Contains: - Users - CustomerProfiles - StoreOwnerProfiles - Stores

#### InventoryDbContext

Contains: - Medicines - Stores - StoreInventories

#### OrderDbContext

Contains: - Orders - OrderItems - Carts - CartItems - Medicines -
Stores - StoreInventories

These contexts support the separate `MediLink.Auth`,
`MediLink.Inventory`, and `MediLink.Order` ASP.NET projects.

### Database connection

Main API: `src/MediLink.Api/Program.cs`

It reads `ConnectionStrings:DefaultConnection`, then configures:

``` text
UseMySql(connectionString, new MySqlServerVersion(8.0.36))
```

`src/MediLink.Api/appsettings.json` contains the database
name/server/user and leaves the password/secret to runtime
configuration. The launcher supplies environment variables.

### Migrations

Located at: `src/MediLink.Infrastructure/Migrations/`

Main migration present: `20260728093203_InitialMySql`

The main API runs `Database.MigrateAsync()` in Development startup.

### Seeder

File: `src/MediLink.Infrastructure/Data/DatabaseSeeder.cs`

Seeds a catalogue of example medicines into `InventoryDbContext`. Note
that the current `MediLink.Api/Program.cs` calls
`DatabaseSeeder.SeedAsync(db)` with `MediLinkDbContext`; because
`MediLinkDbContext` and `InventoryDbContext` are distinct types, this
area should be checked carefully if build/runtime errors occur after
future changes. Do not assume seeding is correct solely from
documentation.

## 9. Database table/domain model

Core entities in `src/MediLink.Core/Entities`:

-   `User`: Id, Email, PasswordHash, FirstName, LastName, Role,
    CreatedAt, role-specific navigation properties.
-   `CustomerProfile`: UserId, DeliveryAddress, PhoneNumber.
-   `StoreOwnerProfile`: UserId, BusinessLicenseNumber, Stores.
-   `Store`: Name, Address, StoreOwnerProfileId.
-   `StoreInventory`: StoreId, MedicineId, CreatedAt.
-   `Medicine`: Name, Description, Category, Price, StockQuantity,
    ImageUrl, IsActive, CreatedAt.
-   `Cart`: UserId and `CartItem` list.
-   `CartItem`: CartId, MedicineId, Quantity.
-   `Order`: UserId, DeliveryAddress, TotalAmount, Status, CreatedAt,
    OrderItems.
-   `OrderItem`: OrderId, MedicineId, MedicineName, UnitPrice, Quantity.

User roles in `UserRole.cs`: - Admin - StoreOwner - Customer

Order statuses: - Placed - Confirmed - Shipped - Delivered - Cancelled

## 10. Core project

Location: `src/MediLink.Core`

Purpose: shared, database-independent contracts and models.

Contains: - Entities - DTOs - Interfaces - Enums

Important interfaces: - `IUserRepository` - `IJwtTokenService`

DTOs: - `LoginRequest` - `RegisterCustomerRequest` -
`RegisterStoreOwnerRequest` - `AuthResponse` - `MedicineCreateRequest` -
`CartItemRequest` - `CheckoutRequest`

Core itself has no EF Core or MySQL package reference.

## 11. Infrastructure project

Location: `src/MediLink.Infrastructure`

Purpose: implementations that touch persistence/security infrastructure.

Contains: - `Data/MediLinkDbContext.cs` - `Data/ServiceDbContexts.cs` -
`Data/DatabaseSeeder.cs` - `Repositories/UserRepository.cs` -
`Services/JwtTokenService.cs` - EF Core migrations

`UserRepository` implements `IUserRepository` using `MediLinkDbContext`.

`JwtTokenService` implements `IJwtTokenService` and signs JWTs with
HMAC-SHA256. Claims include: - NameIdentifier = user Id - Email - Role -
FullName

Token expiration is 8 hours.

## 12. Main API dependency flow

The active main API is not currently a pure controller -\> service -\>
repository chain for every feature. It is mixed:

``` text
AuthController
    -> IUserRepository (Core interface)
    -> UserRepository (Infrastructure)
    -> MediLinkDbContext
    -> MySQL

AuthController
    -> IJwtTokenService (Core interface)
    -> JwtTokenService (Infrastructure)
    -> JWT

MedicinesController
CartController
OrdersController
DashboardController
PortalController
    -> directly inject MediLinkDbContext
    -> Entity Framework Core
    -> MySQL
```

This is important for agents: **do not assume every controller has a
service/repository layer.** In the current code, several controllers
query `MediLinkDbContext` directly.

## 13. Authentication/security flow

``` text
Register/Login React page
    |
    | POST /api/auth/...
    v
AuthController
    |
    +--> UserRepository -> DB
    |
    +--> BCrypt password hash/verify
    |
    +--> JwtTokenService
    |
    v
JWT returned to React
    |
    v
localStorage['medilink-token']
    |
    v
Axios client sends Authorization: Bearer <token>
    |
    v
ASP.NET JwtBearer middleware validates token
    |
    v
[Authorize] / [Authorize(Roles=...)] controller actions
```

The main API uses `JwtSettings:Secret`, issuer and audience. The secret
must be at least 32 characters in `Program.cs`.

## 14. Store portal Java application

Location: `src/MediLink.Store.Java`

Main class:
`src/MediLink.Store.Java/src/main/java/com/medilink/store/StorePortalApplication.java`

Important components: - `config/SecurityConfig.java` -
`config/UploadResourceConfig.java` - entities under `entity/` -
repository definitions under `repository/Repositories.java` -
services: - `StoreInventoryService` - `StoreLookupService` -
`StoreRegistrationService` - `StoreUserDetailsService` - web controller:
`web/StoreController.java` - Thymeleaf templates under
`resources/templates/`

The Java application uses the same MySQL `MediLink` database.
`application.yml` explicitly sets `ddl-auto: none`, because the ASP.NET
Core application owns the existing database tables. SQL initialization
is enabled, and `schema.sql` is present, so agents should inspect both
if modifying schema behavior.

Store images are uploaded to `src/MediLink.Store.Java/uploads` and
exposed as `/uploads/...`.

## 15. Store portal workflow

``` text
Store owner browser :8081
    |
    v
Spring Boot StoreController
    |
    +--> Spring Security authentication
    |
    +--> JPA repositories/services
    |
    v
Same MySQL MediLink database
```

Typical responsibilities: - Store owner registration/login - Store
creation/profile - Product/medicine management - Inventory management -
Image uploads - Store order handling

## 16. Customer workflow

### Customer registration/login

``` text
React Register/Login
  -> POST /api/auth/register/customer or /api/auth/login
  -> AuthController
  -> UserRepository
  -> MySQL Users/Profile tables
  -> BCrypt verify/hash
  -> JWT
  -> React localStorage
```

### Browse medicines

``` text
HomePage.jsx
  -> GET /api/medicines?search=...&category=...&page=...
  -> MedicinesController
  -> MediLinkDbContext.Medicines
  -> MySQL
  -> JSON list
  -> React UI
```

### Cart

``` text
React add-to-cart
  -> POST /api/cart/items (if logged in)
  -> CartController
  -> MediLinkDbContext
  -> Cart/CartItem/Medicine tables
```

The frontend also keeps a local cart in `localStorage['medilink-cart']`.
If the user is not logged in, the cart can remain local.

### Checkout/order

``` text
CartPage
  -> POST /api/orders
  -> OrdersController
  -> validate cart/stock
  -> create Order + OrderItems
  -> reduce Medicine.StockQuantity
  -> remove CartItems
  -> SaveChangesAsync
  -> JSON order response
```

### Cancel order

``` text
PUT /api/orders/{id}/cancel
  -> verify order belongs to current customer and status is Placed
  -> restore medicine stock
  -> set status Cancelled
  -> save
```

## 17. Swagger

Main Swagger setup is in: `src/MediLink.Api/Program.cs`

The main API calls: - `AddEndpointsApiExplorer()` - `AddSwaggerGen()` -
`UseSwagger()` in Development - `UseSwaggerUI()` in Development

Swagger also defines a Bearer JWT security scheme so protected endpoints
can be tested with an Authorization header.

Open: `http://localhost:5140/swagger`

## 18. Middleware and health checks

Main API middleware: - `ExceptionHandlingMiddleware` at
`src/MediLink.Api/Middleware/ExceptionHandlingMiddleware.cs` -
`DatabaseHealthCheck` at
`src/MediLink.Api/Middleware/DatabaseHealthCheck.cs`

The API exposes: `GET /health`

CORS allows: - `http://localhost:5173` - `http://localhost:3000`

## 19. Important architectural truth for future agents

The repository currently contains **three architectural levels at
once**:

### A. Active integrated local runtime

``` text
React -> MediLink.Api -> MySQL
Store Java portal -> MySQL
```

This is what `run.ps1`/`run.cmd` starts.

### B. Clean/layered .NET libraries

``` text
MediLink.Api
  -> MediLink.Core
  -> MediLink.Infrastructure
       -> MediLink.Core
```

### C. Service decomposition experiments/alternate services

``` text
MediLink.Auth
MediLink.Inventory
MediLink.Order
```

These are separate ASP.NET Web projects with their own `Program.cs`,
DbContexts, controllers, Swagger and authentication configuration, but
they are **not started by the current launcher** and their controllers
overlap with the main API.

Therefore, do not describe the repository as a fully deployed
microservices architecture. The safest description is:

> **MediLink is a modular/layered full-stack application with a primary
> ASP.NET Core API, shared Core/Infrastructure libraries, a separate
> Spring Boot store portal, and additional standalone ASP.NET service
> projects for auth/inventory/order that represent a service-oriented
> decomposition path.**

It is not currently a strict microservices system because the active
runtime shares one MySQL database and the current launcher uses one main
ASP.NET API for customer operations.

## 20. Current launcher behavior

`run.ps1 start` starts: - ASP.NET `src/MediLink.Api` - React
`src/MediLink.Web` - Spring Boot `src/MediLink.Store.Java`

`run.ps1 stop` stops those three processes.

`run.ps1 status` reports those three processes.

Environment variables used: - `MEDILINK_DB_USERNAME` -
`MEDILINK_DB_PASSWORD` - `MEDILINK_DB_HOST` - `MEDILINK_DB_PORT` -
`MEDILINK_DB_NAME` - `MEDILINK_DB_URL` - `MEDILINK_JWT_SECRET`

The launcher builds the .NET connection string from the DB variables and
passes it as `ConnectionStrings__DefaultConnection`. It passes the JWT
secret as `JwtSettings__Secret`. It passes JDBC settings to the Java
store portal.

## 21. Tests and verification

Test project: `tests/MediLink.Tests`

Recommended commands from the repository guide:

``` bash
dotnet build
dotnet test
cd src/MediLink.Web && npm run lint && npm run build
cd ../MediLink.Store.Java && mvn compile
```

Agents should actually run relevant checks after changes and should not
claim success without verification.

## 22. Files agents should inspect first

### For API/backend work

1.  `src/MediLink.Api/Program.cs`
2.  `src/MediLink.Api/Controllers/*.cs`
3.  `src/MediLink.Api/Middleware/*.cs`
4.  `src/MediLink.Core/Entities/*.cs`
5.  `src/MediLink.Core/DTOs/*.cs`
6.  `src/MediLink.Core/Interfaces/*.cs`
7.  `src/MediLink.Infrastructure/Data/MediLinkDbContext.cs`
8.  `src/MediLink.Infrastructure/Data/ServiceDbContexts.cs`
9.  `src/MediLink.Infrastructure/Repositories/UserRepository.cs`
10. `src/MediLink.Infrastructure/Services/JwtTokenService.cs`
11. `src/MediLink.Infrastructure/Migrations/*`

### For React work

1.  `src/MediLink.Web/src/App.jsx`
2.  `src/MediLink.Web/src/services/api.js`
3.  `src/MediLink.Web/src/services/medicalStoreService.js`
4.  `src/MediLink.Web/src/pages/**`
5.  `src/MediLink.Web/src/components/**`

### For store portal work

1.  `src/MediLink.Store.Java/pom.xml`
2.  `src/MediLink.Store.Java/src/main/resources/application.yml`
3.  `src/MediLink.Store.Java/src/main/java/com/medilink/store/web/StoreController.java`
4.  `src/MediLink.Store.Java/src/main/java/com/medilink/store/service/**`
5.  `src/MediLink.Store.Java/src/main/java/com/medilink/store/repository/Repositories.java`
6.  `src/MediLink.Store.Java/src/main/java/com/medilink/store/entity/**`
7.  `src/MediLink.Store.Java/src/main/resources/schema.sql`

## 23. Agent rules specific to this repository

-   Treat `MediLink.Api` as the primary active customer API unless the
    user explicitly asks to work on one of the standalone service
    projects.
-   Do not silently replace the existing architecture with
    microservices.
-   Before adding a new endpoint, check for overlapping endpoints in
    `MediLink.Api`, `MediLink.Auth`, `MediLink.Inventory`, and
    `MediLink.Order`.
-   Before changing database entities, inspect `MediLinkDbContext`,
    service DbContexts, migrations, and the Java JPA entities because
    both .NET and Java applications use the same MySQL database.
-   Do not modify `bin`, `obj`, `dist`, `target`, `node_modules`,
    generated migrations, or compiled output unless explicitly required.
-   Do not hardcode real passwords, JWT secrets, API keys, or other
    secrets.
-   Preserve existing route names and DTO shapes unless the task
    explicitly requires a breaking change.
-   When debugging a feature, trace the complete path: React
    page/service -\> HTTP endpoint -\> controller -\> DB/service -\>
    response.
-   When changing store-related data, verify both the ASP.NET model and
    the Java JPA model because they share database tables.
-   When the user asks whether the project is microservices, distinguish
    between the active runtime and the additional standalone service
    projects.

## 24. One-page mental model

``` text
                         MEDILINK
                            |
             +--------------+--------------+
             |                             |
         CUSTOMER                      STORE OWNER
             |                             |
             v                             v
      React/Vite :5173             Spring Boot :8081
             |                             |
             | Axios                       | JPA/Hibernate
             v                             v
      ASP.NET Core :5140             Same MySQL DB
             |
       +-----+-----+
       |           |
       v           v
  Core models   Infrastructure
  DTOs          EF Core
  Interfaces    Repositories
  Enums         JWT service
       |           |
       +-----+-----+
             |
             v
        MySQL MediLink

Additional .NET service projects:
  MediLink.Auth
  MediLink.Inventory
  MediLink.Order

They are separate Web projects and architectural decomposition candidates, but are not launched by the current run.ps1 workflow.
```

## 25. Important limitations / observations found during analysis

1.  `MediLink.Api` controllers for cart, orders, medicines, dashboard
    and portal directly use `MediLinkDbContext`; the architecture is
    therefore only partially repository/service layered.
2.  `PortalController` contains three hard-coded/demo endpoints and
    should not be treated as real DB-backed functionality.
3.  The repository contains duplicate endpoint implementations across
    the main API and standalone service projects.
4.  The main API's development seeding call uses
    `DatabaseSeeder.SeedAsync(db)` while the seeder signature shown in
    the repository accepts `InventoryDbContext`; this mismatch should be
    verified before assuming a clean build.
5.  `appsettings.json` includes a username and an empty JWT secret;
    runtime secrets are expected from environment variables/user
    secrets. Agents must not copy real secrets into documentation.
6.  The Java store portal deliberately uses `ddl-auto: none` so
    Hibernate does not own schema changes.

## 26. Short project description for agents

> MediLink is a hyperlocal medicine-ordering marketplace. The active
> customer flow is React/Vite -\> ASP.NET Core Web API -\> Entity
> Framework Core -\> MySQL. `MediLink.Core` contains shared domain
> entities, DTOs, interfaces and enums. `MediLink.Infrastructure`
> contains EF Core DbContexts, migrations, the user repository and JWT
> service. A separate Spring Boot + Thymeleaf store portal runs on port
> 8081 and accesses the same MySQL database through JPA/Hibernate. The
> repository also contains separate ASP.NET Auth, Inventory and Order
> service projects, but the current launcher does not run them. Treat
> `MediLink.Api` as the primary customer backend and inspect the exact
> source before making architectural assumptions.
