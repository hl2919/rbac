using NUlid;
using SqlSugar;
using Microsoft.Extensions.Configuration;
using RbacWebApi.Models;
using RbacWebApi.Models.Cloud;

namespace RbacWebApi.ORM;

public interface IDbContext
{
    ISqlSugarClient Client { get; }
    void InitializeDatabase();
    void SeedInitialData();
}

public class DbContext : IDbContext
{
    private readonly IConfiguration _configuration;
    public ISqlSugarClient Client { get; private set; }

    public DbContext(IConfiguration configuration)
    {
        _configuration = configuration;
        var connectionString = _configuration.GetConnectionString("DefaultConnection");

        Client = new SqlSugarClient(new ConnectionConfig()
        {
            ConnectionString = connectionString,
            DbType = DbType.Sqlite,
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute
        },
        db =>
        {
            // SQL 日志
            db.Aop.OnLogExecuting = (sql, pars) =>
            {
                Console.WriteLine($"[SQL] {sql}");
            };

            // ============================================================
            //  DataExecuting AOP：自动填充 Id(Ulid) / CreateTime / LastUpdateTime
            //  - Insert：Id 为空时生成 Ulid，CreateTime 填当前时间
            //  - Update：LastUpdateTime 填当前时间
            //  注意：无实体更新模式（Updateable<Dictionary> 等）不会触发此 AOP，
            //        那种场景需人工写入 last_update_time 字段。
            // ============================================================
            db.Aop.DataExecuting = (oldValue, entityInfo) =>
            {
                // ---- 插入 ----
                if (entityInfo.OperationType == DataFilterType.InsertByObject)
                {
                    // 主键 Id：为空时自动生成 Ulid
                    if (entityInfo.PropertyName == "Id")
                    {
                        var currentId = oldValue?.ToString();
                        if (string.IsNullOrEmpty(currentId))
                        {
                            entityInfo.SetValue(Ulid.NewUlid().ToString());
                        }
                    }

                    // CreateTime、LastUpdateTime：为默认值时自动填充
                    if (entityInfo.PropertyName == "CreateTime" || entityInfo.PropertyName == "LastUpdateTime")
                    {
                        if (oldValue == null || (DateTimeOffset)oldValue == default(DateTimeOffset))
                        {
                            entityInfo.SetValue(DateTimeOffset.Now);
                        }
                    }
                }

                // ---- 更新 ----
                if (entityInfo.OperationType == DataFilterType.UpdateByObject)
                {
                    // LastUpdateTime：更新时自动填充
                    if (entityInfo.PropertyName == "LastUpdateTime")
                    {
                        entityInfo.SetValue(DateTimeOffset.Now);
                    }
                }
            };
        });
    }

    public void InitializeDatabase()
    {
        Client.CodeFirst.InitTables(
            typeof(SysUser),
            typeof(SysRole),
            typeof(SysUserRole),
            typeof(SysApi),
            typeof(SysRoleApi),
            typeof(SysFile),
            typeof(UserCloudDisk)
        );
    }

