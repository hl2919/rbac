using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RbacWebApi.Attributes;
using RbacWebApi.DTOs;

namespace RbacWebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    [HttpGet("public")]
    [AllowAnonymous]
    [RbacAllowAnonymous]
    public ActionResult<ApiResponse<string>> Public()
    {
        return Ok(ApiResponse<string>.Success("这是公开接口，任何人都可以访问"));
    }

    [HttpGet("authorized")]
    [Authorize]
    public ActionResult<ApiResponse<string>> Authorized()
    {
        var username = User.Identity?.Name ?? "匿名";
        return Ok(ApiResponse<string>.Success($"登录用户访问成功，当前用户：{username}"));
    }

    [HttpGet("admin")]
    [Authorize]
    [RbacRole("ADMIN", "SUPER_ADMIN")]
    public ActionResult<ApiResponse<string>> AdminOnly()
    {
        return Ok(ApiResponse<string>.Success("管理员接口访问成功，仅 ADMIN / SUPER_ADMIN 角色可以访问"));
    }

    [HttpGet("superadmin")]
    [Authorize]
    [RbacRole("SUPER_ADMIN")]
    public ActionResult<ApiResponse<string>> SuperAdminOnly()
    {
        return Ok(ApiResponse<string>.Success("超级管理员接口访问成功，仅 SUPER_ADMIN 角色可以访问"));
    }
}
