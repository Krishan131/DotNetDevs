using Microsoft.EntityFrameworkCore;
using RetailOrdering.Api.Services.Helpers;
using RetailOrdering.Api.Data;
using RetailOrdering.Api.Data.Models;
using RetailOrdering.Api.DTOs.Cart;
using RetailOrdering.Api.DTOs.Order;
using RetailOrdering.Api.Services.Interfaces;

namespace RetailOrdering.Api.Services.Implementations;

public class CartService(RetailOrderingDbContext dbContext) : ICartService
{
    public async Task<CartDto> GetCartAsync(int userId)
    {
        var cartEntity = await GetOrCreateCartEntityAsync(userId);
        await dbContext.Entry(cartEntity)
            .Collection(c => c.cartitems)
            .Query()
            .Include(ci => ci.Product)
            .LoadAsync();

        return DtoMapper.ToCartDto(cartEntity);
    }

    public async Task<ServiceResult<CartDto>> AddOrUpdateItemAsync(int userId, AddCartItemDto request)
    {
        var cartEntity = await GetOrCreateCartEntityAsync(userId);

        var productEntity = await dbContext.products
            .Include(p => p.inventory)
            .FirstOrDefaultAsync(p => p.Id == request.ProductId);

        if (productEntity is null || !(productEntity.IsAvailable ?? true))
        {
            return ServiceResult<CartDto>.Fail("Product is not available.");
        }

        if (productEntity.inventory is null)
        {
            return ServiceResult<CartDto>.Fail("Inventory entry not found for product.");
        }

        var existingItem = await dbContext.cartitems
            .FirstOrDefaultAsync(ci => ci.CartId == cartEntity.Id && ci.ProductId == request.ProductId);

        var targetQuantity = existingItem is null
            ? request.Quantity
            : existingItem.Quantity + request.Quantity;

        if (targetQuantity > productEntity.inventory.Quantity)
        {
            return ServiceResult<CartDto>.Fail("Requested quantity exceeds available inventory.");
        }

        if (existingItem is null)
        {
            dbContext.cartitems.Add(new cartitem
            {
                CartId = cartEntity.Id,
                ProductId = request.ProductId,
                Quantity = request.Quantity
            });
        }
        else
        {
            existingItem.Quantity = targetQuantity;
        }

        await dbContext.SaveChangesAsync();
        return ServiceResult<CartDto>.Ok(await GetCartAsync(userId), "Cart updated.");
    }

    public async Task<ServiceResult<CartDto>> UpdateItemQuantityAsync(int userId, int cartItemId, UpdateCartItemDto request)
    {
        var cartEntity = await GetOrCreateCartEntityAsync(userId);

        var cartItemEntity = await dbContext.cartitems
            .Include(ci => ci.Product)
            .ThenInclude(p => p!.inventory)
            .FirstOrDefaultAsync(ci => ci.Id == cartItemId && ci.CartId == cartEntity.Id);

        if (cartItemEntity is null)
        {
            return ServiceResult<CartDto>.Fail("Cart item not found.");
        }

        var available = cartItemEntity.Product?.inventory?.Quantity ?? 0;
        if (request.Quantity > available)
        {
            return ServiceResult<CartDto>.Fail("Requested quantity exceeds available inventory.");
        }

        cartItemEntity.Quantity = request.Quantity;
        await dbContext.SaveChangesAsync();

        return ServiceResult<CartDto>.Ok(await GetCartAsync(userId), "Cart item updated.");
    }

    public async Task<ServiceResult<CartDto>> RemoveItemAsync(int userId, int cartItemId)
    {
        var cartEntity = await GetOrCreateCartEntityAsync(userId);

        var cartItemEntity = await dbContext.cartitems
            .FirstOrDefaultAsync(ci => ci.Id == cartItemId && ci.CartId == cartEntity.Id);

        if (cartItemEntity is null)
        {
            return ServiceResult<CartDto>.Fail("Cart item not found.");
        }

        dbContext.cartitems.Remove(cartItemEntity);
        await dbContext.SaveChangesAsync();

        return ServiceResult<CartDto>.Ok(await GetCartAsync(userId), "Item removed from cart.");
    }

    public async Task<ServiceResult<OrderDto>> CheckoutAsync(int userId)
    {
        var cartEntity = await dbContext.carts
            .Include(c => c.User)
            .Include(c => c.cartitems)
            .ThenInclude(ci => ci.Product)
            .ThenInclude(p => p!.inventory)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cartEntity is null || cartEntity.cartitems.Count == 0)
        {
            return ServiceResult<OrderDto>.Fail("Cart is empty.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        decimal totalAmount = 0;
        var orderEntity = new order
        {
            UserId = userId,
            Status = OrderStatuses.Pending,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.orders.Add(orderEntity);
        await dbContext.SaveChangesAsync();

        foreach (var cartItemEntity in cartEntity.cartitems)
        {
            var productEntity = cartItemEntity.Product;
            if (productEntity is null || !(productEntity.IsAvailable ?? true))
            {
                await transaction.RollbackAsync();
                return ServiceResult<OrderDto>.Fail("One or more products are no longer available.");
            }

            var inventoryEntity = productEntity.inventory;
            if (inventoryEntity is null || inventoryEntity.Quantity < cartItemEntity.Quantity)
            {
                await transaction.RollbackAsync();
                return ServiceResult<OrderDto>.Fail($"Insufficient inventory for {productEntity.Name}.");
            }

            inventoryEntity.Quantity -= cartItemEntity.Quantity;
            var price = productEntity.Price;
            totalAmount += price * cartItemEntity.Quantity;

            dbContext.orderitems.Add(new orderitem
            {
                OrderId = orderEntity.Id,
                ProductId = productEntity.Id,
                Quantity = cartItemEntity.Quantity,
                Price = price
            });
        }

        orderEntity.TotalAmount = totalAmount;
        dbContext.cartitems.RemoveRange(cartEntity.cartitems);

        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        var createdOrder = await dbContext.orders
            .AsNoTracking()
            .Include(o => o.User)
            .Include(o => o.orderitems)
            .ThenInclude(oi => oi.Product)
            .FirstAsync(o => o.Id == orderEntity.Id);

        return ServiceResult<OrderDto>.Ok(DtoMapper.ToOrderDto(createdOrder), "Order placed successfully.");
    }

    private async Task<cart> GetOrCreateCartEntityAsync(int userId)
    {
        var existingCart = await dbContext.carts.FirstOrDefaultAsync(c => c.UserId == userId);
        if (existingCart is not null)
        {
            return existingCart;
        }

        var createdCart = new cart
        {
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.carts.Add(createdCart);
        await dbContext.SaveChangesAsync();

        return createdCart;
    }
}

