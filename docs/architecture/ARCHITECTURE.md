# Architecture & Design

This project is built as a web application with a separate frontend and backend.

The frontend uses Next.js (App Router), React, and TypeScript.  
The backend uses ASP.NET Core Web API with Entity Framework Core and SQL Server.  
Validation is handled with Zod on the frontend, and authentication is handled with JWT on the backend.

## System Architecture

At a high level, the frontend handles UI, routing, and session state. It sends HTTP JSON requests to backend API endpoints.

The backend handles business logic and authorization in controller/service layers, then reads and writes data through EF Core.

The data layer is a SQL Server database modeled with domain entities such as users, businesses, comments, promotions, and admin audit data.

Text diagram:

```text
[Frontend (Next.js + React)]
            |
            | HTTP JSON (JWT for protected endpoints)
            v
[Backend (ASP.NET Core API)]
            |
            | EF Core
            v
[SQL Server]
```

## Design Overview

The frontend follows component-based UI design with App Router pages, shared components, typed contracts, and context-based authentication state.  
The backend follows a layered design with domain entities, DTO/interface layer, service layer, and API controllers.

Deployment is container-based with Docker and Kubernetes for both frontend and backend.

## Data Model

Main entities:
- `User`
- `Business`
- `Comment`
- `Promotion`
- `City`
- `AuditLog`
- `NewsletterSubscriber`

Main relationships:
- A `User` owns many `Business` records
- A `User` writes many `Comment` records
- A `Business` has many `Comment` records
- A `Business` has many `Promotion` records
- `AuditLog` references the actor user and, when applicable, a target user

## API Structure

The API is organized by route groups:
- `/api/auth` for authentication
- `/api/users` for user profile/user operations
- `/api/businesses` for business operations
- `/api/comments` for comment operations
- `/api/promotions` for promotion operations
- `/api/opendays` for open-days operations
- `/api/cities`, `/api/subscribe`, `/api/health` for supporting/public endpoints
- `/api/admin` for admin-only operations (users, businesses, reports, categories, audit logs, dashboard)

## Non-Functional Requirements

Performance:
- EF Core is used with database indexing on key query fields
- Distributed caching is used for selected data paths
- Some list endpoints apply bounded result sizes

Security:
- JWT bearer authentication is used for protected endpoints
- Role/policy-based authorization controls admin and business-owner actions
- Passwords are stored using BCrypt hashing
- Frontend session is cleared on `401` responses
- Containers run as non-root

Availability:
- A health endpoint is exposed and used by Kubernetes probes
- Kubernetes uses rolling updates for deployment
- Horizontal pod autoscaling is configured
- Pod disruption budget is configured
