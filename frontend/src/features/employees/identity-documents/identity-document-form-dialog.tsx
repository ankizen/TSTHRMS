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
import { IDENTITY_DOCUMENT_TYPE_LABEL, IDENTITY_DOCUMENT_TYPE_OPTIONS } from "./constants"
import { identityDocumentFormSchema, type IdentityDocumentFormValues } from "./schema"
import type { IdentityDocument, IdentityDocumentType } from "./types"

const emptyValues: IdentityDocumentFormValues = {
  documentType: "Pan",
  number: "",
  expiryDate: "",
}

interface IdentityDocumentFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  document?: IdentityDocument
  availableTypes: IdentityDocumentType[]
  onSubmit: (values: IdentityDocumentFormValues) => void
  isSubmitting: boolean
}

export function IdentityDocumentFormDialog({
  open,
  onOpenChange,
  document,
  availableTypes,
  onSubmit,
  isSubmitting,
}: IdentityDocumentFormDialogProps) {
  const {
    control,
    register,
    handleSubmit,
    reset,
    watch,
    formState: { errors },
  } = useForm<IdentityDocumentFormValues>({
    resolver: zodResolver(identityDocumentFormSchema),
    defaultValues: emptyValues,
  })

  const documentType = watch("documentType")

  useEffect(() => {
    if (!open) return

    reset(
      document
        ? { documentType: document.documentType, number: document.numberDisplay, expiryDate: document.expiryDate ?? "" }
        : { ...emptyValues, documentType: availableTypes[0] ?? "Pan" },
    )
  }, [open, document, availableTypes, reset])

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{document ? "Edit identity document" : "Add identity document"}</DialogTitle>
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
            <Label>Document type</Label>
            {document ? (
              <Input value={IDENTITY_DOCUMENT_TYPE_LABEL[document.documentType]} disabled />
            ) : (
              <Controller
                control={control}
                name="documentType"
                render={({ field }) => (
                  <Select value={field.value} onValueChange={field.onChange}>
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {IDENTITY_DOCUMENT_TYPE_OPTIONS.filter((option) =>
                        availableTypes.includes(option.value),
                      ).map((option) => (
                        <SelectItem key={option.value} value={option.value}>
                          {option.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              />
            )}
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="idDocNumber">Number</Label>
            <Input id="idDocNumber" {...register("number")} />
            {errors.number && <p className="text-sm text-destructive">{errors.number.message}</p>}
          </div>
          {documentType === "Passport" && (
            <div className="flex flex-col gap-2">
              <Label htmlFor="idDocExpiry">Expiry date</Label>
              <Input id="idDocExpiry" type="date" {...register("expiryDate")} />
              {errors.expiryDate && (
                <p className="text-sm text-destructive">{errors.expiryDate.message}</p>
              )}
            </div>
          )}
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
