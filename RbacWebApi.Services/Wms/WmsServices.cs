using System.Linq.Expressions;
using RbacWebApi.DTOs;
using RbacWebApi.Models.Wms;
using RbacWebApi.Repositories;
using SqlSugar;

namespace RbacWebApi.Services.Wms;

// ============================================================
//  通用 WMS 基础服务实现：基于泛型仓储提供标准 CRUD。
//  具体子类只需提供仓储注入和按关键词/ParentId 的筛选表达式。
// ============================================================

public abstract class WmsBaseService<TEntity> : IWmsBaseService<TEntity>
    where TEntity : Models.BaseEntity, new()
{
    protected readonly IBaseRepository<TEntity> Repo;
    protected WmsBaseService(IBaseRepository<TEntity> repo) => Repo = repo;

    /// <summary>子类提供关键词筛选（默认按 Id/Code/Name 模糊匹配；可重写）</summary>
    protected virtual Expression<Func<TEntity, bool>> BuildKeywordPredicate(string? keyword) => _ => true;

    /// <summary>子类提供 ParentId 筛选（库区按仓库等），默认不过滤</summary>
    protected virtual Expression<Func<TEntity, bool>> BuildParentPredicate(string? parentId) => _ => true;

    /// <summary>子类提供 Status 筛选，默认不过滤</summary>
    protected virtual Expression<Func<TEntity, bool>> BuildStatusPredicate(int? status) => _ => true;

    public virtual async Task<WmsListResponse<TEntity>> GetListAsync(WmsQueryRequest request)
    {
        var predicate = CombinePredicates(
            BuildKeywordPredicate(request.Keyword),
            BuildParentPredicate(request.ParentId),
            BuildStatusPredicate(request.Status));

        var page = await Repo.GetPagedListAsync(predicate, request, e => e.CreateTime, OrderByType.Desc);
        return new WmsListResponse<TEntity> { Items = page.Items, Total = page.Total };
    }

    /// <summary>把多个 lambda 谓词用逻辑与合并为一个（参数重绑定）</summary>
    private static Expression<Func<TEntity, bool>> CombinePredicates(params Expression<Func<TEntity, bool>>[] predicates)
    {
        if (predicates is null || predicates.Length == 0)
            return e => true;

        var result = predicates[0];
        for (var i = 1; i < predicates.Length; i++)
            result = AndAlso(result, predicates[i]);
        return result;
    }

    private static Expression<Func<TEntity, bool>> AndAlso(Expression<Func<TEntity, bool>> a, Expression<Func<TEntity, bool>> b)
    {
        var paramA = a.Parameters[0];
        var bBody = new ParameterReplacer(b.Parameters[0], paramA).Visit(b.Body);
        return Expression.Lambda<Func<TEntity, bool>>(Expression.AndAlso(a.Body, bBody), paramA);
    }

    private sealed class ParameterReplacer : ExpressionVisitor
    {
        private readonly ParameterExpression _from;
        private readonly ParameterExpression _to;
        public ParameterReplacer(ParameterExpression from, ParameterExpression to)
        {
            _from = from;
            _to = to;
        }
        protected override Expression VisitParameter(ParameterExpression node)
            => node == _from ? _to : base.VisitParameter(node);
    }

    public virtual Task<TEntity?> GetByIdAsync(string id) => Repo.GetByIdAsync(id);

    public virtual async Task<(bool Success, string Message)> CreateAsync(TEntity entity)
    {
        await Repo.InsertAsync(entity);
        return (true, "创建成功");
    }

    public virtual async Task<(bool Success, string Message)> UpdateAsync(TEntity entity)
    {
        if (!await Repo.AnyAsync(e => e.Id == entity.Id))
            return (false, "记录不存在");
        await Repo.UpdateAsync(entity);
        return (true, "更新成功");
    }

    public virtual async Task<(bool Success, string Message)> DeleteAsync(string id)
    {
        if (!await Repo.AnyAsync(e => e.Id == id))
            return (false, "记录不存在");
        await Repo.DeleteByIdAsync(id);
        return (true, "删除成功");
    }
}

