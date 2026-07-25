import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { Download, Pencil, Plus, Trash2, Upload } from "lucide-react"
import { useRef, useState } from "react"
import { toast } from "sonner"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { downloadDocument } from "@/lib/download-document"
import {
  createPreviousEmploymentRecord,
  deletePreviousEmploymentRecord,
  getPreviousEmploymentRecords,
  updatePreviousEmploymentRecord,
  uploadRelievingLetter,
  uploadSalarySlip,
} from "./api"
import { PreviousEmploymentFormDialog } from "./previous-employment-form-dialog"
import type { PreviousEmploymentFormValues } from "./schema"
import type { PreviousEmploymentRecord } from "./types"

type DocumentSlot = "relieving-letter" | "salary-slip"

function DocumentCell({
  fileName,
  documentId,
  onUploadClick,
}: {
  fileName: string | null
  documentId: string | null
  onUploadClick: () => void
}) {
  if (fileName && documentId) {
    return (
      <Button
        type="button"
        variant="link"
        size="sm"
        className="h-auto p-0"
        onClick={() => downloadDocument(documentId, fileName)}
      >
        <Download />
        {fileName}
      </Button>
    )
  }

  return (
    <Button type="button" variant="outline" size="sm" onClick={onUploadClick}>
      <Upload />
      Upload
    </Button>
  )
}

export function PreviousEmploymentSection({ employeeId }: { employeeId: string }) {
  const queryClient = useQueryClient()
  const queryKey = ["employees", employeeId, "previous-employment"]
  const [dialogOpen, setDialogOpen] = useState(false)
  const [editingRecord, setEditingRecord] = useState<PreviousEmploymentRecord | undefined>(undefined)
  const uploadTarget = useRef<{ recordId: string; slot: DocumentSlot } | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)

  const { data: records = [], isLoading } = useQuery({
    queryKey,
    queryFn: () => getPreviousEmploymentRecords(employeeId),
  })

  const invalidate = () => queryClient.invalidateQueries({ queryKey })

  const saveMutation = useMutation({
    mutationFn: (values: PreviousEmploymentFormValues) => {
      const request = {
        ...values,
        designation: values.designation || null,
        yearsOfExperience: values.yearsOfExperience ?? null,
        reasonForLeaving: values.reasonForLeaving || null,
        previousUan: values.previousUan || null,
      }
      return editingRecord
        ? updatePreviousEmploymentRecord(employeeId, editingRecord.id, request)
        : createPreviousEmploymentRecord(employeeId, request)
    },
    onSuccess: async () => {
      toast.success(editingRecord ? "Previous employment updated" : "Previous employment added")
      setDialogOpen(false)
      await invalidate()
    },
    onError: () => toast.error("Couldn't save the previous employment record."),
  })

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deletePreviousEmploymentRecord(employeeId, id),
    onSuccess: async () => {
      toast.success("Previous employment removed")
      await invalidate()
    },
    onError: () => toast.error("Couldn't remove the record."),
  })

  const uploadMutation = useMutation({
    mutationFn: ({ id, slot, file }: { id: string; slot: DocumentSlot; file: File }) =>
      slot === "relieving-letter"
        ? uploadRelievingLetter(employeeId, id, file)
        : uploadSalarySlip(employeeId, id, file),
    onSuccess: async () => {
      toast.success("File uploaded")
      await invalidate()
    },
    onError: () => toast.error("Couldn't upload the file. PDF, JPG, or PNG under 10MB only."),
  })

  const openCreateDialog = () => {
    setEditingRecord(undefined)
    setDialogOpen(true)
  }

  const openEditDialog = (record: PreviousEmploymentRecord) => {
    setEditingRecord(record)
    setDialogOpen(true)
  }

  const triggerUpload = (recordId: string, slot: DocumentSlot) => {
    uploadTarget.current = { recordId, slot }
    fileInputRef.current?.click()
  }

  const handleFileSelected = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    const target = uploadTarget.current
    event.target.value = ""
    if (file && target) {
      uploadMutation.mutate({ id: target.recordId, slot: target.slot, file })
    }
  }

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between">
        <CardTitle>Previous Employment</CardTitle>
        <Button type="button" variant="outline" size="sm" onClick={openCreateDialog}>
          <Plus />
          Add previous employment
        </Button>
      </CardHeader>
      <CardContent>
        <input
          ref={fileInputRef}
          type="file"
          accept="application/pdf,image/jpeg,image/png"
          className="hidden"
          onChange={handleFileSelected}
        />
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Company</TableHead>
              <TableHead>Designation</TableHead>
              <TableHead>Joining</TableHead>
              <TableHead>Leaving</TableHead>
              <TableHead>Relieving letter</TableHead>
              <TableHead>Salary slip</TableHead>
              <TableHead className="text-right">Actions</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              <TableRow>
                <TableCell colSpan={7} className="h-20 text-center text-muted-foreground">
                  Loading...
                </TableCell>
              </TableRow>
            ) : records.length === 0 ? (
              <TableRow>
                <TableCell colSpan={7} className="h-20 text-center text-muted-foreground">
                  No previous employment added yet.
                </TableCell>
              </TableRow>
            ) : (
              records.map((record) => (
                <TableRow key={record.id}>
                  <TableCell>{record.companyName}</TableCell>
                  <TableCell>{record.designation ?? "-"}</TableCell>
                  <TableCell>{record.dateOfJoining}</TableCell>
                  <TableCell>{record.dateOfLeaving}</TableCell>
                  <TableCell>
                    <DocumentCell
                      fileName={record.relievingLetterFileName}
                      documentId={record.relievingLetterDocumentId}
                      onUploadClick={() => triggerUpload(record.id, "relieving-letter")}
                    />
                  </TableCell>
                  <TableCell>
                    <DocumentCell
                      fileName={record.lastSalarySlipFileName}
                      documentId={record.lastSalarySlipDocumentId}
                      onUploadClick={() => triggerUpload(record.id, "salary-slip")}
                    />
                  </TableCell>
                  <TableCell className="text-right">
                    <Button type="button" variant="ghost" size="icon" onClick={() => openEditDialog(record)}>
                      <Pencil />
                    </Button>
                    <Button
                      type="button"
                      variant="ghost"
                      size="icon"
                      onClick={() => deleteMutation.mutate(record.id)}
                    >
                      <Trash2 />
                    </Button>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </CardContent>

      <PreviousEmploymentFormDialog
        open={dialogOpen}
        onOpenChange={setDialogOpen}
        record={editingRecord}
        onSubmit={(values) => saveMutation.mutate(values)}
        isSubmitting={saveMutation.isPending}
      />
    </Card>
  )
}
