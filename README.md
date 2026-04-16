# Currency API (Backend Interview Task)

This repository contains the backend implementation for the Currency Converter interview task. 
It focuses on delivering a production-ready, clean, and highly observable API over the public Frankfurter API.

> Note: The frontend implementation is intentionally excluded from this submission to focus purely on backend architecture, code quality, and testing practices.

## 🚀 Quick Start

Ensure you have the [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) installed.

### Run the API
```bash
# From the root directory
cd src/CurrencyApi.Api
dotnet run
```
The API will launch and automatically open Swagger UI (in the Development environment).
You can also visit: `http://localhost:5000/swagger` (or whatever port Kestrel selects).

### Run the Tests
```bash
# Run unit and integration tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

---

## 🏛 Architecture

The solution uses a **Clean Architecture** inspired domain-centric approach, split logically into 4 primary layers, plus 2 test projects:

1. **`CurrencyApi.Domain`**: Primitives, validation rules (e.g., Excluded Currencies Policy `!["TRY", "PLN", "THB", "MXN"]`), and strongly typed errors. Has no dependencies.
2. **`CurrencyApi.Application`**: Use cases (CQRS-style Handlers/Services), DTOs, interfaces, and pagination logic. 
3. **`CurrencyApi.Infrastructure`**: Implementation details, specifically the `FrankfurterApiClient`, Resilience pipelines (Polly), caching (`IMemoryCache`), and third-party interactions. Follows the *Provider Factory* pattern as requested.
4. **`CurrencyApi.Api`**: The composition root. Contains Controllers/Minimal APIs, Middleware, Auth mechanisms, Rate Limiting, and Swagger.

---

## 🔑 Authentication & Authorization (RBAC)

I established a credible but focused internal authentication mechanism (JWT). There are pre-seeded users for demonstration purposes:

| Username | Password     | Role  | Use Case                         |
|----------|--------------|-------|----------------------------------|
| `viewer` | `viewer-pass`| User  | Can access `/api/v1/rates/*`     |
| `admin`  | `admin-pass` | Admin | Can access `/api/v1/admin/*`     |

To use the API via Swagger:
1. Hit `POST /api/v1/auth/login` using the `viewer` credentials.
2. Copy the `accessToken` from the response.
3. Click "Authorize" at the top of Swagger and type: `Bearer <your_token>`.

---

## 📡 Endpoints Overview

All endpoints are versioned (`/api/v1/...`) and protected by Rate Limiting and Role-Based Access Control.

**Auth / Platform**
- `POST /api/v1/auth/login` (Anonymous)
- `GET /health` (Anonymous)
- `GET /api/versions` (Anonymous)

**Rates (Requires `User` Role)**
- `GET /api/v1/rates/latest?base=EUR` (Cached: 30s)
- `GET /api/v1/rates/convert?from=EUR&to=USD&amount=100` (Calculates dynamically from latest/cached rates)
- `GET /api/v1/rates/historical?base=EUR&start=2024-01-01&end=2024-01-05&page=1&pageSize=10` (Cached: 5 mins, Paginated)

**Admin (Requires `Admin` Role)**
- `POST /api/v1/admin/cache/clear` (Clears rate cache)

---

## 🧠 Design Decisions & Trade-offs

* **Frankfurter Abstraction:** Created a `FrankfurterCurrencyProvider` implementing `ICurrencyRatesProvider` and managed by a Factory. This ensures if Frankfurter is deprecated, switching to exchange-rates-api requires zero business logic changes.
* **Resilience:** Instead of failing instantly, Outbound calls to Frankfurter are wrapped in a Polly pipeline (Timeout -> Retry with backoff -> Circuit Breaker).
* **Caching:** Implemented `IMemoryCache` initially. If we had to scale horizontally, the interface (`ICurrencyRatesProvider` decorator pattern) allows us to seamlessly swap to Redis (`IDistributedCache`) without touching core logic.
* **Exceptions mapping:** The `ApiExceptionHandlingMiddleware` maps both Domain and Infrastructure exceptions to standard API JSON Error shapes (similar to RFC 7807), hiding internal stack traces from the caller. Status Code `500` from Frankfurter maps safely to `503 Service Unavailable`.

---

## 🔮 Future Improvements

1. **Distributed Caching:** Move from `IMemoryCache` to Redis via `IDistributedCache` for multi-node deployments.
2. **Refresh Tokens:** Currently, the JWT flow lacks a refresh mechanism to keep the scope tied strictly to the task's requirements.
3. **Database:** If user preferences or historical rate snapshots were required locally, EF Core + PostgreSQL would be the logical next step.

---

## 🤖 AI Usage Disclosure

As requested by the assignment criteria, here is a disclosure of AI assistant usage during this project:

- **AI Tools Used:** Google Gemini Pro natively via Antigravity Editor.
- **Where it helped:** Architecting the Slice-by-Slice `BACKEND_EXECUTION_PLAN.md`, writing repetitive test skeletons, auto-generating mock data (`StubHttpMessageHandler`), and configuring `.csproj` and `appsettings.json` boilerplate.
- **What was manually verified:** All domain logic constraints, resilience mappings (e.g. transient vs non-transient status codes), API routes, the `[ExcludeFromCodeCoverage]` setups, and making sure all unit & integration tests mathematically exceeded the 90% Code Coverage constraint.
