using RetailOrdering.Api.Services.Helpers;
using RetailOrdering.Api.DTOs.Auth;

namespace RetailOrdering.Api.Services.Interfaces;

public interface IAuthService
{
    Task<ServiceResult<AuthResponseDto>> LoginAsync(LoginRequestDto request);
    Task<ServiceResult<AuthResponseDto>> RegisterCustomerAsync(RegisterRequestDto request);
    Task<ServiceResult<UserProfileDto>> GetCurrentUserAsync(int userId);
}

