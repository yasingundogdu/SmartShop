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
        OUTBOX[(order.outbox_message)]
    end

    subgraph Order_Publisher[Order.Publisher]
        BGSVC[BackgroundService - OutboxPublisherWorker]
        MQPUB[Event Publisher - RabbitMqEventPublisher]
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

    %% Flows
    A -->|HTTP| OAPI
    OAPI -->|MediatR| OAPP
    OAPP -->|EF Core| ODB

    OAPP -->|Refit and Polly| CAPI
    OAPP -->|Refit and Polly| PAPI
    CAPI --> CDB
    PAPI --> PDB

    %% Outbox: App is writing, Publisher is reading
    OAPP -->|Append OutboxMessage| OUTBOX
    BGSVC -->|Read Pending| OUTBOX
    BGSVC --> MQPUB
    MQPUB --> EX
    EX -->|bind order.fulfilled| Q
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
    }

    class GetFulfilledOrdersQuery {
        +DateTimeOffset From
        +DateTimeOffset To
        +int Page
        +int PageSize
    }

    class GetFulfilledOrdersHandler {
        +Handle(request, ct) PagedResult(FulfilledOrderDto)
        -OrderDbContext
        -ICustomerClient
        -IProductClient
    }

    class MarkFulfilledCommand {
        +Guid OrderId
        +DateTimeOffset FulfilledAtUtc
    }

    class MarkFulfilledCommandHandler {
        +Handle(request, ct) Task
        -OrderDbContext
        -IOutboxWriter
    }

    class OrderDbContext {
        +DbSet(Order)
        +DbSet(OrderLine)
        +DbSet(OutboxMessage)
    }

    class IOutboxWriter {
        +Append(type:string, payload:string) Task
    }

    class OutboxMessage {
        +Guid Id
        +string Type
        +string Payload
        +string Status
        +DateTime OccurredAtUtc
        +DateTime? ProcessedAtUtc
        +int RetryCount
        +string? Error
    }

    class Order {
        +Guid OrderId
        +Guid CustomerId
        +DateTime CreatedAt
        +OrderStatus Status
        +DateTime? PaidAt
        +DateTime? FulfilledAt
        +DateTime? CancelledAt
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
        +DateTime? FulfilledAt
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

    %% Publisher Side
    class OutboxPublisherWorker {
        +ExecuteAsync(ct) Task
        -IOrderDbContext
        -IEventPublisher
    }

    class IEventPublisher {
        +PublishRawAsync(exchange, routingKey, bodyUtf8, ct) Task
    }

    class RabbitMqEventPublisher {
        -IModel channel
        +PublishRawAsync(exchange, routingKey, bodyUtf8, ct) Task
    }

    %% Relations
    OrderApi --> GetFulfilledOrdersQuery
    OrderApi --> MarkFulfilledCommand
    GetFulfilledOrdersHandler --> OrderDbContext
    GetFulfilledOrdersHandler --> ICustomerClient
    GetFulfilledOrdersHandler --> IProductClient

    MarkFulfilledCommandHandler --> OrderDbContext
    MarkFulfilledCommandHandler --> IOutboxWriter
    OrderDbContext --> OutboxMessage

    OutboxPublisherWorker --> IEventPublisher
    OutboxPublisherWorker --> OrderDbContext
    IEventPublisher <|.. RabbitMqEventPublisher

    Order --> OrderLine
    OrderDbContext --> Order
    OrderDbContext --> OrderLine

    note for OutboxMessage "Status values: Pending | Processed | Failed"
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
    participant APP as MarkFulfilledCommandHandler
    participant ODB as OrderDbContext
    participant OBX as Outbox (order.outbox_message)
    participant WRK as OutboxPublisherWorker
    participant PUB as IEventPublisher
    participant MQ as RabbitMQ order.events
    participant CON as OrderEventsAuditConsumer

    C->>OAPI: PATCH /api/orders/{id}/fulfill?fulfilledAtUtc=...
    OAPI->>APP: MediatR.Send(MarkFulfilledCommand)
    APP->>ODB: Load Order + Lines
    ODB-->>APP: Order aggregate
    APP->>APP: order.MarkPaid
    APP->>APP: order.MarkFulfilled utc
    APP->>ODB: SaveChanges
    APP->>OBX: Append OutboxMessage type OrderFulfilledEvent
    OAPI-->>C: 204 No Content

    loop Background loop
        WRK->>OBX: Read Pending batch
        OBX-->>WRK: Messages
        WRK->>PUB: PublishRaw exchange order.events rk order.fulfilled
        PUB->>MQ: Publish
        MQ-->>CON: Deliver to queue order.events.audit
        WRK->>OBX: Mark Processed or Failed then retry
    end
```
