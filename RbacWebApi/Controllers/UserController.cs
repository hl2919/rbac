using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RbacWebApi.DTOs;
using RbacWebApi.Models;
using RbacWebApi.Services;

namespace RbacWebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>分页获取用户列表：固定参数 PageIndex/PageSize + 可选 Keyword</summary>
    [HttpGet("list")]
    public async Task<ActionResult<ApiResponse<PageResponse<SysUser>>>> GetList([FromQuery] PageKeyRequest request)
    {
        var page = await _userService.GetUserListAsync(request);
        return Ok(ApiResponse<PageResponse<SysUser>>.Success(page));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<SysUser>>> GetById(string id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
        {
            return Ok(ApiResponse<SysUser>.Fail("用户不存在", 404));
        }
        return Ok(ApiResponse<SysUser>.Success(user));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<string>>> Create([FromBody] RegisterRequest request)
    {
        var result = await _userService.CreateUserAsync(request);
        if (!result.Success) return Ok(ApiResponse<string>.Fail(result.Message));
        return Ok(ApiResponse<string>.Success(result.Message));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<string>>> Update(string id, [FromBody] RegisterRequest request)
    {
        var result = await _userService.UpdateUserAsync(id, request);
        if (!result.Success) return Ok(ApiResponse<string>.Fail(result.Message));
        return Ok(ApiResponse<string>.Success(result.Message));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<string>>> Delete(string id)
    {
        var result = await _userService.DeleteUserAsync(id);
        if (!result.Success) return Ok(ApiResponse<string>.Fail(result.Message));
        return Ok(ApiResponse<string>.Success(result.Message));
    }
}
