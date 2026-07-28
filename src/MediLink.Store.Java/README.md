# MediLink Store Portal

The Java/Spring Boot portal is intentionally separate from the React customer experience. It reads the same MySQL `MediLink` database and has its own medical-store owner registration and login pages.

## What it includes

- Dedicated medical-store owner registration and BCrypt login against the shared `Users` table. Customer and admin accounts are refused.
- A dashboard for a registered store.
- Product catalogue management with validation, stock levels, image uploads, and owner-scoped inventory.
- Order claiming, delivery address/customer details, and fulfilment status updates.
- Spring Data JPA/Hibernate mappings to the existing .NET tables. Hibernate is configured read/write only and never alters those shared tables; `schema.sql` safely creates the two portal-owned support tables, `StoreInventories` and `StoreOrderAssignments`, when needed.

## Run it

Use the same MySQL server as the ASP.NET Core API. No password is stored in this repository.

```powershell
$env:MEDILINK_DB_URL = 'jdbc:mysql://localhost:3306/MediLink?useSSL=false&serverTimezone=UTC'
$env:MEDILINK_DB_USERNAME = 'root'
$env:MEDILINK_DB_PASSWORD = 'root'
cd src/MediLink.Store.Java
mvn spring-boot:run
```


Open `http://localhost:8081/login`, select **Create medical-store account**, and then sign in with the account you created. Customer and admin accounts are rejected by the Spring Security login.

Product images are saved to `src/MediLink.Store.Java/uploads` while running locally and are served at `/uploads/...`. For deployment, point that directory at persistent storage.
