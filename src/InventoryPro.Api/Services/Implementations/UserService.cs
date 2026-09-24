using InventoryPro.Api.Common.Exceptions;
using InventoryPro.Api.Data;
using InventoryPro.Api.DTOs.Auth;
using InventoryPro.Api.Entities;
using InventoryPro.Api.Mapping;
using InventoryPro.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InventoryPro.Api.Services.Implementations;

public class UserService : IUserService
{
    private readonly AppDbContext _context;

    public UserService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<UserProfileDto>> GetAllAsync()
    {
        var users = await _context.Users
            .AsNoTracking()
            .OrderBy(user => user.FullName)
            .ToListAsync();

        return users.Select(user => user.ToProfileDto()).ToList();
    }

    public async Task<UserProfileDto> GetByIdAsync(int id)
    {
        var user = await FindUserAsync(id);
        return user.ToProfileDto();
    }

    public async Task<UserProfileDto> UpdateRoleAsync(int id, UserRole role)
    {
        var user = await FindUserAsync(id);

        if (user.Role == UserRole.Admin && role != UserRole.Admin)
        {
            await EnsureAnotherActiveAdminExistsAsync(id);
        }

        user.Role = role;
        await _context.SaveChangesAsync();

        return user.ToProfileDto();
    }

    public async Task<UserProfileDto> UpdateStatusAsync(int id, bool isActive)
    {
        var user = await FindUserAsync(id);

        if (!isActive && user.Role == UserRole.Admin)
        {
            await EnsureAnotherActiveAdminExistsAsync(id);
        }

        user.IsActive = isActive;
        await _context.SaveChangesAsync();

        return user.ToProfileDto();
    }

    private async Task<User> FindUserAsync(int id)
    {
        var user = await _context.Users
            .SingleOrDefaultAsync(candidate => candidate.Id == id);

        if (user is null)
        {
            throw new NotFoundException($"User with id {id} was not found.");
        }

        return user;
    }

    /// <summary>
    /// Guards against locking everyone out by demoting/deactivating the last admin.
    /// </summary>
    private async Task EnsureAnotherActiveAdminExistsAsync(int excludedUserId)
    {
        var otherActiveAdmins = await _context.Users.CountAsync(user =>
            user.Role == UserRole.Admin &&
            user.IsActive &&
            user.Id != excludedUserId);

        if (otherActiveAdmins == 0)
        {
            throw new ConflictException(
                "This action would remove the last active administrator. Create another admin first.");
        }
    }
}
