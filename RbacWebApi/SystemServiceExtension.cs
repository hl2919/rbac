using Microsoft.Extensions.DependencyInjection;
using RbacWebApi.ORM;
using RbacWebApi.Repositories;
using RbacWebApi.Services;
using RbacWebApi.Services.Cloud;
using RbacWebApi.Services.Wms;

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

        // WMS 基础数据仓储
        services.AddScoped<IWarehouseRepository, WarehouseRepository>();
        services.AddScoped<IZoneRepository, ZoneRepository>();
        services.AddScoped<IAisleRepository, AisleRepository>();
        services.AddScoped<IRackRepository, RackRepository>();
        services.AddScoped<ILocationRepository, LocationRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IContainerRepository, ContainerRepository>();
        // WMS 业务单据仓储：主表 + 明细表
        services.AddScoped<IReceiveOrderRepository, ReceiveOrderRepository>();
        services.AddScoped<IReceiveOrderDetailRepository, ReceiveOrderDetailRepository>();
        services.AddScoped<IInboundOrderRepository, InboundOrderRepository>();
        services.AddScoped<IInboundOrderDetailRepository, InboundOrderDetailRepository>();
        services.AddScoped<IPutawayOrderRepository, PutawayOrderRepository>();
        services.AddScoped<IPutawayOrderDetailRepository, PutawayOrderDetailRepository>();
        services.AddScoped<ITakeDownOrderRepository, TakeDownOrderRepository>();
        services.AddScoped<ITakeDownOrderDetailRepository, TakeDownOrderDetailRepository>();
        services.AddScoped<IPickOrderRepository, PickOrderRepository>();
        services.AddScoped<IPickOrderDetailRepository, PickOrderDetailRepository>();
        services.AddScoped<IOutboundOrderRepository, OutboundOrderRepository>();
        services.AddScoped<IOutboundOrderDetailRepository, OutboundOrderDetailRepository>();

        // ---- 业务服务层：接口/实现分离，Scoped ----
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IApiService, ApiService>();
        services.AddScoped<ICloudDiskService, CloudDiskService>();
        services.AddScoped<IFileService, FileService>();

        // WMS 基础数据服务
        services.AddScoped<IWarehouseService, WarehouseService>();
        services.AddScoped<IZoneService, ZoneService>();
        services.AddScoped<IAisleService, AisleService>();
        services.AddScoped<IRackService, RackService>();
        services.AddScoped<ILocationService, LocationService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IContainerService, ContainerService>();
        // WMS 业务单据服务
        services.AddScoped<IReceiveOrderService, ReceiveOrderService>();
        services.AddScoped<IInboundOrderService, InboundOrderService>();
        services.AddScoped<IPutawayOrderService, PutawayOrderService>();
        services.AddScoped<ITakeDownOrderService, TakeDownOrderService>();
        services.AddScoped<IPickOrderService, PickOrderService>();
        services.AddScoped<IOutboundOrderService, OutboundOrderService>();

        return services;
    }
}

