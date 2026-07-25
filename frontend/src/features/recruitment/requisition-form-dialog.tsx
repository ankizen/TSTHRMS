import { useQuery } from "@tanstack/react-query"
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
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { Textarea } from "@/components/ui/textarea"
import { getLegalEntities, getProducts } from "@/features/employees/api"
import { EMPLOYMENT_TYPE_OPTIONS } from "@/features/employees/constants"
import type { EmploymentType } from "@/features/employees/types"
import { REQUISITION_REASON_OPTIONS } from "./constants"
import type { JobRequisition, JobRequisitionWriteRequest, RequisitionReason } from "./types"

interface RequisitionFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onSubmit: (request: JobRequisitionWriteRequest) => void
  isSubmitting: boolean
  requisition?: JobRequisition | null
}

const empty: JobRequisitionWriteRequest = {
  title: "",
  legalEntityId: "",
  productId: "",
  grade: null,
  department: null,
  employmentType: "FullTime",
  openings: 1,
  budgetPerOpening: null,
  reason: "NewRole",
  justificationNotes: null,
  interviewRoundCount: 2,
}

export function RequisitionFormDialog({
  open, onOpenChange, onSubmit, isSubmitting, requisition,
}: RequisitionFormDialogProps) {
  const [form, setForm] = useState<JobRequisitionWriteRequest>(empty)

  const { data: legalEntities = [] } = useQuery({ queryKey: ["legal-entities"], queryFn: getLegalEntities, enabled: open })
  const { data: products = [] } = useQuery({ queryKey: ["products"], queryFn: getProducts, enabled: open })

  useEffect(() => {
    if (open) {
      setForm(
        requisition
          ? {
              title: requisition.title,
              legalEntityId: requisition.legalEntityId,
              productId: requisition.productId,
              grade: requisition.grade,
              department: requisition.department,
              employmentType: requisition.employmentType,
              openings: requisition.openings,
              budgetPerOpening: requisition.budgetPerOpening,
              reason: requisition.reason,
              justificationNotes: requisition.justificationNotes,
              interviewRoundCount: requisition.interviewRoundCount,
            }
          : empty,
      )
    }
  }, [open, requisition])

  const isValid = Boolean(form.title && form.legalEntityId && form.productId && form.openings > 0)

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault()
    if (!isValid) return
    onSubmit(form)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[85vh] overflow-y-auto sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>{requisition ? "Edit requisition" : "New requisition"}</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          <div className="flex flex-col gap-2">
            <Label htmlFor="reqTitle">Role title</Label>
            <Input
              id="reqTitle"
              value={form.title}
              onChange={(e) => setForm((f) => ({ ...f, title: e.target.value }))}
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="flex flex-col gap-2">
              <Label>Legal entity</Label>
              <Select value={form.legalEntityId} onValueChange={(v) => setForm((f) => ({ ...f, legalEntityId: v }))}>
                <SelectTrigger><SelectValue placeholder="Select entity" /></SelectTrigger>
                <SelectContent>
                  {legalEntities.map((entity) => (
                    <SelectItem key={entity.id} value={entity.id}>{entity.name}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="flex flex-col gap-2">
              <Label>Product</Label>
              <Select value={form.productId} onValueChange={(v) => setForm((f) => ({ ...f, productId: v }))}>
                <SelectTrigger><SelectValue placeholder="Select product" /></SelectTrigger>
                <SelectContent>
                  {products.map((product) => (
                    <SelectItem key={product.id} value={product.id}>{product.name}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="flex flex-col gap-2">
              <Label htmlFor="reqDepartment">Department</Label>
              <Input
                id="reqDepartment"
                value={form.department ?? ""}
                onChange={(e) => setForm((f) => ({ ...f, department: e.target.value || null }))}
              />
            </div>
            <div className="flex flex-col gap-2">
              <Label htmlFor="reqGrade">Grade</Label>
              <Input
                id="reqGrade"
                value={form.grade ?? ""}
                onChange={(e) => setForm((f) => ({ ...f, grade: e.target.value || null }))}
              />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="flex flex-col gap-2">
              <Label>Employment type</Label>
              <Select
                value={form.employmentType}
                onValueChange={(v) => setForm((f) => ({ ...f, employmentType: v as EmploymentType }))}
              >
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  {EMPLOYMENT_TYPE_OPTIONS.map((option) => (
                    <SelectItem key={option.value} value={option.value}>{option.label}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="flex flex-col gap-2">
              <Label>Reason</Label>
              <Select
                value={form.reason}
                onValueChange={(v) => setForm((f) => ({ ...f, reason: v as RequisitionReason }))}
              >
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  {REQUISITION_REASON_OPTIONS.map((option) => (
                    <SelectItem key={option.value} value={option.value}>{option.label}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          <div className="grid grid-cols-3 gap-4">
            <div className="flex flex-col gap-2">
              <Label htmlFor="reqOpenings">Openings</Label>
              <Input
                id="reqOpenings"
                type="number"
                min={1}
                value={form.openings}
                onChange={(e) => setForm((f) => ({ ...f, openings: Number(e.target.value) || 1 }))}
              />
            </div>
            <div className="flex flex-col gap-2">
              <Label htmlFor="reqBudget">Budget/opening</Label>
              <Input
                id="reqBudget"
                type="number"
                min={0}
                value={form.budgetPerOpening ?? ""}
                onChange={(e) => setForm((f) => ({ ...f, budgetPerOpening: e.target.value ? Number(e.target.value) : null }))}
              />
            </div>
            <div className="flex flex-col gap-2">
              <Label htmlFor="reqRounds">Interview rounds</Label>
              <Input
                id="reqRounds"
                type="number"
                min={1}
                max={6}
                value={form.interviewRoundCount}
                onChange={(e) => setForm((f) => ({ ...f, interviewRoundCount: Number(e.target.value) || 1 }))}
              />
            </div>
          </div>

          <div className="flex flex-col gap-2">
            <Label htmlFor="reqNotes">Justification notes (optional)</Label>
            <Textarea
              id="reqNotes"
              value={form.justificationNotes ?? ""}
              onChange={(e) => setForm((f) => ({ ...f, justificationNotes: e.target.value || null }))}
            />
          </div>

          <DialogFooter>
            <Button type="submit" disabled={!isValid || isSubmitting}>
              {isSubmitting ? "Saving..." : requisition ? "Save changes" : "Create requisition"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
