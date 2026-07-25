import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { Check, ClipboardCheck, X } from "lucide-react"
import { useState } from "react"
import { toast } from "sonner"
import { EmptyState } from "@/components/empty-state"
import { TableSkeletonRows } from "@/components/table-skeleton-rows"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { EDITABLE_FIELD_LABELS } from "@/features/my/constants"
import { approveEditRequest, getPendingEditRequests, rejectEditRequest } from "./api"

export function EditRequestsPage() {
  const queryClient = useQueryClient()
  const queryKey = ["edit-requests", "pending"]
  const [notes, setNotes] = useState<Record<string, string>>({})

  const { data: requests = [], isLoading } = useQuery({ queryKey, queryFn: getPendingEditRequests })

  const invalidate = () => queryClient.invalidateQueries({ queryKey })

  const approveMutation = useMutation({
    mutationFn: ({ id, note }: { id: string; note: string | null }) => approveEditRequest(id, note),
    onSuccess: async () => {
      toast.success("Request approved.")
      await invalidate()
    },
    onError: () => toast.error("Couldn't approve the request."),
  })

  const rejectMutation = useMutation({
    mutationFn: ({ id, note }: { id: string; note: string | null }) => rejectEditRequest(id, note),
    onSuccess: async () => {
      toast.success("Request rejected.")
      await invalidate()
    },
    onError: () => toast.error("Couldn't reject the request."),
  })

  return (
    <div className="flex flex-col gap-4">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Employee Edit Requests</h1>
        <p className="text-muted-foreground">{requests.length} pending</p>
      </div>

      <div className="rounded-xl border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Employee</TableHead>
              <TableHead>Field</TableHead>
              <TableHead>Old Value</TableHead>
              <TableHead>New Value</TableHead>
              <TableHead>Note</TableHead>
              <TableHead className="text-right">Actions</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              <TableSkeletonRows columns={6} />
            ) : requests.length === 0 ? (
              <TableRow>
                <TableCell colSpan={6}>
                  <EmptyState
                    icon={ClipboardCheck}
                    title="No pending requests"
                    description="Employee self-service edit requests will show up here for review."
                  />
                </TableCell>
              </TableRow>
            ) : (
              requests.map((request) => (
                <TableRow key={request.id}>
                  <TableCell>{request.employeeName}</TableCell>
                  <TableCell>{EDITABLE_FIELD_LABELS[request.field]}</TableCell>
                  <TableCell className="text-muted-foreground">{request.oldValue ?? "-"}</TableCell>
                  <TableCell>{request.newValue}</TableCell>
                  <TableCell>
                    <Input
                      placeholder="Optional note"
                      className="h-8 w-[160px]"
                      value={notes[request.id] ?? ""}
                      onChange={(event) => setNotes((prev) => ({ ...prev, [request.id]: event.target.value }))}
                    />
                  </TableCell>
                  <TableCell className="text-right">
                    <div className="flex justify-end gap-1">
                      <Button
                        type="button"
                        variant="outline"
                        size="icon"
                        disabled={approveMutation.isPending || rejectMutation.isPending}
                        onClick={() => approveMutation.mutate({ id: request.id, note: notes[request.id] || null })}
                      >
                        <Check className="text-green-600" />
                      </Button>
                      <Button
                        type="button"
                        variant="outline"
                        size="icon"
                        disabled={approveMutation.isPending || rejectMutation.isPending}
                        onClick={() => rejectMutation.mutate({ id: request.id, note: notes[request.id] || null })}
                      >
                        <X className="text-destructive" />
                      </Button>
                    </div>
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
