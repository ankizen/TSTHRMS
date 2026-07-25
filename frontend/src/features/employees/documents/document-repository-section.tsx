import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { Download, Plus, Trash2 } from "lucide-react"
import { useState } from "react"
import { toast } from "sonner"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { downloadDocument } from "@/lib/download-document"
import { deleteEmployeeDocument, getEmployeeDocuments, uploadEmployeeDocument } from "./api"
import { DocumentUploadDialog } from "./document-upload-dialog"
import type { EmployeeDocumentCategory } from "./types"

export function DocumentRepositorySection({ employeeId }: { employeeId: string }) {
  const queryClient = useQueryClient()
  const queryKey = ["employees", employeeId, "documents"]
  const [dialogOpen, setDialogOpen] = useState(false)

  const { data: documents = [], isLoading } = useQuery({
    queryKey,
    queryFn: () => getEmployeeDocuments(employeeId),
  })

  const invalidate = () => queryClient.invalidateQueries({ queryKey })

  const uploadMutation = useMutation({
    mutationFn: ({ category, notes, file }: { category: EmployeeDocumentCategory; notes: string | null; file: File }) =>
      uploadEmployeeDocument(employeeId, category, notes, file),
    onSuccess: async () => {
      toast.success("Document uploaded")
      setDialogOpen(false)
      await invalidate()
    },
    onError: () => toast.error("Couldn't upload the document. PDF, JPG, or PNG under 10MB only."),
  })

  const deleteMutation = useMutation({
    mutationFn: (standaloneAttachmentId: string) => deleteEmployeeDocument(employeeId, standaloneAttachmentId),
    onSuccess: async () => {
      toast.success("Document removed")
      await invalidate()
    },
    onError: () => toast.error("Couldn't remove the document."),
  })

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between">
        <CardTitle>Documents</CardTitle>
        <Button type="button" variant="outline" size="sm" onClick={() => setDialogOpen(true)}>
          <Plus />
          Upload document
        </Button>
      </CardHeader>
      <CardContent>
        <p className="mb-3 text-xs text-muted-foreground">
          Consolidated view of every document attached to this employee - certificates, letters,
          and identity proofs uploaded elsewhere in this record show up here too, alongside
          documents uploaded directly.
        </p>
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>File</TableHead>
              <TableHead>Category</TableHead>
              <TableHead>Context</TableHead>
              <TableHead>Uploaded</TableHead>
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
                  No documents yet.
                </TableCell>
              </TableRow>
            ) : (
              documents.map((doc) => (
                <TableRow key={`${doc.documentId}-${doc.category}`}>
                  <TableCell>
                    <Button
                      type="button"
                      variant="link"
                      size="sm"
                      className="h-auto p-0"
                      onClick={() => downloadDocument(doc.documentId, doc.fileName)}
                    >
                      <Download />
                      {doc.fileName}
                    </Button>
                  </TableCell>
                  <TableCell>{doc.category}</TableCell>
                  <TableCell>{doc.context ?? "-"}</TableCell>
                  <TableCell>{new Date(doc.uploadedAt).toLocaleDateString()}</TableCell>
                  <TableCell className="text-right">
                    {doc.standaloneAttachmentId && (
                      <Button
                        type="button"
                        variant="ghost"
                        size="icon"
                        onClick={() => deleteMutation.mutate(doc.standaloneAttachmentId!)}
                      >
                        <Trash2 />
                      </Button>
                    )}
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </CardContent>

      <DocumentUploadDialog
        open={dialogOpen}
        onOpenChange={setDialogOpen}
        onSubmit={(category, notes, file) => uploadMutation.mutate({ category, notes, file })}
        isSubmitting={uploadMutation.isPending}
      />
    </Card>
  )
}
