# TSTHRMS

Multi-tenant HR Management System for The Thiinker / ThinkerSteps — built to run internal HR across entities (The Thiinker, ThinkerSteps) and products (SwarnApp, JewelSteps, Miniz), and designed from day one to be sold as a SaaS product to other companies.

## Stack

- **Backend:** ASP.NET Core 10 (C# 14) Web API, layered architecture (Domain / Application / Infrastructure / Api)
- **Database:** MySQL 8.4 via EF Core 9.x + Pomelo.EntityFrameworkCore.MySql
- **Auth:** ASP.NET Core Identity + JWT (access + rotating refresh token)
- **Frontend:** React 19 + TypeScript + Vite, Tailwind CSS + shadcn/ui, Lucide (SVG) icons
- **Data/forms:** TanStack Query + TanStack Table, React Hook Form + Zod
- **Hosting target:** Windows Server, IIS (ANCMv2 → Kestrel), single origin (SPA + API under one site)

See [`docs/architecture.md`](docs/architecture.md) for the full architecture writeup and [`docs/deployment-windows-server-iis.md`](docs/deployment-windows-server-iis.md) for the production deployment runbook.

## Repository Layout

```
backend/    ASP.NET Core solution (API, Application, Domain, Infrastructure, tests)
frontend/   React + TypeScript SPA
docs/       Architecture and deployment documentation
```

## Local Development

### Prerequisites

- .NET 10 SDK
- Node.js 24+
- Docker Desktop (for local MySQL)

### 1. Start local MySQL

```bash
docker compose up -d
```

This starts MySQL 8.4 on `localhost:3306` and Adminer (DB admin UI) on `http://localhost:8080`.

### 2. Run the backend

```bash
cd backend
dotnet restore
dotnet ef database update --project src/TSTHRMS.Infrastructure --startup-project src/TSTHRMS.Api
dotnet run --project src/TSTHRMS.Api
```

API runs at `https://localhost:5001` (Swagger UI at `/swagger`).

### 3. Run the frontend

```bash
cd frontend
npm install
npm run dev
```

App runs at `http://localhost:5173`.

## Build Status

Phase 0 (foundation: auth, multi-tenancy, app shell) — in progress.
Phase 1 (Core HR / Employee Database) — not started.
