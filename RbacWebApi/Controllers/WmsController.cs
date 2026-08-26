using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RbacWebApi.DTOs;
using RbacWebApi.Models;
using RbacWebApi.Models.Wms;
using RbacWebApi.Services.Wms;

namespace RbacWebApi.Controllers;

// ============================================================
//  WMS 控制器：基础数据 + 业务单据
//  - WmsBaseController<T>：通用 CRUD（list / {id} / POST / PUT / DELETE）
//  - WmsDocumentController<TMaster,TDetail>：在基础 CRUD 之上增加明细查询
//  - 13 个具体控制器：仅声明路由 + 注入对应服务
// ============================================================

[ApiController]
[Authorize]
public abstract class WmsBaseController<TEntity> : ControllerBase
    where TEntity : BaseEntity, new()
{
    private readonly IWmsBaseService<TEntity> _service;
    protected WmsBaseController(IWmsBaseService<TEntity> service) => _service = service;

    /// <summary>分页查询：PageIndex/PageSize + 可选 Keyword/Status/ParentId</summary>
    [HttpGet("list")]
    public async Task<ActionResult<ApiResponse<WmsListResponse<TEntity>>>> GetList([FromQuery] WmsQueryRequest request)
    {
        var list = await _service.GetListAsync(request);
        return Ok(ApiResponse<WmsListResponse<TEntity>>.Success(list));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<TEntity>>> GetById(string id)
    {
        var entity = await _service.GetByIdAsync(id);
        if (entity == null) return Ok(ApiResponse<TEntity>.Fail("记录不存在", 404));
        return Ok(ApiResponse<TEntity>.Success(entity));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<string>>> Create([FromBody] TEntity entity)
    {
        var result = await _service.CreateAsync(entity);
        if (!result.Success) return Ok(ApiResponse<string>.Fail(result.Message));
        return Ok(ApiResponse<string>.Success(result.Message));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<string>>> Update(string id, [FromBody] TEntity entity)
    {
        entity.Id = id;
        var result = await _service.UpdateAsync(entity);
        if (!result.Success) return Ok(ApiResponse<string>.Fail(result.Message));
        return Ok(ApiResponse<string>.Success(result.Message));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<string>>> Delete(string id)
    {
        var result = await _service.DeleteAsync(id);
        if (!result.Success) return Ok(ApiResponse<string>.Fail(result.Message));
        return Ok(ApiResponse<string>.Success(result.Message));
    }
}

/// <summary>业务单据控制器基类：基础 CRUD + 带明细查询</summary>
public abstract class WmsDocumentController<TMaster, TDetail> : WmsBaseController<TMaster>
    where TMaster : BaseEntity, new()
    where TDetail : WmsDocumentDetailBase, new()
{
    private readonly IWmsDocumentService<TMaster, TDetail> _docService;
    protected WmsDocumentController(IWmsDocumentService<TMaster, TDetail> service) : base(service)
        => _docService = service;

    /// <summary>按 Id 查询主表 + 明细</summary>
    [HttpGet("full/{id}")]
    public async Task<ActionResult<ApiResponse<TMaster>>> GetWithDetails(string id)
    {
        var master = await _docService.GetWithDetailsAsync(id);
        if (master == null) return Ok(ApiResponse<TMaster>.Fail("记录不存在", 404));
        return Ok(ApiResponse<TMaster>.Success(master));
    }

    /// <summary>按主表 Id 查询明细列表</summary>
    [HttpGet("{id}/details")]
    public async Task<ActionResult<ApiResponse<List<TDetail>>>> GetDetails(string id)
    {
        var list = await _docService.GetDetailsAsync(id);
        return Ok(ApiResponse<List<TDetail>>.Success(list));
    }
}

// ============================================================
//  7 个基础数据控制器
// ============================================================

[Route("api/warehouse")]
public class WarehouseController : WmsBaseController<Warehouse>
{
    public WarehouseController(IWarehouseService service) : base(service) { }
}

[Route("api/zone")]
public class ZoneController : WmsBaseController<Zone>
{
    public ZoneController(IZoneService service) : base(service) { }
}

[Route("api/aisle")]
public class AisleController : WmsBaseController<Aisle>
{
    public AisleController(IAisleService service) : base(service) { }
}

[Route("api/rack")]
public class RackController : WmsBaseController<Rack>
{
    public RackController(IRackService service) : base(service) { }
}

[Route("api/location")]
public class LocationController : WmsBaseController<Location>
{
    public LocationController(ILocationService service) : base(service) { }
}

[Route("api/product")]
public class ProductController : WmsBaseController<Product>
{
    public ProductController(IProductService service) : base(service) { }
}

[Route("api/container")]
public class ContainerController : WmsBaseController<Container>
{
    public ContainerController(IContainerService service) : base(service) { }
}

// ============================================================
//  6 个业务单据控制器
// ============================================================

[Route("api/receive-order")]
public class ReceiveOrderController : WmsDocumentController<ReceiveOrder, ReceiveOrderDetail>
{
    public ReceiveOrderController(IReceiveOrderService service) : base(service) { }
}

[Route("api/inbound-order")]
public class InboundOrderController : WmsDocumentController<InboundOrder, InboundOrderDetail>
{
    public InboundOrderController(IInboundOrderService service) : base(service) { }
}

[Route("api/putaway-order")]
public class PutawayOrderController : WmsDocumentController<PutawayOrder, PutawayOrderDetail>
{
    public PutawayOrderController(IPutawayOrderService service) : base(service) { }
}

[Route("api/takedown-order")]
public class TakeDownOrderController : WmsDocumentController<TakeDownOrder, TakeDownOrderDetail>
{
    public TakeDownOrderController(ITakeDownOrderService service) : base(service) { }
}

[Route("api/pick-order")]
public class PickOrderController : WmsDocumentController<PickOrder, PickOrderDetail>
{
    public PickOrderController(IPickOrderService service) : base(service) { }
}

[Route("api/outbound-order")]
public class OutboundOrderController : WmsDocumentController<OutboundOrder, OutboundOrderDetail>
{
    public OutboundOrderController(IOutboundOrderService service) : base(service) { }
}
