# RetailOrdering API Documentation

## 1) Project Overview
`RetailOrdering.Api` is an ASP.NET Core Web API (`net10.0`) for a retail/food-style ordering flow with:
- JWT authentication
- Role-based authorization (`Admin`, `Customer`)
- Product catalog browsing
- Cart management
- Checkout and order lifecycle
- Admin catalog and order status management

Core stack:
- ASP.NET Core Web API
- Entity Framework Core + MySQL (`MySql.EntityFrameworkCore`)
- JWT Bearer auth
- BCrypt password hashing
- Swagger/OpenAPI
- Built-in rate limiting middleware

---

## 2) Runtime Configuration
Configuration is loaded from `appsettings.json` / `appsettings.Development.json`.

### Connection string
- `ConnectionStrings:DefaultConnection`
- Used by `RetailOrderingDbContext` via `UseMySQL(...)`

### JWT settings (`Jwt` section)
Mapped to `Data/Settings/JwtSettings.cs`:
- `Key`
- `Issuer`
- `Audience`
- `ExpiryMinutes` (default `120`)

### Admin seed settings (`AdminSeed` section)
Mapped to `Data/Settings/AdminSeedSettings.cs`:
- `Name`
- `Email`
- `Password`

> At startup, the app seeds roles, admin user, and starter catalog data.

---

## 3) Application Startup Pipeline (`Program.cs`)
### Services registered
- DbContext: `RetailOrderingDbContext`
- AuthN/AuthZ: JWT bearer + `AddAuthorization()`
- CORS policy: `frontend` allows `http://localhost:4200`
- Rate limiter: fixed window, 100 requests/minute per identity/IP
- Swagger/OpenAPI in Development
- DI registrations:
  - `ITokenService -> TokenService`
  - `IAuthService -> AuthService`
  - `ICatalogService -> CatalogService`
  - `ICartService -> CartService`
  - `IOrderService -> OrderService`
  - `IAdminCatalogService -> AdminCatalogService`
  - `DbSeeder`

### Middleware order
1. HTTPS redirection
2. CORS (`frontend`)
3. Rate limiter
4. Authentication
5. Authorization
6. Controllers mapping

---

## 4) Security Model
### Roles
Defined in `Services/Helpers/AppRoles.cs`:
- `Admin`
- `Customer`

### JWT claims (from `TokenService`)
- `ClaimTypes.NameIdentifier` = user id
- `ClaimTypes.Name` = email
- `ClaimTypes.Email` = email
- `ClaimTypes.Role` = role name
- `displayName` (optional)

### User id extraction
`ClaimsPrincipalExtensions.GetUserId()` parses `NameIdentifier` to `int?`.

### Endpoint protection
- Public endpoints use `[AllowAnonymous]`
- Customer-only endpoints use `[Authorize(Roles = AppRoles.Customer)]`
- Admin-only endpoints use `[Authorize(Roles = AppRoles.Admin)]`

---

## 5) Response Pattern
Most service methods return:
- `ServiceResult` (non-generic)
- `ServiceResult<T>` (generic with `Data`)

Shape:
- `Success` (`bool`)
- `Message` (`string`)
- `Data` (`T?`, generic only)

Controllers map these to HTTP statuses (`200/400/401/404`) based on outcome.

---

## 6) Data Model (EF Core)
Main entities in `Data/Models`:
- `user` -> belongs to `role`, has one `cart`, has many `orders`
- `role` -> has many `users`
- `category` -> has many `products`
- `product` -> belongs to `category` (optional), has one `inventory`, has many `cartitems` and `orderitems`
- `inventory` -> one-to-one with `product`
- `cart` -> one-to-one with `user`, has many `cartitems`
- `cartitem` -> belongs to `cart`, belongs to `product`
- `order` -> belongs to `user`, has many `orderitems`
- `orderitem` -> belongs to `order`, belongs to `product`

Important DB constraints from `RetailOrderingDbContext`:
- Unique: `user.Email`
- Unique: `role.Name`
- Unique: `cart.UserId` (one cart per user)
- Unique: `inventory.ProductId` (one inventory row per product)
- Decimal precision configured for `Price`, `TotalAmount`

---

## 7) DTO Contracts
### Auth DTOs
- `RegisterRequestDto`: `Name`, `Email`, `Password`
- `LoginRequestDto`: `Email`, `Password`
- `AuthResponseDto`: `Token`, `ExpiresAtUtc`, `User`
- `UserProfileDto`: `Id`, `Name`, `Email`, `Role`

### Catalog DTOs
- `CategoryDto`: `Id`, `Name`
- `ProductDto`: `Id`, `Name`, `Description`, `Price`, `Brand`, `IsAvailable`, `CategoryId`, `CategoryName`, `AvailableQuantity`
- `ProductQueryDto`: `CategoryId?`, `Brand?`, `Search?`

### Cart DTOs
- `AddCartItemDto`: `ProductId`, `Quantity`
- `UpdateCartItemDto`: `Quantity`
- `CartDto`: `CartId`, `UserId`, `Items[]`, `TotalAmount`
- `CartItemDto`: `Id`, `ProductId`, `ProductName`, `Brand`, `UnitPrice`, `Quantity`, `LineTotal`

