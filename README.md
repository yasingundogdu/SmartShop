# 🛒 SmartShop Microservices

SmartShop is a simplified online marketplace demo built with a **microservices architecture**.  
It exposes an **Order API** that returns all fulfilled orders within a given date range, enriched with customer and product details from their own services.

---

## 🧩 Architecture Overview

- **Customer API** → manages customer info
- **Product API** → manages product data
- **Order API** → central service that aggregates orders, customers, and products
- **RabbitMQ** → event-driven communication (OrderFulfilledEvent)
- **Outbox Pattern** → ensures reliable event publishing via `Order.Publisher` service
- **PostgreSQL** → persistence for each bounded context
- **Polly + Refit** → resilience and HTTP client abstraction
- **CQRS + MediatR** → clean separation of commands and queries

### 🖇️ High-Level Diagram
See [docs/architecture.md](./docs/architecture.md) for UML and component structure.

---

## ⚙️ Tech Stack

| Technology |
|-------------|
| .NET 8 |
| ASP.NET Core Web API |
| PostgreSQL (via EF Core) |
| RabbitMQ |
| Refit + Polly |
| Microservices, CQRS |
| Docker Compose |
| xUnit, Moq, Coverlet, ReportGenerator |

---

## 🚀 How to Run

### 1️⃣ Prerequisites
- Docker & Docker Compose installed
- .NET SDK 8.0+ (optional, if running locally)

### 2️⃣ Run with Docker
```
docker compose up -d --build
```
### 3️⃣ Check running containers
```
docker compose ps
```
| Service      | Port  | Description      |
| ------------ | ----- | ---------------- |
| customer-api | 5101  | Customer service |
| product-api  | 5102  | Product service  |
| order-api    | 5103  | Order service    |
| postgres     | 5432  | PostgreSQL       |
| adminer      | 8081  | DB admin tool    |
| rabbitmq     | 15672 | RabbitMQ UI      |

## 🧱 Database Migrations & Seed

Each microservice automatically applies EF Core **migrations** and runs an **idempotent seed** on startup.

- **Program.cs (each API)**:
    - `db.Database.MigrateAsync()` creates tables if they don’t exist.
    - `*Seed.EnsureAsync(...)` inserts deterministic initial data safely (idempotent).

- **Deterministic IDs** ensure cross-service consistency:
    - Customers → `aaaaaaaa-...` (Acme Demo), `bbbbbbbb-...` (Example Ltd)
    - Products  → `1111-...` (SKU-1001), `2222-...` (SKU-1002)
    - Order     → `9999-...` (demo order)

> Seeds are idempotent — running them multiple times will not duplicate data.
> Uniqueness is guaranteed via natural keys (`Email` and `Sku`) and fixed GUIDs.

### ✅ Verifying the seed
Check container logs:
```bash

docker compose logs -f customer-api
docker compose logs -f product-api
docker compose logs -f order-api
```

🧾 Order Service

Get Fulfilled Orders
```bash
 
curl "http://localhost:5103/api/orders/fulfilled?from=2025-10-01T00:00:00Z&to=2025-10-31T23:59:59Z"
```
Fulfill Order
```bash

curl -X PATCH "http://localhost:5103/api/orders/{orderId}/fulfill?fulfilledAtUtc=2025-10-15T19:00:00Z"
```


👤 Customer Service
```bash

curl http://localhost:5101/api/customers/{customerId}
```
📦 Product Service
```bash

curl http://localhost:5102/api/products/{productId}

```

### 🐇 RabbitMQ Events

When an order is fulfilled, an OrderFulfilledEvent is published to RabbitMQ.
A background consumer (OrderEventsAuditConsumer) listens and logs the event payload.

RabbitMQ UI:
👉 http://localhost:15672
```
Username: guest
Password: guest
```

### 🧱 CQRS Overview

Command: MarkFulfilledCommand

→ Marks order as fulfilled and publishes OrderFulfilledEvent.


Query: GetFulfilledOrdersQuery

→ Returns paged fulfilled orders between given date range.

### 📊 UML Overview

See docs/architecture.md

## 🧪 Testing

Run unit tests:

```bash

dotnet test tests/SmartShop.Domain.Tests/SmartShop.Domain.Tests.csproj
```
Generate coverage (Cobertura):
```bash

dotnet test tests/SmartShop.Domain.Tests/SmartShop.Domain.Tests.csproj \
  /p:CollectCoverage=true \
  /p:CoverletOutput=./TestResults/coverage.xml \
  /p:CoverletOutputFormat=cobertura

```


## 🏁 Summary

✅ Three microservices (Order, Customer, Product)

✅ REST API for fulfilled orders with aggregation

✅ CQRS + MediatR + EF Core

✅ RabbitMQ event flow

✅ Resilience with Polly

✅ Dockerized deployment

✅ Ready for test coverage and CI/CD integration

✅ Unit Test



---


## 🔭 Future Improvements

- **Integration Testing**
  - Implement end-to-end and contract tests across microservices to validate API communication, database operations, and message queues.
  - Automate test execution in CI/CD pipelines for consistent regression coverage.

- **Error Handling**
  - Introduce a unified global exception-handling layer that returns standardized **RFC 7807 ProblemDetails** responses.
  - Provide consistent status codes and trace identifiers across all services.

- **Redis Caching**
  - Add Redis for distributed caching to improve read performance and reduce redundant external or database calls.
  - Use caching for frequently accessed lookups and aggregated queries.

- **SonarQube Integration**
  - Integrate SonarQube to monitor code quality, maintainability, and test coverage.
  - Enforce quality gates and automate static analysis checks as part of the CI process.
