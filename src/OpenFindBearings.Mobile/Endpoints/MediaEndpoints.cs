using OpenFindBearings.Mobile.Services;

namespace OpenFindBearings.Mobile.Endpoints;

/// <summary>
/// 媒体文件代理（/mobile/media/**）：API 无公网 ingress，图片（轴承图/用户上传头像/预置头像）
/// 统一经 BFF 流式转发到 API 静态文件。仅放行白名单目录前缀，其余 404。
/// </summary>
public static class MediaEndpoints
{
    /// <summary>允许代理的 API 静态目录前缀（防任意路径穿透）</summary>
    private static readonly string[] AllowedPrefixes = { "images/", "uploads/", "avatars/" };

    public static void MapMediaEndpoints(this RouteGroupBuilder group)
    {
        /// <summary>
        /// 按相对路径流式转发 API 静态文件，透传 Content-Type。
        /// </summary>
        group.MapGet("/{**path}", async (
            string path, ApiClient api, CancellationToken ct) =>
        {
            if (string.IsNullOrEmpty(path) || !AllowedPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                return Results.NotFound();

            var response = await api.GetRawAsync($"/{path}", ct);
            if (response == null) return Results.NotFound();

            var stream = await response.Content.ReadAsStreamAsync(ct);
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            return Results.Stream(stream, contentType);
        })
        .WithName("MediaProxy")
        .WithSummary("媒体文件代理");
    }
}
