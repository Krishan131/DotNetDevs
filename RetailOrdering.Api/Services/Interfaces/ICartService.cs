using RetailOrdering.Api.Services.Helpers;
using RetailOrdering.Api.DTOs.Cart;
using RetailOrdering.Api.DTOs.Order;

namespace RetailOrdering.Api.Services.Interfaces;

public interface ICartService
{
    Task<CartDto> GetCartAsync(int userId);
    Task<ServiceResult<CartDto>> AddOrUpdateItemAsync(int userId, AddCartItemDto request);
    Task<ServiceResult<CartDto>> UpdateItemQuantityAsync(int userId, int cartItemId, UpdateCartItemDto request);
    Task<ServiceResult<CartDto>> RemoveItemAsync(int userId, int cartItemId);
    Task<ServiceResult<OrderDto>> CheckoutAsync(int userId);
}

