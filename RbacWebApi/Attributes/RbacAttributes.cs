using Microsoft.AspNetCore.Mvc.Filters;

namespace RbacWebApi.Attributes;

/// <summary>
/// 标记该接口需要特定角色才能访问（可选的补充机制，主要用RBAC中间件）
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class RbacRoleAttribute : Attribute, IFilterMetadata
{
    public string[] RoleCodes { get; }

    public RbacRoleAttribute(params string[] roleCodes)
    {
        RoleCodes = roleCodes;
    }
}

/// <summary>
/// 标记该接口允许匿名访问（跳过权限校验）
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RbacAllowAnonymousAttribute : Attribute, IFilterMetadata
{
}
