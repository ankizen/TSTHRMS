import { zodResolver } from "@hookform/resolvers/zod"
import { useEffect } from "react"
import { Controller, useForm } from "react-hook-form"
import type { FamilyMember } from "@/features/employees/family/types"
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
import { NOMINATION_TYPE_OPTIONS } from "./constants"
import { nomineeFormSchema, type NomineeFormValues } from "./schema"
import type { Nominee } from "./types"

const NONE_VALUE = "none"

const emptyValues: NomineeFormValues = {
  nominationType: "ProvidentFund",
  name: "",
  relation: "",
  sharePercentage: null,
  contactNumber: "",
  familyMemberId: null,
}

interface NomineeFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  nominee?: Nominee
  familyMembers: FamilyMember[]
  onSubmit: (values: NomineeFormValues) => void
  isSubmitting: boolean
}

export function NomineeFormDialog({
  open,
  onOpenChange,
  nominee,
  familyMembers,
  onSubmit,
  isSubmitting,
}: NomineeFormDialogProps) {
  const {
    control,
    register,
    handleSubmit,
    reset,
    setValue,
    watch,
    formState: { errors },
  } = useForm<NomineeFormValues>({
    resolver: zodResolver(nomineeFormSchema),
    defaultValues: emptyValues,
  })

  const nominationType = watch("nominationType")

  useEffect(() => {
    if (!open) return

    reset(
      nominee
        ? {
            nominationType: nominee.nominationType,
            name: nominee.name,
            relation: nominee.relation,
            sharePercentage: nominee.sharePercentage,
            contactNumber: nominee.contactNumber ?? "",
            familyMemberId: nominee.familyMemberId,
          }
        : emptyValues,
    )
  }, [open, nominee, reset])

  const handleFamilyMemberChange = (value: string) => {
    if (value === NONE_VALUE) {
      setValue("familyMemberId", null)
      return
    }

    setValue("familyMemberId", value)
    const member = familyMembers.find((f) => f.id === value)
    if (member) {
      setValue("name", member.name)
      setValue("relation", member.relation)
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{nominee ? "Edit nominee" : "Add nominee"}</DialogTitle>
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
            <Label>Nomination type</Label>
            <Controller
              control={control}
              name="nominationType"
              render={({ field }) => (
                <Select value={field.value} onValueChange={field.onChange}>
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {NOMINATION_TYPE_OPTIONS.map((option) => (
                      <SelectItem key={option.value} value={option.value}>
                        {option.label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
            />
          </div>

          {familyMembers.length > 0 && (
            <div className="flex flex-col gap-2">
              <Label>Link an existing family member (optional)</Label>
              <Select value={watch("familyMemberId") ?? NONE_VALUE} onValueChange={handleFamilyMemberChange}>
                <SelectTrigger>
                  <SelectValue placeholder="None" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={NONE_VALUE}>None - enter details manually</SelectItem>
                  {familyMembers.map((member) => (
                    <SelectItem key={member.id} value={member.id}>
                      {member.name} ({member.relation})
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          )}

          <div className="flex flex-col gap-2">
            <Label htmlFor="nomineeName">Name</Label>
            <Input id="nomineeName" {...register("name")} />
            {errors.name && <p className="text-sm text-destructive">{errors.name.message}</p>}
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="nomineeRelation">Relation</Label>
            <Input id="nomineeRelation" {...register("relation")} />
            {errors.relation && <p className="text-sm text-destructive">{errors.relation.message}</p>}
          </div>
          {nominationType !== "Insurance" && (
            <div className="flex flex-col gap-2">
              <Label htmlFor="sharePercentage">Share percentage</Label>
              <Input
                id="sharePercentage"
                type="number"
                step="0.01"
                {...register("sharePercentage", { valueAsNumber: true })}
              />
              {errors.sharePercentage && (
                <p className="text-sm text-destructive">{errors.sharePercentage.message}</p>
              )}
            </div>
          )}
          <div className="flex flex-col gap-2">
            <Label htmlFor="nomineeContactNumber">Contact number</Label>
            <Input id="nomineeContactNumber" {...register("contactNumber")} />
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
