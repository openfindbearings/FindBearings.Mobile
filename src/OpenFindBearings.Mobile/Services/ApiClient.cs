using System.Net.Http.Json;
using System.Text.Json;

namespace OpenFindBearings.Mobile.Services;

/// <summary>
/// 调用后端 API 的 HTTP 客户端封装
/// 所有请求走 K8s 内部 Service，不经公网
/// </summary>
public class ApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ApiClient> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiClient(IHttpClientFactory httpClientFactory, ILogger<ApiClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// GET 请求（公开接口，不带 JWT）
    /// </summary>
    public async Task<T?> GetAsync<T>(string path, CancellationToken ct = default) where T : class
    {
        try
        {
            var client = _httpClientFactory.CreateClient("Api");
            var response = await client.GetAsync(path, ct);
            response.EnsureSuccessStatusCode();
            var wrapper = await response.Content.ReadFromJsonAsync<ApiResponseWrapper<T>>(JsonOptions, ct);
            return wrapper?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "API GET {Path} 失败", path);
            return null;
        }
    }

    /// <summary>
    /// GET 请求（需认证接口，带 JWT）
    /// </summary>
    public async Task<T?> GetAsync<T>(string path, string? accessToken, CancellationToken ct = default) where T : class
    {
        try
        {
            var client = _httpClientFactory.CreateClient("Api");
            if (!string.IsNullOrEmpty(accessToken))
                client.DefaultRequestHeaders.Authorization = new("Bearer", accessToken);
            var response = await client.GetAsync(path, ct);
            response.EnsureSuccessStatusCode();
            var wrapper = await response.Content.ReadFromJsonAsync<ApiResponseWrapper<T>>(JsonOptions, ct);
            return wrapper?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "API GET {Path} 失败", path);
            return null;
        }
    }

    /// <summary>
    /// POST 请求
    /// </summary>
    public async Task<T?> PostAsync<T>(string path, object body, string? accessToken = null, CancellationToken ct = default) where T : class
    {
        try
        {
            var client = _httpClientFactory.CreateClient("Api");
            if (!string.IsNullOrEmpty(accessToken))
                client.DefaultRequestHeaders.Authorization = new("Bearer", accessToken);
            var response = await client.PostAsJsonAsync(path, body, ct);
            response.EnsureSuccessStatusCode();
            var wrapper = await response.Content.ReadFromJsonAsync<ApiResponseWrapper<T>>(JsonOptions, ct);
            return wrapper?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "API POST {Path} 失败", path);
            return null;
        }
    }

    /// <summary>
    /// 获取分页数据（公开接口）
    /// </summary>
    public async Task<PagedResult<T>?> GetPagedAsync<T>(string path, CancellationToken ct = default) where T : class
    {
        try
        {
            var client = _httpClientFactory.CreateClient("Api");
            var response = await client.GetAsync(path, ct);
            response.EnsureSuccessStatusCode();
            var wrapper = await response.Content.ReadFromJsonAsync<ApiResponseWrapper<PagedResult<T>>>(JsonOptions, ct);
            return wrapper?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "API GET {Path} 分页失败", path);
            return null;
        }
    }

    /// <summary>
    /// 获取分页数据（需认证接口，带 JWT）
    /// </summary>
    public async Task<PagedResult<T>?> GetPagedAsync<T>(string path, string? accessToken, CancellationToken ct = default) where T : class
    {
        try
        {
            var client = _httpClientFactory.CreateClient("Api");
            if (!string.IsNullOrEmpty(accessToken))
                client.DefaultRequestHeaders.Authorization = new("Bearer", accessToken);
            var response = await client.GetAsync(path, ct);
            response.EnsureSuccessStatusCode();
            var wrapper = await response.Content.ReadFromJsonAsync<ApiResponseWrapper<PagedResult<T>>>(JsonOptions, ct);
            return wrapper?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "API GET {Path} 分页失败", path);
            return null;
        }
    }

    /// <summary>
    /// PUT 请求（需认证，带 JWT），返回响应 data。
    /// 改动说明：个人信息编辑代理需要（API PUT /api/me/profile），照抄 Post 模式。
    /// </summary>
    public async Task<T?> PutAsync<T>(string path, object body, string? accessToken = null, CancellationToken ct = default) where T : class
    {
        try
        {
            var client = _httpClientFactory.CreateClient("Api");
            if (!string.IsNullOrEmpty(accessToken))
                client.DefaultRequestHeaders.Authorization = new("Bearer", accessToken);
            var response = await client.PutAsJsonAsync(path, body, ct);
            response.EnsureSuccessStatusCode();
            var wrapper = await response.Content.ReadFromJsonAsync<ApiResponseWrapper<T>>(JsonOptions, ct);
            return wrapper?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "API PUT {Path} 失败", path);
            return null;
        }
    }

    /// <summary>
    /// POST 无响应数据写操作（收藏/关注/记录浏览等，路径参数即语义），返回是否成功。
    /// </summary>
    public async Task<bool> PostVoidAsync(string path, string? accessToken, CancellationToken ct = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("Api");
            if (!string.IsNullOrEmpty(accessToken))
                client.DefaultRequestHeaders.Authorization = new("Bearer", accessToken);
            var response = await client.PostAsync(path, null, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "API POST {Path} 失败", path);
            return false;
        }
    }

    /// <summary>
    /// DELETE 写操作（取消收藏/取关/删历史），返回是否成功。
    /// </summary>
    public async Task<bool> DeleteVoidAsync(string path, string? accessToken, CancellationToken ct = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("Api");
            if (!string.IsNullOrEmpty(accessToken))
                client.DefaultRequestHeaders.Authorization = new("Bearer", accessToken);
            var response = await client.DeleteAsync(path, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "API DELETE {Path} 失败", path);
            return false;
        }
    }

    /// <summary>
    /// 标准 API 响应包装结构
    /// </summary>
    private record ApiResponseWrapper<T>(bool Success, int Code, T? Data, string? Message) where T : class;

    /// <summary>
    /// 分页数据结构
    /// </summary>
    public record PagedResult<T>(List<T> Items, int TotalCount, int Page, int PageSize) where T : class;
}
