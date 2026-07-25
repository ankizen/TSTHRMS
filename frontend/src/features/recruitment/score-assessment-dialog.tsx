import { useEffect, useState } from "react"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import type { AssessmentSummary } from "./types"

interface ScoreAssessmentDialogProps {
  assessment: AssessmentSummary | null
  onOpenChange: (open: boolean) => void
  onSubmit: (score: number, comments: string | null) => void
  isSubmitting: boolean
  submissionText?: string | null
}

export function ScoreAssessmentDialog({
  assessment, onOpenChange, onSubmit, isSubmitting, submissionText,
}: ScoreAssessmentDialogProps) {
  const [score, setScore] = useState(60)
  const [comments, setComments] = useState("")

  useEffect(() => {
    if (assessment) {
      setScore(assessment.score ?? 60)
      setComments("")
    }
  }, [assessment])

  return (
    <Dialog open={Boolean(assessment)} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[85vh] overflow-y-auto sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>Score assessment</DialogTitle>
        </DialogHeader>
        <div className="flex flex-col gap-4">
          {submissionText && (
            <div className="max-h-48 overflow-y-auto rounded-lg border bg-muted/30 p-3 text-sm whitespace-pre-wrap">
              {submissionText}
            </div>
          )}
          <div className="flex flex-col gap-2">
            <Label htmlFor="assessmentScore">Score (0-100)</Label>
            <Input
              id="assessmentScore"
              type="number"
              min={0}
              max={100}
              value={score}
              onChange={(e) => setScore(Number(e.target.value) || 0)}
            />
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="assessmentComments">Comments (optional)</Label>
            <Textarea id="assessmentComments" value={comments} onChange={(e) => setComments(e.target.value)} />
          </div>
          <DialogFooter>
            <Button disabled={isSubmitting} onClick={() => onSubmit(score, comments || null)}>
              {isSubmitting ? "Saving..." : "Save score"}
            </Button>
          </DialogFooter>
        </div>
      </DialogContent>
    </Dialog>
  )
}