// ============================================================
//  基础数据服务：实现通用 CRUD + 关键词筛选
// ============================================================

public class WarehouseService : WmsBaseService<Warehouse>, IWarehouseService
{
    public WarehouseService(IWarehouseRepository repo) : base(repo) { }

    protected override Expression<Func<Warehouse, bool>> BuildKeywordPredicate(string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return _ => true;
        return w => w.WarehouseCode.Contains(keyword) || w.WarehouseName.Contains(keyword);
    }
}

public class ZoneService : WmsBaseService<Zone>, IZoneService
{
    public ZoneService(IZoneRepository repo) : base(repo) { }

    protected override Expression<Func<Zone, bool>> BuildKeywordPredicate(string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return _ => true;
        return z => z.ZoneCode.Contains(keyword) || z.ZoneName.Contains(keyword);
    }

    protected override Expression<Func<Zone, bool>> BuildParentPredicate(string? parentId)
        => string.IsNullOrEmpty(parentId) ? _ => true : z => z.WarehouseId == parentId;
}

public class AisleService : WmsBaseService<Aisle>, IAisleService
{
    public AisleService(IAisleRepository repo) : base(repo) { }

    protected override Expression<Func<Aisle, bool>> BuildKeywordPredicate(string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return _ => true;
        return a => a.AisleCode.Contains(keyword) || a.AisleName.Contains(keyword);
    }

    protected override Expression<Func<Aisle, bool>> BuildParentPredicate(string? parentId)
        => string.IsNullOrEmpty(parentId) ? _ => true : a => a.ZoneId == parentId;
}

public class RackService : WmsBaseService<Rack>, IRackService
{
    public RackService(IRackRepository repo) : base(repo) { }

    protected override Expression<Func<Rack, bool>> BuildKeywordPredicate(string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return _ => true;
        return r => r.RackCode.Contains(keyword) || r.RackName.Contains(keyword);
    }

    protected override Expression<Func<Rack, bool>> BuildParentPredicate(string? parentId)
        => string.IsNullOrEmpty(parentId) ? _ => true : r => r.AisleId == parentId;
}

public class LocationService : WmsBaseService<Location>, ILocationService
{
    public LocationService(ILocationRepository repo) : base(repo) { }

    protected override Expression<Func<Location, bool>> BuildKeywordPredicate(string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return _ => true;
        return l => l.LocationCode.Contains(keyword);
    }

    protected override Expression<Func<Location, bool>> BuildParentPredicate(string? parentId)
        => string.IsNullOrEmpty(parentId) ? _ => true : l => l.RackId == parentId;
}

public class ProductService : WmsBaseService<Product>, IProductService
{
    public ProductService(IProductRepository repo) : base(repo) { }

    protected override Expression<Func<Product, bool>> BuildKeywordPredicate(string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return _ => true;
        return p => p.ProductCode.Contains(keyword)
                    || p.ProductName.Contains(keyword)
                    || (p.Barcode != null && p.Barcode.Contains(keyword));
    }
}

public class ContainerService : WmsBaseService<Container>, IContainerService
{
    public ContainerService(IContainerRepository repo) : base(repo) { }

    protected override Expression<Func<Container, bool>> BuildKeywordPredicate(string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return _ => true;
        return c => c.ContainerCode.Contains(keyword);
    }
}

// ============================================================
//  业务单据服务基类：基础 CRUD + 主表/明细表事务操作
//  - Create：插入主表 + 明细（事务）
//  - Update：更新主表 + 明细全量替换（事务）
//  - Delete：级联删除明细 + 主表（事务）
//  - GetWithDetails：主表 + 明细一起返回
// ============================================================

