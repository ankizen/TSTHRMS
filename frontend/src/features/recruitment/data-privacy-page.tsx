import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { ShieldCheck } from "lucide-react"
import { useState } from "react"
import { toast } from "sonner"
import { EmptyState } from "@/components/empty-state"
import { Button } from "@/components/ui/button"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { TableSkeletonRows } from "@/components/table-skeleton-rows"
import { useAuth } from "@/hooks/use-auth"
import { decideDeletionRequest, getDeletionRequests, runRetentionSweep } from "./api"
import { DecisionDialog } from "./decision-dialog"
import type { CandidateDataDeletionRequest } from "./types"

export function DataPrivacyPage() {
  const { user } = useAuth()
  const isHrAdmin = Boolean(user?.roles.includes("HRAdmin"))
  const queryClient = useQueryClient()
  const queryKey = ["recruitment", "data-privacy", "deletion-requests"]
  const { data: requests = [], isLoading } = useQuery({
    queryKey,
    queryFn: () => getDeletionRequests("Pending"),
  })

  const [dialogTarget, setDialogTarget] = useState<{ request: CandidateDataDeletionRequest; approve: boolean } | null>(null)

  const invalidate = () => queryClient.invalidateQueries({ queryKey })

  const decideMutation = useMutation({
    mutationFn: ({ requestId, approve, notes }: { requestId: string; approve: boolean; notes: string | null }) =>
      decideDeletionRequest(requestId, { approve, notes }),
    onSuccess: async (result) => {
      if (result.succeeded) {
        toast.success(dialogTarget?.approve ? "Candidate data anonymized." : "Request rejected.")
        setDialogTarget(null)
        await invalidate()
      } else {
        toast.error(result.error ?? "Couldn't process this request.")
      }
    },
    onError: () => toast.error("Couldn't process this request."),
  })

  const sweepMutation = useMutation({
    mutationFn: runRetentionSweep,
    onSuccess: (count) => toast.success(`Retention sweep complete - ${count} candidate(s) anonymized.`),
    onError: () => toast.error("Couldn't run the retention sweep."),
  })

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Candidate Data Privacy</h1>
          <p className="text-muted-foreground">Pending self-service deletion requests from the Candidate Portal.</p>
        </div>
        {isHrAdmin && (
          <Button variant="outline" onClick={() => sweepMutation.mutate()} disabled={sweepMutation.isPending}>
            {sweepMutation.isPending ? "Running..." : "Run retention sweep now"}
          </Button>
        )}
      </div>

      <div className="rounded-xl border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Candidate</TableHead>
              <TableHead>Email</TableHead>
              <TableHead>Requested</TableHead>
              <TableHead className="text-right">Decision</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              <TableSkeletonRows columns={4} />
            ) : requests.length === 0 ? (
              <TableRow>
                <TableCell colSpan={4}>
                  <EmptyState
                    icon={ShieldCheck}
                    title="No pending requests"
                    description="Candidate-initiated data deletion requests will show up here for review."
                  />
                </TableCell>
              </TableRow>
            ) : (
              requests.map((request) => (
                <TableRow key={request.id}>
                  <TableCell>{request.candidateName}</TableCell>
                  <TableCell className="text-muted-foreground">{request.candidateEmail}</TableCell>
                  <TableCell className="text-muted-foreground">
                    {new Date(request.requestedAt).toLocaleDateString()}
                  </TableCell>
                  <TableCell className="flex justify-end gap-2">
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={() => setDialogTarget({ request, approve: false })}
                    >
                      Reject
                    </Button>
                    <Button size="sm" onClick={() => setDialogTarget({ request, approve: true })}>
                      Approve
                    </Button>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>

      <DecisionDialog
        open={dialogTarget !== null}
        onOpenChange={(open) => !open && setDialogTarget(null)}
        onSubmit={(notes) =>
          dialogTarget &&
          decideMutation.mutate({ requestId: dialogTarget.request.id, approve: dialogTarget.approve, notes })
        }
        isSubmitting={decideMutation.isPending}
        title={dialogTarget?.approve ? "Approve deletion request" : "Reject deletion request"}
        actionLabel={dialogTarget?.approve ? "Approve & anonymize" : "Reject"}
        variant={dialogTarget?.approve ? "default" : "destructive"}
      />
    </div>
  )
}
