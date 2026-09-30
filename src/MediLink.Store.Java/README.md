# MediLink Store Portal

Spring Boot 3.5 / Java 25 presentation service for medical-store owners.

The portal does **not** connect directly to MySQL. It communicates with the Auth, Inventory and Order microservices over HTTP.

Default services:

- Auth: `http://localhost:5101`
- Inventory: `http://localhost:5201`
- Order: `http://localhost:5301`
- Store Portal: `http://localhost:8081`

Configure with:

- `MEDILINK_AUTH_URL`
- `MEDILINK_INVENTORY_URL`
- `MEDILINK_ORDER_URL`
- `MEDILINK_INTERNAL_KEY`
- `MEDILINK_UPLOADS_DIR`
- `MEDILINK_PUBLIC_UPLOAD_BASE_URL`

Java 25 is required.