### Order DTOs
- `OrderDto`: order header + `Items[]`
- `OrderItemDto`: order line projection
- `UpdateOrderStatusDto`: `Status`

### Admin DTOs
- `CreateCategoryDto`, `UpdateCategoryDto`
- `CreateProductDto`, `UpdateProductDto`
- `UpdateInventoryDto`

> Data annotations are used for validation (`[Required]`, `[MaxLength]`, `[Range]`, `[EmailAddress]`, etc.).

---

## 8) Business Services
### `AuthService`
- Login: validates email + BCrypt password hash
- Register customer:
  - normalizes email
  - prevents duplicate email
  - ensures `Customer` role exists
  - creates user + cart
  - returns JWT
- Get current profile by user id

### `CatalogService`
- Get categories
- Get distinct brands
- Query products by category/brand/search
- Get product by id

### `CartService`
- Lazy creates cart if missing
- Add/update cart item with inventory checks
- Update quantity with stock checks
- Remove item
- Checkout flow:
  - validates availability and stock
  - creates order + orderitems in transaction
  - decrements inventory
  - clears cart items

### `OrderService`
- Customer order history
- Get order by id with ownership/admin guard
- Admin list all orders
- Admin update status with whitelist validation (`OrderStatuses.Allowed`)

### `AdminCatalogService`
- CRUD category (with protection against deleting non-empty category)
- CRUD product (prevents deletion if product has order history)
- Inventory update/create for product

---

## 9) Seed Data (`DbSeeder`)
Executed at startup:
1. Ensures roles `Admin` and `Customer`
2. Ensures admin user from config exists (and creates cart)
3. If no categories exist, seeds starter catalog:
   - Categories: Pizza, Cold Drinks, Breads
   - Example products
   - Inventory quantity `100` per seeded product

---

## 10) API Endpoints
Base route prefix: `api`

## Auth (`api/Auth`)
### `POST /api/Auth/register` (Anonymous)
Registers a customer and returns token.
- Body: `RegisterRequestDto`
- Success: `200 OK` with `ServiceResult<AuthResponseDto>`
- Failure: `400 BadRequest`

### `POST /api/Auth/login` (Anonymous)
Logs in existing user.
- Body: `LoginRequestDto`
- Success: `200 OK`
- Failure: `401 Unauthorized`

### `GET /api/Auth/me` (Authenticated)
Gets current user profile from token user id.
- Success: `200 OK`
- Failure: `401 Unauthorized`, `404 NotFound`

## Catalog (`api/Catalog`) - Anonymous
### `GET /api/Catalog/categories`
Returns all categories.

### `GET /api/Catalog/brands`
Returns distinct brands.

### `GET /api/Catalog/products?CategoryId=&Brand=&Search=`
Returns filtered product list.

### `GET /api/Catalog/products/{id}`
Returns product by id.
- `404` if not found.

## Cart (`api/Cart`) - Customer role only
### `GET /api/Cart`
Returns current customer cart.

### `POST /api/Cart/items`
Adds product to cart or increments existing quantity.
- Body: `AddCartItemDto`
- Validates product availability and stock

### `PUT /api/Cart/items/{itemId}`
Updates cart item quantity.
- Body: `UpdateCartItemDto`

### `DELETE /api/Cart/items/{itemId}`
Removes item from cart.

### `POST /api/Cart/checkout`
Creates order from cart in transaction and clears cart.

## Orders (`api/Orders`)
### `GET /api/Orders/my` (Customer role)
Returns current customer order history.

### `GET /api/Orders/{id}` (Authenticated)
Returns order by id.
- Admin can read any order
- Customer can read only own orders
- `404` for missing or forbidden access (service-level message)

## Admin (`api/Admin`) - Admin role only
### Orders
- `GET /api/Admin/orders` -> all orders
- `PUT /api/Admin/orders/{id}/status` -> update status (`UpdateOrderStatusDto`)

### Categories
- `POST /api/Admin/categories` -> create
- `PUT /api/Admin/categories/{id}` -> update
- `DELETE /api/Admin/categories/{id}` -> delete (fails if category has products)

### Products
- `POST /api/Admin/products` -> create (+ initial inventory)
- `PUT /api/Admin/products/{id}` -> update
- `DELETE /api/Admin/products/{id}` -> delete (blocked if order history exists)

### Inventory
- `PUT /api/Admin/inventory/{productId}` -> set stock quantity

---

## 11) Order Status Lifecycle
Allowed statuses from `OrderStatuses`:
- `Pending`
- `Confirmed`
- `Preparing`
- `Delivered`
- `Cancelled`

Invalid values are rejected in `OrderService.UpdateStatusAsync`.

---

## 12) Swagger and Testing
In Development environment:
- Swagger UI is enabled
- JWT bearer scheme is configured in OpenAPI definition

Typical local URLs from `launchSettings.json`:
- `http://localhost:5258`
- `https://localhost:7153`

---

## 13) Notes for Mentor Walkthrough
Recommended explanation order:
1. `Program.cs` (pipeline + DI + auth)
2. `RetailOrderingDbContext` + model relationships
3. Authentication flow (`AuthController` + `AuthService` + `TokenService`)
4. Catalog browsing flow
5. Cart + checkout transaction flow
6. Order retrieval and admin status management
7. Seeder behavior and default admin credentials source

This sequence shows architecture, security, and business flow end-to-end.
