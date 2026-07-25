import { Briefcase, Building2, CalendarClock, ClipboardCheck, Gift, KeyRound, LayoutDashboard, Network, SlidersHorizontal, Star, UserCog, UserPlus, Users, UsersRound } from "lucide-react"
import { NavLink, useLocation } from "react-router-dom"
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
  { title: "Requisitions", url: "/recruitment/requisitions", icon: Briefcase, end: false, roles: ["HRAdmin", "HRBP", "Manager"] },
  { title: "Talent Pool", url: "/recruitment/talent-pool", icon: Star, end: false, roles: ["HRAdmin", "HRBP"] },
  { title: "My Profile", url: "/my/profile", icon: UserCog, end: false, roles: undefined },
  { title: "My Team", url: "/my/team", icon: UsersRound, end: false, roles: ["Manager"] },
  { title: "My Interviews", url: "/my/interviews", icon: CalendarClock, end: false, roles: undefined },
  { title: "Refer a Candidate", url: "/recruitment/refer", icon: UserPlus, end: false, roles: undefined },
  { title: "My Referrals", url: "/recruitment/my-referrals", icon: Gift, end: false, roles: undefined },
  { title: "Edit Requests", url: "/admin/edit-requests", icon: ClipboardCheck, end: false, roles: ["HRAdmin", "HRBP"] },
  { title: "Users", url: "/admin/users", icon: KeyRound, end: false, roles: ["HRAdmin"] },
  { title: "Custom Fields", url: "/admin/custom-fields", icon: SlidersHorizontal, end: false, roles: ["HRAdmin"] },
] as const

export function AppSidebar() {
  const roles = useAuthStore((s) => s.user?.roles ?? [])
  const visibleItems = navItems.filter((item) => !item.roles || item.roles.some((role) => roles.includes(role)))
  const { pathname } = useLocation()
  const isItemActive = (item: (typeof navItems)[number]) =>
    item.end ? pathname === item.url : pathname.startsWith(item.url)

  return (
    <Sidebar collapsible="icon">
      <SidebarHeader className="px-2 py-3">
        <div className="flex items-center gap-2.5 px-2">
          <div className="flex size-8 shrink-0 items-center justify-center rounded-lg bg-gradient-to-br from-blue-600 to-indigo-600 text-white shadow-sm">
            <Building2 className="size-4.5" />
          </div>
          <span className="font-heading font-semibold tracking-tight group-data-[collapsible=icon]:hidden">
            TSTHRMS
          </span>
        </div>
      </SidebarHeader>
      <SidebarContent>
        <SidebarGroup>
          <SidebarGroupLabel>Workspace</SidebarGroupLabel>
          <SidebarGroupContent>
            <SidebarMenu>
              {visibleItems.map((item) => (
                <SidebarMenuItem key={item.title}>
                  <SidebarMenuButton asChild tooltip={item.title} isActive={isItemActive(item)}>
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
