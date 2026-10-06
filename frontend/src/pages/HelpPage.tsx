import { Alert, Image, Menu, Select, Steps, Table, Typography } from 'antd';
import { BookOutlined, DashboardOutlined, FileTextOutlined, InboxOutlined, SafetyCertificateOutlined, SwapOutlined, TeamOutlined, ToolOutlined } from '@ant-design/icons';
import { useNavigate, useParams } from 'react-router-dom';

const chapters = [
  { key: 'quick-start', label: '快速开始', icon: <BookOutlined />, title: '快速开始与登录', intro: '使用 admin / admin 登录演示系统，验证码填写图片中的 4 位字符。登录成功后进入首页。', steps: ['打开系统登录页', '输入用户名和密码，填写验证码', '点击“登录系统”进入首页'] },
  { key: 'dashboard', label: '首页概览', icon: <DashboardOutlined />, title: '首页与库存分布', intro: '首页展示商品数、仓库数、往来单位、低库存预警，以及各仓库库存分布和近期业务趋势。', steps: ['查看顶部统计卡片', '查看仓库库存分布，比较各仓库数量', '点击左侧菜单进入具体业务页面'] },
  { key: 'inventory', label: '库存查询', icon: <InboxOutlined />, title: '商品与库存查询', intro: '库存查询支持商品、SKU、条码、仓库、库位和批次等信息，列表支持分页。', steps: ['进入“库存查询”', '使用搜索框输入 SKU、条码或商品名称', '查看批次、有效期、状态和数量'] },
  { key: 'documents', label: '正式单据', icon: <FileTextOutlined />, title: '入库、出库与退货单据', intro: '正式单据遵循草稿、提交、审核完成、取消和冲销流程，审核完成后自动更新库存。', steps: ['选择单据类型和仓库', '添加一个或多个商品明细并创建草稿', '提交后由有权限用户审核', '在列表查看详情、打印或生成冲销单'] },
  { key: 'transfers', label: '调拨管理', icon: <SwapOutlined />, title: '调拨单操作', intro: '调拨单包含来源仓库、目标仓库和多商品明细，审核完成后同时写入调出和调入流水。', steps: ['选择来源和目标仓库', '添加商品、数量和批次', '创建草稿并提交', '审核完成后点击“查看详情”核对明细'] },
  { key: 'stocktakes', label: '盘点管理', icon: <SafetyCertificateOutlined />, title: '盘点任务与差异', intro: '盘点任务记录账面数量、实盘数量和差异，审核后调整库存，可导出 CSV。', steps: ['选择仓库和盘点商品', '录入实盘数量并创建任务', '提交任务并审核差异', '打开详情或导出 CSV 留档'] },
  { key: 'reports', label: '库存报表', icon: <DashboardOutlined />, title: '临期与过期库存', intro: '报表按批次展示失效日期、剩余天数、库存数量和临期/过期状态。', steps: ['进入“库存报表”', '按状态和失效日期查看记录', '点击“查看详情”核对批次库存'] },
  { key: 'organization', label: '组织与权限', icon: <TeamOutlined />, title: '部门、角色与权限', intro: '组织与权限用于维护部门层级、角色权限，并将用户分配到对应部门和角色。', steps: ['创建部门并设置上级部门', '创建角色并填写权限编码', '在用户归属页调整角色和部门'] },
  { key: 'security', label: '账户与安全', icon: <ToolOutlined />, title: '账户、密码与退出', intro: '账户页可查看当前用户和最近登录时间，并修改密码。顶部“退出”会清除登录状态并返回登录页。', steps: ['进入“账户”查看个人信息', '输入当前密码和至少 8 位新密码', '点击顶部“退出”结束当前会话'] },
  { key: 'operations', label: '备份与运维', icon: <ToolOutlined />, title: '数据保存与日常运维', intro: '业务数据存放在 WarehouseBarcode/warehouse.sqlite。当前备份由管理员接口提供，尚无浏览器恢复入口。', steps: ['管理员联系运维执行现有备份接口并保管生成的文件', '离线文件备份前停止系统，复制完整数据库，避免遗漏正在写入的数据', '恢复前先备份当前数据库，由运维停机替换并检查数据完整性'] },
  { key: 'demo', label: '完整业务演示', icon: <BookOutlined />, title: '从入库到盘点的演示', intro: '建议使用独立演示环境完成以下操作。创建草稿不会立即改变库存，审核会真实修改库存。', steps: ['登录后查看首页和库存，记录纸箱的当前数量', '创建入库单：选择中心仓库、纸箱、数量 10；添加明细、创建、提交、审核后核对库存增加', '将纸箱从中心仓库调拨 2 件到另一仓库，提交并审核后核对两端库存', '按实际库存录入盘点数量，提交后复核差异再审核', '打开正式单据、调拨单和盘点详情核对结果，查看报表后退出'] },
];

