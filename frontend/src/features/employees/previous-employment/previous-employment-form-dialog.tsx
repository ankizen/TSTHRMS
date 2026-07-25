import { zodResolver } from "@hookform/resolvers/zod"
import { useEffect } from "react"
import { useForm } from "react-hook-form"
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
import { previousEmploymentFormSchema, type PreviousEmploymentFormValues } from "./schema"
import type { PreviousEmploymentRecord } from "./types"

const emptyValues: PreviousEmploymentFormValues = {
  companyName: "",
  designation: "",
  yearsOfExperience: null,
  dateOfJoining: "",
  dateOfLeaving: "",
  reasonForLeaving: "",
  previousUan: "",
}

interface PreviousEmploymentFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  record?: PreviousEmploymentRecord
  onSubmit: (values: PreviousEmploymentFormValues) => void
  isSubmitting: boolean
}

export function PreviousEmploymentFormDialog({
  open,
  onOpenChange,
  record,
  onSubmit,
  isSubmitting,
}: PreviousEmploymentFormDialogProps) {
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<PreviousEmploymentFormValues>({
    resolver: zodResolver(previousEmploymentFormSchema),
    defaultValues: emptyValues,
  })

  useEffect(() => {
    if (!open) return

    reset(
      record
        ? {
            companyName: record.companyName,
            designation: record.designation ?? "",
            yearsOfExperience: record.yearsOfExperience,
            dateOfJoining: record.dateOfJoining,
            dateOfLeaving: record.dateOfLeaving,
            reasonForLeaving: record.reasonForLeaving ?? "",
            previousUan: record.previousUan ?? "",
          }
        : emptyValues,
    )
  }, [open, record, reset])

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{record ? "Edit previous employment" : "Add previous employment"}</DialogTitle>
        </DialogHeader>
        <form
          onSubmit={(event) => {
            // Rendered through a portal - stop this from also bubbling up as a submit
            // on the outer Employee form (React events bubble the component tree, not the DOM).
            event.stopPropagation()
            void handleSubmit(onSubmit)(event)
          }}
          className="flex flex-col gap-4"
        >
          <div className="flex flex-col gap-2">
            <Label htmlFor="companyName">Company name</Label>
            <Input id="companyName" {...register("companyName")} />
            {errors.companyName && (
              <p className="text-sm text-destructive">{errors.companyName.message}</p>
            )}
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div className="flex flex-col gap-2">
              <Label htmlFor="prevDesignation">Designation</Label>
              <Input id="prevDesignation" {...register("designation")} />
            </div>
            <div className="flex flex-col gap-2">
              <Label htmlFor="yearsOfExperience">Years of experience</Label>
              <Input
                id="yearsOfExperience"
                type="number"
                step="0.1"
                {...register("yearsOfExperience", { valueAsNumber: true })}
              />
              {errors.yearsOfExperience && (
                <p className="text-sm text-destructive">{errors.yearsOfExperience.message}</p>
              )}
            </div>
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div className="flex flex-col gap-2">
              <Label htmlFor="prevDateOfJoining">Date of joining</Label>
              <Input id="prevDateOfJoining" type="date" {...register("dateOfJoining")} />
              {errors.dateOfJoining && (
                <p className="text-sm text-destructive">{errors.dateOfJoining.message}</p>
              )}
            </div>
            <div className="flex flex-col gap-2">
              <Label htmlFor="dateOfLeaving">Date of leaving</Label>
              <Input id="dateOfLeaving" type="date" {...register("dateOfLeaving")} />
              {errors.dateOfLeaving && (
                <p className="text-sm text-destructive">{errors.dateOfLeaving.message}</p>
              )}
            </div>
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="reasonForLeaving">Reason for leaving</Label>
            <Textarea id="reasonForLeaving" {...register("reasonForLeaving")} />
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="previousUan">Previous UAN (if different)</Label>
            <Input id="previousUan" {...register("previousUan")} />
          </div>
          <DialogFooter>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? "Saving..." : "Save"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
