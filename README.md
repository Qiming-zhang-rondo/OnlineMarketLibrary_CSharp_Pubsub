# Online Marketplace Pub/Sub Core Library (C#)
===========================================

This repository contains a modular C# library of the core logic and a dapr implementation for the [Online Marketplace Benchmark](https://github.com/diku-dk/EventBenchmark), modeled as an event-driven microservice system. It supports asynchronous inter-service communication through a publish/subscribe architecture and is designed to enable scalable, platform-independent experimentation.

The project is structured into:
- A **platform-agnostic core layer**, `OnlineMarket.Core` contains service-specific modules that encapsulate all business logic, state definitions, and event handling. It has **no dependency on any specific messaging runtime**.
- A **Dapr-based sidecar**  provides a reference integration using [Dapr](https://dapr.io/), including controller bindings and message routing.


## Table of Contents

- [Core Library Design](#core)
  * [Service Responsibilities](#services)
  * [Interface Definitions](#interfaces)
- [Dapr Integration Example](#dapr)
- [Supported Platforms](#platforms)
- [Event Flow Example](#eventflow)
- [Getting Started](#start)
- [Future Work](#future)

---


## <a name="core"></a>Core Library Design

### <a name="services"></a>Service Responsibilities

Each microservice manages its own local state and reacts to relevant domain events. Examples:

| **Event Name**                | **Producer Service** | **Consumer Services**         | **Description**                                   |
|------------------------------|----------------------|-------------------------------|---------------------------------------------------|
| E1: PriceUpdate              | Product              | Cart, Stock                   | Product price update                              |
| E2: ReserveInventory         | Cart                 | Stock                         | Request to reserve inventory                      |
| E3: StockConfirmed           | Stock                | Order                         | Stock successfully reserved                       |
| E4: StockReservationFailed   | Stock                | Order                         | Failed to reserve stock                           |
| E5: InvoiceIssued            | Order                | Payment, Seller               | Invoice issued for order                          |
| E6: PaymentConfirmed         | Payment              | Stock, Order, Customer        | Payment completed                                 |
| E7: PaymentFailed            | Payment              | Stock, Order, Customer        | Payment failed                                    |
| E8: ShipmentNotification     | Shipment             | Seller, Customer              | Shipment dispatched                               |
| E9: DeliveryNotification     | Shipment             | Customer                      | Delivery completed                                |

### <a name="interfaces"></a>Interface Definitions

Each service depends on the following abstract interfaces:

| Interface | Responsibility |
|-----------|----------------|
| `IRepository` | Read/write access to local persistent state |
| `IEventPublisher` | Publish outbound domain events to pub/sub system |
| `IConfig` | Runtime configuration (e.g., enable publish, use in-memory state) |

All interfaces are fully platform-independent and injected by the platform integration layer.

---

## <a name="dapr"></a>Dapr Integration Example

`OnlineMarket.DaprSidecar` provides a complete integration example demonstrating how to connect the core library to Dapr's pub/sub messaging system. The integration layer is composed of three main components(using cartMS as an example):

| Component              | Description                                                                                 |
|------------------------|---------------------------------------------------------------------------------------------|
| `EventController.cs`   | Receives subscribed events from Dapr via `[Topic]` annotation.                              |
| `CartController.cs`    | Exposes RESTful endpoints for synchronous API requests (e.g., adding items, checkout).      |
| `EventGateways/`       | Implements core messaging interfaces (e.g., `IEventPublisher`) using injected `DaprClient`.   |


## Running the Dapr Integration Example

To run each microservice using Dapr sidecar integration, execute the following commands in the project's root directory:

### Cart Service

```bash
dapr run --app-port 5001 --app-id cart --app-protocol http --dapr-http-port 3501 -- dotnet run --urls \"http://*:5001\" --project CartMS/CartMS.csproj
```

### Customer Service

```bash
dapr run --app-port 5007 --app-id customer --app-protocol http --dapr-http-port 3507 -- dotnet run --urls \"http://*:5007\" --project CustomerMS/CustomerMS.csproj
```

### Order Service

```bash
dapr run --app-port 5002 --app-id order --app-protocol http --dapr-http-port 3502 -- dotnet run --urls \"http://*:5002\" --project OrderMS/OrderMS.csproj
```

### Payment Service

```bash
dapr run --app-port 5004 --app-id payment --app-protocol http --dapr-http-port 3504 -- dotnet run --urls \"http://*:5004\" --project PaymentMS/PaymentMS.csproj
```

### Product Service

```bash
dapr run --app-port 5008 --app-id product --app-protocol http --dapr-http-port 3508 -- dotnet run --urls \"http://*:5008\" --project ProductMS/ProductMS.csproj
```

### Seller Service

```bash
dapr run --app-port 5006 --app-id seller --app-protocol http --dapr-http-port 3506 -- dotnet run --urls \"http://*:5006\" --project SellerMS/SellerMS.csproj
```

### Shipment Service

```bash
dapr run --app-port 5005 --app-id shipment --app-protocol http --dapr-http-port 3505 -- dotnet run --urls \"http://*:5005\" --project ShipmentMS/ShipmentMS.csproj
```

### Stock Service

```bash
dapr run --app-port 5003 --app-id stock --app-protocol http --dapr-http-port 3503 -- dotnet run --urls \"http://*:5003\" --project StockMS/StockMS.csproj
```



