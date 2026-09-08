# Mobile BFF API 端点说明 v1.1.0

## 概述

OpenFindBearings.Mobile 作为移动端 BFF，所有端点统一挂载在 `/mobile` 前缀下。BFF 代理后端 API（`http://openfindbearings-api:80`）和 Identity（`http://openfindbearings-identity:80`）的调用，前端无需直接访问这两个服务。

路由按资源分子组注册（Program.cs）：`/mobile/bearings/*`、`/mobile/merchants/*`、`/mobile/auth/*`；`/mobile/home` 与用户资料端点（`/mobile/profile`、`/mobile/favorites`、`/mobile/followed`）挂在 `/mobile` 根下。

## 变更日志

### v1.1.0 (2026-09-08)

- 路由注册确认按资源命名空间子组（此前代码扁平注册导致 `/mobile/bearings/search` 404，已修）。
- BFF→API 内部调用统一附带 `X-Internal-Token` 头（配置 `Internal:ApiToken`），供 API 限流中间件豁免按 IP 的用户级限流（详见《系统配置与限流设计 v1.1.0》）。
- 用户资料路径修正为 `/mobile/profile`、`/mobile/favorites`、`/mobile/followed`（原 follows）。
- 商家搜索/详情、首页推荐商家 DTO 字段对齐 API（新增 companyName/type/productCount/logoUrl，contact→contactPerson，详情补 mobile/email/address/grade/followerCount）。
- `bearings/search`、`merchants/search` 无搜索条件时返回空分页（不再 400）。

### v1.0.0 (2026-09-01)

- 初始版本，完整端点清单、请求/响应结构、认证要求。

## 端点总览

| 分组 | 端点数 | 认证要求 |
|------|--------|----------|
| 首页聚合 | 1 | 无需登录 |
| 轴承 | 4 | 无需登录 |
| 商家 | 4 | 公开端点无需登录，入驻申请需登录 |
| 用户资料 | 3 | 需登录（Bearer JWT） |
| 认证 | 4 | 无需登录 |

## 公开端点（无需登录）

### GET /mobile/home

首页聚合数据，一次请求返回热门轴承、推荐商家、品牌列表、类型列表，减少前端多次请求。

**响应示例**：

```json
{
  "hotBearings": [
    {
      "id": "uuid",
      "partNumber": "6205-2RS",
      "oldNumber": "180205",
      "bearingType": "深沟球轴承",
      "innerDiameter": 25,
      "outerDiameter": 52,
      "width": 15,
      "brandName": "SKF",
      "image3DUrl": "/images/bearings/3d/....gif",
      "image2DUrl": "/images/bearings/2d/....jpg"
    }
  ],
  "merchants": [
    {
      "id": "uuid",
      "name": "洛阳轴承",
      "companyName": "洛阳某某有限公司",
      "isVerified": true,
      "productCount": 123,
      "logoUrl": "/images/merchants/....jpg"
    }
  ],
  "brands": [
    { "id": "uuid", "name": "SKF" }
  ],
  "bearingTypes": [
    { "id": "uuid", "name": "深沟球轴承" }
  ]
}
```

> 图片字段（image3DUrl/image2DUrl/logoUrl）可能是相对路径（如 `/images/...`），前端仅在为绝对 `http(s)` 地址时渲染 `<Image>`，否则回退占位图标。

### GET /mobile/bearings/search

搜索轴承，支持关键字、品牌、类型筛选，分页返回。无任一条件时返回空/全量分页（不再 400）。

**查询参数**：

| 参数 | 类型 | 必填 | 默认 | 说明 |
|------|------|------|------|------|
| keyword | string | 否 | - | 型号/品牌/旧代号关键词 |
| brandName | string | 否 | - | 品牌名精确筛选 |
| bearingType | string | 否 | - | 类型名精确筛选 |
| page | int | 否 | 1 | 页码 |
| pageSize | int | 否 | 20 | 每页条数 |

**响应**：分页结构 `PagedData<BearingItem>`

```json
{
  "items": [
    {
      "id": "uuid",
      "partNumber": "6205-2RS",
      "oldNumber": "180205",
      "bearingType": "深沟球轴承",
      "innerDiameter": 25,
      "outerDiameter": 52,
      "width": 15,
      "brandName": "SKF",
      "image3DUrl": null,
      "image2DUrl": "/images/bearings/2d/....jpg"
    }
  ],
  "totalCount": 1234,
  "page": 1,
  "pageSize": 20
}
```

