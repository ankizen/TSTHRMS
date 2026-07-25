import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { Download, Eye, Pencil, Plus, Trash2, Upload } from "lucide-react"
import { useRef, useState } from "react"
import { toast } from "sonner"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { downloadDocument } from "@/lib/download-document"
import {
  createIdentityDocument,
  deleteIdentityDocument,
  getIdentityDocuments,
  revealIdentityDocumentNumber,
  updateIdentityDocument,
  uploadIdentityDocumentProof,
} from "./api"
import { IDENTITY_DOCUMENT_TYPE_LABEL, IDENTITY_DOCUMENT_TYPE_OPTIONS } from "./constants"
import { IdentityDocumentFormDialog } from "./identity-document-form-dialog"
import type { IdentityDocumentFormValues } from "./schema"
import type { IdentityDocument, IdentityDocumentType } from "./types"

export function IdentityDocumentSection({ employeeId }: { employeeId: string }) {
  const queryClient = useQueryClient()
  const queryKey = ["employees", employeeId, "identity-documents"]
  const [dialogOpen, setDialogOpen] = useState(false)
  const [editingDocument, setEditingDocument] = useState<IdentityDocument | undefined>(undefined)
  const [revealedNumber, setRevealedNumber] = useState<string | null>(null)
  const fileInputTarget = useRef<string | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)

  const { data: documents = [], isLoading } = useQuery({
    queryKey,
    queryFn: () => getIdentityDocuments(employeeId),
  })

  const invalidate = () => queryClient.invalidateQueries({ queryKey })

  const saveMutation = useMutation({
    mutationFn: (values: IdentityDocumentFormValues) => {
      const request = { ...values, expiryDate: values.expiryDate || null }
      return editingDocument
        ? updateIdentityDocument(employeeId, editingDocument.id, request)
        : createIdentityDocument(employeeId, request)
    },
    onSuccess: async () => {
      toast.success(editingDocument ? "Identity document updated" : "Identity document added")
      setDialogOpen(false)
      await invalidate()
    },
    onError: (error: unknown) => {
      const message =
        (error as { response?: { data?: { error?: string } } })?.response?.data?.error ??
        "Couldn't save the identity document."
      toast.error(message)
    },
  })

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteIdentityDocument(employeeId, id),
    onSuccess: async () => {
      toast.success("Identity document removed")
      await invalidate()
    },
    onError: () => toast.error("Couldn't remove the identity document."),
  })

  const uploadMutation = useMutation({
    mutationFn: ({ id, file }: { id: string; file: File }) => uploadIdentityDocumentProof(employeeId, id, file),
    onSuccess: async () => {
      toast.success("Proof uploaded")
      await invalidate()
    },
    onError: () => toast.error("Couldn't upload the file. PDF, JPG, or PNG under 10MB only."),
  })

  const revealMutation = useMutation({
    mutationFn: (id: string) => revealIdentityDocumentNumber(employeeId, id),
  })

  const existingTypes = new Set(documents.map((d) => d.documentType))
  const availableTypes = IDENTITY_DOCUMENT_TYPE_OPTIONS.map((o) => o.value).filter(
    (type) => !existingTypes.has(type),
  ) as IdentityDocumentType[]

  const openCreateDialog = () => {
    setEditingDocument(undefined)
    setDialogOpen(true)
  }

  const openEditDialog = async (document: IdentityDocument) => {
    const realNumber = await revealMutation.mutateAsync(document.id)
    setEditingDocument({ ...document, numberDisplay: realNumber })
    setDialogOpen(true)
  }

  const handleReveal = async (document: IdentityDocument) => {
    const realNumber = await revealMutation.mutateAsync(document.id)
    setRevealedNumber(realNumber)
  }

  const triggerUpload = (documentId: string) => {
    fileInputTarget.current = documentId
    fileInputRef.current?.click()
  }

  const handleFileSelected = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    const documentId = fileInputTarget.current
    event.target.value = ""
    if (file && documentId) {
      uploadMutation.mutate({ id: documentId, file })
    }
  }

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between">
        <CardTitle>Identity Documents</CardTitle>
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={openCreateDialog}
          disabled={availableTypes.length === 0}
        >
          <Plus />
          Add document
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
              <TableHead>Type</TableHead>
              <TableHead>Number</TableHead>
              <TableHead>Expiry</TableHead>
              <TableHead>Proof</TableHead>
              <TableHead className="text-right">Actions</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              <TableRow>
                <TableCell colSpan={5} className="h-20 text-center text-muted-foreground">
                  Loading...
                </TableCell>
              </TableRow>
            ) : documents.length === 0 ? (
              <TableRow>
                <TableCell colSpan={5} className="h-20 text-center text-muted-foreground">
                  No identity documents added yet.
                </TableCell>
              </TableRow>
            ) : (
              documents.map((document) => (
                <TableRow key={document.id}>
                  <TableCell>{IDENTITY_DOCUMENT_TYPE_LABEL[document.documentType]}</TableCell>
                  <TableCell className="font-mono">
                    <div className="flex items-center gap-1">
                      {document.numberDisplay}
                      {document.documentType === "Aadhaar" && (
                        <Button
                          type="button"
                          variant="ghost"
                          size="icon"
                          className="size-6"
                          onClick={() => handleReveal(document)}
                        >
                          <Eye className="size-3.5" />
                        </Button>
                      )}
                    </div>
                  </TableCell>
                  <TableCell>{document.expiryDate ?? "-"}</TableCell>
                  <TableCell>
                    {document.proofFileName && document.proofDocumentId ? (
                      <Button
                        type="button"
                        variant="link"
                        size="sm"
                        className="h-auto p-0"
                        onClick={() => downloadDocument(document.proofDocumentId!, document.proofFileName!)}
                      >
                        <Download />
                        {document.proofFileName}
                      </Button>
                    ) : (
                      <Button type="button" variant="outline" size="sm" onClick={() => triggerUpload(document.id)}>
                        <Upload />
                        Upload
                      </Button>
                    )}
                  </TableCell>
                  <TableCell className="text-right">
                    <Button type="button" variant="ghost" size="icon" onClick={() => openEditDialog(document)}>
                      <Pencil />
                    </Button>
                    <Button
                      type="button"
                      variant="ghost"
                      size="icon"
                      onClick={() => deleteMutation.mutate(document.id)}
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

      <IdentityDocumentFormDialog
        open={dialogOpen}
        onOpenChange={setDialogOpen}
        document={editingDocument}
        availableTypes={editingDocument ? [editingDocument.documentType] : availableTypes}
        onSubmit={(values) => saveMutation.mutate(values)}
        isSubmitting={saveMutation.isPending}
      />

      <Dialog open={revealedNumber !== null} onOpenChange={() => setRevealedNumber(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Aadhaar number</DialogTitle>
          </DialogHeader>
          <p className="font-mono text-lg">{revealedNumber}</p>
          <p className="text-sm text-muted-foreground">
            This reveal has been recorded in the employee&apos;s audit log.
          </p>
        </DialogContent>
      </Dialog>
    </Card>
  )
}
