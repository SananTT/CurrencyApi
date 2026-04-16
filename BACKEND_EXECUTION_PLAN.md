# Backend Execution Plan

## Goal

Build the backend portion of the currency converter task at a strong interview level, while intentionally excluding frontend implementation for now.

This plan is optimized for:

- strict alignment with the PDF task
- clean internal API contracts
- small execution slices
- strong architecture and testing discipline
- clear interview-ready trade-off decisions

## Scope Boundary

In scope:

- ASP.NET Core backend API
- Frankfurter integration
- latest rates endpoint
- currency conversion endpoint
- historical rates endpoint with pagination
- excluded currency validation
- JWT authentication
- RBAC
- rate limiting
- caching
- retry and circuit breaker
- structured logging
- request correlation
- unit and integration tests
- API versioning
- environment-aware configuration
- README-ready architecture notes

Explicitly out of scope for now:

- frontend implementation
- pixel-perfect UI work
- refresh token flow
- external identity provider integration
- distributed cache infrastructure rollout
- production deployment pipeline implementation

## Agreed Decisions

These are now treated as defaults unless we intentionally revise them later.

- Follow the task PDF first, but design the provider layer so future migration is easy.
- Use a clean internal contract instead of exposing Frankfurter response models directly.
- Keep the historical response date-centric, not flat per quote row.
- Add `symbols` filtering for historical responses.
- Use seeded users and local JWT issuance for demo-quality auth.
- Add at least one admin-only endpoint so RBAC is real, not cosmetic.
- Use stricter rate limits because the goal is best-in-class interview quality.
- Historical sorting default: descending by date.
- Historical `pageSize` max: `50`.
- Health endpoint: anonymous and minimal.
- Swagger/OpenAPI: enabled in `Dev` and `Test`; controlled in `Prod`.

## Product Interpretation

The backend is not just a CRUD API. It is a mini platform exercise.

The reviewer will likely evaluate:

- whether the core use cases work
- whether the codebase is easy to extend
- whether integration boundaries are clean
- whether failure modes were thought through
- whether operational concerns were treated seriously
- whether tests protect the important behavior

That means architecture quality matters almost as much as raw functionality.

## Proposed Architecture

Recommended project structure:

- `src/CurrencyApi.Api`
- `src/CurrencyApi.Application`
- `src/CurrencyApi.Domain`
- `src/CurrencyApi.Infrastructure`
- `tests/CurrencyApi.UnitTests`
- `tests/CurrencyApi.IntegrationTests`

Layer responsibilities:

- `Api`
  - controllers or minimal API endpoints
  - middleware
  - authentication and authorization setup
  - rate limiting setup
  - versioning setup
  - Swagger setup
  - dependency registration composition root
- `Application`
  - use cases
  - DTOs
  - validators
  - contracts and interfaces
  - pagination models
  - mapping logic that should not live in controllers
- `Domain`
  - currency business rules
  - excluded currency policy
  - strongly named domain models
  - domain exceptions or result types if needed
- `Infrastructure`
  - Frankfurter HTTP client
  - provider factory implementation
  - resilience policies
  - caching implementation
  - structured logging enrichers
  - correlation support for outbound calls
- `Tests`
  - unit tests for business rules and services
  - integration tests for API behavior and external integration seams

## Internal API Design

### Primary Endpoints

`GET /api/v1/rates/latest?base=EUR`

Purpose:

- return latest exchange rates for a base currency

Suggested response shape:

```json
{
  "baseCurrency": "EUR",
  "asOf": "2026-04-15",
  "rates": {
    "USD": 1.12,
    "GBP": 0.86
  }
}
```

`GET /api/v1/rates/convert?from=USD&to=AZN&amount=100`

Purpose:

- convert an amount using upstream exchange rate data and our own business logic

Suggested response shape:

```json
{
  "fromCurrency": "USD",
  "toCurrency": "AZN",
  "amount": 100,
  "rate": 1.7,
  "convertedAmount": 170
}
```

