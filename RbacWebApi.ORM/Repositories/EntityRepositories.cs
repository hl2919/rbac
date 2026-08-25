using RbacWebApi.Models;
using RbacWebApi.ORM;

namespace RbacWebApi.Repositories;

/// <summary>
/// 用户仓储接口：继承泛型仓储的基础能力，需要扩展用户特有查询时在此处添加
/// </summary>
public interface IUserRepository : IBaseRepository<SysUser>
{
}

/// <summary>
/// 用户仓储实现
/// </summary>
public class UserRepository : BaseRepository<SysUser>, IUserRepository
{
    public UserRepository(IDbContext dbContext) : base(dbContext) { }
}

/// <summary>
/// 角色仓储接口
/// </summary>
public interface IRoleRepository : IBaseRepository<SysRole>
{
}

public class RoleRepository : BaseRepository<SysRole>, IRoleRepository
{
    public RoleRepository(IDbContext dbContext) : base(dbContext) { }
}

/// <summary>
/// API 接口资源仓储接口
/// </summary>
public interface IApiRepository : IBaseRepository<SysApi>
{
}

public class ApiRepository : BaseRepository<SysApi>, IApiRepository
{
    public ApiRepository(IDbContext dbContext) : base(dbContext) { }
}

/// <summary>
/// 用户-角色关联仓储接口
/// </summary>
public interface IUserRoleRepository : IBaseRepository<SysUserRole>
{
}

public class UserRoleRepository : BaseRepository<SysUserRole>, IUserRoleRepository
{
    public UserRoleRepository(IDbContext dbContext) : base(dbContext) { }
}

/// <summary>
/// 角色-API权限关联仓储接口
/// </summary>
public interface IRoleApiRepository : IBaseRepository<SysRoleApi>
{
}

public class RoleApiRepository : BaseRepository<SysRoleApi>, IRoleApiRepository
{
    public RoleApiRepository(IDbContext dbContext) : base(dbContext) { }
}
