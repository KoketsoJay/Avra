# AVRA Backend Microservices

A comprehensive C# microservices architecture for the AVRA wellness lifestyle platform.

## Architecture

### Services

1. **Product Service** (`ProductService/`)
   - Manages product catalog, categories, and inventory
   - Handles product search and filtering
   
2. **Cart Service** (`CartService/`)
   - Manages shopping cart operations
   - Handles orders and checkout
   
3. **User Service** (`UserService/`)
   - User authentication and authorization
   - Account management and profiles
   
4. **Events Service** (`EventsService/`)
   - Event management and scheduling
   - Event registrations and bookings
   
5. **Community Service** (`CommunityService/`)
   - Community circles and groups management
   - Member management and interactions
   
6. **Wellness Service** (`WellnessService/`)
   - Wellness plans and programs
   - Health tracking and recommendations

7. **Ordering Service** (`OrderingService/`)
   - The order lifecycle, modelled with DDD and CQRS after [dotnet/eShop](https://github.com/dotnet/eShop)
   - Order and Buyer aggregates, MediatR commands, Dapper read model, idempotent commands

8. **Payment Service** (`PaymentService/`)
   - Worker that charges confirmed orders and reports the outcome back to Ordering

### Shared building blocks

- **`Avra.ServiceDefaults/`** — logging, security, resilience and API conventions.
- **`Avra.EventBus/`** — integration event contracts, an Azure SQL backed broker, and the
  transactional outbox (`IntegrationEventLog`) each producing service embeds in its own database.

## E-commerce architecture (eShop)

The checkout path is an asynchronous saga coordinated entirely by integration events. No service
calls another synchronously to complete an order, so a slow or unavailable participant delays the
order rather than failing the shopper's request.

```text
Basket  --UserCheckoutAccepted-->  Ordering        (order created, status Submitted)
Ordering --OrderStarted-->         Basket          (basket discarded)
GracePeriodManager --GracePeriodConfirmed--> Ordering  (status AwaitingValidation)
Ordering --OrderStatusChangedToAwaitingValidation--> Catalog  (stock checked)
Catalog --OrderStockConfirmed / OrderStockRejected--> Ordering (status StockConfirmed / Cancelled)
Ordering --OrderStatusChangedToStockConfirmed--> Payment
Payment --OrderPaymentSucceeded / OrderPaymentFailed--> Ordering (status Paid / Cancelled)
Ordering --OrderStatusChangedToPaid--> Catalog       (stock removed permanently)
Catalog --ProductPriceChanged--> Basket             (open baskets re-priced)
```

Key patterns carried over from eShop:

| Pattern | Where |
| --- | --- |
| Aggregates, value objects, domain events | `OrderingService/Domain` |
| CQRS — MediatR commands, Dapper queries | `OrderingService/Application` |
| Pipeline behaviors (logging, validation, transaction) | `OrderingService/Application/Behaviors` |
| Idempotent commands via `x-requestid` | `OrderingService/Infrastructure/Idempotency` |
| Transactional outbox | `Avra.EventBus/Outbox` |
| Integration event bus | `Avra.EventBus/Sql` |
| Grace period before stock validation | `OrderingService/BackgroundTasks` |

### Event bus on Azure SQL

eShop uses PostgreSQL and RabbitMQ; this implementation uses **Azure SQL** for both the service
databases and the broker. `EventBusMessages` holds published events and `EventBusDeliveries`
records one row per message and subscriber. The unique index on that pair is what makes delivery
exactly-once per service even when several instances poll concurrently.

Configure it per service under `EventBus`:

```json
"EventBus": {
  "ConnectionString": "Server=tcp:<server>.database.windows.net,1433;Database=AvraEventBus;Authentication=Active Directory Default;Encrypt=true;",
  "PollingInterval": "00:00:02",
  "BatchSize": 50,
  "MaxDeliveryAttempts": 5,
  "Retention": "7.00:00:00"
}
```

## Tech Stack

- **Framework**: .NET 8
- **Database**: Azure SQL / SQL Server
- **Messaging**: Integration events over an Azure SQL broker, with a transactional outbox
- **API**: RESTful APIs with async/await
- **Authentication**: JWT Bearer tokens
- **Logging**: Serilog
- **DI Container**: Microsoft.Extensions.DependencyInjection
- **Ordering internals**: MediatR, FluentValidation, Dapper, EF Core

## Getting Started

1. **Prerequisites**
   - .NET 8 SDK or later
   - SQL Server (LocalDB or full instance)
   - Visual Studio 2022 or VS Code

2. **Setup**
   ```bash
   # Navigate to backend directory
   cd backend
   
   # Restore dependencies for all services
   dotnet restore
   
   # Build all services
   dotnet build
   ```

3. **Database**
   ```bash
   # Run migrations for each service
   dotnet ef database update --project ProductService
   dotnet ef database update --project CartService
   dotnet ef database update --project UserService
   # ... etc
   ```

4. **Run Services**
   Each service can be run individually:
   ```bash
   dotnet run --project ProductService
   dotnet run --project CartService
   ```

## API Base URLs

- **Product Service**: http://localhost:5001
- **Cart Service**: http://localhost:5002
- **User Service**: http://localhost:5003
- **Events Service**: http://localhost:5004
- **Community Service**: http://localhost:5005
- **Wellness Service**: http://localhost:5006
- **Ordering Service**: http://localhost:5007
- **Payment Service**: worker, no HTTP endpoint

## Checkout endpoints

| Method | Endpoint | Service | Notes |
| --- | --- | --- | --- |
| `POST` | `/api/basket/checkout` | Cart | Requires `x-requestid`; returns 202 and publishes `UserCheckoutAcceptedIntegrationEvent` |
| `GET` | `/api/orders` | Ordering | The signed-in buyer's orders |
| `GET` | `/api/orders/{orderId}` | Ordering | Single order with lines |
| `PUT` | `/api/orders/cancel` | Ordering | Requires `x-requestid` |
| `PUT` | `/api/orders/ship` | Ordering | Administrators only, requires `x-requestid` |
| `GET` | `/api/orders/cardtypes` | Ordering | Accepted card networks |

## API Documentation

Each service includes Swagger/OpenAPI documentation available at:
- `http://localhost:5001/swagger` (Product Service)
- `http://localhost:5002/swagger` (Cart Service)
- etc.

## Authentication

Services use JWT Bearer tokens. Include in request headers:
```
Authorization: Bearer <your-jwt-token>
```

## Contributing

1. Follow C# naming conventions (PascalCase for classes, camelCase for properties)
2. Use async/await for all I/O operations
3. Include XML documentation for public methods
4. Write unit tests for business logic
