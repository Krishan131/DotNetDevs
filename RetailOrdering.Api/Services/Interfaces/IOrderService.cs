using RetailOrdering.Api.Services.Helpers;
using RetailOrdering.Api.DTOs.Order;

namespace RetailOrdering.Api.Services.Interfaces;

public interface IOrderService
{
    Task<List<OrderDto>> GetMyOrdersAsync(int userId);
    Task<ServiceResult<OrderDto>> GetOrderByIdAsync(int requesterUserId, bool isAdmin, int orderId);
    Task<List<OrderDto>> GetAllOrdersAsync();
    Task<ServiceResult<OrderDto>> UpdateStatusAsync(int orderId, string status);
}

