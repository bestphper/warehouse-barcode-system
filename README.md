# 仓库管理系统

《仓库管理系统》V1.0，面向仓储作业的浏览器系统。

## 使用说明

登录后点击顶部“帮助”，打开左侧目录、右侧说明的帮助中心。内容涵盖登录、首页、库存查询、正式单据、调拨、盘点、报表、组织权限、账户和运维，并包含真实页面及详情弹窗截图。

帮助地址：`/react/help/overview`；完整演示：`/react/help/demo`。截图仅展示演示资料，演示中的审核操作应在独立数据库副本上进行。

更新截图：安装 Playwright 后运行 `node scripts/capture-help.cjs`，默认访问 `http://127.0.0.1:5174/react`，可通过 `HELP_URL` 指定地址。脚本使用演示管理员登录并读取页面，不提交业务单据；截图保存到 `frontend/public/help`。

## 当前阶段

- ASP.NET Core 8 浏览器应用，可在 macOS、Windows、Linux 开发运行。
- SQLite 关系表持久化，首次启动自动创建完整表结构并加载正式业务初始资料。
- 库存查询支持 SKU、商品名和条码搜索，适配键盘模拟型条码枪。
- 入库、出库、退货入库流水；出库库存不足时拒绝提交。
- 库存余额、低库存提示、仓库和往来单位概览。
- 基础资料新增接口：商品、仓库、往来单位，带必填与重复编码校验。
- 业务流水查询接口，支持按时间倒序返回商品、仓库、类型、数量和备注。
- 商品/仓库停用接口，并阻止仍有非零库存的对象被停用。
- 库存盘点调整：记录账面数、实盘数和差异，并写入流水。
- 仓库调拨：原仓扣减、目标仓增加，并写入成对调拨流水。
- 管理员和仓库操作员登录接口。

## 运行

需要 .NET 8 SDK。在仓库根目录执行：

```sh
dotnet run --project WarehouseBarcode
```

## React 前端

React 管理台位于 `frontend`，使用 Ant Design 和 Ant Design Charts。开发时分别启动后端和前端：

```sh
cd frontend
npm install
npm run dev
```

浏览器访问 `http://localhost:5173/react/`。Vite 会将 `/api` 和 `/print` 请求代理到 ASP.NET Core。

生产构建：

```sh
cd frontend
npm run build
```

构建后从后端访问 `http://localhost:5188/react/`。旧的 `wwwroot` 页面仍由根路径提供，React 页面完成验收后再切换为默认首页。

首次运行会在 `WarehouseBarcode/warehouse.sqlite` 创建关系表并加载演示资料。数据库只保留这一份 SQLite 文件，浏览器打开控制台显示的地址。

## 后续阶段

1. 货品、仓库、往来单位管理，条码规则及标签打印。
2. 入库/出库/销售退货单据、审核与冲销、批次和库存追溯。
3. 盘点、调拨、邮政编码、往来对账和 Excel 导入导出。
4. 用户、角色、审计日志、备份恢复和报表。
5. 生产部署切换到 PostgreSQL，并验证实际条码枪、打印机及 Windows 环境。

当前版本适合单机或小规模局域网使用；多人正式部署时建议切换 PostgreSQL，并启用安全的密码哈希和会话策略。