### GET /mobile/bearings/{id}

获取轴承详情，包含完整参数（尺寸、重量）和浏览/收藏计数。

**路径参数**：`id` — 轴承 GUID

**响应**：

```json
{
  "id": "uuid",
  "partNumber": "6205-2RS",
  "oldNumber": "180205",
  "englishName": "Deep Groove Ball Bearing",
  "bearingType": "深沟球轴承",
  "innerDiameter": 25,
  "outerDiameter": 52,
  "width": 15,
  "weight": 0.13,
  "brandName": "SKF",
  "brandCountry": "瑞典",
  "image3DUrl": "/images/...",
  "image2DUrl": "/images/...",
  "viewCount": 1234,
  "favoriteCount": 56
}
```

### GET /mobile/bearings/{id}/merchants

获取某轴承的在售商家列表。

**路径参数**：`id` — 轴承 GUID

**查询参数**：`page`（默认 1）、`pageSize`（默认 20）

**响应**：分页结构，每项包含 `merchantId`、`merchantName`、`price`（价格描述文本）、`isOnSale`。

### GET /mobile/bearings/{id}/interchanges

获取轴承替代品列表。

**路径参数**：`id` — 轴承 GUID

**响应**：数组，每项包含 `id`、`partNumber`、`brandName`、`bearingType`、`confidence`（可信度）。

---

## 商家端点

### GET /mobile/merchants/search

搜索商家，支持关键字和认证状态筛选。无 keyword 时返回空/全量分页（不再 400）。

**查询参数**：

| 参数 | 类型 | 必填 | 默认 | 说明 |
|------|------|------|------|------|
| keyword | string | 否 | - | 商家名关键词 |
| verifiedOnly | bool | 否 | - | 仅已认证商家 |
| page | int | 否 | 1 | 页码 |
| pageSize | int | 否 | 20 | 每页条数 |

**响应**：分页结构 `PagedData<MerchantItem>`

```json
{
  "items": [
    {
      "id": "uuid",
      "name": "洛阳轴承",
      "companyName": "洛阳某某有限公司",
      "type": "经销商",
      "isVerified": true,
      "status": "Active",
      "productCount": 123,
      "logoUrl": "/images/merchants/....jpg"
    }
  ],
  "totalCount": 690,
  "page": 1,
  "pageSize": 20
}
```

### GET /mobile/merchants/{id}

获取商家详情。

**路径参数**：`id` — 商家 GUID

**响应**：

```json
{
  "id": "uuid",
  "name": "洛阳轴承",
  "companyName": "洛阳某某有限公司",
  "type": "经销商",
  "contactPerson": "张经理",
  "phone": "0379-...",
  "mobile": "13800138000",
  "email": "...",
  "address": "...",
  "isVerified": true,
  "status": "Active",
  "grade": "...",
  "followerCount": 0,
  "productCount": 123,
  "logoUrl": "/images/merchants/....jpg"
}
```

### GET /mobile/merchants/{id}/bearings

获取商家在售商品列表。

**路径参数**：`id` — 商家 GUID

**查询参数**：`page`（默认 1）、`pageSize`（默认 20）

**响应**：分页结构，每项包含 `bearingId`、`bearingPartNumber`、`oldNumber`、`bearingTypeName`、`brandName`、尺寸三字段、`price`、`isOnSale`。

### POST /mobile/merchants/apply

商家入驻申请。**需要登录**。

**请求体**：

```json
{
  "contactName": "张经理",
  "phone": "13800138000",
  "description": "主营轴承销售",
  "licenseUrl": "https://..."
}
```

**响应**：

```json
{
  "message": "申请已提交，等待审核"
}
```

---

## 用户资料端点（需登录）

所有端点需要 `Authorization: Bearer {token}` 头。路径挂在 `/mobile` 根下（非 `/mobile/profile/*`）。

### GET /mobile/profile

获取当前用户资料（聚合 Identity 用户信息）。

**响应**：

```json
{
  "id": "uuid",
  "userName": "13800138000",
  "phoneNumber": "13800138000",
  "isActive": true,
  "createdAt": "2026-08-31T12:00:00Z",
  "lastLoginAt": "2026-09-01T10:00:00Z"
}
```

