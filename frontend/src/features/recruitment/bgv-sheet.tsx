import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { useState } from "react"
import { toast } from "sonner"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Checkbox } from "@/components/ui/checkbox"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet"
import { Textarea } from "@/components/ui/textarea"
import { getBgv, initiateBgv, updateBgvStatus } from "./api"
import { BGV_STATUS_BADGE_VARIANT, BGV_STATUS_LABELS } from "./constants"
import type { BgvStatus } from "./types"

interface BgvSheetProps {
  applicationId: string | null
  candidateName: string | null
  onOpenChange: (open: boolean) => void
}

export function BgvSheet({ applicationId, candidateName, onOpenChange }: BgvSheetProps) {
  const queryClient = useQueryClient()
  const queryKey = ["recruitment", "applications", applicationId, "bgv"]

  const [vendorReference, setVendorReference] = useState("")
  const [isConditionalJoining, setIsConditionalJoining] = useState(false)
  const [discrepancyNotes, setDiscrepancyNotes] = useState("")

  const { data: bgv, isLoading } = useQuery({
    queryKey,
    queryFn: () => getBgv(applicationId!),
    enabled: Boolean(applicationId),
  })

  const invalidate = () => queryClient.invalidateQueries({ queryKey })

  const initiateMutation = useMutation({
    mutationFn: () => initiateBgv(applicationId!, { vendorReference: vendorReference || null, isConditionalJoining }),
    onSuccess: async () => { toast.success("Background verification initiated."); await invalidate() },
    onError: () => toast.error("Couldn't initiate background verification."),
  })

  const statusMutation = useMutation({
    mutationFn: (status: BgvStatus) =>
      updateBgvStatus(applicationId!, { status, notes: status === "DiscrepancyFound" ? discrepancyNotes || null : null }),
    onSuccess: async () => { toast.success("Status updated."); await invalidate() },
    onError: () => toast.error("Couldn't update the status."),
  })

  return (
    <Sheet open={Boolean(applicationId)} onOpenChange={onOpenChange}>
      <SheetContent className="sm:max-w-md">
        <SheetHeader>
          <SheetTitle>Background Verification{candidateName ? ` - ${candidateName}` : ""}</SheetTitle>
        </SheetHeader>

        <div className="flex flex-col gap-4 overflow-y-auto px-4 pb-4">
          {isLoading ? (
            <p className="text-sm text-muted-foreground">Loading...</p>
          ) : bgv ? (
            <>
              <div className="flex items-center justify-between">
                <Badge variant={BGV_STATUS_BADGE_VARIANT[bgv.status]}>{BGV_STATUS_LABELS[bgv.status]}</Badge>
                {bgv.isConditionalJoining && <Badge variant="secondary">Conditional joining</Badge>}
              </div>

              {bgv.vendorReference && (
                <p className="text-sm text-muted-foreground">Vendor reference: {bgv.vendorReference}</p>
              )}
              {bgv.discrepancyNotes && (
                <p className="rounded-lg bg-destructive/10 px-3 py-2 text-sm text-destructive">{bgv.discrepancyNotes}</p>
              )}

              {bgv.status === "NotStarted" ? (
                <div className="flex flex-col gap-3 rounded-xl border p-3">
                  <div className="flex flex-col gap-1.5">
                    <Label className="text-xs">Vendor reference (optional)</Label>
                    <Input
                      className="h-9"
                      value={vendorReference}
                      onChange={(e) => setVendorReference(e.target.value)}
                      placeholder="e.g. AuthBridge case ID"
                    />
                  </div>
                  <div className="flex items-center gap-2">
                    <Checkbox
                      id="conditionalJoining"
                      checked={isConditionalJoining}
                      onCheckedChange={(checked) => setIsConditionalJoining(checked === true)}
                    />
                    <Label htmlFor="conditionalJoining" className="cursor-pointer font-normal text-muted-foreground">
                      Allow joining while verification is in progress
                    </Label>
                  </div>
                  <Button size="sm" onClick={() => initiateMutation.mutate()} disabled={initiateMutation.isPending}>
                    Initiate verification
                  </Button>
                </div>
              ) : bgv.status !== "Clear" && (
                <div className="flex flex-col gap-3 rounded-xl border p-3">
                  <div className="flex flex-wrap gap-2">
                    <Button size="sm" variant="outline" onClick={() => statusMutation.mutate("InProgress")} disabled={statusMutation.isPending}>
                      Mark in progress
                    </Button>
                    <Button size="sm" onClick={() => statusMutation.mutate("Clear")} disabled={statusMutation.isPending}>
                      Mark clear
                    </Button>
                  </div>
                  <div className="flex flex-col gap-1.5">
                    <Label className="text-xs">Discrepancy notes (if flagging one)</Label>
                    <Textarea rows={2} value={discrepancyNotes} onChange={(e) => setDiscrepancyNotes(e.target.value)} />
                    <Button
                      size="sm"
                      variant="destructive"
                      className="w-fit"
                      onClick={() => statusMutation.mutate("DiscrepancyFound")}
                      disabled={statusMutation.isPending || !discrepancyNotes.trim()}
                    >
                      Flag discrepancy
                    </Button>
                  </div>
                </div>
              )}
            </>
          ) : null}
        </div>
      </SheetContent>
    </Sheet>
  )
}
