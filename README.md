# 仓库条码管理系统

[![License](https://img.shields.io/badge/license-AGPL--3.0%20%7C%20Commercial-blue.svg)](LICENSE)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4.svg)
![React](https://img.shields.io/badge/React-18-61DAFB.svg)
![SQLite](https://img.shields.io/badge/SQLite-3-003B57.svg)

面向仓储作业场景的**单体 Web 应用**：覆盖商品、仓库、库位与批次库存，入库 / 出库 / 销售退货单据，仓库调拨、库存盘点与临期报表，并附带组织架构、角色权限、审计日志和图文帮助中心。

后端是 .NET 8 Minimal API，前端是 React 18 + Ant Design 5，数据全部落在单个 SQLite 文件里——**克隆下来构建一次前端就能跑，不需要额外安装数据库或中间件。**

## 功能特性

| 模块 | 能力 |
| --- | --- |
| 认证与账户 | 图片验证码（SVG，5 分钟有效，点击刷新）、Cookie 会话、PBKDF2-SHA256 加盐密码、连续失败 5 次锁定 15 分钟、账户中心查看资料与修改密码（至少 8 位） |
| 首页看板 | 在用商品 / 仓库 / 往来单位 / 低库存预警统计，仓库库存分布柱状图、近七日入出库趋势与最近业务流水 |
| 基础资料 | 商品（SKU 与条码唯一、单位、安全库存、最大库存、批次与有效期标记、FIFO / FEFO 出库策略）、仓库、库位（库区 / 货架 / 层 / 编码 / 容量）、往来单位；仍有非零库存的商品与仓库不允许停用 |
| 库存查询 | 按 SKU、条码、商品名、仓库、批次检索，展示库位、批次、有效期、状态与数量，适配键盘模拟型条码枪 |
| 正式单据 | 入库单 / 出库单 / 销售退货单：草稿 → 提交 → 审核完成，可取消；出库审核按 FIFO / FEFO 逐批次扣减，可用库存不足直接拒绝；已完成单据可生成反向冲销草稿；支持打印或另存 PDF |
| 调拨管理 | 来源仓库与目标仓库（必须不同）、多商品明细、批次与库位；审核后来源仓扣减、目标仓增加，并写入成对的调拨出库 / 入库流水 |
| 盘点管理 | 按仓库创建盘点任务，创建时记录账面数量，录入实盘数量并计算差异，审核后调整库存并写入盘点调整流水，已完成任务可导出 CSV |
| 库存报表 | 按批次展示失效日期、剩余天数、库存数量与临期（剩余 0–30 天）/ 已过期状态 |
| 组织与权限 | 部门层级、角色与权限点、用户角色与部门归属调整；服务端按权限点强校验 |
| 审计与运维 | 组织归属调整、系统备份等关键操作写入审计日志（管理员可查看最近 500 条），管理员可调用数据库备份接口 |
| 帮助中心 | 12 个章节的使用说明（概述、快速开始、首页、库存、单据、调拨、盘点、报表、组织、账户、运维、完整演示），内含真实页面与详情弹窗截图 |

## 技术栈

| 层 | 选型 |
| --- | --- |
| 后端 | .NET 8（`net8.0`）ASP.NET Core **Minimal API**，无控制器；唯一 NuGet 依赖 `Microsoft.Data.Sqlite` 8.0.10 |
| 数据库 | SQLite 单文件 `WarehouseBarcode/warehouse.sqlite`，每个集合一张关系表，并带外键与索引；首次启动自动建表并写入演示资料，重启不丢数据（旧 JSON 数据会在启动时自动迁移） |
| 前端 | React 18 + TypeScript（严格模式）+ Ant Design 5 + @ant-design/plots + TanStack Query 5 + react-router-dom 6 |
| 构建 | Vite 5，`base: /react/`，产物输出到 `frontend/dist`，由 ASP.NET Core 以 `/react` 路径托管 |

## 快速开始

### 环境要求

- .NET SDK 8.0 或更高
- Node.js 18 或更高（构建前端需要）

### 运行

```bash
git clone https://github.com/bestphper/warehouse-barcode-system.git
cd warehouse-barcode-system
cd frontend && npm install && npm run build && cd ..
dotnet run --project WarehouseBarcode
```

浏览器打开 <http://127.0.0.1:5188/react/>（访问根路径 <http://127.0.0.1:5188/> 会自动跳转过来）。默认账号 `admin` / `admin`，登录需要输入页面生成的 4 位图形验证码。

> `frontend/dist` 不入版本管理，因此首次运行前必须先构建一次前端；`WarehouseBarcode/wwwroot` 下保留了早期版本的页面，可通过 `/legacy` 访问。

### 前端开发

前端源码位于 `frontend/`。开发时分别启动 API 和 Vite：

```bash
# 终端 1：后端
dotnet run --project WarehouseBarcode

# 终端 2：前端
cd frontend
npm install
npm run dev
```

Vite 开发服务器监听 <http://127.0.0.1:5178/react/>，并把 `/api`、`/print`、`/assets` 请求代理到 `http://127.0.0.1:5188`。也可以直接运行 `./scripts/start-dev.sh` 一次启动两端（脚本会先清理占用 5188 端口的旧进程）。

### 发布构建

```bash
cd frontend && npm run build
```

该脚本执行 `tsc -b && vite build`，会清空并写入 `frontend/dist`，随后由 ASP.NET Core 一并托管。只做类型检查可用 `npm run typecheck`。

修改后端代码后需要重启 ASP.NET Core 进程，单独刷新浏览器不会更新运行中的 API。

### 帮助中心截图

安装 Playwright 后运行 `node scripts/capture-help.cjs`，默认访问 `http://127.0.0.1:5174/react`，可用环境变量 `HELP_URL` 指定地址。脚本使用演示管理员登录并读取页面，不提交业务单据；截图保存到 `frontend/public/help`。

## 默认账号与演示数据

- 默认管理员 `admin` / `admin`；演示操作员 `operator` / `operator123`。
- 仓库内附带一份演示数据库（`WarehouseBarcode/warehouse.sqlite`）：7 个仓库、100 个商品、批量库存、10 个往来单位，以及覆盖各状态的业务单据、调拨单、盘点任务和审计日志，首页与帮助中心截图即来自这份数据。
- 删除该文件后重启，系统会自动建表并写入基础演示资料（仓库、商品、库存、部门、角色和两个账号）；数据库始终只保留这一份 SQLite 文件。

## 目录结构

```
Program.cs                     # 全部 HTTP 路由（Minimal API），项目入口
Domain.cs BusinessModels.cs    # 实体、请求 record、验证码与密码服务
RelationalPersistence.cs       # 唯一持久化边界：建表、加载、整体保存、在线备份
WarehouseBarcode.csproj        # 后端项目文件（net8.0）
warehouse.sqlite               # SQLite 数据库（随仓库提供演示数据）
wwwroot/                       # 早期版本页面（/legacy、/documents）与静态资源
frontend/
  src/main.tsx                 # 入口：ConfigProvider / QueryClient / BrowserRouter
  src/App.tsx                  # 登录态判定、路由表与错误边界
  src/api/client.ts            # 带 Cookie 的 fetch 封装，401 自动跳回登录页
  src/api/services.ts          # 全部 API 类型与调用封装
  src/pages/*.tsx              # 首页、库存、单据、调拨、盘点、报表、组织、账户、帮助
  vite.config.ts               # 开发端口与代理，构建输出到 dist
scripts/start-dev.sh           # 一键启动后端 + Vite
scripts/capture-help.cjs       # 帮助中心截图脚本
```

架构要点：

- `DataStore` 是进程内的**全量内存模型**，`RelationalPersistence` 负责把整个模型读写到 SQLite。所有写接口都通过 `store.Mutate(...)` 修改内存并在同一把锁内整体落盘，因此并发写是串行的，适合单机或小规模局域网。
- 登录态就是 `warehouse_session` Cookie 中的用户名，服务端每次请求查库确认；没有单独的服务端会话表，Cookie 有效期 8 小时。
- 权限校验集中在 `Program.cs` 末尾的中间件：按请求路径与 HTTP 方法映射到权限点（`inventory.read`、`document.approve`、`transfer.manage`、`stocktake.manage` 等），管理员角色与持 `*` 的角色直接放行。

## 单据流转与库存影响

```
入库单      草稿 ──提交──▶ 已提交 ──审核──▶ 已完成（库存增加）
出库单      草稿 ──提交──▶ 已提交 ──审核──▶ 已完成（按 FIFO / FEFO 扣减批次库存，可用库存不足则拒绝）
销售退货单   草稿 ──提交──▶ 已提交 ──审核──▶ 已完成（退回入库）
调拨单      草稿 ──提交──▶ 已提交 ──审核并完成──▶ 已完成（来源仓扣减、目标仓增加，同时写入两条流水）
盘点单      草稿 ──提交──▶ 已提交 ──审核并调整──▶ 已完成（按实盘数量覆盖库存并记录差异）
```

- 草稿与已提交都可以取消；已完成单据不能直接取消，只能生成反向的冲销草稿。
- 明细支持批次、有效期、库位与质量状态（正常 / 待检 / 报废）；入库按“商品 + 仓库 + 库位 + 批次 + 质量状态”合并到同一条库存记录。
- 出库校验可用库存时会同时排除已过期批次，退货入库按明细的质量状态写入对应库存。

## API 一览

除验证码、登录、健康检查外，其余接口都需要有效会话；写操作还会按权限点校验。

**认证与账户**

- `GET /api/auth/captcha`、`POST /api/auth/login`、`POST /api/auth/logout`、`GET /api/auth/status`
- `GET /api/account/profile`、`POST /api/account/password`
- `POST /api/login`（早期页面使用的简化登录）

**基础资料与库存**

- `GET/POST /api/products`、`DELETE /api/products/{id}`
- `GET/POST /api/warehouses`、`DELETE /api/warehouses/{id}`
- `GET/POST /api/partners`、`GET/POST /api/locations`
- `GET /api/stock`、`GET/POST /api/transactions`
- `GET /api/dashboard`、`GET /api/dashboard/alerts`、`GET /api/dashboard/warehouse-distribution`、`GET /api/dashboard/daily-trend`

**单据、调拨与盘点**

- `GET/POST /api/documents`、`GET /api/documents/{id}`、`POST /api/documents/{id}/submit|approve|cancel|reverse`
- `GET /print/document/{id}`（打印页，可另存 PDF）
- `GET/POST /api/transfer-documents`、`POST /api/transfer-documents/{id}/submit|approve`、`POST /api/transfers`（简化调拨）
- `GET/POST /api/stocktake-documents`、`POST /api/stocktake-documents/{id}/submit|approve`、`GET /api/stocktake-documents/{id}/export`、`POST /api/stocktakes`（简化盘点）

**组织、报表与运维**

- `GET/POST /api/organization/departments`、`GET/POST /api/organization/roles`、`GET /api/organization/users`、`POST /api/organization/users/{username}/assignment`
- `GET /api/reports/expiry`
- `GET /api/audit-logs`、`POST /api/system/backup`、`GET /api/system/health`

## 数据存储与备份

- 数据库：`WarehouseBarcode/warehouse.sqlite`。启动时若检测到早期版本的 `data_store` JSON 表，会自动迁移成关系表。
- 备份：管理员调用 `POST /api/system/backup`，服务端用 SQLite 在线备份 API 生成副本并返回文件路径；该路径是服务器本地路径，不是浏览器下载链接。
- 迁移：停机后复制整个 `warehouse.sqlite` 即可；若目录中存在 WAL / SHM 文件，先确保已完成检查点，或直接使用备份接口。

## 安全说明（重要）

本项目是**演示性质**的实现，请勿不加改造直接用于生产环境。已知限制：

- 默认凭据 `admin` / `admin`、`operator` / `operator123`；密码使用 PBKDF2-SHA256（12 万次迭代、随机盐）存储，但没有密码强度策略，也不会强制修改初始密码。
- 全部数据常驻进程内存并整体落盘，**不支持多实例部署**；单次写操作会重写整库，数据量大时写入开销随规模上升。
- 未启用 HTTPS / HSTS、请求限流与 CSRF 防护；Cookie 的 `Secure` 标记仅在 HTTPS 请求下生效。
- 库位新增、简化调拨 / 盘点等接口只校验登录与固定权限点，未像正式单据那样区分创建、提交与审核角色。
- 审计日志目前覆盖组织归属调整与系统备份等操作，业务单据的变更依赖单据自身的创建人、审核人与时间字段留痕。

用于生产前，请至少替换默认账号、启用 HTTPS、补齐按操作的权限与审计，并使用可信的数据库备份与恢复策略。

## 许可与商业授权

本项目采用 **AGPL-3.0 与商业授权双许可**模式，你可以按需二选一：

### 免费使用（AGPL-3.0）

在遵守 [GNU Affero General Public License v3.0](LICENSE) 的前提下，可**免费**用于个人学习、研究、测试、内部自用，以及任何你愿意将衍生作品同样以 AGPL 开源的使用方式。

AGPL-3.0 的核心义务：如果你修改了本程序并通过网络向他人提供服务，必须向这些用户提供对应的完整源代码。

### 商业授权

以下场景需要购买商业授权，以获得免于 AGPL 开源义务的权利：

- 将本项目或其衍生作品集成进**闭源**产品或对外销售的软件；
- 以 SaaS / 网络服务形式对外提供，且不愿公开源代码；
- 企业内部商业用途中无法接受 AGPL 条款的情形。

商业授权联系：**bestphper@gmail.com**

### 贡献

除非你另有说明，你提交的 Pull Request 将按 AGPL-3.0 授权，并允许本项目以商业授权方式再许可——这是双许可模式得以运作的前提。

Copyright (C) 2026 bestphper

## 相关文档

- `仓库条码管理系统.pdf`：2021 年的需求文档。旧版 .NET Framework 源码已不存在，本项目不承诺旧版二进制或界面兼容。
- 内置帮助中心：登录后点击顶部「帮助」，或直接访问 `/react/help/overview`，另含完整业务演示章节 `/react/help/demo`。