`GET /api/v1/rates/historical?base=EUR&start=2020-01-01&end=2020-01-31&page=1&pageSize=10&symbols=USD,GBP`

Purpose:

- return historical exchange rates by date, with pagination

Suggested response shape:

```json
{
  "baseCurrency": "EUR",
  "startDate": "2020-01-01",
  "endDate": "2020-01-31",
  "page": 1,
  "pageSize": 10,
  "totalItems": 31,
  "totalPages": 4,
  "sort": "date_desc",
  "items": [
    {
      "date": "2020-01-31",
      "rates": {
        "USD": 1.1,
        "GBP": 0.84
      }
    }
  ]
}
```

### Auth and Admin Endpoints

`POST /api/v1/auth/login`

Purpose:

- issue JWT for seeded users

Suggested request shape:

```json
{
  "username": "admin",
  "password": "..."
}
```

Suggested response shape:

```json
{
  "accessToken": "...",
  "expiresAtUtc": "2026-04-15T12:00:00Z",
  "role": "Admin"
}
```

`POST /api/v1/admin/cache/clear`

Purpose:

- admin-only operational endpoint to demonstrate RBAC and cache control

`GET /api/v1/health`

Purpose:

- simple anonymous readiness/liveness-style health endpoint

## Business Rules

These rules should live outside controllers and be covered by unit tests.

- Excluded currencies: `TRY`, `PLN`, `THB`, `MXN`
- If excluded currency appears as source or target in conversion:
  - return `400 Bad Request`
  - include a clear error message
- Base currency validation must reject unsupported or malformed values
- Amount must be greater than zero
- Historical range must reject:
  - missing dates
  - `start > end`
  - invalid date formats
- Pagination must reject invalid values:
  - `page < 1`
  - `pageSize < 1`
  - `pageSize > 50`
- Default historical sort order: newest first
- `symbols` filter should remove excluded currencies automatically or reject them consistently

Decision for `symbols` behavior:

- If `symbols` contains excluded currencies, reject with `400` and a clear validation message.

## Frankfurter Integration Strategy

Use Frankfurter as the upstream data source, but never expose its raw models directly.

Provider strategy:

- define an application-level provider contract
- implement `FrankfurterCurrencyProvider`
- add a provider factory even if there is only one provider initially
- register the factory through dependency injection

Reasons:

- task explicitly asks for factory pattern
- future providers can be added without changing controllers
- upstream model drift is isolated to infrastructure

## Resilience Strategy

Resilience must be visible in the design, not just implied.

Recommended behavior:

- timeout for outbound Frankfurter calls
- retry only on transient failures
- exponential backoff for retries
- circuit breaker for repeated upstream failure
- graceful upstream failure response from our API

Recommended implementation direction:

- `HttpClientFactory`
- resilience policies via modern .NET resilience pipeline or Polly-backed registration

Behavior goals:

- no blind retry on validation or client errors
- no leaking low-level upstream exception details to API consumers
- logs should preserve the failure reason and correlation data

## Caching Strategy

Use caching to reduce repeated upstream calls.

Recommended approach:

- cache latest rates by base currency
- cache historical responses by base, date range, and symbols set
- avoid overcomplicated caching for conversion; compute conversion from cached/latest rate data when possible

Suggested cache keys:

- `latest:{base}`
- `historical:{base}:{start}:{end}:{symbolsHash}`

Suggested TTL policy:

- latest rates: short TTL, for example `5` minutes
- historical data: longer TTL, for example `1` hour

Important note:

- use `IMemoryCache` first because it is enough for the interview task
- document that horizontal scaling would move this to distributed cache such as Redis

## Authentication and Authorization Strategy

Keep auth simple but credible.

Authentication plan:

- seeded in-memory or config-based users
- password check for demo accounts
- JWT issuance from login endpoint
- include `sub`, `client_id`, and `role` claims

Authorization plan:

