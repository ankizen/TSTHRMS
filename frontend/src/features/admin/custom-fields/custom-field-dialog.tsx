import { useEffect, useState } from "react"
import { Button } from "@/components/ui/button"
import { Checkbox } from "@/components/ui/checkbox"
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
import { FIELD_TYPE_OPTIONS } from "./constants"
import type { CustomFieldDefinition, CustomFieldDefinitionWriteRequest } from "./types"

interface CustomFieldDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onSubmit: (request: CustomFieldDefinitionWriteRequest) => void
  isSubmitting: boolean
  editing: CustomFieldDefinition | null
}

const empty: CustomFieldDefinitionWriteRequest = {
  name: "",
  label: "",
  fieldType: "Text",
  options: null,
  isRequired: false,
  displayOrder: 0,
}

export function CustomFieldDialog({ open, onOpenChange, onSubmit, isSubmitting, editing }: CustomFieldDialogProps) {
  const [form, setForm] = useState(empty)
  const [optionsText, setOptionsText] = useState("")

  useEffect(() => {
    if (editing) {
      setForm({
        name: editing.name,
        label: editing.label,
        fieldType: editing.fieldType,
        options: editing.options,
        isRequired: editing.isRequired,
        displayOrder: editing.displayOrder,
      })
      setOptionsText((editing.options ?? []).join(", "))
    } else {
      setForm(empty)
      setOptionsText("")
    }
  }, [editing, open])

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault()
    const options = form.fieldType === "Select"
      ? optionsText.split(",").map((o) => o.trim()).filter(Boolean)
      : null

    onSubmit({ ...form, options })
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{editing ? "Edit custom field" : "New custom field"}</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          <div className="flex flex-col gap-2">
            <Label htmlFor="fieldName">Name (machine key, e.g. shirt_size)</Label>
            <Input
              id="fieldName"
              value={form.name}
              onChange={(event) => setForm((f) => ({ ...f, name: event.target.value.toLowerCase() }))}
            />
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="fieldLabel">Label</Label>
            <Input
              id="fieldLabel"
              value={form.label}
              onChange={(event) => setForm((f) => ({ ...f, label: event.target.value }))}
            />
          </div>
          <div className="flex flex-col gap-2">
            <Label>Type</Label>
            <Select value={form.fieldType} onValueChange={(value) => setForm((f) => ({ ...f, fieldType: value as typeof f.fieldType }))}>
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {FIELD_TYPE_OPTIONS.map((option) => (
                  <SelectItem key={option.value} value={option.value}>
                    {option.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          {form.fieldType === "Select" && (
            <div className="flex flex-col gap-2">
              <Label htmlFor="fieldOptions">Options (comma-separated)</Label>
              <Input id="fieldOptions" value={optionsText} onChange={(event) => setOptionsText(event.target.value)} />
            </div>
          )}
          <div className="flex flex-col gap-2">
            <Label htmlFor="fieldDisplayOrder">Display order</Label>
            <Input
              id="fieldDisplayOrder"
              type="number"
              value={form.displayOrder}
              onChange={(event) => setForm((f) => ({ ...f, displayOrder: Number(event.target.value) }))}
            />
          </div>
          <div className="flex items-center gap-2">
            <Checkbox
              id="fieldRequired"
              checked={form.isRequired}
              onCheckedChange={(checked) => setForm((f) => ({ ...f, isRequired: checked === true }))}
            />
            <Label htmlFor="fieldRequired" className="font-normal">
              Required
            </Label>
          </div>
          <DialogFooter>
            <Button type="submit" disabled={!form.name || !form.label || isSubmitting}>
              {isSubmitting ? "Saving..." : "Save"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
