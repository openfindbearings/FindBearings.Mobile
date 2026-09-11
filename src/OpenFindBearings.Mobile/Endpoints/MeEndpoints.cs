using OpenFindBearings.Mobile.Services;

namespace OpenFindBearings.Mobile.Endpoints;

/// <summary>
/// "我"的写操作代理端点（/mobile/me/*）：收藏、关注、浏览历史、资料编辑。
/// 统一模式：从入站 Authorization 头取用户 access token，透传给业务 API 的 /api/me/*；
/// 缺 token 一律 401（不静默回空数据）。写操作返回 {success}，查询返回 API 原数据结构。
/// </summary>
public static class MeEndpoints
{
    public static void MapMeEndpoints(this RouteGroupBuilder group)
    {
        // ============ 收藏轴承 ============

        /// <summary>收藏某轴承（已收藏时 API 返回 400，代理如实透出 success:false）</summary>
        group.MapPost("/favorites/{bearingId}", async (
            string bearingId, HttpContext http, ApiClient api, CancellationToken ct) =>
        {
            var token = GetToken(http);
            if (string.IsNullOrEmpty(token)) return Results.Unauthorized();
            var ok = await api.PostVoidAsync($"/api/me/favorites/bearings/{bearingId}", token, ct);
            return ok
                ? Results.Ok(new { success = true, message = "收藏成功" })
                : Results.Ok(new { success = false, message = "收藏失败或已收藏过" });
        })
        .WithName("FavoriteBearing")
        .WithSummary("收藏轴承");

        /// <summary>取消收藏</summary>
        group.MapDelete("/favorites/{bearingId}", async (
            string bearingId, HttpContext http, ApiClient api, CancellationToken ct) =>
        {
            var token = GetToken(http);
            if (string.IsNullOrEmpty(token)) return Results.Unauthorized();
            var ok = await api.DeleteVoidAsync($"/api/me/favorites/bearings/{bearingId}", token, ct);
            return Results.Ok(new { success = ok, message = ok ? "已取消收藏" : "取消失败" });
        })
        .WithName("UnfavoriteBearing")
        .WithSummary("取消收藏轴承");

        /// <summary>查询是否已收藏（详情页红心状态回显）</summary>
        group.MapGet("/favorites/{bearingId}/check", async (
            string bearingId, HttpContext http, ApiClient api, CancellationToken ct) =>
        {
            var token = GetToken(http);
            if (string.IsNullOrEmpty(token)) return Results.Unauthorized();
            var data = await api.GetAsync<CheckFavorited>($"/api/me/favorites/bearings/{bearingId}/check", token, ct);
            return Results.Ok(new { isFavorited = data?.IsFavorited ?? false });
        })
        .WithName("CheckFavorite")
        .WithSummary("查询轴承收藏状态");

        // ============ 关注商家 ============

        /// <summary>关注某商家</summary>
        group.MapPost("/follows/{merchantId}", async (
            string merchantId, HttpContext http, ApiClient api, CancellationToken ct) =>
        {
            var token = GetToken(http);
            if (string.IsNullOrEmpty(token)) return Results.Unauthorized();
            var ok = await api.PostVoidAsync($"/api/me/follows/merchants/{merchantId}", token, ct);
            return ok
                ? Results.Ok(new { success = true, message = "关注成功" })
                : Results.Ok(new { success = false, message = "关注失败或已关注过" });
        })
        .WithName("FollowMerchant")
        .WithSummary("关注商家");

        /// <summary>取消关注</summary>
        group.MapDelete("/follows/{merchantId}", async (
            string merchantId, HttpContext http, ApiClient api, CancellationToken ct) =>
        {
            var token = GetToken(http);
            if (string.IsNullOrEmpty(token)) return Results.Unauthorized();
            var ok = await api.DeleteVoidAsync($"/api/me/follows/merchants/{merchantId}", token, ct);
            return Results.Ok(new { success = ok, message = ok ? "已取消关注" : "取消失败" });
        })
        .WithName("UnfollowMerchant")
        .WithSummary("取消关注商家");

        /// <summary>查询是否已关注</summary>
        group.MapGet("/follows/{merchantId}/check", async (
            string merchantId, HttpContext http, ApiClient api, CancellationToken ct) =>
        {
            var token = GetToken(http);
            if (string.IsNullOrEmpty(token)) return Results.Unauthorized();
            var data = await api.GetAsync<CheckFollowed>($"/api/me/follows/merchants/{merchantId}/check", token, ct);
            return Results.Ok(new { isFollowed = data?.IsFollowed ?? false });
        })
        .WithName("CheckFollow")
        .WithSummary("查询商家关注状态");

        // ============ 浏览历史 ============

        /// <summary>轴承浏览历史（分页）</summary>
        group.MapGet("/history/bearings", async (
            HttpContext http, ApiClient api, ProfileEndpoints.PageQuery query, CancellationToken ct) =>
        {
            var token = GetToken(http);
            if (string.IsNullOrEmpty(token)) return Results.Unauthorized();
            var path = $"/api/me/history/bearings?page={query.Page}&pageSize={query.PageSize}";
            var result = await api.GetPagedAsync<BearingHistoryItem>(path, token, ct);
            return Results.Ok(result ?? new ApiClient.PagedResult<BearingHistoryItem>([], 0, 1, 20));
        })
        .WithName("GetBearingHistory")
        .WithSummary("轴承浏览历史");

        /// <summary>商家浏览历史（分页）</summary>
        group.MapGet("/history/merchants", async (
            HttpContext http, ApiClient api, ProfileEndpoints.PageQuery query, CancellationToken ct) =>
        {
            var token = GetToken(http);
            if (string.IsNullOrEmpty(token)) return Results.Unauthorized();
            var path = $"/api/me/history/merchants?page={query.Page}&pageSize={query.PageSize}";
            var result = await api.GetPagedAsync<MerchantHistoryItem>(path, token, ct);
            return Results.Ok(result ?? new ApiClient.PagedResult<MerchantHistoryItem>([], 0, 1, 20));
        })
        .WithName("GetMerchantHistory")
        .WithSummary("商家浏览历史");

        /// <summary>上报轴承浏览（详情页进入时前端自动调用）</summary>
        group.MapPost("/history/bearings/{bearingId}", async (
            string bearingId, HttpContext http, ApiClient api, CancellationToken ct) =>
        {
            var token = GetToken(http);
            if (string.IsNullOrEmpty(token)) return Results.Unauthorized();
            await api.PostVoidAsync($"/api/me/history/bearings/{bearingId}", token, ct);
            // 上报属尽力而为，不向前端报错
            return Results.Ok(new { success = true });
        })
        .WithName("RecordBearingView")
        .WithSummary("上报轴承浏览");

        /// <summary>上报商家浏览</summary>
        group.MapPost("/history/merchants/{merchantId}", async (
            string merchantId, HttpContext http, ApiClient api, CancellationToken ct) =>
        {
            var token = GetToken(http);
            if (string.IsNullOrEmpty(token)) return Results.Unauthorized();
            await api.PostVoidAsync($"/api/me/history/merchants/{merchantId}", token, ct);
            return Results.Ok(new { success = true });
        })
        .WithName("RecordMerchantView")
        .WithSummary("上报商家浏览");

        /// <summary>删除单条轴承浏览历史（API 按 userId+bearingId 收敛归属）</summary>
        group.MapDelete("/history/bearings/{bearingId}", async (
            string bearingId, HttpContext http, ApiClient api, CancellationToken ct) =>
        {
            var token = GetToken(http);
            if (string.IsNullOrEmpty(token)) return Results.Unauthorized();
            var ok = await api.DeleteVoidAsync($"/api/me/history/bearings/{bearingId}", token, ct);
            return Results.Ok(new { success = ok });
        })
        .WithName("DeleteBearingHistory")
        .WithSummary("删除单条轴承历史");

        /// <summary>删除单条商家浏览历史</summary>
        group.MapDelete("/history/merchants/{merchantId}", async (
            string merchantId, HttpContext http, ApiClient api, CancellationToken ct) =>
        {
            var token = GetToken(http);
            if (string.IsNullOrEmpty(token)) return Results.Unauthorized();
            var ok = await api.DeleteVoidAsync($"/api/me/history/merchants/{merchantId}", token, ct);
            return Results.Ok(new { success = ok });
        })
        .WithName("DeleteMerchantHistory")
        .WithSummary("删除单条商家历史");

        /// <summary>清空全部浏览历史</summary>
        group.MapDelete("/history/clear", async (
            HttpContext http, ApiClient api, CancellationToken ct) =>
        {
            var token = GetToken(http);
            if (string.IsNullOrEmpty(token)) return Results.Unauthorized();
            var ok = await api.DeleteVoidAsync("/api/me/history/clear", token, ct);
            return Results.Ok(new { success = ok, message = ok ? "历史记录已清空" : "清空失败" });
        })
        .WithName("ClearHistory")
        .WithSummary("清空浏览历史");

        // ============ 资料编辑 ============

        /// <summary>
        /// 更新个人信息：双写 Identity（nickname/pictureUrl）与业务 API
        /// （nickname/avatar/occupation/companyName/industry），保证两侧一致；
        /// 以 API 侧结果为主返回值（业务库是展示事实源）。
        /// </summary>
        group.MapPut("/profile", async (
            UpdateProfileBody body, HttpContext http, ApiClient api, AuthClient authClient, CancellationToken ct) =>
        {
            var token = GetToken(http);
            if (string.IsNullOrEmpty(token)) return Results.Unauthorized();

            // Identity 侧仅在带 nickname/avatar 时同步（部分更新语义，null 不动）
            if (body.Nickname != null || body.Avatar != null)
            {
                await authClient.UpdateProfileAsync(token, body.Nickname, body.Avatar, ct);
            }

            var bizBody = new
            {
                nickname = body.Nickname,
                avatar = body.Avatar,
                occupation = body.Occupation,
                companyName = body.CompanyName,
                industry = body.Industry
            };
            var result = await api.PutAsync<string>("/api/me/profile", bizBody, token, ct);
            // ApiClient 失败返回 null（内部已记日志），据此判定保存结果
            return result != null
                ? Results.Ok(new { success = true, message = "保存成功" })
                : Results.Ok(new { success = false, message = "保存失败" });
        })
        .WithName("UpdateProfile")
        .WithSummary("更新个人信息");
    }

    // ============ 工具 ============

    /// <summary>从入站请求提取用户 access token（与 ProfileEndpoints.GetAccessToken 同逻辑）</summary>
    private static string? GetToken(HttpContext http) =>
        http.Request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "");

    // ============ DTO ============

    /// <summary>API 收藏 check 端点响应 {isFavorited}</summary>
    public record CheckFavorited(bool IsFavorited);

    /// <summary>API 关注 check 端点响应 {isFollowed}</summary>
    public record CheckFollowed(bool IsFollowed);

    /// <summary>轴承历史条目（对齐 API BearingHistoryDto）</summary>
    public record BearingHistoryItem(
        Guid Id, Guid BearingId, string BearingPartNumber, string? BrandName,
        DateTime ViewedAt, int ViewCount);

    /// <summary>商家历史条目（对齐 API MerchantHistoryDto）</summary>
    public record MerchantHistoryItem(
        Guid Id, Guid MerchantId, string MerchantName, string? CompanyName,
        DateTime ViewedAt, int ViewCount);

    /// <summary>资料编辑请求体（全部可选，部分更新）</summary>
    public record UpdateProfileBody(
        string? Nickname,
        string? Avatar,
        int? Occupation,
        string? CompanyName,
        string? Industry);
}