- standard user role for rates endpoints
- admin role for cache-clear endpoint

Suggested roles:

- `User`
- `Admin`

Suggested seeded accounts:

- `viewer` -> `User`
- `admin` -> `Admin`

Important constraints:

- do not spend time on refresh tokens
- do not build full identity management
- do not let auth sprawl dominate the task

## Rate Limiting Strategy

Use separate policies by endpoint category.

Recommended limits:

- login: `5/minute/IP`
- latest: `60/minute/client`
- convert: `60/minute/client`
- historical: `20/minute/client`
- admin cache clear: `10/minute/client`

Client identity strategy:

- prefer JWT `client_id` or `sub`
- fall back to client IP if request is anonymous

Why this matters:

- shows differentiated thinking
- demonstrates abuse-awareness
- aligns policy strictness with endpoint cost

## Observability Strategy

This is a high-signal interview area and should be treated as first-class.

Per incoming request, capture:

- request correlation id
- client IP
- client ID from JWT if present
- HTTP method
- route or endpoint
- response status code
- response time

For outbound Frankfurter calls, capture:

- inherited or propagated correlation id
- upstream URL or endpoint template
- response status code
- latency
- failure reason if any

Recommended implementation direction:

- request logging middleware
- logging scopes or structured enrichment
- Serilog for structured logging
- correlation id middleware
- outbound header propagation for correlation

Recommended log style:

- structured fields, not string-only logs
- no sensitive secret logging
- auth failures should be observable without exposing tokens

## Error Handling Strategy

Use a consistent error response shape.

Suggested structure:

```json
{
  "code": "excluded_currency",
  "message": "Currency TRY is not supported for conversion.",
  "traceId": "00-..."
}
```

Suggested error categories:

- validation errors
- authentication errors
- authorization errors
- rate-limit errors
- upstream unavailable errors
- unexpected server errors

Design goals:

- clear to API consumer
- easy to test
- easy to correlate with logs

## API Versioning

Use URL versioning from the start.

Recommended shape:

- `/api/v1/...`

Reason:

- the task explicitly asks for versioning
- this is the simplest and clearest interview-friendly approach

## Environment Strategy

Support:

- `Development`
- `Test`
- `Production`

Configuration areas:

- Frankfurter base URL
- JWT signing key
- cache TTLs
- rate limits
- logging level
- Swagger exposure

Environment expectations:

- `Development`: Swagger enabled, easier diagnostics
- `Test`: predictable config for integration tests
- `Production`: safer defaults, no overly verbose diagnostics

## Testing Strategy

Target:

- minimum `90%` unit coverage on important logic
- integration coverage for external API behavior and middleware-sensitive behavior

### Unit Test Focus

- currency exclusion rules
- amount validation
- date range validation
- pagination validation
- symbols validation
- conversion calculation logic
- historical pagination mapping
- provider factory selection
- error mapping behavior

### Integration Test Focus

- authenticated latest endpoint success
- authenticated convert endpoint success
- historical endpoint pagination behavior
- excluded currency returns `400`
- missing or invalid JWT returns correct auth status
- admin endpoint rejects non-admin user
- rate limiting policy behavior
- upstream failure maps to stable API error response
- correlation header or trace behavior present in logs or response context when applicable

### Testing Notes

- prefer deterministic test doubles for upstream behavior
- do not rely on live Frankfurter in automated integration tests
- if helpful, add one optional smoke test that can hit real upstream manually

## README Expectations

The task explicitly cares about AI usage transparency.

README must include:

- setup instructions
- architecture overview
- endpoint summary
- auth usage
- test commands
- assumptions
- trade-offs
- future improvements
- explicit AI usage disclosure

AI disclosure section should state:

- where AI helped
- what was verified manually
- what suggestions were rejected or corrected

## Delivery Principles

Each slice must:

- have one clear purpose
- be small enough to complete safely
- leave the codebase in a runnable state
- avoid mixing unrelated concerns
- include at least basic verification

