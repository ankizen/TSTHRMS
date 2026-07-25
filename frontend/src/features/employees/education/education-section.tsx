import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { Download, Pencil, Plus, Trash2, Upload } from "lucide-react"
import { useRef, useState } from "react"
import { toast } from "sonner"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { downloadDocument } from "@/lib/download-document"
import {
  createEducationRecord,
  deleteEducationRecord,
  getEducationRecords,
  updateEducationRecord,
  updateVerificationStatus,
  uploadCertificate,
} from "./api"
import { QUALIFICATION_LEVEL_LABEL, VERIFICATION_STATUS_BADGE_VARIANT } from "./constants"
import { EducationFormDialog } from "./education-form-dialog"
import type { EducationFormValues } from "./schema"
import type { EducationRecord } from "./types"

export function EducationSection({ employeeId }: { employeeId: string }) {
  const queryClient = useQueryClient()
  const queryKey = ["employees", employeeId, "education"]
  const [dialogOpen, setDialogOpen] = useState(false)
  const [editingRecord, setEditingRecord] = useState<EducationRecord | undefined>(undefined)
  const fileInputTarget = useRef<string | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)

  const { data: records = [], isLoading } = useQuery({
    queryKey,
    queryFn: () => getEducationRecords(employeeId),
  })

  const invalidate = () => queryClient.invalidateQueries({ queryKey })

  const saveMutation = useMutation({
    mutationFn: (values: EducationFormValues) => {
      const request = { ...values, specialization: values.specialization || null }
      return editingRecord
        ? updateEducationRecord(employeeId, editingRecord.id, request)
        : createEducationRecord(employeeId, request)
    },
    onSuccess: async () => {
      toast.success(editingRecord ? "Qualification updated" : "Qualification added")
      setDialogOpen(false)
      await invalidate()
    },
    onError: () => toast.error("Couldn't save the qualification."),
  })

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteEducationRecord(employeeId, id),
    onSuccess: async () => {
      toast.success("Qualification removed")
      await invalidate()
    },
    onError: () => toast.error("Couldn't remove the qualification."),
  })

  const verifyMutation = useMutation({
    mutationFn: (record: EducationRecord) =>
      updateVerificationStatus(
        employeeId,
        record.id,
        record.verificationStatus === "Pending" ? "Verified" : "Pending",
      ),
    onSuccess: invalidate,
    onError: () => toast.error("Couldn't update verification status."),
  })

  const uploadMutation = useMutation({
    mutationFn: ({ id, file }: { id: string; file: File }) => uploadCertificate(employeeId, id, file),
    onSuccess: async () => {
      toast.success("Certificate uploaded")
      await invalidate()
    },
    onError: () => toast.error("Couldn't upload the certificate. PDF, JPG, or PNG under 10MB only."),
  })

  const openCreateDialog = () => {
    setEditingRecord(undefined)
    setDialogOpen(true)
  }

  const openEditDialog = (record: EducationRecord) => {
    setEditingRecord(record)
    setDialogOpen(true)
  }

  const triggerUpload = (recordId: string) => {
    fileInputTarget.current = recordId
    fileInputRef.current?.click()
  }

  const handleFileSelected = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    const recordId = fileInputTarget.current
    event.target.value = ""
    if (file && recordId) {
      uploadMutation.mutate({ id: recordId, file })
    }
  }

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between">
        <CardTitle>Education</CardTitle>
        <Button type="button" variant="outline" size="sm" onClick={openCreateDialog}>
          <Plus />
          Add qualification
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
              <TableHead>Level</TableHead>
              <TableHead>Degree</TableHead>
              <TableHead>Institute</TableHead>
              <TableHead>Year</TableHead>
              <TableHead>Specialization</TableHead>
              <TableHead>Certificate</TableHead>
              <TableHead>Status</TableHead>
              <TableHead className="text-right">Actions</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              <TableRow>
                <TableCell colSpan={8} className="h-20 text-center text-muted-foreground">
                  Loading...
                </TableCell>
              </TableRow>
            ) : records.length === 0 ? (
              <TableRow>
                <TableCell colSpan={8} className="h-20 text-center text-muted-foreground">
                  No qualifications added yet.
                </TableCell>
              </TableRow>
            ) : (
              records.map((record) => (
                <TableRow key={record.id}>
                  <TableCell>{QUALIFICATION_LEVEL_LABEL[record.qualificationLevel]}</TableCell>
                  <TableCell>{record.degreeName}</TableCell>
                  <TableCell>{record.instituteName}</TableCell>
                  <TableCell>{record.yearOfPassing}</TableCell>
                  <TableCell>{record.specialization ?? "-"}</TableCell>
                  <TableCell>
                    {record.certificateFileName ? (
                      <Button
                        type="button"
                        variant="link"
                        size="sm"
                        className="h-auto p-0"
                        onClick={() =>
                          downloadDocument(record.certificateDocumentId!, record.certificateFileName!)
                        }
                      >
                        <Download />
                        {record.certificateFileName}
                      </Button>
                    ) : (
                      <Button type="button" variant="outline" size="sm" onClick={() => triggerUpload(record.id)}>
                        <Upload />
                        Upload
                      </Button>
                    )}
                  </TableCell>
                  <TableCell>
                    <Badge
                      variant={VERIFICATION_STATUS_BADGE_VARIANT[record.verificationStatus]}
                      className="cursor-pointer"
                      onClick={() => verifyMutation.mutate(record)}
                    >
                      {record.verificationStatus}
                    </Badge>
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

      <EducationFormDialog
        open={dialogOpen}
        onOpenChange={setDialogOpen}
        record={editingRecord}
        onSubmit={(values) => saveMutation.mutate(values)}
        isSubmitting={saveMutation.isPending}
      />
    </Card>
  )
}
