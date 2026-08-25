using System.Linq.Expressions;
using RbacWebApi.DTOs;
using RbacWebApi.Models;
using RbacWebApi.Services;
using SqlSugar;

namespace RbacWebApi.Repositories;

/// <summary>
/// 泛型仓储基接口：提供实体的标准增删改查和分页查询能力
/// </summary>
/// <typeparam name="T">实体类型，需继承 BaseEntity 且有默认构造函数</typeparam>
public interface IBaseRepository<T> where T : BaseEntity, new()
{
    /// <summary>
    /// 直接访问底层 SqlSugar 客户端（复杂联表查询时使用）
    /// </summary>
    ISqlSugarClient Client { get; }

    Task<T?> GetByIdAsync(string id);
    Task<List<T>> GetAllAsync();
    Task<List<T>> GetListAsync(Expression<Func<T, bool>> predicate);

    /// <summary>分页查询：默认 CreateTime 降序</summary>
    Task<PageResponse<T>> GetPagedListAsync(
        Expression<Func<T, bool>> predicate,
        PageRequest page);

    /// <summary>分页查询：自定义排序字段（字段表达式返回 object）</summary>
    Task<PageResponse<T>> GetPagedListAsync(
        Expression<Func<T, bool>> predicate,
        PageRequest page,
        Expression<Func<T, object>> orderBy,
        OrderByType orderType = OrderByType.Desc);

    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
    Task<int> InsertAsync(T entity);
    Task<int> InsertRangeAsync(List<T> entities);
    Task<int> UpdateAsync(T entity);
    Task<int> DeleteAsync(T entity);
    Task<int> DeleteByIdAsync(string id);
    Task<int> DeleteBatchAsync(Expression<Func<T, bool>> predicate);
}
