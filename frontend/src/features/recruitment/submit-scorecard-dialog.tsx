import { useState } from "react"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Label } from "@/components/ui/label"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { Textarea } from "@/components/ui/textarea"
import { INTERVIEW_RECOMMENDATION_OPTIONS } from "./constants"
import type { InterviewRecommendation, SubmitScorecardRequest } from "./types"

interface SubmitScorecardDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onSubmit: (request: SubmitScorecardRequest) => void
  isSubmitting: boolean
  candidateName: string | null
}

const emptyRatings = {
  technicalSkillsRating: 3,
  communicationRating: 3,
  problemSolvingRating: 3,
  cultureFitRating: 3,
}

const RATING_CRITERIA: { key: keyof typeof emptyRatings; label: string }[] = [
  { key: "technicalSkillsRating", label: "Technical Skills" },
  { key: "communicationRating", label: "Communication" },
  { key: "problemSolvingRating", label: "Problem Solving" },
  { key: "cultureFitRating", label: "Culture Fit" },
]

export function SubmitScorecardDialog({
  open, onOpenChange, onSubmit, isSubmitting, candidateName,
}: SubmitScorecardDialogProps) {
  const [ratings, setRatings] = useState({ ...emptyRatings })
  const [recommendation, setRecommendation] = useState<InterviewRecommendation>("Yes")
  const [comments, setComments] = useState("")

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault()
    onSubmit({ ...ratings, recommendation, comments: comments || null })
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>Interview feedback{candidateName ? ` - ${candidateName}` : ""}</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          {RATING_CRITERIA.map((criterion) => (
            <div key={criterion.key} className="flex flex-col gap-2">
              <div className="flex items-center justify-between">
                <Label>{criterion.label}</Label>
                <span className="text-sm text-muted-foreground">{ratings[criterion.key]}/5</span>
              </div>
              <input
                type="range"
                min={1}
                max={5}
                value={ratings[criterion.key]}
                onChange={(e) => setRatings((prev) => ({ ...prev, [criterion.key]: Number(e.target.value) }))}
                className="w-full"
              />
            </div>
          ))}

          <div className="flex flex-col gap-2">
            <Label>Overall recommendation</Label>
            <Select value={recommendation} onValueChange={(value) => setRecommendation(value as InterviewRecommendation)}>
              <SelectTrigger><SelectValue /></SelectTrigger>
              <SelectContent>
                {INTERVIEW_RECOMMENDATION_OPTIONS.map((option) => (
                  <SelectItem key={option.value} value={option.value}>{option.label}</SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="flex flex-col gap-2">
            <Label htmlFor="scorecardComments">Comments (optional)</Label>
            <Textarea id="scorecardComments" value={comments} onChange={(e) => setComments(e.target.value)} />
          </div>

          <DialogFooter>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? "Submitting..." : "Submit feedback"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
