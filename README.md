# Online Marketplace Pub/Sub Core Library (C#)
===========================================

This repository contains a modular C# library of the core logic and a dapr implementation for the [Online Marketplace Benchmark](https://github.com/diku-dk/EventBenchmark), modeled as an event-driven microservice system. It supports asynchronous inter-service communication through a publish/subscribe architecture and is designed to enable scalable, platform-independent experimentation.

The project is structured into:
- A **platform-agnostic core layer**, containing reusable service logic;
- A **Dapr-based sidecar** implementation for integration and demonstration purposes.

---

## Table of Contents

- [Overview](#overview)
- [Repository Structure](#structure)
- [Core Library Design](#core)
  * [Service Responsibilities](#services)
  * [Interface Definitions](#interfaces)
- [Dapr Integration Example](#dapr)
- [Supported Platforms](#platforms)
- [Event Flow Example](#eventflow)
- [Getting Started](#start)
- [Future Work](#future)

---

## <a name="overview"></a>Overview

Online Marketplace simulates a typical e-commerce backend where multiple microservices (e.g., Cart, Order, Payment, Shipment) interact via asynchronous domain events.

This repository implements the business logic for each service in a pub/sub model, fully decoupled from infrastructure concerns. It is designed in compliance with the event semantics and consistency models defined in the [EventBenchmark](https://github.com/diku-dk/EventBenchmark).

---

## <a name="structure"></a>Repository Structure
OnlineMarket-PubSub-CSharp/
├── OnlineMarket.Core/             # Platform-independent core logic
│   ├── Cart/
│   ├── Order/
│   ├── Payment/
│   ├── Stock/
│   ├── Shipment/
│   └── Common/
│       ├── Events/
│       ├── Interfaces/
│       └── Utils/
│
├── OnlineMarket.DaprSidecar/     # Dapr integration example (sidecar)
│   ├── Controllers/
│   ├── Adapters/
│   ├── Program.cs
│   └── components/                # Dapr pubsub configuration
│
└── README.md

- `OnlineMarket.Core` contains service-specific modules that encapsulate all business logic, state definitions, and event handling. It has **no dependency on any specific messaging runtime**.
- `OnlineMarket.DaprSidecar` provides a reference integration using [Dapr](https://dapr.io/), including controller bindings and message routing.

---

## <a name="core"></a>Core Library Design

### <a name="services"></a>Service Responsibilities

Each microservice manages its own local state and reacts to relevant domain events. Examples:

| Service  | Consumed Events                          | Emitted Events                |
|----------|-------------------------------------------|-------------------------------|
| Cart     | `PriceUpdated`, `CustomerCheckout`        | `TransactionMark`, `ABORT`    |
| Order    | `Checkout`, `PaymentConfirmed`, `Delivered` | Status updates                |
| Payment  | `InvoiceIssued`                           | `PaymentConfirmed`, `Failed`  |
| Shipment | `PaymentConfirmed`                        | `DeliveryNotification`        |

### <a name="interfaces"></a>Interface Definitions

Each service depends on the following abstract interfaces:

| Interface | Responsibility |
|-----------|----------------|
| `IRepository` | Read/write access to local persistent state |
| `IEventPublisher` | Publish outbound domain events to pub/sub system |
| `ICoreService` | Encapsulates all core logic for event handling |
| `IConfig` | Runtime configuration (e.g., enable publish, use in-memory state) |

All interfaces are fully platform-independent and injected by the platform integration layer.

---

## <a name="dapr"></a>Dapr Integration Example

`OnlineMarket.DaprSidecar` contains a complete integration example using Dapr:

| Component | Description |
|-----------|-------------|
| `EventController.cs` | Receives events via Dapr topic subscription |
| `CartController.cs`  | Exposes external REST endpoints |
| `Adapters/` | Implements repository and publisher interfaces |
| `components/pubsub.yaml` | Dapr pub/sub component configuration |

Example Dapr startup command:

```bash
dapr run --app-id cart --app-port 5001 -- dotnet run