    public void SeedInitialData()
    {
        // 1. 幂等补齐角色表
        if (!Client.Queryable<SysRole>().Any())
        {
            Client.Insertable(new List<SysRole>
            {
                new() { RoleName = "超级管理员", RoleCode = "SUPER_ADMIN", Description = "拥有所有权限", Status = 1 },
                new() { RoleName = "管理员", RoleCode = "ADMIN", Description = "管理员角色", Status = 1 },
                new() { RoleName = "普通用户", RoleCode = "USER", Description = "普通用户角色", Status = 1 }
            }).ExecuteCommand();
        }
        else
        {
            EnsureRoleExists("SUPER_ADMIN", "超级管理员", "拥有所有权限");
            EnsureRoleExists("ADMIN", "管理员", "管理员角色");
            EnsureRoleExists("USER", "普通用户", "普通用户角色");
        }

        // 2. 幂等补齐API接口表
        var defaultApis = new List<(string Url, string Method, string Name, bool NeedAuth)>
        {
            ("/api/auth/login", "POST", "用户登录", false),
            ("/api/auth/register", "POST", "用户注册", false),
            ("/api/auth/me", "GET", "获取当前用户信息", true),
            ("/api/user/list", "GET", "获取用户列表", true),
            ("/api/user/{id}", "GET", "获取用户详情", true),
            ("/api/user", "POST", "新增用户", true),
            ("/api/user/{id}", "PUT", "修改用户", true),
            ("/api/user/{id}", "DELETE", "删除用户", true),
            ("/api/role/list", "GET", "获取角色列表", true),
            ("/api/role", "POST", "新增角色", true),
            ("/api/role/{id}", "PUT", "修改角色", true),
            ("/api/role/{id}", "DELETE", "删除角色", true),
            ("/api/role/{roleId}/assign/{userId}", "POST", "分配用户角色", true),
            ("/api/role/{roleId}/permissions", "GET", "获取角色API权限", true),
            ("/api/role/{roleId}/permissions", "PUT", "设置角色API权限", true),
            ("/api/api/list", "GET", "获取API列表", true),
            ("/api/api", "POST", "新增API", true),
            ("/api/api/{id}", "PUT", "修改API", true),
            ("/api/api/{id}", "DELETE", "删除API", true),
            ("/api/test/public", "GET", "公开测试接口", false),
            ("/api/test/authorized", "GET", "登录测试接口", true),
            ("/api/test/admin", "GET", "管理员测试接口", true),
            ("/api/test/superadmin", "GET", "超管测试接口", true)
        };

        foreach (var api in defaultApis)
        {
            EnsureApiExists(api.Url, api.Method, api.Name, api.NeedAuth);
        }

        // 3. 初始化超管用户（AOP 自动填充 Id / CreateTime）
        var superAdminRole = Client.Queryable<SysRole>().First(r => r.RoleCode == "SUPER_ADMIN");
        var adminRole = Client.Queryable<SysRole>().First(r => r.RoleCode == "ADMIN");
        var userRole = Client.Queryable<SysRole>().First(r => r.RoleCode == "USER");

        if (!Client.Queryable<SysUser>().Any(u => u.Username == "superadmin"))
        {
            var user = new SysUser
            {
                Username = "superadmin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                Nickname = "超级管理员",
                Email = "superadmin@example.com",
                Status = 1
            };
            Client.Insertable(user).ExecuteCommand();
            // AOP 已将 user.Id 填充为 Ulid
            Client.Insertable(new SysUserRole { UserId = user.Id, RoleId = superAdminRole.Id }).ExecuteCommand();
        }

        if (!Client.Queryable<SysUser>().Any(u => u.Username == "admin"))
        {
            var user = new SysUser
            {
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                Nickname = "管理员",
                Email = "admin@example.com",
                Status = 1
            };
            Client.Insertable(user).ExecuteCommand();
            Client.Insertable(new SysUserRole { UserId = user.Id, RoleId = adminRole.Id }).ExecuteCommand();
        }

        if (!Client.Queryable<SysUser>().Any(u => u.Username == "user"))
        {
            var user = new SysUser
            {
                Username = "user",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                Nickname = "普通用户",
                Email = "user@example.com",
                Status = 1
            };
            Client.Insertable(user).ExecuteCommand();
            Client.Insertable(new SysUserRole { UserId = user.Id, RoleId = userRole.Id }).ExecuteCommand();
        }

        // 4. 幂等补齐角色API权限关联（按角色分别补齐）
        var allApis = Client.Queryable<SysApi>().ToList();
        EnsureRoleApis(superAdminRole.Id, allApis.Select(a => a.Id).ToList());

        var adminApiNames = new[]
        {
            "用户登录", "用户注册", "获取当前用户信息", "获取用户列表", "获取用户详情",
            "获取角色列表", "获取角色API权限", "获取API列表",
            "公开测试接口", "登录测试接口", "管理员测试接口"
        };
        var adminApiIds = allApis.Where(a => adminApiNames.Contains(a.ApiName)).Select(a => a.Id).ToList();
        EnsureRoleApis(adminRole.Id, adminApiIds);

        var userApiNames = new[]
        {
            "用户登录", "用户注册", "获取当前用户信息",
            "公开测试接口", "登录测试接口"
        };
        var userApiIds = allApis.Where(a => userApiNames.Contains(a.ApiName)).Select(a => a.Id).ToList();
        EnsureRoleApis(userRole.Id, userApiIds);
    }

    private void EnsureRoleExists(string roleCode, string roleName, string description)
    {
        var role = Client.Queryable<SysRole>().First(r => r.RoleCode == roleCode);
        if (role == null)
        {
            Client.Insertable(new SysRole
            {
                RoleName = roleName,
                RoleCode = roleCode,
                Description = description,
                Status = 1
            }).ExecuteCommand();
        }
    }

    private void EnsureApiExists(string apiUrl, string method, string apiName, bool needAuth)
    {
        var api = Client.Queryable<SysApi>().First(a => a.ApiUrl == apiUrl && a.RequestMethod == method);
        if (api == null)
        {
            Client.Insertable(new SysApi
            {
                ApiUrl = apiUrl,
                RequestMethod = method,
                ApiName = apiName,
                NeedAuth = needAuth
            }).ExecuteCommand();
        }
    }

    private void EnsureRoleApis(string roleId, List<string> apiIds)
    {
        var existing = Client.Queryable<SysRoleApi>().Where(r => r.RoleId == roleId).Select(r => r.ApiId).ToList().ToHashSet();
        var toInsert = apiIds.Where(id => !existing.Contains(id)).Select(id => new SysRoleApi { RoleId = roleId, ApiId = id }).ToList();
        if (toInsert.Any())
        {
            Client.Insertable(toInsert).ExecuteCommand();
        }
    }
}
