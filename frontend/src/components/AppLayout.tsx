import { Layout, Menu, Button, Space, Typography } from 'antd';
import { AppstoreOutlined, BarChartOutlined, FileTextOutlined, InboxOutlined, LogoutOutlined, SwapOutlined, UserOutlined, TeamOutlined, AlertOutlined } from '@ant-design/icons';
import { Link, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { useQueryClient } from '@tanstack/react-query';
import { authApi } from '../api/services';
import type { User } from '../types';
const { Header, Sider, Content } = Layout;
export function AppLayout({ user }: { user: User }) {
  const location = useLocation(); const navigate = useNavigate(); const queryClient = useQueryClient();
  const items = [{ key: '/dashboard', icon: <BarChartOutlined />, label: <Link to="/dashboard">首页</Link> }, { key: '/inventory', icon: <InboxOutlined />, label: <Link to="/inventory">库存查询</Link> }, { key: '/documents', icon: <FileTextOutlined />, label: <Link to="/documents">正式单据</Link> }, { key: '/transfers', icon: <SwapOutlined />, label: <Link to="/transfers">调拨管理</Link> }, { key: '/stocktakes', icon: <AppstoreOutlined />, label: <Link to="/stocktakes">盘点管理</Link> }, { key: '/reports', icon: <AlertOutlined />, label: <Link to="/reports">库存报表</Link> }, { key: '/organization', icon: <TeamOutlined />, label: <Link to="/organization">组织与权限</Link> }, { key: '/account', icon: <UserOutlined />, label: <Link to="/account">账户</Link> }];
  return <Layout style={{ minHeight: '100vh' }}><Sider breakpoint="lg" collapsedWidth="0"><div style={{ color: '#fff', padding: '16px 18px', display: 'flex', alignItems: 'center', gap: 10 }}><img src="/react/logo.svg" alt="" width="34" height="34" /><Typography.Title level={4} style={{ color: '#fff', margin: 0 }}>仓库条码</Typography.Title></div><Menu theme="dark" mode="inline" selectedKeys={[`/${location.pathname.split('/')[1] || 'dashboard'}`]} items={items} /></Sider><Layout><Header style={{ background: '#fff', padding: '0 24px', display: 'flex', justifyContent: 'flex-end', alignItems: 'center' }}><Space><span>{user.displayName || user.username} · {user.role}</span><Button icon={<LogoutOutlined />} onClick={async () => { await authApi.logout(); queryClient.setQueryData(['auth'], { authenticated: false }); navigate('/', { replace: true }); }}>退出</Button></Space></Header><Content style={{ margin: 24 }}><Outlet /></Content></Layout></Layout>;
}
