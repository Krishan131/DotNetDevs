# DotNetDevs - RetailOrdering API

RetailOrdering.Api is an ASP.NET Core Web API for a retail and food-style ordering workflow with JWT authentication, role-based access, product catalog browsing, cart management, checkout, and admin operations.

## Key Features

- JWT authentication and role-based authorization (Admin, Customer)
- Product catalog with category, brand, and search filters
- Cart operations with stock validation
- Checkout flow with inventory updates and order creation
- Admin management for categories, products, inventory, and order status
- Swagger/OpenAPI support in Development
- Fixed-window rate limiting middleware
- Startup seeding for roles, admin user, and starter catalog data

## Tech Stack

- .NET 10 (ASP.NET Core Web API)
- Entity Framework Core 10 + MySQL
- JWT Bearer Authentication
- BCrypt password hashing
- Swashbuckle (Swagger/OpenAPI)

## Project Structure

- backend.sln
- RetailOrdering.Api/
- RetailOrdering.Api/Program.cs
- RetailOrdering.Api/Services/
- RetailOrdering.Api/DTOs/
- RetailOrdering.Api/API_DOCUMENTATION.md

## Getting Started

### Prerequisites

- .NET 10 SDK
- MySQL Server

### 1) Clone

```bash
git clone https://github.com/Krishan131/DotNetDevs.git
cd DotNetDevs
```

### 2) Configure appsettings

Edit these files as needed:

- RetailOrdering.Api/appsettings.json
- RetailOrdering.Api/appsettings.Development.json

Minimum required configuration:

- ConnectionStrings:DefaultConnection
- Jwt:Key, Jwt:Issuer, Jwt:Audience, Jwt:ExpiryMinutes
- AdminSeed:Name, AdminSeed:Email, AdminSeed:Password

### 3) Restore and Run

```bash
dotnet restore backend.sln
dotnet build backend.sln
dotnet run --project RetailOrdering.Api
```

Local URLs from launch settings:

- http://localhost:5258
- https://localhost:7153

Swagger UI (Development):

- http://localhost:5258/swagger
- https://localhost:7153/swagger

## Authentication

Use Bearer token auth for protected endpoints:

Authorization: Bearer <jwt_token>

Roles used in the API:

- Admin
- Customer

## API Areas

- Auth: register, login, current profile
- Catalog: categories, brands, product listing, product details
- Cart (Customer): view cart, add/update/remove items, checkout
- Orders: customer history, order details
- Admin: order status updates, category/product management, inventory updates

Detailed endpoint docs are available at:

- RetailOrdering.Api/API_DOCUMENTATION.md

## Seed Data Behavior

On startup, the app seeds:

- Roles: Admin and Customer
- Admin user from AdminSeed config
- Starter catalog if categories are empty

## Security Notes

- Replace default JWT key and seed credentials before deploying.
- Do not use development secrets in production.
- Restrict CORS origins for production clients.

## License

No license file is currently included in this repository.
