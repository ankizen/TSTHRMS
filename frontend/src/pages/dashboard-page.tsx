import { ArrowRight, ClipboardList, Network, Users } from "lucide-react"
import { Link } from "react-router-dom"
import { Button } from "@/components/ui/button"
import {
  Card,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { useAuth } from "@/hooks/use-auth"

export function DashboardPage() {
  const { user } = useAuth()

  return (
    <div className="flex flex-col gap-6">
      <div className="rounded-3xl bg-gradient-to-br from-blue-600 via-blue-600 to-indigo-600 p-8 text-white shadow-lg shadow-blue-600/20">
        <p className="text-sm font-medium text-blue-100">Signed in as {user?.email}</p>
        <h1 className="mt-1 font-heading text-3xl font-semibold tracking-tight">Welcome back</h1>
        <p className="mt-2 max-w-xl text-sm text-blue-100">
          Core HR is live - employee records, documents, org chart, and change history for every
          legal entity and product line you run.
        </p>
        <Button asChild className="mt-5 rounded-full bg-white text-blue-700 hover:bg-blue-50">
          <Link to="/employees">
            Go to Employees
            <ArrowRight />
          </Link>
        </Button>
      </div>

      <div className="grid gap-4 sm:grid-cols-3">
        <Card>
          <CardHeader>
            <div className="flex size-9 items-center justify-center rounded-xl bg-blue-500/10 text-blue-600">
              <Users className="size-4.5" />
            </div>
            <CardTitle className="mt-2 text-base">Employee Database</CardTitle>
            <CardDescription>
              Personal, contact, employment, and statutory details in one master record.
            </CardDescription>
          </CardHeader>
        </Card>
        <Card>
          <CardHeader>
            <div className="flex size-9 items-center justify-center rounded-xl bg-indigo-500/10 text-indigo-600">
              <Network className="size-4.5" />
            </div>
            <CardTitle className="mt-2 text-base">Org Hierarchy</CardTitle>
            <CardDescription>
              A live, filterable reporting chart built from each employee's manager link.
            </CardDescription>
          </CardHeader>
        </Card>
        <Card>
          <CardHeader>
            <div className="flex size-9 items-center justify-center rounded-xl bg-violet-500/10 text-violet-600">
              <ClipboardList className="size-4.5" />
            </div>
            <CardTitle className="mt-2 text-base">Change History</CardTitle>
            <CardDescription>
              Every field-level change captured automatically, with a masked audit trail.
            </CardDescription>
          </CardHeader>
        </Card>
      </div>
    </div>
  )
}