const notes: Record<string, { fields: Array<[string, string]>; caution: string }> = {
  'quick-start': { fields: [['用户名与密码', '演示环境默认 admin / admin；生产环境使用管理员分配的账户。'], ['验证码', '点击图片可刷新；输入图片中的 4 位字符。']], caution: '连续输错密码 5 次将锁定 15 分钟。演示账户不应直接用于生产环境。' },
  dashboard: { fields: [['在用商品与仓库', '统计当前启用的基础资料。'], ['库存分布', '按仓库汇总数量；不同商品可能采用不同单位，不能把总数量理解为统一重量或价值。'], ['七日趋势', '依据库存流水展示入库和出库变化。']], caution: '首页用于概览，批次、库位和有效期应在库存查询中逐项核对。' },
  inventory: { fields: [['SKU、条码', '用于识别商品，可输入搜索框检索。'], ['批次、库位', '标识库存所在位置和批次。'], ['有效期、状态', '核对失效日期以及正常、待检或报废状态。'], ['分页', '列表底部切换页码或每页条数。']], caution: '条码枪采用键盘输入模式时，应先将光标放到搜索框。该页为查询页面，不提供商品和仓库维护表单。' },
  documents: { fields: [['单据类型', '入库单增加库存，出库单减少库存，销售退货单回收入库。'], ['商品、数量、批次', '点击添加明细可录入多商品；数量必须大于零。'], ['查看详情与打印', '详情复核明细，打印打开浏览器打印页，可保存 PDF。'], ['取消与冲销', '草稿或已提交可取消；已完成不能直接取消，可生成反向草稿再复核。']], caution: '创建和提交不会改变库存，审核完成才会变更。冲销需人工复核单据类型，特别是销售退货冲销；当前不要依赖其自动恢复原库存。出库库存不足会被拒绝。' },
  transfers: { fields: [['来源和目标仓库', '必须是不同仓库。'], ['商品与数量', '添加明细可调拨多商品，数量需大于零。'], ['批次', '指定批次时从相应批次调出；留空时按现有规则选取库存。'], ['详情', '核对仓库、批次及来源和目标库位。']], caution: '审核真实扣减来源库存并增加目标库存。当前跨多个批次的大数量调拨需分批次核对，建议分别填写明细，避免源批次不足。' },
  stocktakes: { fields: [['账面数量', '创建任务时记录的账面库存。'], ['实盘数量', '现场实际清点结果，可为零。'], ['差异', '实盘减账面；列表差异为各行差异绝对值合计。'], ['导出 CSV', '已完成任务可导出商品、批次、账面、实盘及差异。']], caution: '当前尚未提供冻结范围功能。盘点期间应暂停相关商品批次的出入库，并指定准确批次后复核审核，避免覆盖期间发生的业务变动。' },
  reports: { fields: [['剩余天数', '按当前时间与失效日期计算。'], ['临期', '剩余天数为 0 至 30 天。'], ['已过期', '剩余天数小于零。'], ['正常', '剩余天数超过 30 天；本页也包含这些有效期库存。']], caution: '该页只包含设置有效期的库存。数量和失效日期须通过详情核对；未设置有效期的库存不会出现在本报表。' },
  organization: { fields: [['部门', '编码唯一；上级部门 ID 用于表达层级。'], ['角色', '编码唯一，权限编码用英文逗号分隔。'], ['用户归属', '选择角色与部门后点击保存。'], ['权限编码', 'inventory.read、report.read、document.create、document.submit、document.approve、document.reverse、transfer.manage、stocktake.manage、master.manage；管理员使用 *。']], caution: '组织与权限页面仅管理员可访问。当前角色限制以接口校验为主，菜单并不会全部按角色隐藏；普通用户看到无权限提示时应联系管理员。' },
  security: { fields: [['当前账户', '用户名、显示名称、角色和最近登录时间。'], ['修改密码', '填写当前密码，新密码至少 8 位。'], ['退出', '点击顶部退出，返回登录页；再次进入需重新登录。']], caution: '新密码应妥善保存，当前没有浏览器中的管理员密码重置入口。共用终端使用后应退出。' },
  operations: { fields: [['数据库', '唯一业务数据库为 WarehouseBarcode/warehouse.sqlite；请勿用旧 JSON 文件覆盖。'], ['管理员备份接口', 'POST /api/system/backup；需已登录管理员，返回服务器上备份文件路径。'], ['备份保存', '返回路径不是浏览器下载链接，需由运维取出并存放到安全位置。'], ['恢复', '尚无自动恢复页面；必须停机、备份当前库、由运维替换数据库，并核对库存和单据。']], caution: '自动备份、浏览器恢复和恢复前自动备份尚未实现。手工备份应先停机，存在 SQLite WAL/SHM 文件时必须确保已完成检查点或使用数据库备份 API。' },
  demo: { fields: [['演示前', '独立副本记录纸箱和两个仓库的初始数量。'], ['入库结果', '审核 10 件入库后，来源仓库库存应增加 10。'], ['调拨结果', '调拨 2 件后来源减少 2、目标增加 2，总库存不变。'], ['盘点结果', '指定相同批次录入准确实盘量；审核后核对账面与结果。']], caution: '审核会真实修改数据。演示应使用数据库副本，不能在生产库存上练习；详情和导出用于逐步核对，不要只看“已完成”状态。' },
};

