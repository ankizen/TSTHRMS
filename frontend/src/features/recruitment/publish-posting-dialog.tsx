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
import type { JobPosting, PublishJobPostingRequest } from "./types"

interface PublishPostingDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onSubmit: (request: PublishJobPostingRequest) => void
  isSubmitting: boolean
  existingPosting: JobPosting | null
}

export function PublishPostingDialog({
  open, onOpenChange, onSubmit, isSubmitting, existingPosting,
}: PublishPostingDialogProps) {
  const [description, setDescription] = useState("")
  const [location, setLocation] = useState("")

  useEffect(() => {
    if (open) {
      setDescription(existingPosting?.description ?? "")
      setLocation(existingPosting?.location ?? "")
    }
  }, [open, existingPosting])

  const isNew = !existingPosting
  const isValid = !isNew || description.trim().length > 0

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault()
    if (!isValid) return
    onSubmit({ description: description || null, location: location || null })
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>{isNew ? "Publish to career site" : "Republish to career site"}</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          <div className="flex flex-col gap-2">
            <Label htmlFor="postingDescription">Job description</Label>
            <Textarea
              id="postingDescription"
              rows={8}
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Responsibilities, requirements, and what makes this role great..."
            />
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="postingLocation">Location</Label>
            <Input
              id="postingLocation"
              value={location}
              onChange={(e) => setLocation(e.target.value)}
              placeholder="e.g. Mumbai (Hybrid)"
            />
          </div>
          <DialogFooter>
            <Button type="submit" disabled={!isValid || isSubmitting}>
              {isSubmitting ? "Publishing..." : "Publish"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
