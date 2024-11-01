# repro-marten-wolverine-integration-no-response

Repro to investigate why adding `.IntegrateWithWolverine();` in Marten configuration causes a Wolverine response not to be sent.

## Scenario:

- RetailClient: Console App which places new orders in Order Service via Wolverine/RabbitMQ (3.1.0/3.1.0-rc-3) by invoking `PlaceOrder` commands (`RetailClient/Worker.cs`)
- Orders: Service, which receives the `PlaceOrder` commands, creates a pending `PurchaseOrder` (Inline SingleStreamProjection `PurchaseOrderProjection`) from it, sends the order back to the client and publishes a `OrderPlaced` event. `OrderPlaced` is consumed by the `Orders/Sagas/Order.cs` Saga and will invoke the `ReserveCredit` command - which already should matter for the issue described below.
- Messages: shared message types

## Setup

### Depedencies

```bash
cd database
./clear-data.sh
docker compose up
```

### Service / Client

```bash
cd src/Orders
dotnet run
```

```bash
cd src/RetailClient
dotnet run
```

## Issues 

The client is expected to receive an `PurchaseOrder` instance back from the `Orders/PlaceOrderHandler.cs` after invoking the `PlaceOrder` command.

This works fine until this line is added to the `Orders/Program.cs` Marten Configuration in line 38:

```csharp
 .IntegrateWithWolverine();
```

As soon as this line is being added, invoking the `PlaceOrder` command fails with this error message in the RetailClient:

```text
fail: Wolverine.Runtime.RemoteInvocation.FailureAcknowledgementHandler[0]
Received failure acknowledgement on reply for message 08dcfa93-7a7d-c24c-d843-ae60e95c0000 from service Orders with message 'No response was created for expected response 'Orders.PurchaseOrder''
fail: Microsoft.Extensions.Hosting.Internal.Host[9]
BackgroundService failed
Wolverine.Runtime.RemoteInvocation.WolverineRequestReplyException: Request failed: No response was created for expected response 'Orders.PurchaseOrder'
at Wolverine.Runtime.Routing.MessageRoute.InvokeAsync[T](Object message, MessageBus bus, CancellationToken cancellation, Nullable`1 timeout, String tenantId) in /home/runner/work/wolverine/wolverine/src/Wolverine/Runtime/Routing/MessageRoute.cs:line 151
```
