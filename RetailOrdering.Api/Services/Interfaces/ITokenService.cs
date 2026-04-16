using RetailOrdering.Api.Data.Models;

namespace RetailOrdering.Api.Services.Interfaces;

public interface ITokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateToken(user user, string roleName);
}