Do not create giant slices such as:

- "build whole API"
- "implement all security"
- "write all tests"

## Execution Slices

Below is the execution sequence. The slice sizes are intentionally kept moderate to small.

### Slice 01 - Solution Skeleton

Goal:

- create the solution and core project structure

Deliverables:

- solution file
- `Api`, `Application`, `Domain`, `Infrastructure` projects
- `UnitTests`, `IntegrationTests` projects
- project references wired correctly
- base dependency injection composition root

Done when:

- solution builds
- test projects restore and compile

### Slice 02 - Domain Primitives and Rules

Goal:

- model currencies, exclusions, and domain validation primitives

Deliverables:

- excluded currency policy
- currency normalization and validation helpers
- domain result or exception patterns

Done when:

- excluded currency logic has unit tests
- malformed currency cases are handled consistently

### Slice 03 - Application Contracts

Goal:

- define request and response contracts without implementation details

Deliverables:

- DTOs for latest, convert, historical, auth, and error responses
- pagination models
- provider interfaces
- cache abstractions if needed

Done when:

- contracts compile cleanly
- no Frankfurter models leak into application contracts

### Slice 04 - Validation Layer

Goal:

- validate inbound application requests centrally

Deliverables:

- validators for latest, convert, historical, login, and admin actions
- clear error codes and messages

Done when:

- invalid amount, date range, page, pageSize, and symbols are test-covered

### Slice 05 - Frankfurter Client Foundation

Goal:

- establish a clean outbound integration seam

Deliverables:

- typed HTTP client
- upstream request/response models isolated in infrastructure
- mapping into internal provider models

Done when:

- provider can fetch latest and historical data in isolation
- no controller dependency on HTTP details exists

### Slice 06 - Provider Factory and Selection

Goal:

- satisfy extensibility requirement explicitly

Deliverables:

- provider factory contract and implementation
- default provider registration

Done when:

- application services depend on factory or provider abstraction only

### Slice 07 - Latest Rates Use Case

Goal:

- implement the latest rates business flow end-to-end

Deliverables:

- application service
- mapping into internal latest response
- endpoint wiring

Done when:

- authenticated request returns expected response
- validation and not-found or unsupported cases behave correctly

### Slice 08 - Conversion Use Case

Goal:

- implement conversion flow with excluded currency rule

Deliverables:

- conversion calculation logic
- endpoint wiring
- error mapping for excluded currencies

Done when:

- conversion tests cover normal and excluded cases
- `400` is returned with a clear message for blocked currencies

### Slice 09 - Historical Use Case Core

Goal:

- implement historical data retrieval and internal shaping

Deliverables:

- historical query service
- date-centric item mapping
- optional `symbols` filter handling

Done when:

- service returns sorted internal records
- symbols filtering works correctly

### Slice 10 - Historical Pagination

Goal:

- paginate internal historical records cleanly

Deliverables:

- pagination metadata
- descending sort behavior
- page slicing logic

Done when:

- page boundaries are tested
- `totalItems` and `totalPages` are correct

### Slice 11 - Unified Error Handling

Goal:

- standardize errors across the API

Deliverables:

- exception or result translation middleware
- stable error contract
- trace id propagation in error responses

Done when:

- validation and server errors return consistent JSON shape

### Slice 12 - JWT Authentication

Goal:

- add credible demo authentication without bloating scope

Deliverables:

- seeded users
- login endpoint
- JWT generation
- auth configuration

Done when:

- login returns a valid token
- protected endpoints reject anonymous access

### Slice 13 - RBAC

Goal:

- enforce role-based access with at least one meaningful admin path

Deliverables:

- `User` and `Admin` role policies
- admin cache-clear endpoint or equivalent operational endpoint

Done when:

- `User` token cannot access admin endpoint
- `Admin` token can

### Slice 14 - Caching

Goal:

- reduce repeated upstream calls and show performance awareness

Deliverables:

