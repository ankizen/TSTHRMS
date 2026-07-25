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

interface DecisionDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onSubmit: (comment: string | null) => void
  isSubmitting: boolean
  title: string
  actionLabel: string
  variant?: "default" | "destructive"
}

export function DecisionDialog({
  open, onOpenChange, onSubmit, isSubmitting, title, actionLabel, variant = "default",
}: DecisionDialogProps) {
  const [comment, setComment] = useState("")

  useEffect(() => {
    if (open) setComment("")
  }, [open])

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
        </DialogHeader>
        <div className="flex flex-col gap-4">
          <div className="flex flex-col gap-2">
            <Label htmlFor="decisionComment">Comment (optional)</Label>
            <Textarea id="decisionComment" value={comment} onChange={(e) => setComment(e.target.value)} />
          </div>
          <DialogFooter>
            <Button
              variant={variant}
              disabled={isSubmitting}
              onClick={() => onSubmit(comment || null)}
            >
              {isSubmitting ? "Saving..." : actionLabel}
            </Button>
          </DialogFooter>
        </div>
      </DialogContent>
    </Dialog>
  )
}
