using RbacWebApi.DTOs;
using RbacWebApi.Models;
using RbacWebApi.Models.Wms;

namespace RbacWebApi.Services.Wms;

// ============================================================
//  基础数据服务接口：通用 CRUD（接口签名一致，按类型区分）
//  每个服务提供：分页查询、按 Id 查询、新增、更新、删除
// ============================================================

public interface IWmsBaseService<TEntity> where TEntity : class, new()
{
    Task<WmsListResponse<TEntity>> GetListAsync(WmsQueryRequest request);
    Task<TEntity?> GetByIdAsync(string id);
    Task<(bool Success, string Message)> CreateAsync(TEntity entity);
    Task<(bool Success, string Message)> UpdateAsync(TEntity entity);
    Task<(bool Success, string Message)> DeleteAsync(string id);
}

/// <summary>业务单据服务接口：基础 CRUD + 带明细的查询</summary>
public interface IWmsDocumentService<TMaster, TDetail> : IWmsBaseService<TMaster>
    where TMaster : BaseEntity, new()
    where TDetail : WmsDocumentDetailBase, new()
{
    Task<TMaster?> GetWithDetailsAsync(string id);
    Task<List<TDetail>> GetDetailsAsync(string masterId);
}

// 7 个基础数据服务的具体接口（按类型参数区分）
public interface IWarehouseService : IWmsBaseService<Warehouse> { }
public interface IZoneService : IWmsBaseService<Zone> { }
public interface IAisleService : IWmsBaseService<Aisle> { }
public interface IRackService : IWmsBaseService<Rack> { }
public interface ILocationService : IWmsBaseService<Location> { }
public interface IProductService : IWmsBaseService<Product> { }
public interface IContainerService : IWmsBaseService<Container> { }

// 6 个业务单据服务的具体接口（带明细查询）
public interface IReceiveOrderService : IWmsDocumentService<ReceiveOrder, ReceiveOrderDetail> { }
public interface IInboundOrderService : IWmsDocumentService<InboundOrder, InboundOrderDetail> { }
public interface IPutawayOrderService : IWmsDocumentService<PutawayOrder, PutawayOrderDetail> { }
public interface ITakeDownOrderService : IWmsDocumentService<TakeDownOrder, TakeDownOrderDetail> { }
public interface IPickOrderService : IWmsDocumentService<PickOrder, PickOrderDetail> { }
public interface IOutboundOrderService : IWmsDocumentService<OutboundOrder, OutboundOrderDetail> { }

