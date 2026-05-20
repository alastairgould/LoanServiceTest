# Loan Service

REST API that accepts loan applications, evaluates them against eligibility rules in a background job, and publishes a domain event via an outbox.

## Run it

```bash
dotnet build

dotnet run --project LoanApplication
```

SQLite schema is built on startup via `EnsureCreated()`. Delete `LoanApplication/loan-application.db` to reset. Using `EnsureCreated` for easy local running; would use migrations in a real service.

## Test it

```bash
dotnet test
```

## Endpoints

### `POST /loan-applications` → `201 Created`

```json
{
  "name": "Alice Example",
  "email": "alice@example.com",
  "monthlyIncome": 3500,
  "requestedAmount": 8000,
  "termMonths": 36
}
```

Returns `{ id, status: "Pending", createdAt }`. `400` on missing/invalid input.

### `GET /loan-applications/{id}` → `200 OK` / `404 Not Found`

Returns the full application plus the per-rule `decisionLog` array.

## Optional feature implemented

**Outbox.** On approve/reject, `OutboxEventPublisher` writes a `LoanApproved` / `LoanRejected` row to `OutboxMessages` in the same transaction as the loan update (atomic). `OutboxWorker` polls every 1 s, logs each row (the "simulate publishing" step), and sets `PublishedAt`.

## Architecture notes

### Handling 5,000,000 applications/day

- Use a message broker to communicate rather than sql tables.
  - HTTP Post to create a LoanApplication should publish a LoanApplicationSubmitted event
  - The eligibility service listens LoanApplicationSubmitted, and checks eligibility and publishes LoanApproved/LoanRejected which other services could listen to.
  - LoanApplication updates its Loan aggregate based on LoanApproved/LoanRejected
- Add authentication and authorization. 
- Message processors should scale up or down on queue length. Web Apis should scale on resource usage
- A database that can horizontally scale via a partition key I.e LoanApplication.Id would allow for easy automatic scaling of the database
- Lots of databases implement change feeds or change data capture which would be a good choice for implementing the outbox pattern rather than polling.
- Some kind of job to clear out the outbox table. I've kept messages in there, but just marked them as published. This allows for replay. But the outbox table will grow without something to clear.
- If keeping communication via sql scaling out workers then we need `SELECT … FOR UPDATE SKIP LOCKED` (row lease) so instances don't double-process. Avoid optimistic concurrency control as there will be contention for same rows. LoanApplications and Outbox need row locks to avoid workers picking up the same item if scaling out.

- **Split deployments.** Move `EligibilityService` and `OutboxPublisherService` out of the web host into their own processes — the multi-project layout already supports it. I've kept it all in one process for now for easy running.
- **Observability.** Structured logs, correlation IDs all the way through, OpenTelemetry traces, outbox-lag metrics.
- **Idempotency-Key** on POST.
- **EF migrations** instead of `EnsureCreated`.

### Shortcuts I'd revisit with more time

- `EnsureCreated()` instead of versioned migrations.
- No `Attempts` counter or DLQ on the Outbox/LoanApplications — a bad entry will simply just keep getting retried.
- Worker intervals are hard-coded constants; should be `IOptions`-bound.
- Logging is minimal (the doc's other optional feature). Picked outbox instead.
- No idempotency on POST.
- Correlation id for distributed tracing
- Hand-written validator; would reach for FluentValidation on a larger surface.
