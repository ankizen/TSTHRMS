import { zodResolver } from "@hookform/resolvers/zod"
import { useEffect } from "react"
import { Controller, useForm } from "react-hook-form"
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
import { FAMILY_RELATION_OPTIONS } from "./constants"
import { familyFormSchema, type FamilyFormValues } from "./schema"
import type { FamilyMember } from "./types"

const emptyValues: FamilyFormValues = {
  relation: "Spouse",
  name: "",
  dateOfBirth: "",
  isDependent: true,
  isDifferentlyAbled: false,
}

interface FamilyFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  member?: FamilyMember
  onSubmit: (values: FamilyFormValues) => void
  isSubmitting: boolean
}

export function FamilyFormDialog({
  open,
  onOpenChange,
  member,
  onSubmit,
  isSubmitting,
}: FamilyFormDialogProps) {
  const {
    control,
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<FamilyFormValues>({
    resolver: zodResolver(familyFormSchema),
    defaultValues: emptyValues,
  })

  useEffect(() => {
    if (!open) return

    reset(
      member
        ? {
            relation: member.relation,
            name: member.name,
            dateOfBirth: member.dateOfBirth ?? "",
            isDependent: member.isDependent,
            isDifferentlyAbled: member.isDifferentlyAbled,
          }
        : emptyValues,
    )
  }, [open, member, reset])

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{member ? "Edit family member" : "Add family member"}</DialogTitle>
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
            <Label>Relation</Label>
            <Controller
              control={control}
              name="relation"
              render={({ field }) => (
                <Select value={field.value} onValueChange={field.onChange}>
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {FAMILY_RELATION_OPTIONS.map((option) => (
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
            <Label htmlFor="familyName">Name</Label>
            <Input id="familyName" {...register("name")} />
            {errors.name && <p className="text-sm text-destructive">{errors.name.message}</p>}
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="familyDateOfBirth">Date of birth</Label>
            <Input id="familyDateOfBirth" type="date" {...register("dateOfBirth")} />
          </div>
          <div className="flex items-center gap-2">
            <Controller
              control={control}
              name="isDependent"
              render={({ field }) => (
                <Checkbox
                  id="isDependent"
                  checked={field.value}
                  onCheckedChange={(checked) => field.onChange(checked === true)}
                />
              )}
            />
            <Label htmlFor="isDependent" className="font-normal">
              Dependent (for insurance / ESIC)
            </Label>
          </div>
          <div className="flex items-center gap-2">
            <Controller
              control={control}
              name="isDifferentlyAbled"
              render={({ field }) => (
                <Checkbox
                  id="isDifferentlyAbled"
                  checked={field.value}
                  onCheckedChange={(checked) => field.onChange(checked === true)}
                />
              )}
            />
            <Label htmlFor="isDifferentlyAbled" className="font-normal">
              Differently-abled (affects gratuity nomination)
            </Label>
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
