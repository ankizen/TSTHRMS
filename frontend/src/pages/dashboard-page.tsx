import { ArrowRight } from "lucide-react"
import { Link } from "react-router-dom"
import { Button } from "@/components/ui/button"
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
          <CardTitle>Core HR / Employee Database</CardTitle>
          <CardDescription>
            The employee master record (personal, contact, and employment details) is live.
            Education, family, documents, and the rest of the Core HR spec land in upcoming
            slices.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Button asChild variant="outline">
            <Link to="/employees">
              Go to Employees
              <ArrowRight />
            </Link>
          </Button>
        </CardContent>
      </Card>
    </div>
  )
}