export function HelpPage() {
  const { section = 'quick-start' } = useParams(); const navigate = useNavigate(); const chapter = chapters.find(x => x.key === section) ?? chapters[0];
  return <div className="help-layout"><aside className="help-directory"><Typography.Title level={4}>使用说明</Typography.Title><Menu selectedKeys={[chapter.key]} items={chapters.map(x => ({ key: x.key, icon: x.icon, label: x.label }))} onClick={e => navigate(`/help/${e.key}`)} /></aside><Select className="help-mobile-directory" aria-label="选择说明章节" value={chapter.key} options={chapters.map(x => ({ value: x.key, label: x.label }))} onChange={value => navigate(`/help/${value}`)} /><article className="help-article" key={chapter.key}><Typography.Title level={2}>{chapter.title}</Typography.Title><Typography.Paragraph type="secondary">{chapter.intro}</Typography.Paragraph><Steps direction="vertical" items={chapter.steps.map((title, index) => ({ title: `步骤 ${index + 1}`, description: title }))} /><Typography.Title level={4}>字段与操作</Typography.Title><Table size="small" pagination={false} rowKey="name" dataSource={notes[chapter.key].fields.map(([name, description]) => ({ name, description }))} columns={[{ title: '项目', dataIndex: 'name', width: 145 }, { title: '说明', dataIndex: 'description' }]} /><Alert style={{ marginTop: 20 }} type="warning" showIcon message="操作注意事项" description={notes[chapter.key].caution} />{chapter.key !== 'operations' && <><Typography.Title level={4}>实际页面</Typography.Title><Image src={`/react/help/${chapter.key}.png`} alt={`${chapter.title}真实界面截图`} width="100%" />{['documents', 'transfers', 'stocktakes', 'reports'].includes(chapter.key) && <><Typography.Title level={4}>查看详情</Typography.Title><Image src={`/react/help/${chapter.key}-detail.png`} alt={`${chapter.title}详情弹窗截图`} width="100%" /></>}</>}</article></div>;
}
