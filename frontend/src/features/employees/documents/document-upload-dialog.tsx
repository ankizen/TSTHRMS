import { useState } from "react"
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
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { Textarea } from "@/components/ui/textarea"
import { EMPLOYEE_DOCUMENT_CATEGORY_OPTIONS } from "./constants"
import type { EmployeeDocumentCategory } from "./types"

interface DocumentUploadDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onSubmit: (category: EmployeeDocumentCategory, notes: string | null, file: File) => void
  isSubmitting: boolean
}

export function DocumentUploadDialog({ open, onOpenChange, onSubmit, isSubmitting }: DocumentUploadDialogProps) {
  const [category, setCategory] = useState<EmployeeDocumentCategory>("OfferLetter")
  const [notes, setNotes] = useState("")
  const [file, setFile] = useState<File | null>(null)

  const handleSubmit = (event: React.FormEvent) => {
    // Rendered through a portal - stop this from also bubbling up as a submit on the outer
    // Employee form (React events bubble the component tree, not the DOM).
    event.preventDefault()
    event.stopPropagation()
    if (!file) return
    onSubmit(category, notes || null, file)
  }

  return (
    <Dialog
      open={open}
      onOpenChange={(next) => {
        if (!next) {
          setCategory("OfferLetter")
          setNotes("")
          setFile(null)
        }
        onOpenChange(next)
      }}
    >
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Upload document</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          <div className="flex flex-col gap-2">
            <Label>Category</Label>
            <Select value={category} onValueChange={(value) => setCategory(value as EmployeeDocumentCategory)}>
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {EMPLOYEE_DOCUMENT_CATEGORY_OPTIONS.map((option) => (
                  <SelectItem key={option.value} value={option.value}>
                    {option.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="documentNotes">Notes (optional)</Label>
            <Textarea id="documentNotes" value={notes} onChange={(e) => setNotes(e.target.value)} />
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="documentFile">File (PDF, JPG, or PNG, up to 10MB)</Label>
            <Input
              id="documentFile"
              type="file"
              accept="application/pdf,image/jpeg,image/png"
              onChange={(e) => setFile(e.target.files?.[0] ?? null)}
            />
          </div>
          <DialogFooter>
            <Button type="submit" disabled={!file || isSubmitting}>
              {isSubmitting ? "Uploading..." : "Upload"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
