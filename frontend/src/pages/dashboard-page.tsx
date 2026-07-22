import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { useAuth } from "@/hooks/use-auth"

export function DashboardPage() {
  const { user } = useAuth()

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Welcome back</h1>
        <p className="text-muted-foreground">Signed in as {user?.email}</p>
      </div>
      <Card>
        <CardHeader>
          <CardTitle>Core HR is next</CardTitle>
          <CardDescription>
            Phase 0 (auth, multi-tenancy, app shell) is done. The Employee Database module
            (Phase 1) will appear here once it&apos;s built.
          </CardDescription>
        </CardHeader>
        <CardContent className="text-sm text-muted-foreground">
          Roles: {user?.roles.join(", ") || "-"}
        </CardContent>
      </Card>
    </div>
  )
}
