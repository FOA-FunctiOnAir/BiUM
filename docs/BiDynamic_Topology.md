# BiDynamic Mimari Topoloji

## Topoloji görseli (PNG)

![BiDynamic Platform Topology](BiDynamic_Topology.png)

---

Aşağıdaki Mermaid diyagramı [mermaid.live](https://mermaid.live) veya VS Code Mermaid eklentisi ile PNG/SVG olarak dışa aktarılabilir.

## Güncellemeler
- **Client Apps** altında **BiDynamic Web** ve **BiDynamic Mobile** aynı font stiliyle gösterilir (parantez yok).
- Servisler arası iletişim: **REST + gRPC** ve **RabbitMQ (event-driven)** belirtilir.
- **SigNoz** (Traces & Logs) observability katmanı olarak eklenir.

```mermaid
flowchart LR
    subgraph Clients[" "]
        CA["Client Apps<br/>BiDynamic Web<br/>BiDynamic Mobile"]
    end

    subgraph Gateway[" "]
        GW[BiApp.Gateway<br/>Ocelot · JWT · Rate Limit · Health]
    end

    subgraph Comms["Servisler arası iletişim"]
        REST[REST + gRPC]
        MQ["RabbitMQ (event-driven)"]
    end

    subgraph Services["BiApp Mikroservisler"]
        direction TB
        S1[Authentication · Accounting · Accounts · AiAssistant]
        S2[Bpmn · Configuration · Customers · Dms]
        S3[EnergyTracking · Expenses · Information · Messaging]
        S4[Parameters · PortalConfiguration · Products]
        S5[Purchases · Sales · Scheduler · Stocks · Treasury]
    end

    subgraph Platform[" "]
        BIUM[BiUM · Core · Infra · Specialized · Bolt]
    end

    subgraph Observability[" "]
        SigNoz[SigNoz<br/>Traces & Logs]
    end

    subgraph Data[" "]
        Rabbit[RabbitMQ]
        DB[(PostgreSQL / SQL Server)]
        Redis[Redis]
    end

    CA --> GW
    GW --> REST
    GW --> MQ
    REST --> Services
    MQ --> Services
    Services --> BIUM
    Services --> SigNoz
    BIUM --> SigNoz
    Services --> Rabbit
    Services --> DB
    Services --> Redis
```

## Basit kutu diyagramı (sunum için)

```mermaid
flowchart TB
    subgraph Client["İstemci"]
        CA[Client Apps]
        BDW[BiDynamic Web]
        BDM[BiDynamic Mobile]
    end

    GW[BiApp.Gateway]

    subgraph Comm["İletişim"]
        direction LR
        R[REST + gRPC]
        E[RabbitMQ event-driven]
    end

    subgraph Micro["Mikroservisler"]
        direction LR
        M[Authentication, Accounting, Sales, Bpmn, ...]
    end

    BIUM[BiUM]
    SigNoz[SigNoz - Traces & Logs]
    Rabbit[RabbitMQ]
    DB[(PostgreSQL/SQL Server)]

    Client --> GW
    GW --> Comm
    Comm --> Micro
    Micro --> BIUM
    Micro --> SigNoz
    Micro --> Rabbit
    Micro --> DB
```
