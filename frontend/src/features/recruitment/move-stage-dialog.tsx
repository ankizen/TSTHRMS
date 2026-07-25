import { useEffect, useState } from "react"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { APPLICATION_STAGE_LABELS } from "./constants"
import type { ApplicationStage } from "./types"

interface MoveStageDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onConfirm: (reason: string) => void
  isSubmitting: boolean
  targetStage: ApplicationStage | null
}

export function MoveStageDialog({ open, onOpenChange, onConfirm, isSubmitting, targetStage }: MoveStageDialogProps) {
  const [reason, setReason] = useState("")

  useEffect(() => {
    if (open) setReason("")
  }, [open])

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>
            Move to {targetStage ? APPLICATION_STAGE_LABELS[targetStage] : ""}
          </DialogTitle>
        </DialogHeader>
        <div className="flex flex-col gap-4">
          <div className="flex flex-col gap-2">
            <Label htmlFor="stageReason">Reason (required)</Label>
            <Textarea id="stageReason" value={reason} onChange={(e) => setReason(e.target.value)} />
          </div>
          <DialogFooter>
            <Button
              variant={targetStage === "Rejected" ? "destructive" : "default"}
              disabled={!reason.trim() || isSubmitting}
              onClick={() => onConfirm(reason)}
            >
              {isSubmitting ? "Saving..." : "Confirm"}
            </Button>
          </DialogFooter>
        </div>
      </DialogContent>
    </Dialog>
  )
}
