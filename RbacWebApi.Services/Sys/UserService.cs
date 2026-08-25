using RbacWebApi.DTOs;
using RbacWebApi.Models;
using RbacWebApi.Repositories;

namespace RbacWebApi.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IUserRoleRepository _userRoleRepo;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPermissionService _permissionService;

    public UserService(
        IUserRepository userRepo,
        IRoleRepository roleRepo,
        IUserRoleRepository userRoleRepo,
        IJwtTokenService jwtTokenService,
        IPermissionService permissionService)
    {
        _userRepo = userRepo;
        _roleRepo = roleRepo;
        _userRoleRepo = userRoleRepo;
        _jwtTokenService = jwtTokenService;
        _permissionService = permissionService;
    }

    public async Task<(bool Success, string Message, LoginResponse? Response)> LoginAsync(LoginRequest request)
    {
        var user = await _userRepo.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (user == null)
        {
            return (false, "用户名或密码错误", null);
        }

        if (user.Status != 1)
        {
            return (false, "账号已被禁用", null);
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return (false, "用户名或密码错误", null);
        }

        var roles = await _permissionService.GetUserRolesAsync(user.Id);
        var (token, expiresAt) = _jwtTokenService.GenerateToken(user, roles);
        var roleCodes = roles.Select(r => r.RoleCode).ToList();

        return (true, "登录成功", new LoginResponse
        {
            Token = token,
            UserId = user.Id,
            Username = user.Username,
            Nickname = user.Nickname,
            Roles = roleCodes,
            ExpiresAt = expiresAt
        });
    }

    public async Task<(bool Success, string Message)> RegisterAsync(RegisterRequest request)
    {
        if (await _userRepo.AnyAsync(u => u.Username == request.Username))
        {
            return (false, "用户名已存在");
        }

        var user = new SysUser
        {
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Nickname = request.Nickname,
            Email = request.Email,
            Phone = request.Phone,
            Status = 1
        };

        // Ulid / CreateTime 由 AOP 自动填充，InsertAsync 后 user.Id 已回填
        await _userRepo.InsertAsync(user);

        // 默认分配普通用户角色
        var userRole = await _roleRepo.FirstOrDefaultAsync(r => r.RoleCode == "USER");
        if (userRole != null)
        {
            await _userRoleRepo.InsertAsync(new SysUserRole { UserId = user.Id, RoleId = userRole.Id });
        }

        return (true, "注册成功");
    }

    public Task<SysUser?> GetUserByIdAsync(string userId)
    {
        return _userRepo.GetByIdAsync(userId);
    }

    public Task<PageResponse<SysUser>> GetUserListAsync(PageKeyRequest request)
    {
        var keyword = request.Keyword?.Trim();
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return _userRepo.GetPagedListAsync(_ => true, request);
        }
        return _userRepo.GetPagedListAsync(
            u => u.Username.Contains(keyword) || (u.Nickname != null && u.Nickname.Contains(keyword)),
            request);
    }

    public Task<(bool Success, string Message)> CreateUserAsync(RegisterRequest request)
    {
        return RegisterAsync(request);
    }

    public async Task<(bool Success, string Message)> UpdateUserAsync(string id, RegisterRequest request)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null)
        {
            return (false, "用户不存在");
        }

        if (!string.IsNullOrWhiteSpace(request.Username) && request.Username != user.Username)
        {
            if (await _userRepo.AnyAsync(u => u.Username == request.Username && u.Id != id))
            {
                return (false, "用户名已存在");
            }
            user.Username = request.Username;
        }

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        }

        user.Nickname = request.Nickname;
        user.Email = request.Email;
        user.Phone = request.Phone;
        // LastUpdateTime 由 AOP 在 Update 时自动填充

        await _userRepo.UpdateAsync(user);
        return (true, "更新成功");
    }

    public async Task<(bool Success, string Message)> DeleteUserAsync(string id)
    {
        if (!await _userRepo.AnyAsync(u => u.Id == id))
        {
            return (false, "用户不存在");
        }
        await _userRepo.DeleteByIdAsync(id);
        await _userRoleRepo.DeleteBatchAsync(ur => ur.UserId == id);
        return (true, "删除成功");
    }

    public async Task<(bool Success, string Message)> AssignRoleAsync(string userId, string roleId)
    {
        if (!await _userRepo.AnyAsync(u => u.Id == userId))
        {
            return (false, "用户不存在");
        }
        if (!await _roleRepo.AnyAsync(r => r.Id == roleId))
        {
            return (false, "角色不存在");
        }
        await _userRoleRepo.DeleteBatchAsync(ur => ur.UserId == userId);
        await _userRoleRepo.InsertAsync(new SysUserRole { UserId = userId, RoleId = roleId });
        return (true, "分配成功");
    }
}