- latest cache
- historical cache
- cache invalidation strategy for admin endpoint if relevant

Done when:

- repeated requests avoid unnecessary upstream calls
- cache behavior is testable

### Slice 15 - Resilience Policies

Goal:

- harden upstream integration

Deliverables:

- timeout
- retry with exponential backoff
- circuit breaker

Done when:

- transient failure path is test-covered
- repeated failures produce controlled degradation

### Slice 16 - Structured Logging and Correlation

Goal:

- make the API diagnosable

Deliverables:

- Serilog setup
- request logging middleware
- correlation id propagation
- outbound request correlation

Done when:

- logs contain the required fields
- upstream calls can be correlated with inbound requests

### Slice 17 - Rate Limiting

Goal:

- add endpoint-aware throttling

Deliverables:

- separate limit policies
- client identity resolution strategy

Done when:

- login and historical endpoints enforce different limits
- limit breaches return expected responses

### Slice 18 - Health, Versioning, and Swagger

Goal:

- complete the API surface professionally

Deliverables:

- health endpoint
- API versioning
- Swagger with JWT support in non-prod-friendly environments

Done when:

- docs reflect protected endpoints
- versioned routes are visible and testable

### Slice 19 - Unit Test Expansion

Goal:

- push core logic coverage to the target threshold

Deliverables:

- focused tests for domain, application, validation, mapping, factory, and auth helpers

Done when:

- unit coverage is at or above target for important code paths

### Slice 20 - Integration Test Expansion

Goal:

- prove real API behavior through the outer boundary

Deliverables:

- authenticated and unauthorized scenarios
- admin authorization scenarios
- rate limit scenarios
- upstream failure scenarios

Done when:

- key user journeys and failure paths are covered

### Slice 21 - Configuration and Environment Hardening

Goal:

- prepare the app for multiple environments cleanly

Deliverables:

- environment-specific configuration
- secret placeholders and safe defaults
- documented config keys

Done when:

- app can run predictably in `Development` and `Test`

### Slice 22 - README and Interview Polish

Goal:

- make the submission understandable and persuasive

Deliverables:

- setup and run guide
- architecture explanation
- AI usage disclosure
- assumptions and trade-offs
- future improvements

Done when:

- reviewer can clone, run, test, and understand the design without guessing

## Slice Ordering Rationale

This sequence is intentional.

- first establish structure
- then lock business rules and contracts
- then integrate upstream
- then implement core use cases
- then harden with auth, cache, resilience, and observability
- then expand tests and polish documentation

This reduces rework and keeps each slice focused.

## Risks and Mitigations

### Risk 1 - Overengineering too early

Mitigation:

- keep slices small
- avoid infrastructure complexity before core contracts stabilize

### Risk 2 - Auth scope explosion

Mitigation:

- stay with seeded users and JWT only
- explicitly avoid refresh tokens and identity management features

### Risk 3 - Historical endpoint complexity

Mitigation:

- standardize on date-centric records early
- paginate after internal mapping, not before

### Risk 4 - Fragile integration tests

Mitigation:

- use deterministic upstream stubs
- keep live upstream checks optional

### Risk 5 - Horizontal scaling claim not credible

Mitigation:

- avoid sticky in-memory assumptions in architecture notes
- document distributed cache path clearly

## Non-Goals During Implementation

When implementation starts, avoid these traps unless a later slice truly requires them.

- building frontend components
- introducing CQRS complexity for its own sake
- adding MediatR unless it clearly improves clarity
- building a database when the task does not require one
- creating a complex auth subsystem
- exposing raw upstream DTOs

## Definition of Success

The backend should feel:

- clean
- stable
- explainable
- testable
- production-aware

If a reviewer opens the repository, they should quickly see:

- good layering
- careful contracts
- operational maturity
- strong test coverage
- honest trade-offs

## Immediate Next Step

Start with `Slice 01 - Solution Skeleton` and do not mix it with business logic yet.
