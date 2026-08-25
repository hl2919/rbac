using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using RbacWebApi;
using RbacWebApi.Middleware;
using RbacWebApi.ORM;
using RbacWebApi.Services;

var builder = WebApplication.CreateBuilder(args);

// -------- 1. 配置选项 --------
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.Configure<RbacWebApi.DTOs.FileStorageSettings>(builder.Configuration.GetSection("FileStorage"));

// -------- 2. 注册核心服务 --------
builder.Services.AddSystemService();

// -------- 3. 控制器 --------
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// -------- 4. OpenAPI（.NET 10 内置 OpenAPI 支持） --------
builder.Services.AddOpenApi();

// -------- 5. JWT 身份认证 --------
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>() ?? new JwtSettings();
var key = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// -------- 6. CORS（开发友好配置） --------
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ======================================================
// ==== 构建应用 & 配置中间件管道 ====
// ======================================================
var app = builder.Build();

// -------- 7. 启动时初始化数据库 & 种子数据（幂等补齐） --------
if (builder.Configuration.GetSection("InitDb").Get<int>() == 0)
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<IDbContext>();
        db.InitializeDatabase();
        db.SeedInitialData();
        Console.WriteLine("[Startup] 数据库初始化与种子数据补齐完成");
    }

// -------- 8. 开发环境中间件 --------
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.MapOpenApi();
}

// -------- 9. 中间件顺序（非常重要，不要改顺序） --------
// 顺序说明：
//   1. UseRouting        -> 解析路由
//   2. UseCors           -> 跨域
//   3. UseAuthentication -> 解析 JWT，填充 context.User
//   4. UseAuthorization  -> 处理 [Authorize] / [AllowAnonymous]
//   5. RbacPermissionMiddleware -> 基于数据库的 RBAC 接口权限校验
//   6. MapControllers    -> 执行 Controller 动作
app.UseRouting();
app.UseCors("AllowAll");

// ✅ 认证必须在授权之前；RBAC 中间件必须在认证授权之后
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<RbacPermissionMiddleware>();

app.MapControllers();

app.Run();
