using RbacWebApi.DTOs;
using RbacWebApi.Models;
using RbacWebApi.Repositories;

namespace RbacWebApi.Services;

public class RoleService : IRoleService
{
    private readonly IRoleRepository _roleRepo;
    private readonly IUserRoleRepository _userRoleRepo;
    private readonly IRoleApiRepository _roleApiRepo;

    public RoleService(IRoleRepository roleRepo, IUserRoleRepository userRoleRepo, IRoleApiRepository roleApiRepo)
    {
        _roleRepo = roleRepo;
        _userRoleRepo = userRoleRepo;
        _roleApiRepo = roleApiRepo;
    }

    public Task<PageResponse<SysRole>> GetRoleListAsync(PageKeyRequest request)
    {
        var keyword = request.Keyword?.Trim();
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return _roleRepo.GetPagedListAsync(_ => true, request, r => r.CreateTime, SqlSugar.OrderByType.Asc);
        }
        return _roleRepo.GetPagedListAsync(
            r => r.RoleName.Contains(keyword) || r.RoleCode.Contains(keyword)
                 || (r.Description != null && r.Description.Contains(keyword)),
            request,
            r => r.CreateTime,
            SqlSugar.OrderByType.Asc);
    }

    public Task<SysRole?> GetRoleByIdAsync(string id)
    {
        return _roleRepo.GetByIdAsync(id);
    }

    public async Task<(bool Success, string Message)> CreateRoleAsync(SysRole role)
    {
        if (await _roleRepo.AnyAsync(r => r.RoleCode == role.RoleCode))
        {
            return (false, "角色编码已存在");
        }
        await _roleRepo.InsertAsync(role);
        return (true, "创建成功");
    }

    public async Task<(bool Success, string Message)> UpdateRoleAsync(SysRole role)
    {
        var existing = await _roleRepo.GetByIdAsync(role.Id);
        if (existing == null)
        {
            return (false, "角色不存在");
        }
        if (await _roleRepo.AnyAsync(r => r.RoleCode == role.RoleCode && r.Id != role.Id))
        {
            return (false, "角色编码已存在");
        }
        // LastUpdateTime 由 AOP 自动填充
        await _roleRepo.UpdateAsync(role);
        return (true, "更新成功");
    }

    public async Task<(bool Success, string Message)> DeleteRoleAsync(string id)
    {
        if (!await _roleRepo.AnyAsync(r => r.Id == id))
        {
            return (false, "角色不存在");
        }
        await _roleRepo.DeleteByIdAsync(id);
        await _userRoleRepo.DeleteBatchAsync(ur => ur.RoleId == id);
        await _roleApiRepo.DeleteBatchAsync(ra => ra.RoleId == id);
        return (true, "删除成功");
    }
}
