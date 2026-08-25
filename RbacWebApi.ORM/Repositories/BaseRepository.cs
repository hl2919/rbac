using System.Linq.Expressions;
using RbacWebApi.DTOs;
using RbacWebApi.Models;
using RbacWebApi.ORM;
using SqlSugar;

namespace RbacWebApi.Repositories;

/// <summary>
/// 泛型仓储基类：统一封装增删改查/分页，减少业务层重复代码
/// 注：SqlSugar 的 Insertable/Updateable/Deleteable 要求 T 具备无参构造函数
/// </summary>
public class BaseRepository<T> : IBaseRepository<T> where T : BaseEntity, new()
{
    protected readonly IDbContext DbContext;
    public ISqlSugarClient Client => DbContext.Client;

    public BaseRepository(IDbContext dbContext)
    {
        DbContext = dbContext;
    }

    public virtual async Task<T?> GetByIdAsync(string id)
    {
        var ret = await Client.Queryable<T>().InSingleAsync(id);
        return ret;
    }

    public virtual Task<List<T>> GetAllAsync()
    {
        return Client.Queryable<T>()
            .OrderBy(e => e.CreateTime, OrderByType.Desc)
            .ToListAsync();
    }

    public virtual Task<List<T>> GetListAsync(Expression<Func<T, bool>> predicate)
    {
        return Client.Queryable<T>()
            .Where(predicate)
            .OrderBy(e => e.CreateTime, OrderByType.Desc)
            .ToListAsync();
    }

    /// <summary>分页查询：默认 CreateTime 降序</summary>
    public virtual async Task<PageResponse<T>> GetPagedListAsync(
        Expression<Func<T, bool>> predicate,
        PageRequest page)
    {
        var safe = NormalizePage(page);
        var query = Client.Queryable<T>().Where(predicate).OrderBy(e => e.CreateTime, OrderByType.Desc);
        var total = await query.CountAsync();
        var items = total > 0
            ? await query.Skip((safe.PageIndex - 1) * safe.PageSize).Take(safe.PageSize).ToListAsync()
            : [];
        return new PageResponse<T>
        {
            PageIndex = safe.PageIndex,
            PageSize = safe.PageSize,
            Total = (int)total,
            Items = items
        };
    }

    /// <summary>分页查询：自定义排序字段（SqlSugar 要求 Expression 返回 object）</summary>
    public virtual async Task<PageResponse<T>> GetPagedListAsync(
        Expression<Func<T, bool>> predicate,
        PageRequest page,
        Expression<Func<T, object>> orderBy,
        OrderByType orderType = OrderByType.Desc)
    {
        var safe = NormalizePage(page);
        var query = Client.Queryable<T>()
            .Where(predicate)
            .OrderBy(orderBy, orderType);
        var total = await query.CountAsync();
        var items = total > 0
            ? await query.Skip((safe.PageIndex - 1) * safe.PageSize).Take(safe.PageSize).ToListAsync()
            : [];
        return new PageResponse<T>
        {
            PageIndex = safe.PageIndex,
            PageSize = safe.PageSize,
            Total = (int)total,
            Items = items
        };
    }

    public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
    {
        var ret = await Client.Queryable<T>().FirstAsync(predicate);
        return ret;
    }

    public virtual Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
    {
        return Client.Queryable<T>().AnyAsync(predicate);
    }

    public virtual Task<int> InsertAsync(T entity)
    {
        return Client.Insertable(entity).ExecuteCommandAsync();
    }

    public virtual Task<int> InsertRangeAsync(List<T> entities)
    {
        if (entities.Count == 0) return Task.FromResult(0);
        return Client.Insertable(entities).ExecuteCommandAsync();
    }

    public virtual Task<int> UpdateAsync(T entity)
    {
        return Client.Updateable(entity).ExecuteCommandAsync();
    }

    public virtual Task<int> DeleteAsync(T entity)
    {
        return Client.Deleteable(entity).ExecuteCommandAsync();
    }

    public virtual Task<int> DeleteByIdAsync(string id)
    {
        return Client.Deleteable<T>().In(id).ExecuteCommandAsync();
    }

    public virtual Task<int> DeleteBatchAsync(Expression<Func<T, bool>> predicate)
    {
        return Client.Deleteable<T>().Where(predicate).ExecuteCommandAsync();
    }

    private static PageRequest NormalizePage(PageRequest page)
    {
        if (page.PageIndex <= 0) page.PageIndex = 1;
        if (page.PageSize <= 0) page.PageSize = 20;
        if (page.PageSize > 100) page.PageSize = 100;
        return page;
    }
}
