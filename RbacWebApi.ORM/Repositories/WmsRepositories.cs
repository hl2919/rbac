using RbacWebApi.Models.Wms;
using RbacWebApi.ORM;

namespace RbacWebApi.Repositories;

// ============================================================
//  WMS 基础数据仓储
// ============================================================

public interface IWarehouseRepository : IBaseRepository<Warehouse> { }
public class WarehouseRepository : BaseRepository<Warehouse>, IWarehouseRepository
{
    public WarehouseRepository(IDbContext dbContext) : base(dbContext) { }
}

public interface IZoneRepository : IBaseRepository<Zone> { }
public class ZoneRepository : BaseRepository<Zone>, IZoneRepository
{
    public ZoneRepository(IDbContext dbContext) : base(dbContext) { }
}

public interface IAisleRepository : IBaseRepository<Aisle> { }
public class AisleRepository : BaseRepository<Aisle>, IAisleRepository
{
    public AisleRepository(IDbContext dbContext) : base(dbContext) { }
}

public interface IRackRepository : IBaseRepository<Rack> { }
public class RackRepository : BaseRepository<Rack>, IRackRepository
{
    public RackRepository(IDbContext dbContext) : base(dbContext) { }
}

public interface ILocationRepository : IBaseRepository<Location> { }
public class LocationRepository : BaseRepository<Location>, ILocationRepository
{
    public LocationRepository(IDbContext dbContext) : base(dbContext) { }
}

public interface IProductRepository : IBaseRepository<Product> { }
public class ProductRepository : BaseRepository<Product>, IProductRepository
{
    public ProductRepository(IDbContext dbContext) : base(dbContext) { }
}

public interface IContainerRepository : IBaseRepository<Container> { }
public class ContainerRepository : BaseRepository<Container>, IContainerRepository
{
    public ContainerRepository(IDbContext dbContext) : base(dbContext) { }
}

// ============================================================
//  WMS 业务单据仓储：主表 + 明细表
// ============================================================

public interface IReceiveOrderRepository : IBaseRepository<ReceiveOrder> { }
public class ReceiveOrderRepository : BaseRepository<ReceiveOrder>, IReceiveOrderRepository
{
    public ReceiveOrderRepository(IDbContext dbContext) : base(dbContext) { }
}

public interface IReceiveOrderDetailRepository : IBaseRepository<ReceiveOrderDetail> { }
public class ReceiveOrderDetailRepository : BaseRepository<ReceiveOrderDetail>, IReceiveOrderDetailRepository
{
    public ReceiveOrderDetailRepository(IDbContext dbContext) : base(dbContext) { }
}

public interface IInboundOrderRepository : IBaseRepository<InboundOrder> { }
public class InboundOrderRepository : BaseRepository<InboundOrder>, IInboundOrderRepository
{
    public InboundOrderRepository(IDbContext dbContext) : base(dbContext) { }
}

public interface IInboundOrderDetailRepository : IBaseRepository<InboundOrderDetail> { }
public class InboundOrderDetailRepository : BaseRepository<InboundOrderDetail>, IInboundOrderDetailRepository
{
    public InboundOrderDetailRepository(IDbContext dbContext) : base(dbContext) { }
}

public interface IPutawayOrderRepository : IBaseRepository<PutawayOrder> { }
public class PutawayOrderRepository : BaseRepository<PutawayOrder>, IPutawayOrderRepository
{
    public PutawayOrderRepository(IDbContext dbContext) : base(dbContext) { }
}

public interface IPutawayOrderDetailRepository : IBaseRepository<PutawayOrderDetail> { }
public class PutawayOrderDetailRepository : BaseRepository<PutawayOrderDetail>, IPutawayOrderDetailRepository
{
    public PutawayOrderDetailRepository(IDbContext dbContext) : base(dbContext) { }
}

public interface ITakeDownOrderRepository : IBaseRepository<TakeDownOrder> { }
public class TakeDownOrderRepository : BaseRepository<TakeDownOrder>, ITakeDownOrderRepository
{
    public TakeDownOrderRepository(IDbContext dbContext) : base(dbContext) { }
}

public interface ITakeDownOrderDetailRepository : IBaseRepository<TakeDownOrderDetail> { }
public class TakeDownOrderDetailRepository : BaseRepository<TakeDownOrderDetail>, ITakeDownOrderDetailRepository
{
    public TakeDownOrderDetailRepository(IDbContext dbContext) : base(dbContext) { }
}

public interface IPickOrderRepository : IBaseRepository<PickOrder> { }
public class PickOrderRepository : BaseRepository<PickOrder>, IPickOrderRepository
{
    public PickOrderRepository(IDbContext dbContext) : base(dbContext) { }
}

public interface IPickOrderDetailRepository : IBaseRepository<PickOrderDetail> { }
public class PickOrderDetailRepository : BaseRepository<PickOrderDetail>, IPickOrderDetailRepository
{
    public PickOrderDetailRepository(IDbContext dbContext) : base(dbContext) { }
}

public interface IOutboundOrderRepository : IBaseRepository<OutboundOrder> { }
public class OutboundOrderRepository : BaseRepository<OutboundOrder>, IOutboundOrderRepository
{
    public OutboundOrderRepository(IDbContext dbContext) : base(dbContext) { }
}

public interface IOutboundOrderDetailRepository : IBaseRepository<OutboundOrderDetail> { }
public class OutboundOrderDetailRepository : BaseRepository<OutboundOrderDetail>, IOutboundOrderDetailRepository
{
    public OutboundOrderDetailRepository(IDbContext dbContext) : base(dbContext) { }
}
