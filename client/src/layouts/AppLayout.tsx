import { Outlet } from 'react-router-dom'
import { Sidebar } from '../components/layout/Sidebar'
import { TopNavbar } from '../components/layout/TopNavbar'
import { PageContainer } from '../components/layout/PageContainer'

export function AppLayout() { return <div className="flex min-h-screen"><Sidebar /><div className="flex min-w-0 flex-1 flex-col"><TopNavbar /><PageContainer><Outlet /></PageContainer></div></div> }
