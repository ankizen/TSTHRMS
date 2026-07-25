import { useQuery } from "@tanstack/react-query"
import { ArrowRight, Building2, ClipboardList, Network, UserCheck, Users } from "lucide-react"
import { Link } from "react-router-dom"
import { getPendingEditRequests } from "@/features/admin/edit-requests/api"
import { getDashboardSummary } from "@/features/employees/api"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import { useAuth } from "@/hooks/use-auth"

function getGreeting() {
  const hour = new Date().getHours()
  if (hour < 12) return "Good morning"
  if (hour < 17) return "Good afternoon"
  return "Good evening"
}

function displayNameFromEmail(email?: string) {
  if (!email) return "there"
  const local = email.split("@")[0]
  const first = local.split(/[._-]/)[0]
  return first.charAt(0).toUpperCase() + first.slice(1)
}

export function DashboardPage() {
  const { user } = useAuth()
  const isHrRole = Boolean(user?.roles.includes("HRAdmin") || user?.roles.includes("HRBP"))

  const { data: summary } = useQuery({
    queryKey: ["employees", "dashboard-summary"],
    queryFn: getDashboardSummary,
    enabled: isHrRole,
  })

  const { data: pending = [] } = useQuery({
    queryKey: ["edit-requests", "pending", "count"],
    queryFn: getPendingEditRequests,
    enabled: isHrRole,
  })

  const stats = [
    { label: "Employees", value: summary?.totalEmployees, icon: Users, tint: "blue" as const },
    { label: "Active", value: summary?.activeEmployees, icon: UserCheck, tint: "emerald" as const },
    { label: "Departments", value: summary?.departmentCount, icon: Building2, tint: "indigo" as const },
    { label: "Pending Requests", value: pending.length, icon: ClipboardList, tint: "amber" as const },
  ]

  const tintClasses: Record<string, string> = {
    blue: "bg-blue-500/10 text-blue-600",
    emerald: "bg-emerald-500/10 text-emerald-600",
    indigo: "bg-indigo-500/10 text-indigo-600",
    amber: "bg-amber-500/10 text-amber-600",
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="rounded-3xl bg-gradient-to-br from-blue-600 via-blue-600 to-indigo-600 p-8 text-white shadow-lg shadow-blue-600/20">
        <p className="text-sm font-medium text-blue-100">
          {getGreeting()}, {displayNameFromEmail(user?.email)}
        </p>
        <h1 className="mt-1 font-heading text-3xl font-semibold tracking-tight">
          Welcome to your workspace
        </h1>
        <p className="mt-2 max-w-xl text-sm text-blue-100">
          Core HR is live - employee records, documents, org chart, and change history for every
          legal entity and product line you run.
        </p>
        {isHrRole && (
          <Button asChild className="mt-5 rounded-full bg-white text-blue-700 hover:bg-blue-50">
            <Link to="/employees">
              Go to Employees
              <ArrowRight />
            </Link>
          </Button>
        )}
      </div>

      {isHrRole && (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {stats.map((stat) => (
            <Card
              key={stat.label}
              className="group cursor-default transition-all duration-200 hover:-translate-y-0.5 hover:shadow-md"
            >
              <CardContent className="flex items-center gap-3.5">
                <div
                  className={`flex size-11 shrink-0 items-center justify-center rounded-xl transition-transform duration-200 group-hover:scale-105 ${tintClasses[stat.tint]}`}
                >
                  <stat.icon className="size-5" />
                </div>
                <div>
                  <p className="font-heading text-2xl font-semibold tracking-tight">
                    {stat.value ?? "-"}
                  </p>
                  <p className="text-xs text-muted-foreground">{stat.label}</p>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      {isHrRole && (
        <div className="grid gap-4 lg:grid-cols-3">
          <Card className="lg:col-span-2">
            <CardContent>
              <div className="mb-4 flex items-center justify-between">
                <h2 className="text-sm font-semibold">Recent Joinees</h2>
                <Button asChild variant="ghost" size="sm">
                  <Link to="/employees">
                    View all
                    <ArrowRight />
                  </Link>
                </Button>
              </div>
              {summary && summary.recentJoinees.length === 0 ? (
                <p className="py-8 text-center text-sm text-muted-foreground">
                  No employees yet - add your first one to get started.
                </p>
              ) : (
                <div className="flex flex-col divide-y divide-border">
                  {summary?.recentJoinees.map((joinee) => (
                    <Link
                      key={joinee.id}
                      to={`/employees/${joinee.id}`}
                      className="flex items-center gap-3 py-2.5 transition-colors hover:bg-muted/50"
                    >
                      <div className="flex size-9 shrink-0 items-center justify-center rounded-full bg-gradient-to-br from-blue-500 to-indigo-500 text-xs font-semibold text-white">
                        {joinee.firstName[0]}
                        {joinee.lastName[0]}
                      </div>
                      <div className="min-w-0 flex-1">
                        <p className="truncate text-sm font-medium">
                          {joinee.firstName} {joinee.lastName}
                        </p>
                        <p className="truncate text-xs text-muted-foreground">
                          {joinee.designation ?? joinee.employeeCode}
                          {joinee.department ? ` · ${joinee.department}` : ""}
                        </p>
                      </div>
                      <span className="shrink-0 text-xs text-muted-foreground">
                        {new Date(joinee.dateOfJoining).toLocaleDateString(undefined, {
                          month: "short",
                          day: "numeric",
                        })}
                      </span>
                    </Link>
                  ))}
                </div>
              )}
            </CardContent>
          </Card>

          <Card>
            <CardContent className="flex flex-col gap-3">
              <h2 className="text-sm font-semibold">Quick links</h2>
              <Link
                to="/org-chart"
                className="flex items-center gap-3 rounded-xl border border-transparent p-3 transition-all duration-200 hover:-translate-y-0.5 hover:border-border hover:bg-muted/50 hover:shadow-sm"
              >
                <div className="flex size-9 items-center justify-center rounded-lg bg-indigo-500/10 text-indigo-600">
                  <Network className="size-4.5" />
                </div>
                <div>
                  <p className="text-sm font-medium">Org Chart</p>
                  <p className="text-xs text-muted-foreground">View reporting structure</p>
                </div>
              </Link>
              <Link
                to="/admin/edit-requests"
                className="flex items-center gap-3 rounded-xl border border-transparent p-3 transition-all duration-200 hover:-translate-y-0.5 hover:border-border hover:bg-muted/50 hover:shadow-sm"
              >
                <div className="flex size-9 items-center justify-center rounded-lg bg-amber-500/10 text-amber-600">
                  <ClipboardList className="size-4.5" />
                </div>
                <div>
                  <p className="text-sm font-medium">Edit Requests</p>
                  <p className="text-xs text-muted-foreground">
                    {pending.length} awaiting review
                  </p>
                </div>
              </Link>
            </CardContent>
          </Card>
        </div>
      )}

      {!isHrRole && (
        <Card>
          <CardContent className="flex flex-col gap-3">
            <h2 className="text-sm font-semibold">Get started</h2>
            <Link
              to="/my/profile"
              className="flex items-center gap-3 rounded-xl border border-transparent p-3 transition-all duration-200 hover:-translate-y-0.5 hover:border-border hover:bg-muted/50 hover:shadow-sm"
            >
              <div className="flex size-9 items-center justify-center rounded-lg bg-blue-500/10 text-blue-600">
                <Users className="size-4.5" />
              </div>
              <div>
                <p className="text-sm font-medium">My Profile</p>
                <p className="text-xs text-muted-foreground">View and request changes to your details</p>
              </div>
            </Link>
          </CardContent>
        </Card>
      )}
    </div>
  )
}
