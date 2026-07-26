import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { Gift } from "lucide-react"
import { toast } from "sonner"
import { EmptyState } from "@/components/empty-state"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { TableSkeletonRows } from "@/components/table-skeleton-rows"
import { getReferralPayouts, markReferralBonusPaid } from "./api"

export function ReferralPayoutsPage() {
  const queryClient = useQueryClient()
  const queryKey = ["referrals", "payouts"]
  const { data: payouts = [], isLoading } = useQuery({ queryKey, queryFn: getReferralPayouts })

  const markPaidMutation = useMutation({
    mutationFn: markReferralBonusPaid,
    onSuccess: async () => {
      toast.success("Marked as paid.")
      await queryClient.invalidateQueries({ queryKey })
    },
    onError: () => toast.error("Couldn't mark this bonus as paid."),
  })

  return (
    <div className="flex flex-col gap-4">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Referral Payouts</h1>
        <p className="text-muted-foreground">
          Referral bonuses become Payable once the referred candidate is converted to an employee.
        </p>
      </div>

      <div className="rounded-xl border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Candidate</TableHead>
              <TableHead>Referred by</TableHead>
              <TableHead>Job</TableHead>
              <TableHead>Bonus</TableHead>
              <TableHead>Status</TableHead>
              <TableHead className="text-right">Action</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              <TableSkeletonRows columns={6} />
            ) : payouts.length === 0 ? (
              <TableRow>
                <TableCell colSpan={6}>
                  <EmptyState
                    icon={Gift}
                    title="No referral bonuses yet"
                    description="Bonuses show up here once a referred candidate is converted to an employee."
                  />
                </TableCell>
              </TableRow>
            ) : (
              payouts.map((payout) => (
                <TableRow key={payout.candidateId}>
                  <TableCell>{payout.candidateName}</TableCell>
                  <TableCell>{payout.referredByEmployeeName}</TableCell>
                  <TableCell>{payout.jobPostingTitle}</TableCell>
                  <TableCell>₹{payout.bonusAmount.toLocaleString()}</TableCell>
                  <TableCell>
                    <Badge variant={payout.status === "Paid" ? "default" : "secondary"}>
                      {payout.status === "Paid" ? "Paid" : "Payable"}
                    </Badge>
                  </TableCell>
                  <TableCell className="text-right">
                    {payout.status === "Payable" && (
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={() => markPaidMutation.mutate(payout.candidateId)}
                        disabled={markPaidMutation.isPending}
                      >
                        Mark Paid
                      </Button>
                    )}
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
