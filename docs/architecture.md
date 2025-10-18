# 🧩 SmartShop Architecture

## 1) Component Overview
```mermaid
flowchart LR
    subgraph Client
        A[Client - Postman - Curl]
    end

    subgraph Order_Service[Order API]
        OAPI[ASP.NET Core Controllers]
        OAPP[Application - CQRS and MediatR]
        OINF[Infrastructure - EF Core Refit Messaging]
        ODB[(PostgreSQL)]
        MQPUB[Event Publisher - RabbitMqEventPublisher]
        BGSVC[BackgroundService - OrderEventsAuditConsumer]
    end

    subgraph Customer_Service[Customer API]
        CAPI[ASP.NET Core]
        CDB[(PostgreSQL)]
    end

    subgraph Product_Service[Product API]
        PAPI[ASP.NET Core]
        PDB[(PostgreSQL)]
    end

    subgraph RabbitMQ
        EX[Exchange order.events]
        Q[Queue order.events.audit]
    end

    A -->|HTTP| OAPI
    OAPI -->|MediatR| OAPP
    OAPP -->|EF Core| ODB

    OAPP -->|Refit and Polly| CAPI
    OAPP -->|Refit and Polly| PAPI
    CAPI --> CDB
    PAPI --> PDB

    OAPP -->|IEventPublisher| MQPUB
    MQPUB --> EX
    EX -->|bind order.fulfilled| Q
    BGSVC --> Q
```
> Note: DB schemas — order, customer, product.

## 2) High‑Level Class Diagram
```mermaid
classDiagram
    direction LR

    class OrderApi {
        +GET_orders_fulfilled()
        +PATCH_orders_id_fulfill()
        -OrderDbContext
        -ICustomerClient
        -IProductClient
        -IEventPublisher
    }

    class GetFulfilledOrdersQuery {
        +DateTimeOffset From
        +DateTimeOffset To
        +int Page
        +int PageSize
    }

    class GetFulfilledOrdersHandler {
        +Handle(request, ct) PagedResult(FulfilledOrderDto)
    }

    class MarkFulfilledCommand {
        +Guid OrderId
        +DateTimeOffset FulfilledAtUtc
    }

    class MarkFulfilledCommandHandler {
        +Handle(request, ct) Task
    }

    class OrderDbContext {
        +DbSet(Order)
        +DbSet(OrderLine)
    }

    class Order {
        +Guid OrderId
        +Guid CustomerId
        +DateTime CreatedAt
        +OrderStatus Status
        +DateTime PaidAt
        +DateTime FulfilledAt
        +DateTime CancelledAt
        +ICollection(OrderLine) Lines
        +MarkPaid()
        +MarkFulfilled(utcDate)
        +Cancel()
    }

    class OrderLine {
        +Guid OrderId
        +Guid ProductId
        +int Quantity
        +decimal UnitPrice
    }

    class FulfilledOrderDto {
        +Guid OrderId
        +Guid CustomerId
        +string CustomerName
        +string CustomerEmail
        +DateTime FulfilledAt
        +decimal Total
        +IReadOnlyList(FulfilledOrderLineDto) Lines
    }

    class FulfilledOrderLineDto {
        +Guid ProductId
        +string ProductName
        +string Sku
        +int Quantity
        +decimal UnitPrice
        +decimal LineTotal
    }

    class ICustomerClient {
        +GetCustomer(id, ct) CustomerDto
    }
    class IProductClient {
        +GetProduct(id, ct) ProductDto
    }

    class IEventPublisher {
        +PublishOrderFulfilledAsync(evt, ct) Task
    }

    class RabbitMqEventPublisher {
        -IModel channel
        +PublishOrderFulfilledAsync(evt, ct) Task
    }

    class OrderEventsAuditConsumer {
        +ExecuteAsync(ct) Task
    }

    class OrderFulfilledEvent {
        +Guid OrderId
        +Guid CustomerId
        +DateTimeOffset FulfilledAt
        +decimal Total
        +IReadOnlyList(OrderFulfilledLine) Lines
    }

    class OrderFulfilledLine {
        +Guid ProductId
        +int Quantity
        +decimal UnitPrice
    }

    OrderApi --> GetFulfilledOrdersQuery
    OrderApi --> MarkFulfilledCommand
    GetFulfilledOrdersHandler --> OrderDbContext
    GetFulfilledOrdersHandler --> ICustomerClient
    GetFulfilledOrdersHandler --> IProductClient
    MarkFulfilledCommandHandler --> OrderDbContext
    MarkFulfilledCommandHandler --> IEventPublisher
    IEventPublisher <|.. RabbitMqEventPublisher
    RabbitMqEventPublisher --> OrderFulfilledEvent
    OrderEventsAuditConsumer --> OrderFulfilledEvent
    Order --> OrderLine
    OrderDbContext --> Order
    OrderDbContext --> OrderLine
```
> Endpoints: GET /api/orders/fulfilled , PATCH /api/orders/{id}/fulfill

## 3) Sequence – Get Fulfilled Orders
```mermaid
sequenceDiagram
    autonumber
    participant C as Client
    participant OAPI as Order API Controller
    participant APP as Application GetFulfilledOrdersHandler
    participant ODB as OrderDbContext
    participant CC as CustomerClient Refit
    participant PC as ProductClient Refit

    C->>OAPI: GET /api/orders/fulfilled?from&to&page&pageSize
    OAPI->>APP: MediatR.Send(GetFulfilledOrdersQuery)
    APP->>ODB: Query fulfilled orders (UTC range) + include Lines
    ODB-->>APP: Paged order list

    APP->>CC: GetCustomer for distinct customerIds (Polly)
    APP->>PC: GetProduct for distinct productIds (Polly)
    CC-->>APP: CustomerDto or 404 -> UNKNOWN
    PC-->>APP: ProductDto or 404 -> UNKNOWN

    APP-->>OAPI: PagedResult(FulfilledOrderDto)
    OAPI-->>C: 200 OK JSON
```

## 4) Sequence – Fulfill Order -> Publish Event
```mermaid
sequenceDiagram
    autonumber
    participant C as Client
    participant OAPI as Order API Controller
    participant APP as Application MarkFulfilledCommandHandler
    participant ODB as OrderDbContext
    participant PUB as IEventPublisher
    participant MQ as RabbitMQ order.events
    participant CON as OrderEventsAuditConsumer

    C->>OAPI: PATCH /api/orders/{id}/fulfill?fulfilledAtUtc=...
    OAPI->>APP: MediatR.Send(MarkFulfilledCommand)
    APP->>ODB: Load Order + Lines
    ODB-->>APP: Order aggregate
    APP->>APP: order.MarkPaid()
    APP->>APP: order.MarkFulfilled(utc)
    APP->>ODB: SaveChanges()
    APP->>PUB: PublishOrderFulfilledAsync(evt)
    PUB->>MQ: Publish event routingKey=order.fulfilled
    MQ-->>CON: Deliver message to queue order.events.audit
    OAPI-->>C: 204 No Content
```
