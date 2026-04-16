using Microsoft.EntityFrameworkCore;
using RetailOrdering.Api.Services.Helpers;
using RetailOrdering.Api.Data;
using RetailOrdering.Api.Data.Models;
using RetailOrdering.Api.DTOs.Auth;
using RetailOrdering.Api.Services.Interfaces;

namespace RetailOrdering.Api.Services.Implementations;

public class AuthService(RetailOrderingDbContext dbContext, ITokenService tokenService) : IAuthService
{
    public async Task<ServiceResult<AuthResponseDto>> LoginAsync(LoginRequestDto request)
    {
        var existingUser = await dbContext.users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (existingUser is null)
        {
            return ServiceResult<AuthResponseDto>.Fail("Invalid email or password.");
        }

        var passwordOk = BCrypt.Net.BCrypt.Verify(request.Password, existingUser.PasswordHash);
        if (!passwordOk)
        {
            return ServiceResult<AuthResponseDto>.Fail("Invalid email or password.");
        }

        var roleName = existingUser.Role?.Name ?? AppRoles.Customer;
        var tokenData = tokenService.CreateToken(existingUser, roleName);

        return ServiceResult<AuthResponseDto>.Ok(new AuthResponseDto
        {
            Token = tokenData.Token,
            ExpiresAtUtc = tokenData.ExpiresAtUtc,
            User = new UserProfileDto
            {
                Id = existingUser.Id,
                Name = existingUser.Name ?? string.Empty,
                Email = existingUser.Email,
                Role = roleName
            }
        });
    }

    public async Task<ServiceResult<AuthResponseDto>> RegisterCustomerAsync(RegisterRequestDto request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var alreadyExists = await dbContext.users.AnyAsync(u => u.Email == normalizedEmail);
        if (alreadyExists)
        {
            return ServiceResult<AuthResponseDto>.Fail("Email is already registered.");
        }

        var customerRole = await EnsureRoleExistsAsync(AppRoles.Customer);

        var createdUser = new user
        {
            Name = request.Name.Trim(),
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            RoleId = customerRole.Id,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.users.Add(createdUser);
        await dbContext.SaveChangesAsync();

        dbContext.carts.Add(new cart
        {
            UserId = createdUser.Id,
            CreatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var tokenData = tokenService.CreateToken(createdUser, customerRole.Name);

        return ServiceResult<AuthResponseDto>.Ok(new AuthResponseDto
        {
            Token = tokenData.Token,
            ExpiresAtUtc = tokenData.ExpiresAtUtc,
            User = new UserProfileDto
            {
                Id = createdUser.Id,
                Name = createdUser.Name ?? string.Empty,
                Email = createdUser.Email,
                Role = customerRole.Name
            }
        }, "Registration successful.");
    }

    public async Task<ServiceResult<UserProfileDto>> GetCurrentUserAsync(int userId)
    {
        var profile = await dbContext.users
            .AsNoTracking()
            .Include(u => u.Role)
            .Where(u => u.Id == userId)
            .Select(u => new UserProfileDto
            {
                Id = u.Id,
                Name = u.Name ?? string.Empty,
                Email = u.Email,
                Role = u.Role != null ? u.Role.Name : AppRoles.Customer
            })
            .FirstOrDefaultAsync();

        return profile is null
            ? ServiceResult<UserProfileDto>.Fail("User not found.")
            : ServiceResult<UserProfileDto>.Ok(profile);
    }

    private async Task<role> EnsureRoleExistsAsync(string roleName)
    {
        var roleEntity = await dbContext.roles.FirstOrDefaultAsync(r => r.Name == roleName);
        if (roleEntity is not null)
        {
            return roleEntity;
        }

        roleEntity = new role { Name = roleName };
        dbContext.roles.Add(roleEntity);
        await dbContext.SaveChangesAsync();

        return roleEntity;
    }
}

