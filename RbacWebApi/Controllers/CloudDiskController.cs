using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RbacWebApi.DTOs;
using RbacWebApi.Services;
using RbacWebApi.Services.Cloud;

namespace RbacWebApi.Controllers;

/// <summary>
/// 云盘管理控制器：开通云盘、查询状态
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CloudDiskController : ControllerBase
{
    private readonly ICloudDiskService _cloudDiskService;
    private readonly IJwtTokenService _jwtTokenService;

    public CloudDiskController(ICloudDiskService cloudDiskService, IJwtTokenService jwtTokenService)
    {
        _cloudDiskService = cloudDiskService;
        _jwtTokenService = jwtTokenService;
    }

    /// <summary>开通云盘</summary>
    [HttpPost("activate")]
    public async Task<ActionResult<ApiResponse<string>>> Activate([FromBody] ActivateCloudDiskRequest? request)
    {
        var userId = _jwtTokenService.GetUserIdFromClaims(User);
        if (userId == null) return Ok(ApiResponse<string>.Unauthorized());

        var req = request ?? new ActivateCloudDiskRequest();
        var result = await _cloudDiskService.ActivateAsync(userId, req);
        if (!result.Success) return Ok(ApiResponse<string>.Fail(result.Message));
        return Ok(ApiResponse<string>.Success(result.Message));
    }

    /// <summary>查询云盘状态</summary>
    [HttpGet("status")]
    public async Task<ActionResult<ApiResponse<CloudDiskStatusResponse>>> GetStatus()
    {
        var userId = _jwtTokenService.GetUserIdFromClaims(User);
        if (userId == null) return Ok(ApiResponse<CloudDiskStatusResponse>.Unauthorized());

        var status = await _cloudDiskService.GetStatusAsync(userId);
        if (status == null) return Ok(ApiResponse<CloudDiskStatusResponse>.Fail("云盘未开通", 404));
        return Ok(ApiResponse<CloudDiskStatusResponse>.Success(status));
    }
}
