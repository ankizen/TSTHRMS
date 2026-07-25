import { LogOut } from "lucide-react"
import { Outlet } from "react-router-dom"
import { AppSidebar } from "@/components/app-sidebar"
import { NotificationBell } from "@/components/notification-bell"
import { ThemeToggle } from "@/components/theme-toggle"
import { TopSearch } from "@/components/top-search"
import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { Separator } from "@/components/ui/separator"
import { SidebarInset, SidebarProvider, SidebarTrigger } from "@/components/ui/sidebar"
import { useAuth } from "@/hooks/use-auth"
import { useAuthStore } from "@/stores/auth-store"

export function AppShell() {
  const { user, logout } = useAuth()
  const roles = useAuthStore((s) => s.user?.roles ?? [])
  const isHrRole = roles.includes("HRAdmin") || roles.includes("HRBP")

  const initials = user?.email
    ? user.email
        .split("@")[0]
        .split(/[._-]/)
        .slice(0, 2)
        .map((part) => part[0]?.toUpperCase())
        .join("")
    : "?"

  return (
    <SidebarProvider>
      <AppSidebar />
      <SidebarInset>
        <header className="sticky top-0 z-10 flex h-14 shrink-0 items-center justify-between gap-3 border-b bg-background/80 px-4 backdrop-blur-sm">
          <div className="flex flex-1 items-center gap-3">
            <SidebarTrigger />
            <Separator orientation="vertical" className="h-6" />
            <span className="hidden text-sm font-medium text-muted-foreground sm:inline">
              ThinkerSteps Group
            </span>
            {isHrRole && (
              <>
                <Separator orientation="vertical" className="hidden h-6 md:block" />
                <div className="hidden md:block">
                  <TopSearch />
                </div>
              </>
            )}
          </div>
          <div className="flex items-center gap-1.5">
            {isHrRole && <NotificationBell />}
            <ThemeToggle />
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="ghost" size="sm" className="gap-2 pl-1.5">
                  <span className="flex size-6 items-center justify-center rounded-full bg-gradient-to-br from-blue-600 to-indigo-600 text-[11px] font-semibold text-white">
                    {initials}
                  </span>
                  <span className="hidden sm:inline">{user?.email}</span>
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                <DropdownMenuLabel>Signed in as {user?.email}</DropdownMenuLabel>
                <DropdownMenuSeparator />
                <DropdownMenuItem onClick={() => logout()}>
                  <LogOut />
                  Log out
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        </header>
        <main className="flex-1 p-6">
          <Outlet />
        </main>
      </SidebarInset>
    </SidebarProvider>
  )
}
