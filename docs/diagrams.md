# Diagrams

All diagrams are [Mermaid](https://mermaid.js.org), so GitHub renders them and they stay
editable as text. VS Code shows them with the *Markdown Preview Mermaid Support* extension.

1. [System architecture (target)](#1-system-architecture-target)
2. [Clean Architecture layers](#2-clean-architecture-layers)
3. [Entity-relationship diagram](#3-entity-relationship-diagram)
4. [Order status](#order-status)
5. [Payment status](#payment-status)
6. [Checkout and payment sequence](#6-checkout-and-payment-sequence)
7. [Pluggable payment gateways (Strategy pattern)](#7-pluggable-payment-gateways-strategy-pattern)
8. [Events and messaging](#8-events-and-messaging)
9. [Deployment](#9-deployment)

---

## 1. System architecture (target)

What the finished system looks like. Grey items are not built yet.

```mermaid
flowchart LR
    subgraph Users
        C["Customer<br/>browser"]
        A["Admin<br/>browser"]
    end

    subgraph Frontend["React app (Vite)"]
        Shop["Storefront pages"]
        Admin["/admin dashboard"]
    end

    subgraph Backend["Audiophile API (ASP.NET Core 10)"]
        API["REST controllers<br/>+ JWT auth"]
        Worker["Expiry worker"]
        Consumer["Message consumers"]
    end

    PG[("PostgreSQL")]
    Redis[("Redis<br/>cache")]
    MQ[["RabbitMQ"]]
    FW["Fawaterak"]
    Mail["Email (SMTP)"]

    C --> Shop --> API
    A --> Admin --> API
    API --> PG
    API --> Redis
    API -->|publish events| MQ --> Consumer --> Mail
    API -->|create invoice| FW
    FW -->|webhook| API
    Worker --> PG
    Worker --> FW

    classDef todo fill:#eee,stroke:#999,color:#666
    class Admin,Redis,MQ,Consumer,Mail todo
```

## 2. Clean Architecture layers

```mermaid
flowchart TB
    API["<b>API</b><br/>Controllers · CurrentUser · rate limits · ExceptionHandler · Program.cs (DI)"]
    Infra["<b>Infrastructure</b><br/>AppDbContext + migrations (PostgreSQL) · Identity · JwtTokenService · FawaterakGateway · (Redis, RabbitMQ)"]
    App["<b>Application</b><br/>AuthService · ProductService · AddressService · CartService · OrderService · PaymentService · PaymentResultApplier<br/>IAppDbContext · ICurrentUser · ITokenService · DTOs"]
    Dom["<b>Domain</b><br/>Product · Cart · Order · Payment · User · Address<br/>state machines · Guard"]

    API --> App
    API -. "registers only" .-> Infra
    Infra --> App
    App --> Dom
```

Arrows mean "depends on". Nothing points outward from `Domain`.

## 3. Entity-relationship diagram

Only the important columns are shown. `User` also has every ASP.NET Identity column
(password hash, lockout, …), and roles live in Identity's `AspNetRoles` / `AspNetUserRoles` tables.

```mermaid
erDiagram
    USER ||--|| CART : "has one"
    USER ||--o{ ADDRESS : "saves"
    USER ||--o{ ORDER : "places"
    USER ||--o{ REFRESH_TOKEN : "holds"
    CART ||--o{ CART_ITEM : "contains"
    PRODUCT ||--o{ CART_ITEM : "is in"
    ORDER ||--|{ ORDER_ITEM : "contains"
    PRODUCT ||--o{ ORDER_ITEM : "was bought as"
    ORDER ||--o| PAYMENT : "is paid by"
    PAYMENT ||--o{ PAYMENT_EVENT : "is reported by"

    USER {
        guid Id PK
        string Name
        string Email UK
        datetime CreatedAt
    }
    PRODUCT {
        guid Id PK
        string Name
        string Description
        decimal Price
        int Stock
        string Category "Headphones | Speakers | Earphones"
        bool IsActive
        string ImageUrl
    }
    CART {
        guid Id PK
        guid UserId FK, UK
        datetime CreatedAt
    }
    CART_ITEM {
        guid Id PK
        guid CartId FK
        guid ProductId FK
        int Quantity "1..100"
    }
    ADDRESS {
        guid Id PK
        guid UserId FK
        string FullAddressName
        string Street
        string City
        string Country
        string Phone
        bool IsActive "soft delete"
    }
    ORDER {
        guid Id PK
        guid UserId FK
        string Status
        string PaymentMethod
        string Shipping_snapshot "name, phone, street, city..."
        decimal SubTotal
        decimal ShippingFee
        decimal TotalAmount
        string IdempotencyKey "UK with UserId"
        datetime CreatedAt
    }
    ORDER_ITEM {
        guid Id PK
        guid OrderId FK
        guid ProductId FK
        int Quantity
        decimal UnitPrice "snapshot"
        string ProductName "snapshot"
    }
    PAYMENT {
        guid Id PK
        guid OrderId FK, UK
        string Status
        decimal Amount
        string Provider "fawaterak, null for COD"
        string ProviderOrderId
        string ProviderTransactionId
        string CheckoutUrl
    }
    PAYMENT_EVENT {
        guid Id PK
        guid PaymentId FK "nullable"
        string Provider "UK with ExternalId"
        string ExternalId
        string EventType
        string RawPayload
        datetime ReceivedAt
    }
    REFRESH_TOKEN {
        guid Id PK
        guid UserId FK
        string Token UK
        datetime ExpiresAt
        datetime RevokedAt
    }
```

**How to read it:** `||--o{` means "one to zero-or-many", `||--||` means "exactly one to exactly one",
and `||--o|` means "one to zero-or-one".

**Things worth explaining in an interview:**
- **Unique `(CartId, ProductId)`** → a product appears once per cart; the database enforces it, not only the code.
- **Unique `(UserId, IdempotencyKey)`** → a retried checkout can never create two orders.
- **Unique `(Provider, ExternalId)`** on `PAYMENT_EVENT` → a webhook delivered twice is applied once.
- **Snapshots on `ORDER` / `ORDER_ITEM`** → order history doesn't change when prices or addresses do.
- **`ORDER_ITEM → PRODUCT` is `Restrict`** → you can't delete a product that has been sold; deactivate it instead.

## Order status

```mermaid
stateDiagram-v2
    [*] --> Pending : customer places order
    Pending --> Confirmed : payment succeeded / COD accepted
    Pending --> Cancelled : customer cancels, payment expired, gateway failed
    Confirmed --> Processing : admin starts packing
    Confirmed --> Cancelled : customer or admin cancels
    Processing --> Shipped : admin ships
    Processing --> Cancelled : admin cancels
    Shipped --> Delivered : admin marks delivered
    Delivered --> [*]
    Cancelled --> [*]
```

Every cancellation puts the stock back (`Order.Cancel`). The table lives in `Order.AllowedNextStatuses`.

## Payment status

```mermaid
stateDiagram-v2
    [*] --> Pending : order placed
    Pending --> Succeeded : webhook / sync says paid
    Pending --> Failed : order cancelled, expired or gateway error
    Succeeded --> [*]
    Failed --> [*]
```

## 6. Checkout and payment sequence

The flow for a card payment once Phases 1–2 are done (checkout from the cart, pluggable gateway).

```mermaid
sequenceDiagram
    autonumber
    actor C as Customer
    participant R as React app
    participant A as API
    participant DB as Database
    participant G as Fawaterak

    C->>R: Place order
    R->>A: POST /orders (Idempotency-Key, addressId, method)
    A->>DB: load cart, re-read prices and stock
    A->>DB: decrease stock, save Order (Pending) + Payment, clear cart
    A->>G: create invoice (amount, our order id)
    G-->>A: checkout URL
    A-->>R: 201 Created { order, payment.checkoutUrl }
    R->>G: redirect customer
    C->>G: enters card details
    G->>A: POST webhook (signed)
    A->>A: verify signature
    A->>G: get invoice (confirm paid + amount)
    A->>DB: PaymentEvent + Payment Succeeded + Order Confirmed (one transaction)
    A-->>G: 200 OK
    G-->>R: redirect back to /orders
    R->>A: GET /orders/{id}
    A-->>R: status Confirmed
```

For **cash on delivery**, steps 5–15 are skipped: the order is confirmed straight away.

## 7. Pluggable payment gateways (Strategy pattern)

The target design for "add any gateway later". `OrderService` only knows `IPaymentGateway`;
each gateway is one class in `Infrastructure`. Adding Paymob or Stripe means adding one class
and one line of DI, with no changes to checkout.

```mermaid
classDiagram
    direction LR
    class IPaymentGateway {
        <<interface>>
        +string Name
        +bool Supports(PaymentMethod)
        +CreateCheckoutAsync(CheckoutRequest) CheckoutSession
        +GetTransactionAsync(string id) GatewayTransaction
        +ParseWebhookAsync(string body, headers) GatewayCallback
    }
    class IPaymentGatewayResolver {
        <<interface>>
        +ForMethod(PaymentMethod) IPaymentGateway
        +ByName(string) IPaymentGateway
    }
    class FawaterakGateway
    class PaymobGateway {
        future
    }
    class StripeGateway {
        future
    }
    class OrderService
    class PaymentResultApplier
    class PaymentsController {
        POST /payments/{provider}/webhook
    }

    IPaymentGateway <|.. FawaterakGateway
    IPaymentGateway <|.. PaymobGateway
    IPaymentGateway <|.. StripeGateway
    OrderService --> IPaymentGatewayResolver
    PaymentsController --> IPaymentGatewayResolver
    PaymentsController --> PaymentResultApplier
    IPaymentGatewayResolver --> IPaymentGateway
```

- **Strategy** = interchangeable algorithms behind one interface (`IPaymentGateway`).
- **Adapter** = each implementation translates one provider's API into our types.
- **Resolver (a small factory)** = picks the strategy from configuration or from the URL's `{provider}`.
  .NET 8+ *keyed services* (`AddKeyedScoped<IPaymentGateway, FawaterakGateway>("fawaterak")`) make it a few lines.

## 8. Events and messaging

Phase 6. Things that don't have to happen *inside* the request (emails, low-stock alerts, cache
invalidation) are published as events and handled later by consumers.

```mermaid
flowchart LR
    subgraph Request["Inside the checkout transaction"]
        O["Order saved"] --> OB[("Outbox table")]
    end
    OB -->|MassTransit delivers| MQ[["RabbitMQ<br/>exchange"]]
    MQ --> E1["OrderPlaced → email 'we got your order'"]
    MQ --> E2["PaymentSucceeded → email receipt"]
    MQ --> E3["OrderShipped → email tracking info"]
    MQ --> E4["StockLow → alert admin"]
```

**Why an outbox:** if we saved the order and then published to RabbitMQ in two steps, a crash in
between would lose the event (or publish an event for an order that was never saved). The outbox
writes the event to the same database transaction as the order, and a background sender
forwards it. *"At-least-once delivery, made safe by idempotent consumers"* is the interview sentence.

## 9. Deployment

Phase 7. One `docker compose up` runs everything locally; the same images go to the cloud.

```mermaid
flowchart LR
    Dev["git push"] --> GH["GitHub Actions<br/>build · test · docker build"]
    GH --> Reg["Container registry<br/>(GHCR)"]
    Reg --> Host

    subgraph Host["Cloud host"]
        Web["client<br/>(nginx, static files)"]
        Api["api container"]
        Pg[("PostgreSQL")]
        Rd[("Redis")]
        Mq[["RabbitMQ"]]
    end
    Web --> Api
    Api --> Pg & Rd & Mq
```