public abstract class WmsDocumentServiceBase<TMaster, TDetail> : WmsBaseService<TMaster>, IWmsDocumentService<TMaster, TDetail>
    where TMaster : Models.BaseEntity, new()
    where TDetail : WmsDocumentDetailBase, new()
{
    protected readonly IBaseRepository<TDetail> DetailRepo;
    protected WmsDocumentServiceBase(IBaseRepository<TMaster> masterRepo, IBaseRepository<TDetail> detailRepo) : base(masterRepo)
        => DetailRepo = detailRepo;

    /// <summary>从主表读取明细列表（子类返回 master.Details）</summary>
    protected abstract List<TDetail> GetDetails(TMaster master);
    /// <summary>把明细列表写回主表（子类赋值给 master.Details）</summary>
    protected abstract void SetDetails(TMaster master, List<TDetail> details);

    public override async Task<(bool Success, string Message)> CreateAsync(TMaster entity)
    {
        var details = GetDetails(entity) ?? [];
        for (var i = 0; i < details.Count; i++) details[i].LineNo = i + 1;
        await Repo.Client.Ado.UseTranAsync(async () =>
        {
            await Repo.InsertAsync(entity);
            // AOP 已为 master 生成 Id，回填到明细
            foreach (var d in details) d.MasterId = entity.Id;
            if (details.Count > 0) await DetailRepo.InsertRangeAsync(details);
        });
        return (true, "创建成功");
    }

    public override async Task<(bool Success, string Message)> UpdateAsync(TMaster entity)
    {
        if (!await Repo.AnyAsync(e => e.Id == entity.Id))
            return (false, "记录不存在");
        var details = GetDetails(entity) ?? [];
        for (var i = 0; i < details.Count; i++) details[i].LineNo = i + 1;
        await Repo.Client.Ado.UseTranAsync(async () =>
        {
            await Repo.UpdateAsync(entity);
            await DetailRepo.DeleteBatchAsync(d => d.MasterId == entity.Id);
            if (details.Count > 0)
            {
                foreach (var d in details) d.MasterId = entity.Id;
                await DetailRepo.InsertRangeAsync(details);
            }
        });
        return (true, "更新成功");
    }

    public override async Task<(bool Success, string Message)> DeleteAsync(string id)
    {
        if (!await Repo.AnyAsync(e => e.Id == id))
            return (false, "记录不存在");
        await Repo.Client.Ado.UseTranAsync(async () =>
        {
            await DetailRepo.DeleteBatchAsync(d => d.MasterId == id);
            await Repo.DeleteByIdAsync(id);
        });
        return (true, "删除成功");
    }

    public async Task<TMaster?> GetWithDetailsAsync(string id)
    {
        var master = await Repo.GetByIdAsync(id);
        if (master == null) return null;
        SetDetails(master, await DetailRepo.GetListAsync(d => d.MasterId == id));
        return master;
    }

    public async Task<List<TDetail>> GetDetailsAsync(string masterId)
        => await DetailRepo.GetListAsync(d => d.MasterId == masterId);
}

// ============================================================
//  6 个业务单据服务实现
// ============================================================

public class ReceiveOrderService : WmsDocumentServiceBase<ReceiveOrder, ReceiveOrderDetail>, IReceiveOrderService
{
    public ReceiveOrderService(IReceiveOrderRepository repo, IReceiveOrderDetailRepository detailRepo) : base(repo, detailRepo) { }

    protected override List<ReceiveOrderDetail> GetDetails(ReceiveOrder master) => master.Details;
    protected override void SetDetails(ReceiveOrder master, List<ReceiveOrderDetail> details) => master.Details = details;

    protected override Expression<Func<ReceiveOrder, bool>> BuildKeywordPredicate(string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return _ => true;
        return r => r.ReceiveNo.Contains(keyword) || (r.Supplier != null && r.Supplier.Contains(keyword));
    }

    protected override Expression<Func<ReceiveOrder, bool>> BuildParentPredicate(string? parentId)
        => string.IsNullOrEmpty(parentId) ? _ => true : r => r.WarehouseId == parentId;
}

