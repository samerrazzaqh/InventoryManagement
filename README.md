# Inventory Management System - Backend API

A robust, enterprise-grade backend API built with **.NET** following **Clean Architecture** principles. Designed for managing multi-warehouse inventories, stock transactions, categories, products, and secure identity management with JWT and Refresh Tokens.

---

## 🏗️ Architecture & Tech Stack

This project follows the **Clean Architecture** pattern, separating concerns into distinct layers:
* **Domain Layer:** Contains enterprise entities, business logic, enums, and exceptions.
* **Application Layer:** Contains application business rules, interfaces, DTOs, validators, and handlers.
* **Infrastructure Layer:** Handles database persistence (Entity Framework Core), Identity, caching (Redis), and external services.
* **API Layer (Presentation):** ASP.NET Core Web API controllers, middleware, extensions, and OpenAPI/Swagger documentation.

### Core Technologies & Packages
* **Framework:** .NET (10/11)
* **Database & ORM:** SQL Server with Entity Framework Core (EF Core)
* **Authentication & Authorization:** ASP.NET Core Identity & JSON Web Tokens (JWT) with Refresh Token rotation
* **Caching:** Redis (`StackExchange.Redis`)
* **API Documentation:** OpenAPI & Swagger UI
* **Resiliency & Performance:** Rate Limiting & Health Checks

---

## 📁 Project Structure

```text
InventoryManagement/
│
├── InventoryManagement.Api/          # Presentation layer (Controllers, Middleware, Program.cs)
├── InventoryManagement.Application/  # Business logic & application use cases
├── InventoryManagement.Domain/       # Enterprise entities & business rules
└── InventoryManagement.Infrastructure/# Data access, EF Core Migrations, Identity & external services
```

---

## 📖 API Documentation (Swagger)

Once the application is running, open your browser and navigate to:
* **Swagger UI:** `https://localhost:[port]/swagger/index.html`

---

## 🔒 Authentication Flow

1. **Register:** Send a POST request to `/api/v1/auth/register` to create a new user.
2. **Login:** Send a POST request to `/api/v1/auth/login` to obtain your `accessToken` and `refreshToken`.
3. **Authorize:** Use the `accessToken` as a **Bearer Token** in the Authorization header of protected endpoints.
4. **Refresh Token:** Use `/api/v1/auth/refresh` to renew expired access tokens using a valid refresh token.

---

## 🚀 Getting Started

### Prerequisites
* [.NET SDK](https://dotnet.microsoft.com/) installed on your machine.
* **SQL Server** (running locally or via Docker).
* **Redis** (optional, for caching features).

### 1. Clone the Repository
```bash
git clone https://github.com/your-username/InventoryManagement.git
cd InventoryManagement
```

### 2. Configure Settings
Update your connection strings and JWT configuration in `appsettings.Development.json` under `InventoryManagement.Api`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=InventoryDb;TrustServerCertificate=True;User=sa;Password=YourPassword123!",
    "Redis": "localhost:6379"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Jwt": {
    "Issuer": "InventoryManagement.Api",
    "Audience": "InventoryManagement.Clients",
    "SigningKey": "YourSuperSecretSigningKeyWithAtLeast32CharactersHere123!",
    "AccessTokenMinutes": 15,
    "RefreshTokenDays": 7
  },
  "Cache": {
    "InstanceName": "inventory",
    "DefaultTtlMinutes": 5
  }
}
```

### 3. Apply Database Migrations
Run the following command to update and create tables in your SQL Server database:

```bash
dotnet ef database update --project InventoryManagement.Infrastructure --startup-project InventoryManagement.Api
```

### 4. Run the Application
Start the API project:

```bash
dotnet run --project InventoryManagement.Api
```
