import { ClipboardCheck, KeyRound, LayoutDashboard, Network, UserCog, Users, UsersRound } from "lucide-react"
import { NavLink } from "react-router-dom"
import {
  Sidebar,
  SidebarContent,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
} from "@/components/ui/sidebar"
import { useAuthStore } from "@/stores/auth-store"

/** Undefined `roles` means visible to everyone. This only controls what's shown - the API is
 * the real gate, so a role without access to a hidden link still can't reach its data. */
const navItems = [
  { title: "Dashboard", url: "/", icon: LayoutDashboard, end: true, roles: undefined },
  { title: "Employees", url: "/employees", icon: Users, end: false, roles: ["HRAdmin", "HRBP"] },
  { title: "Org Chart", url: "/org-chart", icon: Network, end: false, roles: ["HRAdmin", "HRBP"] },
  { title: "My Profile", url: "/my/profile", icon: UserCog, end: false, roles: undefined },
  { title: "My Team", url: "/my/team", icon: UsersRound, end: false, roles: ["Manager"] },
  { title: "Edit Requests", url: "/admin/edit-requests", icon: ClipboardCheck, end: false, roles: ["HRAdmin", "HRBP"] },
  { title: "Users", url: "/admin/users", icon: KeyRound, end: false, roles: ["HRAdmin"] },
] as const

export function AppSidebar() {
  const roles = useAuthStore((s) => s.user?.roles ?? [])
  const visibleItems = navItems.filter((item) => !item.roles || item.roles.some((role) => roles.includes(role)))

  return (
    <Sidebar collapsible="icon">
      <SidebarHeader className="px-2 py-3">
        <div className="flex items-center gap-2 px-2">
          <div className="flex size-8 shrink-0 items-center justify-center rounded-md bg-primary font-semibold text-primary-foreground">
            T
          </div>
          <span className="font-semibold group-data-[collapsible=icon]:hidden">TSTHRMS</span>
        </div>
      </SidebarHeader>
      <SidebarContent>
        <SidebarGroup>
          <SidebarGroupLabel>Workspace</SidebarGroupLabel>
          <SidebarGroupContent>
            <SidebarMenu>
              {visibleItems.map((item) => (
                <SidebarMenuItem key={item.title}>
                  <SidebarMenuButton asChild tooltip={item.title}>
                    <NavLink to={item.url} end={item.end}>
                      <item.icon />
                      <span>{item.title}</span>
                    </NavLink>
                  </SidebarMenuButton>
                </SidebarMenuItem>
              ))}
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>
      </SidebarContent>
    </Sidebar>
  )
}
