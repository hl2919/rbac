using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RbacWebApi.DTOs;
using RbacWebApi.Models;
using RbacWebApi.Services;

namespace RbacWebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RoleController : ControllerBase
{
    private readonly IRoleService _roleService;
    private readonly IPermissionService _permissionService;
    private readonly IUserService _userService;

    public RoleController(IRoleService roleService, IPermissionService permissionService, IUserService userService)
    {
        _roleService = roleService;
        _permissionService = permissionService;
        _userService = userService;
    }

    /// <summary>分页获取角色列表：固定参数 PageIndex/PageSize + 可选 Keyword</summary>
    [HttpGet("list")]
    public async Task<ActionResult<ApiResponse<PageResponse<SysRole>>>> GetList([FromQuery] PageKeyRequest request)
    {
        var page = await _roleService.GetRoleListAsync(request);
        return Ok(ApiResponse<PageResponse<SysRole>>.Success(page));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<string>>> Create([FromBody] SysRole role)
    {
        var result = await _roleService.CreateRoleAsync(role);
        if (!result.Success) return Ok(ApiResponse<string>.Fail(result.Message));
        return Ok(ApiResponse<string>.Success(result.Message));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<string>>> Update(string id, [FromBody] SysRole role)
    {
        role.Id = id;
        var result = await _roleService.UpdateRoleAsync(role);
        if (!result.Success) return Ok(ApiResponse<string>.Fail(result.Message));
        return Ok(ApiResponse<string>.Success(result.Message));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<string>>> Delete(string id)
    {
        var result = await _roleService.DeleteRoleAsync(id);
        if (!result.Success) return Ok(ApiResponse<string>.Fail(result.Message));
        return Ok(ApiResponse<string>.Success(result.Message));
    }

    [HttpPost("{roleId}/assign/{userId}")]
    public async Task<ActionResult<ApiResponse<string>>> AssignRole(string roleId, string userId)
    {
        var result = await _userService.AssignRoleAsync(userId, roleId);
        if (!result.Success) return Ok(ApiResponse<string>.Fail(result.Message));
        return Ok(ApiResponse<string>.Success(result.Message));
    }

    [HttpGet("{roleId}/permissions")]
    public async Task<ActionResult<ApiResponse<List<SysApi>>>> GetPermissions(string roleId)
    {
        var list = await _permissionService.GetRoleApisAsync(roleId);
        return Ok(ApiResponse<List<SysApi>>.Success(list));
    }

    [HttpPut("{roleId}/permissions")]
    public async Task<ActionResult<ApiResponse<string>>> SetPermissions(string roleId, [FromBody] List<string> apiIds)
    {
        await _permissionService.SetRoleApisAsync(roleId, apiIds ?? []);
        return Ok(ApiResponse<string>.Success("设置成功"));
    }
}
