using System.Text.Json.Serialization;

namespace SphereBlog.API.Models;

public class ApiResponse<T>
{
    public string Status { get; set; } = string.Empty;
    public int Code { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T? Data { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Reference { get; set; }

    public static ApiResponse<T> Success(T data, int code = 200) => new()
    {
        Status = "success",
        Code = code,
        Data = data
    };

    public static ApiResponse<object> Error(string message, int code, string? reference = null) => new()
    {
        Status = "error",
        Code = code,
        Message = message,
        Reference = reference
    };
}

public class PagedData<T>
{
    public IEnumerable<T> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}
