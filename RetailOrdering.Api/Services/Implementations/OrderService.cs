using Microsoft.EntityFrameworkCore;
using RetailOrdering.Api.Services.Helpers;
using RetailOrdering.Api.Data;
using RetailOrdering.Api.DTOs.Order;
using RetailOrdering.Api.Services.Interfaces;

namespace RetailOrdering.Api.Services.Implementations;

public class OrderService(RetailOrderingDbContext dbContext) : IOrderService
{
    public async Task<List<OrderDto>> GetMyOrdersAsync(int userId)
    {
        var orders = await dbContext.orders
            .AsNoTracking()
            .Include(o => o.User)
            .Include(o => o.orderitems)
            .ThenInclude(oi => oi.Product)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(DtoMapper.ToOrderDto).ToList();
    }

    public async Task<ServiceResult<OrderDto>> GetOrderByIdAsync(int requesterUserId, bool isAdmin, int orderId)
    {
        var orderEntity = await dbContext.orders
            .AsNoTracking()
            .Include(o => o.User)
            .Include(o => o.orderitems)
            .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (orderEntity is null)
        {
            return ServiceResult<OrderDto>.Fail("Order not found.");
        }

        if (!isAdmin && orderEntity.UserId != requesterUserId)
        {
            return ServiceResult<OrderDto>.Fail("You do not have access to this order.");
        }

        return ServiceResult<OrderDto>.Ok(DtoMapper.ToOrderDto(orderEntity));
    }

    public async Task<List<OrderDto>> GetAllOrdersAsync()
    {
        var orders = await dbContext.orders
            .AsNoTracking()
            .Include(o => o.User)
            .Include(o => o.orderitems)
            .ThenInclude(oi => oi.Product)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(DtoMapper.ToOrderDto).ToList();
    }

    public async Task<ServiceResult<OrderDto>> UpdateStatusAsync(int orderId, string status)
    {
        var normalizedStatus = status.Trim();
        if (!OrderStatuses.Allowed.Contains(normalizedStatus))
        {
            return ServiceResult<OrderDto>.Fail("Invalid order status.");
        }

        var orderEntity = await dbContext.orders
            .Include(o => o.User)
            .Include(o => o.orderitems)
            .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (orderEntity is null)
        {
            return ServiceResult<OrderDto>.Fail("Order not found.");
        }

        orderEntity.Status = normalizedStatus;
        await dbContext.SaveChangesAsync();

        return ServiceResult<OrderDto>.Ok(DtoMapper.ToOrderDto(orderEntity), "Order status updated.");
    }
}

