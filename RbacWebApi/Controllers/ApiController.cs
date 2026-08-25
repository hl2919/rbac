using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RbacWebApi.DTOs;
using RbacWebApi.Models;
using RbacWebApi.Services;

namespace RbacWebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ApiController : ControllerBase
{
    private readonly IApiService _apiService;

    public ApiController(IApiService apiService)
    {
        _apiService = apiService;
    }

    /// <summary>分页获取 API 列表：固定参数 PageIndex/PageSize + 可选 Keyword</summary>
    [HttpGet("list")]
    public async Task<ActionResult<ApiResponse<PageResponse<SysApi>>>> GetList([FromQuery] PageKeyRequest request)
    {
        var page = await _apiService.GetApiListAsync(request);
        return Ok(ApiResponse<PageResponse<SysApi>>.Success(page));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<string>>> Create([FromBody] SysApi api)
    {
        var result = await _apiService.CreateApiAsync(api);
        if (!result.Success) return Ok(ApiResponse<string>.Fail(result.Message));
        return Ok(ApiResponse<string>.Success(result.Message));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<string>>> Update(string id, [FromBody] SysApi api)
    {
        api.Id = id;
        var result = await _apiService.UpdateApiAsync(api);
        if (!result.Success) return Ok(ApiResponse<string>.Fail(result.Message));
        return Ok(ApiResponse<string>.Success(result.Message));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<string>>> Delete(string id)
    {
        var result = await _apiService.DeleteApiAsync(id);
        if (!result.Success) return Ok(ApiResponse<string>.Fail(result.Message));
        return Ok(ApiResponse<string>.Success(result.Message));
    }
}
