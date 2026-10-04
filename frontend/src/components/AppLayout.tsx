import { Layout, Menu, Button, Space, Typography } from 'antd';
import { AppstoreOutlined, BarChartOutlined, FileTextOutlined, InboxOutlined, LogoutOutlined, SwapOutlined, UserOutlined } from '@ant-design/icons';
import { Link, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { authApi } from '../api/services';
import type { User } from '../types';
const { Header, Sider, Content } = Layout;
export function AppLayout({ user }: { user: User }) {
  const location = useLocation(); const navigate = useNavigate();
  const items = [{ key: '/dashboard', icon: <BarChartOutlined />, label: <Link to="/dashboard">首页</Link> }, { key: '/inventory', icon: <InboxOutlined />, label: <Link to="/inventory">库存查询</Link> }, { key: '/documents', icon: <FileTextOutlined />, label: <Link to="/documents">正式单据</Link> }, { key: '/transfers', icon: <SwapOutlined />, label: <Link to="/transfers">调拨管理</Link> }, { key: '/stocktakes', icon: <AppstoreOutlined />, label: <Link to="/stocktakes">盘点管理</Link> }, { key: '/account', icon: <UserOutlined />, label: <Link to="/account">账户</Link> }];
  return <Layout style={{ minHeight: '100vh' }}><Sider breakpoint="lg" collapsedWidth="0"><Typography.Title level={4} style={{ color: '#fff', padding: 18, margin: 0 }}>仓库条码</Typography.Title><Menu theme="dark" mode="inline" selectedKeys={[`/${location.pathname.split('/')[1] || 'dashboard'}`]} items={items} /></Sider><Layout><Header style={{ background: '#fff', padding: '0 24px', display: 'flex', justifyContent: 'flex-end', alignItems: 'center' }}><Space><span>{user.displayName || user.username} · {user.role}</span><Button icon={<LogoutOutlined />} onClick={async () => { await authApi.logout(); navigate('/'); }}>退出</Button></Space></Header><Content style={{ margin: 24 }}><Outlet /></Content></Layout></Layout>;
}
