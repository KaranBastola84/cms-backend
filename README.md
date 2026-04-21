# CMS Backend API

A production-oriented ASP.NET Core Web API for an education + operations platform (student lifecycle, course/batch management, attendance, inquiries/CRM, payments, documents, certificates, reporting, and product/order flows).

This backend is built around JWT authentication, role + permission-based authorization, PostgreSQL via EF Core, Stripe payment integration, and auditability features for administrative operations.

## Table of Contents

- [Overview](#overview)
- [Core Capabilities](#core-capabilities)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [Authentication and Authorization](#authentication-and-authorization)
- [API Modules (Route Map)](#api-modules-route-map)
- [Database and Migrations](#database-and-migrations)
- [File Upload and Storage Behavior](#file-upload-and-storage-behavior)
- [Operational and Security Notes](#operational-and-security-notes)
- [Troubleshooting](#troubleshooting)
- [Useful Commands](#useful-commands)

## Overview

This service provides backend APIs for:

- User/admin/staff/trainer/student authentication and account lifecycle
- Course, batch, and student management
- Attendance operations and attendance analytics
- Inquiry intake + assignment + follow-up workflow
- Fee structures, payment plans, installment tracking, receipts, and financial reporting
- Stripe payment intent and webhook-based payment confirmations
- Product catalog, product reviews, and order handling
- Certificate recommendation, issuance, verification, and download
- Dashboard summaries, charts, alerts, timelines, and notification read-state
- Role permissions + per-user permission overrides
- Audit logging and PDF export of audit data

## Core Capabilities

- JWT access + refresh token flow for staff/admin/trainer/user and student login paths
- Role-based access control with roles:
  - `Admin`, `Staff`, `Trainer`, `Student`, `EnrolledStudent`, `User`
- Fine-grained permissions with seeded defaults and user-level grants/revokes
- Centralized API response wrapper (`ApiResponse<T>`) with helper methods
- Built-in health check endpoint (`/health`)
- Development Swagger/OpenAPI with Bearer auth support
- Startup seeding:
  - Seeds default role-permission mappings
  - Optionally bootstraps initial admin if none exists and config values are provided
- Security hardening:
  - CORS allowlist
  - rate limiting middleware
  - standard security headers
  - strict webhook signature validation for Stripe
- File handling:
  - Public static hosting only for product images (`/Uploads/Products`)
  - Student documents are intentionally not served as public static files

## Tech Stack

- Runtime: .NET 9 (`net9.0`)
- Framework: ASP.NET Core Web API
- Data access: Entity Framework Core 9
- Database: PostgreSQL (Npgsql provider)
- Authentication: JWT Bearer
- Password hashing:
  - BCrypt for `ApplicationUser`
  - ASP.NET Core `IPasswordHasher<T>` for student passwords
- API docs: Swashbuckle / Swagger
- Payments: Stripe.NET
- PDF generation: QuestPDF
- QR support: QRCoder

## Project Structure

```text
cms-backend/
|- Controllers/           # API endpoints by business module
|- Data/                  # ApplicationDbContext and EF Core data layer
|- Services/              # Business services and integrations
|- Models/                # Entities, DTOs, enums, constants
|- Middleware/            # Custom request pipeline middleware (rate limiting)
|- Helpers/               # Shared utility helpers (e.g., response helper)
|- Migrations/            # EF Core migrations and model snapshot
|- Uploads/               # File storage root (product images + student documents)
|- Program.cs             # App bootstrap, DI, middleware, startup seeding
|- appsettings.json       # Base config template
|- appsettings.Development.json
|- cms-backend.csproj
```

## Getting Started

### 1. Prerequisites

- .NET SDK 9.x
- PostgreSQL 14+ (or compatible)
- Optional for migrations: `dotnet-ef` global/local tool

### 2. Restore and build

```bash
dotnet restore
dotnet build -p:UseAppHost=false
```

Note: `-p:UseAppHost=false` avoids a common file-lock warning when a prior `dotnet run` process is still holding `cms-backend.exe`.

### 3. Configure settings

Update configuration values (see [Configuration](#configuration)) for:

- Database connection string
- JWT key/issuer/audience
- Allowed CORS origins
- SMTP credentials
- Stripe keys and webhook secret
- Optional bootstrap admin settings

### 4. Apply database migrations

```bash
# install once if needed
dotnet tool install --global dotnet-ef

# from project folder
dotnet ef database update
```

### 5. Run API

```bash
dotnet run --launch-profile https
```

Default launch URLs from `launchSettings.json`:

- `https://localhost:7133`
- `http://localhost:5299`

### 6. Verify health

- `GET /health`

### 7. Open Swagger (Development only)

- `GET /swagger`

## Configuration

Configuration is primarily sourced from:

- `appsettings.json`
- `appsettings.Development.json`
- Environment variables (recommended for secrets)

### Required keys

#### Connection string

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=...;Username=...;Password=..."
}
```

#### JWT

```json
"Jwt": {
  "Key": "your-strong-secret-key",
  "Issuer": "https://localhost:5001",
  "Audience": "https://localhost:5001"
}
```

Production startup enforces `Jwt:Issuer` and `Jwt:Audience`.

#### CORS

```json
"AllowedOrigins": [
  "http://localhost:5173",
  "http://localhost:5174"
]
```

#### Stripe

```json
"Stripe": {
  "PublishableKey": "...",
  "SecretKey": "...",
  "WebhookSecret": "...",
  "Currency": "usd"
}
```

#### SMTP

```json
"SmtpSettings": {
  "Server": "smtp.gmail.com",
  "Port": 587,
  "FromEmail": "...",
  "FromName": "...",
  "Username": "...",
  "Password": "...",
  "EnableSsl": true
}
```

#### Optional bootstrap admin

```json
"BootstrapAdmin": {
  "Email": "admin@example.com",
  "Username": "admin",
  "Password": "change-me"
}
```

If no admin exists at startup and `BootstrapAdmin:Email` + `BootstrapAdmin:Password` are provided, the API auto-creates one admin account.

### Recommended secret handling

Do not commit real secrets. Prefer environment variables / secret stores.

Example environment variable mapping:

- `ConnectionStrings__DefaultConnection`
- `Jwt__Key`
- `Jwt__Issuer`
- `Jwt__Audience`
- `Stripe__SecretKey`
- `Stripe__WebhookSecret`
- `SmtpSettings__Password`

## Authentication and Authorization

### Token flow

- Login endpoints issue:
  - Access token (JWT, 1 hour)
  - Refresh token (JWT, 7 days)
- Refresh endpoint validates:
  - token signature/lifetime
  - token type (`refresh`)
  - DB-stored refresh token match
  - user active state

### Roles

- `Admin`
- `Staff`
- `Trainer`
- `Student`
- `EnrolledStudent`
- `User` (legacy compatibility)

### Permission system

Permission keys include:

- `dashboard`
- `view-students`
- `manage-students`
- `courses-batches`
- `attendance`
- `inquiries`
- `payment-finance`
- `student-documents`
- `reports`
- `certificates`

Defaults are seeded by role on startup. User-specific overrides (grant/revoke) are supported via `/api/permissions/users/{id}`.

## API Modules (Route Map)

All controllers are under `/api/*`.

| Module             | Base Route               | Highlights                                                                            |
| ------------------ | ------------------------ | ------------------------------------------------------------------------------------- |
| Auth               | `/api/auth`              | register, login, student-login, refresh, logout, register-admin                       |
| User Management    | `/api/usermanagement`    | users CRUD, activate/deactivate, role change, profile, password change                |
| Staff Management   | `/api/staffmanagement`   | create, OTP verification, activation/deactivation, password-change OTP flow           |
| Trainer Management | `/api/trainermanagement` | create, OTP verification, activation/deactivation, password-change OTP flow           |
| Permissions        | `/api/permissions`       | permission catalog, role permission replace, user grant/revoke overrides              |
| Course             | `/api/course`            | CRUD + active courses                                                                 |
| Batch              | `/api/batch`             | CRUD + active batches + by-course queries                                             |
| Student            | `/api/student`           | CRUD, status updates, details, registration summary, payments/documents helpers       |
| Student Documents  | `/api/studentdocument`   | upload, bulk upload, get/download, delete, filter by type                             |
| Attendance         | `/api/attendance`        | single/bulk mark, batch/student reports, update/delete                                |
| Inquiry            | `/api/inquiry`           | public submit + admin/staff/trainer assignment, status updates, follow-ups, analytics |
| Fee Structure      | `/api/feestructure`      | CRUD, course fee totals                                                               |
| Payment Plans      | `/api/paymentplan`       | plan create/query, installment status/pay, overdue/upcoming lists                     |
| Stripe Payments    | `/api/stripepayment`     | create payment intent, confirm, webhook handling, student payment history             |
| Receipts           | `/api/receipt`           | create/get/download/delete receipts                                                   |
| Financial Reports  | `/api/financialreport`   | summary, outstanding payments, defaulters, date-range revenue                         |
| Dashboard          | `/api/dashboard`         | role-based overviews, quick actions, timeline, alerts, charts, search, notifications  |
| Certificates       | `/api/certificate`       | recommendations, issue/revoke, eligibility, student downloads, public verify          |
| Products           | `/api/product`           | public catalog browsing + admin product CRUD, stock, image management                 |
| Product Reviews    | `/api/productreview`     | public review submission/view + admin moderation                                      |
| Orders             | `/api/order`             | public order creation + admin order/payment status management                         |
| Audit Logs         | `/api/auditlog`          | admin-only log pagination/filters and PDF export                                      |

For complete request/response contracts, use Swagger in Development mode.

## Database and Migrations

- DbContext: `ApplicationDbContext`
- Provider: PostgreSQL via `UseNpgsql(...)`
- Migrations location: `Migrations/`

Apply latest migrations:

```bash
dotnet ef database update
```

Create a new migration:

```bash
dotnet ef migrations add <MigrationName>
```

## File Upload and Storage Behavior

- Global request body limit is configured to 30 MB.
- Student document upload endpoints additionally enforce per-endpoint limits (5 MB single, 20 MB multiple).
- Student document validation checks extension + content type.
- Public static files are limited to product images at:
  - URL: `/Uploads/Products`
  - Disk: `Uploads/Products`
- Student documents are stored under `Uploads/StudentDocuments` and accessed via authorized API endpoints.

## Operational and Security Notes

- CORS is allowlist-based via `AllowedOrigins`.
- Forwarded headers are enabled for proxy deployments (`X-Forwarded-For`, `X-Forwarded-Proto`).
- Rate limiting middleware applies endpoint-specific limits plus a default policy.
- Security headers are added globally (`X-Frame-Options`, `X-Content-Type-Options`, `X-XSS-Protection`, `Referrer-Policy`, `Content-Security-Policy`).
- Swagger UI is only enabled in Development.
- Health checks are mapped to `/health`.

### Rate limiting highlights (requests/minute)

- `/api/auth/login`: 10
- `/api/order`: 100
- `/api/productreview`: 60
- `/api/inquiry`: 60
- `/api/staffmanagement/verify-otp`: 15
- `/api/trainermanagement/verify-otp`: 15
- `/api/staffmanagement/resend-otp`: 5
- `/api/trainermanagement/resend-otp`: 5
- `/api/staffmanagement/request-password-change`: 5
- `/api/trainermanagement/request-password-change`: 5
- `/api/staffmanagement/verify-password-change`: 10
- `/api/trainermanagement/verify-password-change`: 10
- default for other endpoints: 100

## Troubleshooting

### Build warning: locked `cms-backend.exe`

If build shows an MSB3061 warning because the executable is locked by a running process:

- stop the running API process, or
- build with:

```bash
dotnet build -p:UseAppHost=false
```

### Swagger not available

Swagger is enabled only when `ASPNETCORE_ENVIRONMENT=Development`.

### Stripe webhook failures

- Ensure `Stripe:WebhookSecret` is configured.
- Confirm `Stripe-Signature` header reaches the API (reverse proxy/webhook forwarding).

### Auth issues in production

Ensure all are set correctly:

- `Jwt:Key`
- `Jwt:Issuer`
- `Jwt:Audience`

### CORS errors from frontend

Add frontend origin to `AllowedOrigins` and restart API.

## Useful Commands

```bash
# Restore + build
dotnet restore
dotnet build -p:UseAppHost=false

# Run (HTTPS profile)
dotnet run --launch-profile https

# Apply DB migrations
dotnet ef database update

# Add new migration
dotnet ef migrations add <MigrationName>
```

---

If you want, I can also generate a frontend-facing API quick reference (`README.api.md`) with example request/response payloads for each module.
