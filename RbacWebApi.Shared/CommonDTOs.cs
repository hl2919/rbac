using System.ComponentModel.DataAnnotations;

namespace RbacWebApi.DTOs;

/// <summary>
/// 分页请求基础参数：页码 + 每页条数（固定字段）
/// </summary>
public class PageRequest
{
    /// <summary>
    /// 页码，从 1 开始，默认 1
    /// </summary>
    public int PageIndex { get; set; } = 1;

    /// <summary>
    /// 每页数据条数，默认 20，最大 100
    /// </summary>
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// 带关键词的分页请求基类
/// </summary>
public class PageKeyRequest : PageRequest
{
    public string? Keyword { get; set; }
}

/// <summary>
/// 分页响应结果
/// </summary>
/// <typeparam name="T">列表元素类型</typeparam>
public class PageResponse<T>
{
    /// <summary>
    /// 当前页码
    /// </summary>
    public int PageIndex { get; set; }

    /// <summary>
    /// 每页条数
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 总条数
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// 总页数
    /// </summary>
    public int Pages => PageSize <= 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);

    /// <summary>
    /// 当前页数据
    /// </summary>
    public List<T> Items { get; set; } = [];
}

public class LoginRequest
{
    [Required(ErrorMessage = "用户名不能为空")]
    public string Username { get; set; }

    [Required(ErrorMessage = "密码不能为空")]
    public string Password { get; set; }
}

public class LoginResponse
{
    public string Token { get; set; }
    public string UserId { get; set; }
    public string Username { get; set; }
    public string? Nickname { get; set; }
    public List<string> Roles { get; set; } = [];
    public DateTime ExpiresAt { get; set; }
}

public class RegisterRequest
{
    [Required(ErrorMessage = "用户名不能为空")]
    [MaxLength(50, ErrorMessage = "用户名长度不能超过50字符")]
    public string Username { get; set; }

    [Required(ErrorMessage = "密码不能为空")]
    [MinLength(6, ErrorMessage = "密码长度至少6位")]
    public string Password { get; set; }

    [MaxLength(50, ErrorMessage = "昵称长度不能超过50字符")]
    public string? Nickname { get; set; }

    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    public string? Email { get; set; }

    [Phone(ErrorMessage = "手机号格式不正确")]
    public string? Phone { get; set; }
}

public class ApiResponse<T>
{
    public int Code { get; set; }
    public string Message { get; set; }
    public T? Data { get; set; }

    public static ApiResponse<T> Success(T? data = default, string message = "操作成功")
    {
        return new ApiResponse<T> { Code = 200, Message = message, Data = data };
    }

    public static ApiResponse<T> Fail(string message, int code = 400)
    {
        return new ApiResponse<T> { Code = code, Message = message, Data = default };
    }

    public static ApiResponse<T> Unauthorized(string message = "未授权")
    {
        return new ApiResponse<T> { Code = 401, Message = message, Data = default };
    }

    public static ApiResponse<T> Forbidden(string message = "无权限访问")
    {
        return new ApiResponse<T> { Code = 403, Message = message, Data = default };
    }
}