### GET /mobile/favorites

获取当前用户的收藏轴承列表。

**查询参数**：`page`（默认 1）、`pageSize`（默认 20）

**响应**：分页结构，每项包含 `id`（轴承 ID）、`partNumber`、`brandName`、`image3DUrl`。

### GET /mobile/followed

获取当前用户的关注商家列表。

**查询参数**：`page`（默认 1）、`pageSize`（默认 20）

**响应**：分页结构，每项包含 `id`（商家 ID）、`name`、`isVerified`。

> 浏览历史（`/mobile/history`）尚未在 BFF 实现，待登录功能落地时补齐。

---

## 认证端点（代理 Identity）

### POST /mobile/auth/login

密码登录。BFF 代理 Identity 的 OAuth password grant。

**请求体**：

```json
{
  "username": "13800138000",
  "password": "your-password",
  "deviceId": "随机GUID"
}
```

**成功响应（200）**：

```json
{
  "success": true,
  "accessToken": "eyJhbGciOi...",
  "refreshToken": "...",
  "expiresIn": 600
}
```

**失败响应（401）**：

```json
{ "success": false, "message": "手机号或密码错误" }
```

### POST /mobile/auth/login-sms

短信验证码登录/注册。BFF 代理 Identity 的 SMS grant。

**请求体**：

```json
{ "phone": "13800138000", "code": "123456", "deviceId": "随机GUID" }
```

**响应**：同 `/login`。

### POST /mobile/auth/send-code

发送短信验证码。

**请求体**：

```json
{ "phone": "13800138000" }
```

**成功响应（200）**：

```json
{ "success": true, "message": "验证码已发送" }
```

### POST /mobile/auth/refresh

刷新令牌。需要 `refreshToken` 和 `deviceId`（两者必须与签发时一致）。

**请求体**：

```json
{ "refreshToken": "旧refresh_token", "deviceId": "登录时的同一个GUID" }
```

**响应**：同 `/login`，返回新的 access_token 和 refresh_token。

---

## 通用响应结构

### 分页响应

```json
{ "items": [...], "totalCount": 1234, "page": 1, "pageSize": 20 }
```

### 错误响应

BFF 返回 HTTP 状态码 + JSON body：

```json
{ "statusCode": 401, "message": "Unauthorized" }
```

## 认证要求汇总

| 端点 | 方法 | 认证 | 说明 |
|------|------|------|------|
| `/mobile/home` | GET | 无 | 首页聚合 |
| `/mobile/bearings/search` | GET | 无 | 轴承搜索 |
| `/mobile/bearings/{id}` | GET | 无 | 轴承详情 |
| `/mobile/bearings/{id}/merchants` | GET | 无 | 在售商家 |
| `/mobile/bearings/{id}/interchanges` | GET | 无 | 替代品 |
| `/mobile/merchants/search` | GET | 无 | 商家搜索 |
| `/mobile/merchants/{id}` | GET | 无 | 商家详情 |
| `/mobile/merchants/{id}/bearings` | GET | 无 | 在售商品 |
| `/mobile/merchants/apply` | POST | Bearer | 入驻申请 |
| `/mobile/profile` | GET | Bearer | 用户资料 |
| `/mobile/favorites` | GET | Bearer | 收藏列表 |
| `/mobile/followed` | GET | Bearer | 关注列表 |
| `/mobile/auth/login` | POST | 无 | 密码登录 |
| `/mobile/auth/login-sms` | POST | 无 | 短信登录 |
| `/mobile/auth/send-code` | POST | 无 | 发送验证码 |
| `/mobile/auth/refresh` | POST | 无 | 刷新令牌 |

## BFF→API 内部令牌

BFF 调用 API 时统一附带请求头 `X-Internal-Token`（值取自配置 `Internal:ApiToken`），API 限流中间件识别匹配后豁免按 IP 的用户级限流。原因：API 无公网 ingress，全部 App 流量经单一 BFF Pod IP 转发，若按 IP 限流会误伤聚合流量（guest 30/min 触发 429 → 前端空结果）。令牌经 K8s ConfigMap 注入 API 与 BFF 两侧，必须同值。详见《系统配置与限流设计 v1.1.0》。
