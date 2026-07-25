import { zodResolver } from "@hookform/resolvers/zod"
import { useEffect } from "react"
import { Controller, useForm } from "react-hook-form"
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
import { QUALIFICATION_LEVEL_OPTIONS } from "./constants"
import { educationFormSchema, type EducationFormValues } from "./schema"
import type { EducationRecord } from "./types"

const emptyValues: EducationFormValues = {
  qualificationLevel: "Graduate",
  degreeName: "",
  instituteName: "",
  yearOfPassing: new Date().getFullYear(),
  specialization: "",
}

interface EducationFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  record?: EducationRecord
  onSubmit: (values: EducationFormValues) => void
  isSubmitting: boolean
}

export function EducationFormDialog({
  open,
  onOpenChange,
  record,
  onSubmit,
  isSubmitting,
}: EducationFormDialogProps) {
  const {
    control,
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<EducationFormValues>({
    resolver: zodResolver(educationFormSchema),
    defaultValues: emptyValues,
  })

  useEffect(() => {
    if (!open) return

    reset(
      record
        ? {
            qualificationLevel: record.qualificationLevel,
            degreeName: record.degreeName,
            instituteName: record.instituteName,
            yearOfPassing: record.yearOfPassing,
            specialization: record.specialization ?? "",
          }
        : emptyValues,
    )
  }, [open, record, reset])

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{record ? "Edit qualification" : "Add qualification"}</DialogTitle>
        </DialogHeader>
        <form
          onSubmit={(event) => {
            // This dialog renders through a portal, so without this the submit would
            // still bubble to the outer Employee form via React's tree (portals don't
            // stop React event propagation even though the DOM isn't actually nested).
            event.stopPropagation()
            void handleSubmit(onSubmit)(event)
          }}
          className="flex flex-col gap-4"
        >
          <div className="flex flex-col gap-2">
            <Label>Qualification level</Label>
            <Controller
              control={control}
              name="qualificationLevel"
              render={({ field }) => (
                <Select value={field.value} onValueChange={field.onChange}>
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {QUALIFICATION_LEVEL_OPTIONS.map((option) => (
                      <SelectItem key={option.value} value={option.value}>
                        {option.label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
            />
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="degreeName">Degree / course name</Label>
            <Input id="degreeName" placeholder="B.Com, MBA..." {...register("degreeName")} />
            {errors.degreeName && (
              <p className="text-sm text-destructive">{errors.degreeName.message}</p>
            )}
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="instituteName">Institute / university</Label>
            <Input id="instituteName" {...register("instituteName")} />
            {errors.instituteName && (
              <p className="text-sm text-destructive">{errors.instituteName.message}</p>
            )}
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div className="flex flex-col gap-2">
              <Label htmlFor="yearOfPassing">Year of passing</Label>
              <Input
                id="yearOfPassing"
                type="number"
                {...register("yearOfPassing", { valueAsNumber: true })}
              />
              {errors.yearOfPassing && (
                <p className="text-sm text-destructive">{errors.yearOfPassing.message}</p>
              )}
            </div>
            <div className="flex flex-col gap-2">
              <Label htmlFor="specialization">Specialization</Label>
              <Input id="specialization" placeholder="Optional" {...register("specialization")} />
            </div>
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
