import { useState } from "react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import type { CreateOrReviseOfferRequest, OfferVersion } from "./types"

interface OfferFormProps {
  onSubmit: (request: CreateOrReviseOfferRequest) => void
  onCancel?: () => void
  isSubmitting: boolean
  isRevision: boolean
  latestVersion?: OfferVersion
}

export function OfferForm({ onSubmit, onCancel, isSubmitting, isRevision, latestVersion }: OfferFormProps) {
  const [designation, setDesignation] = useState(latestVersion?.designation ?? "")
  const [dateOfJoining, setDateOfJoining] = useState(latestVersion?.dateOfJoining ?? "")
  const [annualCtc, setAnnualCtc] = useState(latestVersion?.annualCtc ?? 0)
  const [fixedComponent, setFixedComponent] = useState(latestVersion?.fixedComponent ?? null)
  const [variableComponent, setVariableComponent] = useState(latestVersion?.variableComponent ?? null)
  const [joiningBonus, setJoiningBonus] = useState(latestVersion?.joiningBonus ?? null)
  const [offerLetterText, setOfferLetterText] = useState("")
  const [revisionReason, setRevisionReason] = useState("")

  const isValid = Boolean(dateOfJoining) && annualCtc > 0

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault()
    if (!isValid) return
    onSubmit({
      designation: designation || null,
      dateOfJoining,
      annualCtc,
      fixedComponent,
      variableComponent,
      joiningBonus,
      offerLetterText: offerLetterText || null,
      revisionReason: isRevision ? revisionReason || null : null,
    })
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-3 rounded-xl border bg-muted/30 p-3">
      <div className="flex flex-col gap-1.5">
        <Label className="text-xs">Designation</Label>
        <Input className="h-9" value={designation} onChange={(e) => setDesignation(e.target.value)} />
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div className="flex flex-col gap-1.5">
          <Label className="text-xs">Date of joining</Label>
          <Input
            type="date"
            className="h-9"
            value={dateOfJoining}
            onChange={(e) => setDateOfJoining(e.target.value)}
          />
        </div>
        <div className="flex flex-col gap-1.5">
          <Label className="text-xs">Annual CTC</Label>
          <Input
            type="number"
            min={0}
            className="h-9"
            value={annualCtc}
            onChange={(e) => setAnnualCtc(Number(e.target.value) || 0)}
          />
        </div>
      </div>

      <div className="grid grid-cols-3 gap-3">
        <div className="flex flex-col gap-1.5">
          <Label className="text-xs">Fixed</Label>
          <Input
            type="number"
            min={0}
            className="h-9"
            value={fixedComponent ?? ""}
            onChange={(e) => setFixedComponent(e.target.value ? Number(e.target.value) : null)}
          />
        </div>
        <div className="flex flex-col gap-1.5">
          <Label className="text-xs">Variable</Label>
          <Input
            type="number"
            min={0}
            className="h-9"
            value={variableComponent ?? ""}
            onChange={(e) => setVariableComponent(e.target.value ? Number(e.target.value) : null)}
          />
        </div>
        <div className="flex flex-col gap-1.5">
          <Label className="text-xs">Joining bonus</Label>
          <Input
            type="number"
            min={0}
            className="h-9"
            value={joiningBonus ?? ""}
            onChange={(e) => setJoiningBonus(e.target.value ? Number(e.target.value) : null)}
          />
        </div>
      </div>

      <div className="flex flex-col gap-1.5">
        <Label className="text-xs">Offer letter text (optional - auto-generated if left blank)</Label>
        <Textarea rows={4} value={offerLetterText} onChange={(e) => setOfferLetterText(e.target.value)} />
      </div>

      {isRevision && (
        <div className="flex flex-col gap-1.5">
          <Label className="text-xs">Reason for revision</Label>
          <Input className="h-9" value={revisionReason} onChange={(e) => setRevisionReason(e.target.value)} />
        </div>
      )}

      <div className="flex justify-end gap-2">
        {onCancel && (
          <Button type="button" variant="ghost" size="sm" onClick={onCancel}>Cancel</Button>
        )}
        <Button type="submit" size="sm" disabled={!isValid || isSubmitting}>
          {isSubmitting ? "Saving..." : isRevision ? "Save revision" : "Create offer"}
        </Button>
      </div>
    </form>
  )
}
