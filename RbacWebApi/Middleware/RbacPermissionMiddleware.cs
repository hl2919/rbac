using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using RbacWebApi.Attributes;
using RbacWebApi.DTOs;
using RbacWebApi.Services;

namespace RbacWebApi.Middleware;

/// <summary>
/// RBAC 权限校验中间件
/// 注意：必须放在 UseAuthentication 和 UseAuthorization 之后，MapControllers 之前
/// 这样 context.User 已经被 JWT Bearer 正确填充
/// </summary>
public class RbacPermissionMiddleware
{
    private readonly RequestDelegate _next;

    public RbacPermissionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IPermissionService permissionService, IJwtTokenService jwtTokenService)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint == null)
        {
            await _next(context);
            return;
        }

        // 1. 检查 [AllowAnonymous] / [RbacAllowAnonymous]
        var allowAnonymous1 = endpoint.Metadata.GetMetadata<IAllowAnonymous>();
        var allowAnonymous2 = endpoint.Metadata.GetMetadata<RbacAllowAnonymousAttribute>();
        if (allowAnonymous1 != null || allowAnonymous2 != null)
        {
            await _next(context);
            return;
        }

        // 2. 未登录用户直接返回401（让Authorize先走，但这里也兜一下）
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            await WriteUnauthorizedResponse(context, "未登录或登录已过期");
            return;
        }

        // 3. 从已认证的身份中取用户ID（由JWT Bearer中间件注入到context.User）
        var userId = jwtTokenService.GetUserIdFromClaims(context.User);
        if (userId == null)
        {
            await WriteUnauthorizedResponse(context, "无效的用户身份");
            return;
        }

        // 4. 获取当前请求的路径和方法
        var requestPath = context.Request.Path.Value ?? "/";
        var requestMethod = context.Request.Method;

        // 5. 检查是否满足 [RbacRole] 显式要求（与RBAC数据库权限是 AND 关系：两者都要过）
        var rbacRoleAttr = endpoint.Metadata.GetMetadata<RbacRoleAttribute>();
        if (rbacRoleAttr != null && rbacRoleAttr.RoleCodes.Length > 0)
        {
            var userRoles = jwtTokenService.GetRolesFromClaims(context.User);
            if (!rbacRoleAttr.RoleCodes.Any(r => userRoles.Contains(r, StringComparer.OrdinalIgnoreCase)))
            {
                await WriteForbiddenResponse(context, "当前用户角色不允许访问此接口");
                return;
            }
        }

        // 6. 数据库 RBAC 接口权限校验
        var hasPermission = await permissionService.CheckApiPermissionAsync(userId!, requestPath, requestMethod);
        if (!hasPermission)
        {
            await WriteForbiddenResponse(context, "无权限访问该接口，请联系管理员分配权限");
            return;
        }

        await _next(context);
    }

    private static Task WriteUnauthorizedResponse(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json; charset=utf-8";
        var response = ApiResponse<string>.Unauthorized(message);
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return context.Response.WriteAsync(json, Encoding.UTF8);
    }

    private static Task WriteForbiddenResponse(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json; charset=utf-8";
        var response = ApiResponse<string>.Forbidden(message);
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return context.Response.WriteAsync(json, Encoding.UTF8);
    }
}
