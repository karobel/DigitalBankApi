# DigitalBank API

A demo **core banking REST API** built with **.NET 8**, **PostgreSQL**, and **Clean Architecture** — covering customer onboarding, account management, deposits, withdrawals, and inter-account transfers, secured with **JWT authentication**.

This project reflects the kind of backend work I do professionally in the banking sector: layered architecture, transactional integrity for money movement, centralized error handling, audit logging, and automated tests.

[![CI](https://github.com/karobel/DigitalBankApi/actions/workflows/ci.yml/badge.svg)](https://github.com/karobel/DigitalBankApi/actions/workflows/ci.yml)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791)
![License](https://img.shields.io/badge/license-MIT-green)

---

## ✨ Features

- **Customer management** — onboarding with email uniqueness and age validation
- **Account management** — checking / savings / business accounts, multi-currency support
- **Core transactions** — deposit, withdrawal, and inter-account transfer (atomic, two-leg ledger entries)
- **Transaction history** per account
- **JWT authentication** — register/login, all banking endpoints protected with `[Authorize]`
- **Centralized exception handling** — consistent JSON error responses, no try/catch noise in controllers
- **Audit logging** — every money-moving operation is recorded
- **FluentValidation** on all input DTOs
- **Swagger / OpenAPI** with built-in JWT "Authorize" support
- **Health checks** endpoint (`/health`) for container orchestration
- **Unit tests** (xUnit + Moq + FluentAssertions) covering business rules
- **Integration tests** (WebApplicationFactory + EF Core InMemory) covering full HTTP flows
- **Dockerized** — API + PostgreSQL via a single `docker-compose up`
- **CI pipeline** (GitHub Actions) — builds and runs the full test suite on every push

---

## 🏗️ Architecture

The solution follows **Clean Architecture** (a.k.a. Onion Architecture), with dependencies flowing inward:

```
┌─────────────────────────────────────────────┐
│              DigitalBank.Api                 │  Controllers, Middleware,
│         (ASP.NET Core Web API)               │  Swagger, JWT setup
└───────────────────┬───────────────────────────┘
                     │ depends on
┌───────────────────▼───────────────────────────┐
│           DigitalBank.Infrastructure          │  EF Core + Npgsql, Repositories,
│   (PostgreSQL, JWT signing, BCrypt hashing)    │  JWT token service, password hashing
└───────────────────┬───────────────────────────┘
                     │ depends on
┌───────────────────▼───────────────────────────┐
│            DigitalBank.Application             │  Services (business logic),
│   (use cases / business rules, no I/O)         │  DTOs, Validators, Interfaces
└───────────────────┬───────────────────────────┘
                     │ depends on
┌───────────────────▼───────────────────────────┐
│              DigitalBank.Domain                │  Entities, Enums, Domain
│        (zero framework dependencies)           │  Exceptions — the core model
└─────────────────────────────────────────────────┘
```

**Why this matters:** the `Domain` and `Application` layers have **no dependency on EF Core, ASP.NET Core, or PostgreSQL** — they could be reused with a different database or exposed through gRPC instead of REST without touching business logic. This is verified by the project references themselves (`Domain` has zero NuGet dependencies).

---

## 🧱 Tech Stack

| Layer | Technology |
|---|---|
| API | ASP.NET Core 8, Swagger / Swashbuckle |
| Auth | JWT Bearer tokens, BCrypt password hashing |
| Data access | Entity Framework Core 8, Npgsql (PostgreSQL) |
| Validation | FluentValidation |
| Testing | xUnit, Moq, FluentAssertions, EF Core InMemory |
| Containerization | Docker, Docker Compose |
| CI/CD | GitHub Actions |

---

## 🚀 Getting Started

### Option A — Run with Docker (fastest, no local PostgreSQL needed)

```bash
git clone https://github.com/karobel/DigitalBankApi.git
cd DigitalBankApi
docker-compose up --build
```

The API will be available at **http://localhost:8080**, with Swagger UI at the root.

### Option B — Run locally with .NET SDK

**Prerequisites:** [.NET 8 SDK](https://dotnet.microsoft.com/download), PostgreSQL 14+ (local or via Docker).

```bash
git clone https://github.com/karobel/DigitalBankApi.git
cd DigitalBankApi

# Start just PostgreSQL via Docker, if you don't have it locally
docker run --name digitalbank-pg -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=digitalbank -p 5432:5432 -d postgres:16-alpine

# Restore & set the JWT secret (never commit real secrets to appsettings.json)
dotnet restore
cd src/DigitalBank.Api
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "a-long-random-development-only-secret-key"
cd ../..

# Apply EF Core migrations (creates the schema)
# First time only — generates the initial migration:
chmod +x scripts/add-initial-migration.sh
./scripts/add-initial-migration.sh
dotnet ef database update --project src/DigitalBank.Infrastructure --startup-project src/DigitalBank.Api

# Run the API
cd src/DigitalBank.Api
dotnet run
```

The API will be available at **http://localhost:5080**, with Swagger UI at the root (`/`).

---

## 🔑 Authentication Flow

1. `POST /api/v1/auth/register` — create a user, receive a JWT
2. `POST /api/v1/auth/login` — authenticate, receive a JWT
3. In Swagger, click **Authorize** and enter `Bearer <your-token>`
4. All `/api/v1/customers` and `/api/v1/accounts` endpoints require this token

---

## 📡 API Reference

| Method | Endpoint | Description | Auth required |
|---|---|---|---|
| POST | `/api/v1/auth/register` | Register a new user | No |
| POST | `/api/v1/auth/login` | Authenticate and receive a JWT | No |
| GET | `/api/v1/customers` | List all customers | Yes |
| GET | `/api/v1/customers/{id}` | Get a customer by ID | Yes |
| POST | `/api/v1/customers` | Create a new customer | Yes |
| GET | `/api/v1/accounts/{id}` | Get an account by ID | Yes |
| GET | `/api/v1/accounts/customer/{customerId}` | List a customer's accounts | Yes |
| POST | `/api/v1/accounts` | Open a new account | Yes |
| POST | `/api/v1/accounts/{id}/deposit` | Deposit funds | Yes |
| POST | `/api/v1/accounts/{id}/withdraw` | Withdraw funds | Yes |
| POST | `/api/v1/accounts/transfer` | Transfer funds between accounts | Yes |
| GET | `/api/v1/accounts/{id}/transactions` | Get transaction history | Yes |
| GET | `/health` | Health check (DB connectivity) | No |

Full interactive documentation is available via Swagger UI once the API is running.

### Example: deposit request

```http
POST /api/v1/accounts/{accountId}/deposit
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
Content-Type: application/json

{
  "amount": 250.00,
  "description": "Initial deposit"
}
```

---

## 🧪 Running the Tests

```bash
# Unit tests (business logic, no database needed)
dotnet test tests/DigitalBank.UnitTests

# Integration tests (full HTTP pipeline, EF Core InMemory — no PostgreSQL needed)
dotnet test tests/DigitalBank.IntegrationTests

# Everything
dotnet test
```

The integration tests spin up the API in-process via `WebApplicationFactory<Program>` and exercise full HTTP flows: register → login → create customer → open account → deposit → verify balance.

---

## 🔒 A Note on Money-Moving Operations

Transfers are implemented as **two ledger entries (debit + credit) committed in a single `SaveChanges` call**, so a transfer either fully succeeds or fully rolls back — there's no intermediate state where money has left one account but not arrived in the other. Every deposit, withdrawal, and transfer also writes an `AuditLog` entry, mirroring the audit trail requirements of real banking systems.

---

## 📁 Project Structure

```
DigitalBankApi/
├── src/
│   ├── DigitalBank.Domain/          # Entities, enums, domain exceptions
│   ├── DigitalBank.Application/     # Services, DTOs, interfaces, validators
│   ├── DigitalBank.Infrastructure/  # EF Core, repositories, JWT, password hashing
│   └── DigitalBank.Api/             # Controllers, middleware, Program.cs
├── tests/
│   ├── DigitalBank.UnitTests/
│   └── DigitalBank.IntegrationTests/
├── .github/workflows/ci.yml
├── docker-compose.yml
├── Dockerfile
└── DigitalBank.sln
```

---

## 🗺️ Possible Next Steps

- [ ] Add pagination to list endpoints
- [ ] Add a refresh-token flow alongside the access JWT
- [ ] Add rate limiting on `/auth` endpoints
- [ ] Add a minimal Angular or React front-end consuming this API
- [ ] Add OpenTelemetry tracing

---

## 👩‍💻 Author

**Karima Belkhatir** — .NET Developer & Microsoft Dynamics 365 CRM Consultant
[LinkedIn](https://ma.linkedin.com/in/karima-belkhatir) · [GitHub](https://github.com/karobel)

---

## 📄 License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.
