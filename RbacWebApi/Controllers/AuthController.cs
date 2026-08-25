using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RbacWebApi.Attributes;
using RbacWebApi.DTOs;
using RbacWebApi.Services;

namespace RbacWebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPermissionService _permissionService;

    public AuthController(IUserService userService, IJwtTokenService jwtTokenService, IPermissionService permissionService)
    {
        _userService = userService;
        _jwtTokenService = jwtTokenService;
        _permissionService = permissionService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [RbacAllowAnonymous]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
    {
        var result = await _userService.LoginAsync(request);
        if (!result.Success)
        {
            return Ok(ApiResponse<LoginResponse>.Fail(result.Message, 401));
        }
        return Ok(ApiResponse<LoginResponse>.Success(result.Response, result.Message));
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [RbacAllowAnonymous]
    public async Task<ActionResult<ApiResponse<string>>> Register([FromBody] RegisterRequest request)
    {
        var result = await _userService.RegisterAsync(request);
        if (!result.Success)
        {
            return Ok(ApiResponse<string>.Fail(result.Message));
        }
        return Ok(ApiResponse<string>.Success(result.Message));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> GetMe()
    {
        var userId = _jwtTokenService.GetUserIdFromClaims(User);
        if (userId == null)
        {
            return Ok(ApiResponse<object>.Unauthorized());
        }
        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
        {
            return Ok(ApiResponse<object>.Fail("用户不存在", 404));
        }
        var roles = await _permissionService.GetUserRoleCodesAsync(userId);
        return Ok(ApiResponse<object>.Success(new
        {
            user.Id,
            user.Username,
            user.Nickname,
            user.Email,
            user.Phone,
            user.Status,
            user.CreateTime,
            user.LastUpdateTime,
            Roles = roles
        }));
    }
}