public class InboundOrderService : WmsDocumentServiceBase<InboundOrder, InboundOrderDetail>, IInboundOrderService
{
    public InboundOrderService(IInboundOrderRepository repo, IInboundOrderDetailRepository detailRepo) : base(repo, detailRepo) { }

    protected override List<InboundOrderDetail> GetDetails(InboundOrder master) => master.Details;
    protected override void SetDetails(InboundOrder master, List<InboundOrderDetail> details) => master.Details = details;

    protected override Expression<Func<InboundOrder, bool>> BuildKeywordPredicate(string? keyword)
        => string.IsNullOrWhiteSpace(keyword) ? _ => true : r => r.InboundNo.Contains(keyword);

    protected override Expression<Func<InboundOrder, bool>> BuildParentPredicate(string? parentId)
        => string.IsNullOrEmpty(parentId) ? _ => true : r => r.WarehouseId == parentId;
}

public class PutawayOrderService : WmsDocumentServiceBase<PutawayOrder, PutawayOrderDetail>, IPutawayOrderService
{
    public PutawayOrderService(IPutawayOrderRepository repo, IPutawayOrderDetailRepository detailRepo) : base(repo, detailRepo) { }

    protected override List<PutawayOrderDetail> GetDetails(PutawayOrder master) => master.Details;
    protected override void SetDetails(PutawayOrder master, List<PutawayOrderDetail> details) => master.Details = details;

    protected override Expression<Func<PutawayOrder, bool>> BuildKeywordPredicate(string? keyword)
        => string.IsNullOrWhiteSpace(keyword) ? _ => true : r => r.PutawayNo.Contains(keyword);
}

public class TakeDownOrderService : WmsDocumentServiceBase<TakeDownOrder, TakeDownOrderDetail>, ITakeDownOrderService
{
    public TakeDownOrderService(ITakeDownOrderRepository repo, ITakeDownOrderDetailRepository detailRepo) : base(repo, detailRepo) { }

    protected override List<TakeDownOrderDetail> GetDetails(TakeDownOrder master) => master.Details;
    protected override void SetDetails(TakeDownOrder master, List<TakeDownOrderDetail> details) => master.Details = details;

    protected override Expression<Func<TakeDownOrder, bool>> BuildKeywordPredicate(string? keyword)
        => string.IsNullOrWhiteSpace(keyword) ? _ => true : r => r.TakeDownNo.Contains(keyword);
}

public class PickOrderService : WmsDocumentServiceBase<PickOrder, PickOrderDetail>, IPickOrderService
{
    public PickOrderService(IPickOrderRepository repo, IPickOrderDetailRepository detailRepo) : base(repo, detailRepo) { }

    protected override List<PickOrderDetail> GetDetails(PickOrder master) => master.Details;
    protected override void SetDetails(PickOrder master, List<PickOrderDetail> details) => master.Details = details;

    protected override Expression<Func<PickOrder, bool>> BuildKeywordPredicate(string? keyword)
        => string.IsNullOrWhiteSpace(keyword) ? _ => true : r => r.PickNo.Contains(keyword);
}

public class OutboundOrderService : WmsDocumentServiceBase<OutboundOrder, OutboundOrderDetail>, IOutboundOrderService
{
    public OutboundOrderService(IOutboundOrderRepository repo, IOutboundOrderDetailRepository detailRepo) : base(repo, detailRepo) { }

    protected override List<OutboundOrderDetail> GetDetails(OutboundOrder master) => master.Details;
    protected override void SetDetails(OutboundOrder master, List<OutboundOrderDetail> details) => master.Details = details;

    protected override Expression<Func<OutboundOrder, bool>> BuildKeywordPredicate(string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return _ => true;
        return r => r.OutboundNo.Contains(keyword) || (r.Customer != null && r.Customer.Contains(keyword));
    }

    protected override Expression<Func<OutboundOrder, bool>> BuildParentPredicate(string? parentId)
        => string.IsNullOrEmpty(parentId) ? _ => true : r => r.WarehouseId == parentId;
}
