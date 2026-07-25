import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { Download, Pencil, Plus, Trash2, Upload } from "lucide-react"
import { useRef, useState } from "react"
import { toast } from "sonner"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { getFamilyMembers } from "@/features/employees/family/api"
import { downloadDocument } from "@/lib/download-document"
import {
  createNominee,
  deleteNominee,
  getNominees,
  updateNominee,
  uploadNomineeConsentDocument,
} from "./api"
import { NOMINATION_TYPE_OPTIONS } from "./constants"
import { NomineeFormDialog } from "./nominee-form-dialog"
import type { NomineeFormValues } from "./schema"
import type { Nominee, NominationType } from "./types"

export function NomineeSection({ employeeId }: { employeeId: string }) {
  const queryClient = useQueryClient()
  const queryKey = ["employees", employeeId, "nominees"]
  const [dialogOpen, setDialogOpen] = useState(false)
  const [editingNominee, setEditingNominee] = useState<Nominee | undefined>(undefined)
  const fileInputTarget = useRef<string | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)

  const { data: nominees = [], isLoading } = useQuery({
    queryKey,
    queryFn: () => getNominees(employeeId),
  })

  const { data: familyMembers = [] } = useQuery({
    queryKey: ["employees", employeeId, "family"],
    queryFn: () => getFamilyMembers(employeeId),
  })

  const invalidate = () => queryClient.invalidateQueries({ queryKey })

  const saveMutation = useMutation({
    mutationFn: (values: NomineeFormValues) => {
      const request = {
        ...values,
        contactNumber: values.contactNumber || null,
        sharePercentage: values.sharePercentage ?? null,
        familyMemberId: values.familyMemberId ?? null,
      }
      return editingNominee
        ? updateNominee(employeeId, editingNominee.id, request)
        : createNominee(employeeId, request)
    },
    onSuccess: async () => {
      toast.success(editingNominee ? "Nominee updated" : "Nominee added")
      setDialogOpen(false)
      await invalidate()
    },
    onError: (error: unknown) => {
      const message =
        (error as { response?: { data?: { error?: string } } })?.response?.data?.error ??
        "Couldn't save the nominee."
      toast.error(message)
    },
  })

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteNominee(employeeId, id),
    onSuccess: async () => {
      toast.success("Nominee removed")
      await invalidate()
    },
    onError: () => toast.error("Couldn't remove the nominee."),
  })

  const uploadMutation = useMutation({
    mutationFn: ({ id, file }: { id: string; file: File }) => uploadNomineeConsentDocument(employeeId, id, file),
    onSuccess: async () => {
      toast.success("Consent document uploaded")
      await invalidate()
    },
    onError: () => toast.error("Couldn't upload the file. PDF, JPG, or PNG under 10MB only."),
  })

  const openCreateDialog = () => {
    setEditingNominee(undefined)
    setDialogOpen(true)
  }

  const openEditDialog = (nominee: Nominee) => {
    setEditingNominee(nominee)
    setDialogOpen(true)
  }

  const triggerUpload = (nomineeId: string) => {
    fileInputTarget.current = nomineeId
    fileInputRef.current?.click()
  }

  const handleFileSelected = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    const nomineeId = fileInputTarget.current
    event.target.value = ""
    if (file && nomineeId) {
      uploadMutation.mutate({ id: nomineeId, file })
    }
  }

  const groupTotal = (type: NominationType) =>
    nominees
      .filter((n) => n.nominationType === type)
      .reduce((sum, n) => sum + (n.sharePercentage ?? 0), 0)

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between">
        <CardTitle>Nominees</CardTitle>
        <Button type="button" variant="outline" size="sm" onClick={openCreateDialog}>
          <Plus />
          Add nominee
        </Button>
      </CardHeader>
      <CardContent className="flex flex-col gap-6">
        <input
          ref={fileInputRef}
          type="file"
          accept="application/pdf,image/jpeg,image/png"
          className="hidden"
          onChange={handleFileSelected}
        />
        {NOMINATION_TYPE_OPTIONS.map((typeOption) => {
          const rows = nominees.filter((n) => n.nominationType === typeOption.value)
          const total = groupTotal(typeOption.value)

          if (!isLoading && rows.length === 0) {
            return null
          }

          return (
            <div key={typeOption.value} className="flex flex-col gap-2">
              <div className="flex items-center gap-2">
                <h3 className="font-medium">{typeOption.label}</h3>
                {typeOption.value !== "Insurance" && rows.length > 0 && (
                  <Badge variant={total === 100 ? "default" : "secondary"}>{total}% allocated</Badge>
                )}
              </div>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Name</TableHead>
                    <TableHead>Relation</TableHead>
                    {typeOption.value !== "Insurance" && <TableHead>Share %</TableHead>}
                    <TableHead>Contact</TableHead>
                    <TableHead>Consent form</TableHead>
                    <TableHead className="text-right">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {isLoading ? (
                    <TableRow>
                      <TableCell colSpan={6} className="h-16 text-center text-muted-foreground">
                        Loading...
                      </TableCell>
                    </TableRow>
                  ) : (
                    rows.map((nominee) => (
                      <TableRow key={nominee.id}>
                        <TableCell>
                          {nominee.name}
                          {nominee.familyMemberName && (
                            <span className="ml-1 text-xs text-muted-foreground">(family member)</span>
                          )}
                        </TableCell>
                        <TableCell>{nominee.relation}</TableCell>
                        {typeOption.value !== "Insurance" && (
                          <TableCell>{nominee.sharePercentage ?? "-"}</TableCell>
                        )}
                        <TableCell>{nominee.contactNumber ?? "-"}</TableCell>
                        <TableCell>
                          {nominee.consentFileName && nominee.consentDocumentId ? (
                            <Button
                              type="button"
                              variant="link"
                              size="sm"
                              className="h-auto p-0"
                              onClick={() => downloadDocument(nominee.consentDocumentId!, nominee.consentFileName!)}
                            >
                              <Download />
                              {nominee.consentFileName}
                            </Button>
                          ) : (
                            <Button
                              type="button"
                              variant="outline"
                              size="sm"
                              onClick={() => triggerUpload(nominee.id)}
                            >
                              <Upload />
                              Upload
                            </Button>
                          )}
                        </TableCell>
                        <TableCell className="text-right">
                          <Button
                            type="button"
                            variant="ghost"
                            size="icon"
                            onClick={() => openEditDialog(nominee)}
                          >
                            <Pencil />
                          </Button>
                          <Button
                            type="button"
                            variant="ghost"
                            size="icon"
                            onClick={() => deleteMutation.mutate(nominee.id)}
                          >
                            <Trash2 />
                          </Button>
                        </TableCell>
                      </TableRow>
                    ))
                  )}
                </TableBody>
              </Table>
            </div>
          )
        })}
        {!isLoading && nominees.length === 0 && (
          <p className="text-center text-muted-foreground">No nominees added yet.</p>
        )}
      </CardContent>

      <NomineeFormDialog
        open={dialogOpen}
        onOpenChange={setDialogOpen}
        nominee={editingNominee}
        familyMembers={familyMembers}
        onSubmit={(values) => saveMutation.mutate(values)}
        isSubmitting={saveMutation.isPending}
      />
    </Card>
  )
}
