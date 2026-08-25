using Microsoft.Extensions.DependencyInjection;
using RbacWebApi.ORM;
using RbacWebApi.Repositories;
using RbacWebApi.Services;
using RbacWebApi.Services.Cloud;

namespace RbacWebApi;

public static class SystemServiceExtension
{
    /// <summary>
    /// 统一注册：ORM层 DbContext + 仓储层(Repository) + 业务层(Services)
    /// </summary>
    public static IServiceCollection AddSystemService(this IServiceCollection services)
    {
        // ---- ORM 层（SqlSugar 客户端持有，单例即可）----
        services.AddSingleton<IDbContext, DbContext>();

        // ---- 仓储层：每个实体一个专用仓储，Scoped ----
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IApiRepository, ApiRepository>();
        services.AddScoped<IUserRoleRepository, UserRoleRepository>();
        services.AddScoped<IRoleApiRepository, RoleApiRepository>();
        services.AddScoped<ISysFileRepository, SysFileRepository>();
        services.AddScoped<IUserCloudDiskRepository, UserCloudDiskRepository>();

        // ---- 业务服务层：接口/实现分离，Scoped ----
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IApiService, ApiService>();
        services.AddScoped<ICloudDiskService, CloudDiskService>();
        services.AddScoped<IFileService, FileService>();

        return services;
    }
}
