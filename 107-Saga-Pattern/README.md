# 107-Saga-Pattern

## 1. Giới thiệu Saga Pattern
Saga Pattern là một mô hình kiến trúc dùng để quản lý các transaction phân tán (distributed transactions) trong microservices. Thay vì sử dụng cơ chế khóa phân tán như 2-Phase Commit (2PC), Saga chia một transaction lớn thành chuỗi các local transaction. Nếu một bước thất bại, Saga sẽ thực thi các transaction bù trừ (compensating transactions) để hoàn tác các bước trước đó.

## 2. Choreography vs Orchestration

| Tính năng | Choreography | Orchestration |
| :--- | :--- | :--- |
| **Định nghĩa** | Các service tự giao tiếp với nhau bằng cách publish/subscribe event. | Một service trung tâm (Orchestrator) điều phối toàn bộ luồng. |
| **Điểm mạnh** | Decentralized, không có single point of failure, dễ triển khai cho luồng đơn giản. | Centralized logic, dễ theo dõi trạng thái, dễ debug và bảo trì cho luồng phức tạp. |
| **Điểm yếu** | Khó theo dõi trạng thái, dễ dẫn đến "circular dependencies". | Thêm điểm tập trung, có thể thành single point of failure nếu không scale tốt. |
| **Khi nào dùng** | Luồng nghiệp vụ ít bước (2-4 services), ít thay đổi. | Luồng nghiệp vụ phức tạp, nhiều bước, cần retry và compensating rõ ràng. |

## 3. Mermaid sequence: Order Fulfillment Saga flow (happy path)
`mermaid
sequenceDiagram
    participant API as Orders API
    participant Saga as OrderSaga
    participant Pay as PaymentService
    participant Inv as InventoryService
    participant Ship as ShippingService

    API->>Saga: OrderPlaced
    Saga->>Pay: ProcessPaymentCommand
    Pay-->>Saga: PaymentProcessed
    Saga->>Inv: ReserveInventoryCommand
    Inv-->>Saga: InventoryReserved
    Saga->>Ship: ShipOrderCommand
    Ship-->>Saga: OrderShipped
    Saga-->>Saga: Completed
`

## 4. Mermaid sequence: Compensation flow (failure path)
`mermaid
sequenceDiagram
    participant API as Orders API
    participant Saga as OrderSaga
    participant Pay as PaymentService
    participant Inv as InventoryService

    API->>Saga: OrderPlaced
    Saga->>Pay: ProcessPaymentCommand
    Pay-->>Saga: PaymentProcessed
    Saga->>Inv: ReserveInventoryCommand
    Inv-->>Saga: InventoryFailed
    Saga-->>Saga: Failed (Compensate)
`

## 5. Mermaid stateDiagram: OrderSaga states
`mermaid
stateDiagram-v2
    [*] --> Placed: OrderPlaced
    Placed --> PaymentProcessing: ProcessPaymentCommand
    PaymentProcessing --> InventoryReserving: PaymentProcessed
    PaymentProcessing --> Failed: PaymentFailed
    InventoryReserving --> Shipping: InventoryReserved
    InventoryReserving --> Failed: InventoryFailed
    Shipping --> Completed: OrderShipped
    Completed --> [*]
    Failed --> [*]
`

## 6. Cấu trúc dự án
- **SagaPattern.Api**: ASP.NET Core 10 Web API chứa Orchestration Saga (OrderSaga) và các Consumers.
- **SagaPattern.Tests**: xUnit project dùng MassTransit TestHarness để integration test.

## 7. Cách chạy
- dotnet build SagaPattern.slnx
- cd SagaPattern.Api
- dotnet run
Mở trình duyệt: http://localhost:5307/swagger

## 8. Endpoints
- POST /api/orders: Place a new order
`json
{
  "customerName": "John Doe",
  "totalAmount": 100.0
}
`

## 9. Kết quả test
Project bao gồm 10 tests tự động bao phủ các case: happy path, payment failed, inventory failed, consumer behaviors, states transition.
Chạy test:
dotnet test SagaPattern.slnx (10/10 Passed)

## 10. Khi nào dùng Saga, khi nào dùng 2PC, khi nào dùng Outbox
- **Saga**: Khi các service độc lập database, cần long-running transaction, chấp nhận eventual consistency.
- **2PC**: Khi các thành phần dùng chung database hoặc database support XA transaction, yêu cầu tính nhất quán (ACID) tức thì. Rất ít dùng trong microservices.
- **Outbox Pattern**: Dùng kết hợp với Saga để đảm bảo "At-least-once delivery" khi lưu data vào DB cục bộ và publish event ra message broker đồng thời (tránh dual-write problem).
