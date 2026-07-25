import { useQuery } from "@tanstack/react-query"
import { Badge } from "@/components/ui/badge"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { EMPLOYEE_STATUS_BADGE_VARIANT } from "@/features/employees/constants"
import { getMyDirectReports } from "./api"

export function MyTeamPage() {
  const { data: reports = [], isLoading } = useQuery({ queryKey: ["my-direct-reports"], queryFn: getMyDirectReports })

  return (
    <div className="flex flex-col gap-4">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">My Team</h1>
        <p className="text-muted-foreground">{reports.length} direct report{reports.length === 1 ? "" : "s"}</p>
      </div>

      <div className="rounded-md border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Code</TableHead>
              <TableHead>Name</TableHead>
              <TableHead>Designation</TableHead>
              <TableHead>Department</TableHead>
              <TableHead>Work Location</TableHead>
              <TableHead>Date of Joining</TableHead>
              <TableHead>Status</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              <TableRow>
                <TableCell colSpan={7} className="h-24 text-center text-muted-foreground">
                  Loading...
                </TableCell>
              </TableRow>
            ) : reports.length === 0 ? (
              <TableRow>
                <TableCell colSpan={7} className="h-24 text-center text-muted-foreground">
                  No one reports to you yet.
                </TableCell>
              </TableRow>
            ) : (
              reports.map((report) => (
                <TableRow key={report.id}>
                  <TableCell>{report.employeeCode}</TableCell>
                  <TableCell>{report.firstName} {report.lastName}</TableCell>
                  <TableCell>{report.designation ?? "-"}</TableCell>
                  <TableCell>{report.department ?? "-"}</TableCell>
                  <TableCell>{report.workLocation ?? "-"}</TableCell>
                  <TableCell>{report.dateOfJoining}</TableCell>
                  <TableCell>
                    <Badge variant={EMPLOYEE_STATUS_BADGE_VARIANT[report.status]}>{report.status}</Badge>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>
    </div>
  )
}
