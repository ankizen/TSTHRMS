import { useQuery } from "@tanstack/react-query"
import { UsersRound } from "lucide-react"
import { EmptyState } from "@/components/empty-state"
import { Badge } from "@/components/ui/badge"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { TableSkeletonRows } from "@/components/table-skeleton-rows"
import { APPLICATION_STAGE_LABELS } from "../constants"
import { getMyReferrals } from "./api"

export function MyReferralsPage() {
  const { data: referrals = [], isLoading } = useQuery({ queryKey: ["referrals", "mine"], queryFn: getMyReferrals })

  return (
    <div className="flex flex-col gap-4">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">My Referrals</h1>
        <p className="text-muted-foreground">{referrals.length} candidate{referrals.length === 1 ? "" : "s"} referred</p>
      </div>

      <div className="rounded-xl border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Candidate</TableHead>
              <TableHead>Job</TableHead>
              <TableHead>Status</TableHead>
              <TableHead>Referred on</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              <TableSkeletonRows columns={4} />
            ) : referrals.length === 0 ? (
              <TableRow>
                <TableCell colSpan={4}>
                  <EmptyState
                    icon={UsersRound}
                    title="No referrals yet"
                    description="Candidates you refer for an open role will show up here with their pipeline status."
                  />
                </TableCell>
              </TableRow>
            ) : (
              referrals.map((referral) => (
                <TableRow key={`${referral.candidateId}-${referral.jobPostingTitle}`}>
                  <TableCell>{referral.candidateName}</TableCell>
                  <TableCell>{referral.jobPostingTitle}</TableCell>
                  <TableCell>
                    <Badge variant="secondary">{APPLICATION_STAGE_LABELS[referral.stage]}</Badge>
                  </TableCell>
                  <TableCell className="text-muted-foreground">
                    {new Date(referral.appliedAt).toLocaleDateString()}
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
