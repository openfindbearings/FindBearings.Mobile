using System.Security.Claims;
using OpenFindBearings.Mobile.Services;

namespace OpenFindBearings.Mobile.Endpoints;

/// <summary>
/// 用户资料端点
/// 聚合 Identity 用户信息 + API 业务数据（收藏/关注等）
/// </summary>
public static class ProfileEndpoints
{
    public static void MapProfileEndpoints(this RouteGroupBuilder group)
    {

        /// <summary>
        /// 获取用户资料（聚合 Identity + API）
        /// </summary>
        group.MapGet("/profile", async (
            HttpContext http,
            AuthClient authClient,
            ApiClient api,
            CancellationToken ct) =>
        {
            var accessToken = GetAccessToken(http);
            if (string.IsNullOrEmpty(accessToken))
                return Results.Unauthorized();

            // 从 Identity 获取用户基本信息
            var userInfo = await authClient.GetUserInfoAsync(accessToken, ct);

            return Results.Ok(new UserProfile
            {
                Id = userInfo?.Id ?? "",
                UserName = userInfo?.UserName ?? "",
                PhoneNumber = userInfo?.PhoneNumber ?? "",
                IsActive = userInfo?.IsActive ?? true,
                CreatedAt = userInfo?.CreatedAt ?? "",
                LastLoginAt = userInfo?.LastLoginAt ?? "",
            });
        })
        .WithName("GetProfile")
        .WithSummary("获取用户资料");

        /// <summary>
        /// 我的收藏轴承
        /// </summary>
        group.MapGet("/favorites", async (
            HttpContext http,
            ApiClient api,
            [AsParameters] PageQuery query,
            CancellationToken ct) =>
        {
            var accessToken = GetAccessToken(http);
            // 改动说明：API 实际路由为 /api/me/favorites/bearings（用户自有资源组 /api/me），
            // 原 /api/favorites/bearings 不存在会恒 404→空数据，补 /me 前缀。
            var path = $"/api/me/favorites/bearings?page={query.Page}&pageSize={query.PageSize}";
            var result = await api.GetPagedAsync<FavoriteBearing>(path, accessToken, ct);
            return Results.Ok(result ?? new ApiClient.PagedResult<FavoriteBearing>([], 0, 1, 20));
        })
        .WithName("GetFavorites")
        .WithSummary("我的收藏轴承");

        /// <summary>
        /// 我的关注商家
        /// </summary>
        group.MapGet("/followed", async (
            HttpContext http,
            ApiClient api,
            [AsParameters] PageQuery query,
            CancellationToken ct) =>
        {
            var accessToken = GetAccessToken(http);
            // 改动说明：同上，补 /me 前缀对齐 API 实际路由 /api/me/follows/merchants。
            var path = $"/api/me/follows/merchants?page={query.Page}&pageSize={query.PageSize}";
            var result = await api.GetPagedAsync<FollowedMerchant>(path, accessToken, ct);
            return Results.Ok(result ?? new ApiClient.PagedResult<FollowedMerchant>([], 0, 1, 20));
        })
        .WithName("GetFollows")
        .WithSummary("我的关注商家");
    }

    // ============ 工具 ============

    private static string? GetAccessToken(HttpContext http)
    {
        return http.Request.Headers.Authorization
            .FirstOrDefault()?.Replace("Bearer ", "");
    }

    // ============ 参数 ============

    public class PageQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    // ============ DTO ============

    public class UserProfile
    {
        public string Id { get; set; } = "";
        public string UserName { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public bool IsActive { get; set; }
        public string CreatedAt { get; set; } = "";
        public string LastLoginAt { get; set; } = "";
    }

    public record FavoriteBearing(Guid Id, string PartNumber, string BrandName, string? Image3DUrl);
    public record FollowedMerchant(Guid Id, string Name, bool IsVerified);
}
