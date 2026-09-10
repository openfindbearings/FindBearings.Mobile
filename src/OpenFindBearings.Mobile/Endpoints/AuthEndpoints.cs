using OpenFindBearings.Mobile.Services;

namespace OpenFindBearings.Mobile.Endpoints;

/// <summary>
/// 认证端点：代理 Identity 的注册/登录/刷新/验证码。
/// BFF 层做薄封装、不存储凭证；把 Identity 的失败原因映射为明确的错误码返回移动端，
/// 成功时返回扁平结构 { success, accessToken, refreshToken, expiresIn }。
/// </summary>
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this RouteGroupBuilder group)
    {
        /// <summary>
        /// 注册即登录：先调 Identity signup 建 OIDC 用户，再用 password grant 换令牌。
        /// 改动说明：Identity signup 不返回令牌，故 BFF 链式两步；agreeTerms 由移动端真实勾选后透传（合规）。
        /// </summary>
        group.MapPost("/register", async (
            RegisterRequest body,
            AuthClient authClient,
            CancellationToken ct) =>
        {
            var signUp = await authClient.SignUpAsync(body.Phone, body.Password, body.AgreeTerms, ct);
            if (!signUp.Success)
            {
                return MapFailure(signUp, "REGISTER_INVALID");
            }

            var login = await authClient.LoginAsync(body.Phone, body.Password, body.DeviceId, ct);
            return login.Success ? Success(login.Token!) : MapFailure(login, "INVALID_CREDENTIALS");
        })
        .WithName("Register")
        .WithSummary("注册并登录")
        .AllowAnonymous();

        /// <summary>
        /// 密码登录。
        /// </summary>
        group.MapPost("/login", async (
            LoginRequest body,
            AuthClient authClient,
            CancellationToken ct) =>
        {
            var result = await authClient.LoginAsync(body.Username, body.Password, body.DeviceId, ct);
            return result.Success ? Success(result.Token!) : MapFailure(result, "INVALID_CREDENTIALS");
        })
        .WithName("Login")
        .WithSummary("密码登录")
        .AllowAnonymous();

        /// <summary>
        /// 短信验证码登录/注册（P2 真实通道接入后可用；参数已对齐 phone/code）。
        /// </summary>
        group.MapPost("/login-sms", async (
            SmsLoginRequest body,
            AuthClient authClient,
            CancellationToken ct) =>
        {
            var result = await authClient.LoginWithSmsAsync(body.Phone, body.Code, body.DeviceId, ct);
            return result.Success ? Success(result.Token!) : MapFailure(result, "SMS_INVALID");
        })
        .WithName("LoginSms")
        .WithSummary("短信验证码登录")
        .AllowAnonymous();

        /// <summary>
        /// 发送短信验证码。
        /// </summary>
        group.MapPost("/send-code", async (
            SendCodeRequest body,
            AuthClient authClient,
            CancellationToken ct) =>
        {
            var ok = await authClient.SendSmsCodeAsync(body.Phone, ct);
            return ok
                ? Results.Ok(new { success = true, message = "验证码已发送" })
                : Results.Json(new { success = false, code = "UPSTREAM_ERROR", message = "发送失败" }, statusCode: 502);
        })
        .WithName("SendSmsCode")
        .WithSummary("发送短信验证码")
        .AllowAnonymous();

        /// <summary>
        /// 刷新令牌。
        /// </summary>
        group.MapPost("/refresh", async (
            RefreshRequest body,
            AuthClient authClient,
            CancellationToken ct) =>
        {
            var result = await authClient.RefreshAsync(body.RefreshToken, body.DeviceId, ct);
            return result.Success ? Success(result.Token!) : MapFailure(result, "REFRESH_INVALID");
        })
        .WithName("RefreshToken")
        .WithSummary("刷新令牌")
        .AllowAnonymous();

        /// <summary>
        /// 登出：吊销该会话的刷新令牌（access 短时效自然过期）。尽力而为，即便上游失败也返回成功，
        /// 因移动端本地令牌无论如何都会清除。
        /// </summary>
        group.MapPost("/logout", async (
            LogoutRequest body,
            AuthClient authClient,
            CancellationToken ct) =>
        {
            if (!string.IsNullOrEmpty(body.RefreshToken))
            {
                await authClient.RevokeRefreshTokenAsync(body.RefreshToken, ct);
            }
            return Results.Ok(new { success = true });
        })
        .WithName("Logout")
        .WithSummary("登出（吊销刷新令牌）")
        .AllowAnonymous();
    }

    /// <summary>构造成功响应（扁平结构，供移动端 request.ts 直接读 accessToken）</summary>
    private static IResult Success(AuthClient.TokenResult token) => Results.Ok(new
    {
        success = true,
        accessToken = token.Access_Token,
        refreshToken = token.Refresh_Token,
        expiresIn = token.Expires_In,
    });

    /// <summary>
    /// 把 Identity 失败映射为移动端错误码 + HTTP 状态。
    /// 改动说明：invalid_grant 可能来自密码错误、账号不可用、验证码错误等，据 error_description 细分，
    /// 避免一律 401 "手机号或密码错误" 掩盖真实原因（如禁用/上游故障）。
    /// </summary>
    private static IResult MapFailure(AuthResult result, string fallbackCode)
    {
        var (code, status) = result.Error switch
        {
            "USER_EXISTS" => ("USER_EXISTS", 409),
            "REGISTER_INVALID" => ("REGISTER_INVALID", 400),
            "invalid_grant" when (result.ErrorDescription ?? "").Contains("not available", StringComparison.OrdinalIgnoreCase)
                => ("ACCOUNT_DISABLED", 403),
            "invalid_grant" => (fallbackCode, 401),
            "UPSTREAM_ERROR" => ("UPSTREAM_ERROR", 502),
            _ => (fallbackCode, 401)
        };
        return Results.Json(new { success = false, code, message = result.ErrorDescription ?? "请求失败" }, statusCode: status);
    }

    // ============ 参数 ============

    public record LoginRequest(string Username, string Password, string DeviceId);
    public record RegisterRequest(string Phone, string Password, bool AgreeTerms, string DeviceId);
    public record SmsLoginRequest(string Phone, string Code, string DeviceId);
    public record SendCodeRequest(string Phone);
    public record RefreshRequest(string RefreshToken, string DeviceId);
    public record LogoutRequest(string? RefreshToken);
}
