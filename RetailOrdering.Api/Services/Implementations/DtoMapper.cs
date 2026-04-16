using RetailOrdering.Api.Data.Models;
using RetailOrdering.Api.DTOs.Cart;
using RetailOrdering.Api.DTOs.Catalog;
using RetailOrdering.Api.DTOs.Order;

namespace RetailOrdering.Api.Services.Implementations;

public static class DtoMapper
{
    public static ProductDto ToProductDto(product productEntity)
    {
        var quantity = productEntity.inventory?.Quantity ?? 0;

        return new ProductDto
        {
            Id = productEntity.Id,
            Name = productEntity.Name,
            Description = productEntity.Description ?? string.Empty,
            Price = productEntity.Price,
            Brand = productEntity.Brand ?? string.Empty,
            IsAvailable = productEntity.IsAvailable ?? true,
            CategoryId = productEntity.CategoryId,
            CategoryName = productEntity.Category?.Name ?? string.Empty,
            AvailableQuantity = quantity
        };
    }

    public static CartDto ToCartDto(cart cartEntity)
    {
        var items = cartEntity.cartitems
            .Select(ci => new CartItemDto
            {
                Id = ci.Id,
                ProductId = ci.ProductId ?? 0,
                ProductName = ci.Product?.Name ?? string.Empty,
                Brand = ci.Product?.Brand ?? string.Empty,
                UnitPrice = ci.Product?.Price ?? 0,
                Quantity = ci.Quantity,
                LineTotal = (ci.Product?.Price ?? 0) * ci.Quantity
            })
            .ToList();

        return new CartDto
        {
            CartId = cartEntity.Id,
            UserId = cartEntity.UserId ?? 0,
            Items = items,
            TotalAmount = items.Sum(i => i.LineTotal)
        };
    }

    public static OrderDto ToOrderDto(order orderEntity)
    {
        var items = orderEntity.orderitems
            .Select(oi =>
            {
                var unitPrice = oi.Price ?? 0;
                var quantity = oi.Quantity ?? 0;

                return new OrderItemDto
                {
                    ProductId = oi.ProductId ?? 0,
                    ProductName = oi.Product?.Name ?? string.Empty,
                    Brand = oi.Product?.Brand ?? string.Empty,
                    Quantity = quantity,
                    UnitPrice = unitPrice,
                    LineTotal = unitPrice * quantity
                };
            })
            .ToList();

        return new OrderDto
        {
            Id = orderEntity.Id,
            UserId = orderEntity.UserId ?? 0,
            CustomerName = orderEntity.User?.Name ?? string.Empty,
            CustomerEmail = orderEntity.User?.Email ?? string.Empty,
            TotalAmount = orderEntity.TotalAmount ?? 0,
            Status = orderEntity.Status ?? string.Empty,
            CreatedAtUtc = orderEntity.CreatedAt ?? DateTime.UtcNow,
            Items = items
        };
    }
}
