import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { Briefcase, Plus } from "lucide-react"
import { useState } from "react"
import { toast } from "sonner"
import { useNavigate } from "react-router-dom"
import { EmptyState } from "@/components/empty-state"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { TableSkeletonRows } from "@/components/table-skeleton-rows"
import { createRequisition, getRequisitions } from "./api"
import { REQUISITION_STATUS_BADGE_VARIANT, REQUISITION_STATUS_LABELS } from "./constants"
import { RequisitionFormDialog } from "./requisition-form-dialog"
import type { JobRequisitionWriteRequest } from "./types"

export function RequisitionsListPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [dialogOpen, setDialogOpen] = useState(false)
  const queryKey = ["recruitment", "requisitions"]

  const { data: requisitions = [], isLoading } = useQuery({ queryKey, queryFn: () => getRequisitions() })

  const createMutation = useMutation({
    mutationFn: (request: JobRequisitionWriteRequest) => createRequisition(request),
    onSuccess: async (requisition) => {
      toast.success("Requisition created as a draft.")
      setDialogOpen(false)
      await queryClient.invalidateQueries({ queryKey })
      navigate(`/recruitment/requisitions/${requisition.id}`)
    },
    onError: () => toast.error("Couldn't create the requisition."),
  })

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Requisitions</h1>
          <p className="text-muted-foreground">{requisitions.length} total</p>
        </div>
        <Button onClick={() => setDialogOpen(true)}>
          <Plus />
          New requisition
        </Button>
      </div>

      <div className="rounded-xl border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Code</TableHead>
              <TableHead>Title</TableHead>
              <TableHead>Entity</TableHead>
              <TableHead>Product</TableHead>
              <TableHead>Openings</TableHead>
              <TableHead>Status</TableHead>
              <TableHead>Live</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              <TableSkeletonRows columns={7} />
            ) : requisitions.length === 0 ? (
              <TableRow>
                <TableCell colSpan={7}>
                  <EmptyState
                    icon={Briefcase}
                    title="No requisitions yet"
                    description="Raise a requisition to start hiring for a new or backfilled role."
                    action={
                      <Button size="sm" className="mt-1" onClick={() => setDialogOpen(true)}>
                        <Plus />
                        New requisition
                      </Button>
                    }
                  />
                </TableCell>
              </TableRow>
            ) : (
              requisitions.map((requisition) => (
                <TableRow
                  key={requisition.id}
                  className="cursor-pointer"
                  onClick={() => navigate(`/recruitment/requisitions/${requisition.id}`)}
                >
                  <TableCell className="font-mono text-xs">{requisition.requisitionCode}</TableCell>
                  <TableCell>{requisition.title}</TableCell>
                  <TableCell>{requisition.legalEntityName}</TableCell>
                  <TableCell>{requisition.productName}</TableCell>
                  <TableCell>{requisition.openings}</TableCell>
                  <TableCell>
                    <Badge variant={REQUISITION_STATUS_BADGE_VARIANT[requisition.status]}>
                      {REQUISITION_STATUS_LABELS[requisition.status]}
                    </Badge>
                  </TableCell>
                  <TableCell>
                    {requisition.isPublished ? (
                      <Badge variant="default">Live on career site</Badge>
                    ) : (
                      <span className="text-muted-foreground">-</span>
                    )}
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>

      <RequisitionFormDialog
        open={dialogOpen}
        onOpenChange={setDialogOpen}
        onSubmit={(request) => createMutation.mutate(request)}
        isSubmitting={createMutation.isPending}
      />
    </div>
  )
}
