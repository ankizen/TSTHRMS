import { useMutation } from "@tanstack/react-query"
import { CheckCircle2, Download, Upload, XCircle } from "lucide-react"
import { useState } from "react"
import { Link } from "react-router-dom"
import { toast } from "sonner"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { commitBulkImport, downloadBulkImportTemplate, validateBulkImport } from "./api"
import type { BulkImportSummary } from "./types"

export function BulkImportPage() {
  const [file, setFile] = useState<File | null>(null)
  const [preview, setPreview] = useState<BulkImportSummary | null>(null)
  const [committed, setCommitted] = useState<BulkImportSummary | null>(null)

  const previewMutation = useMutation({
    mutationFn: (selected: File) => validateBulkImport(selected),
    onSuccess: (summary) => setPreview(summary),
    onError: () => toast.error("Couldn't read that file. Make sure it's an .xlsx workbook using the template."),
  })

  const commitMutation = useMutation({
    mutationFn: (selected: File) => commitBulkImport(selected),
    onSuccess: (summary) => {
      setCommitted(summary)
      toast.success(`Created ${summary.createdCount} employee${summary.createdCount === 1 ? "" : "s"}.`)
    },
    onError: () => toast.error("Couldn't import the file."),
  })

  const handleFileChange = (selected: File | null) => {
    setFile(selected)
    setPreview(null)
    setCommitted(null)
  }

  const summary = committed ?? preview

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Bulk Import Employees</h1>
          <p className="text-muted-foreground">
            Upload a spreadsheet to create many employees at once.
          </p>
        </div>
        <Button asChild variant="outline">
          <Link to="/employees">Back to Employees</Link>
        </Button>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>1. Download the template</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="mb-3 text-sm text-muted-foreground">
            Fill in one row per employee, delete the example row, then upload the file below.
            Legal Entity and Product must match existing names exactly.
          </p>
          <Button type="button" variant="outline" onClick={() => void downloadBulkImportTemplate()}>
            <Download />
            Download Template
          </Button>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>2. Upload and preview</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col gap-3">
          <input
            type="file"
            accept=".xlsx"
            className="text-sm"
            onChange={(event) => handleFileChange(event.target.files?.[0] ?? null)}
          />
          <div className="flex gap-2">
            <Button
              type="button"
              variant="outline"
              disabled={!file || previewMutation.isPending}
              onClick={() => file && previewMutation.mutate(file)}
            >
              {previewMutation.isPending ? "Checking..." : "Preview"}
            </Button>
            <Button
              type="button"
              disabled={!file || !preview || preview.validRows === 0 || commitMutation.isPending || committed !== null}
              onClick={() => file && commitMutation.mutate(file)}
            >
              <Upload />
              {commitMutation.isPending ? "Importing..." : `Import ${preview?.validRows ?? 0} Valid Row(s)`}
            </Button>
          </div>
        </CardContent>
      </Card>

      {summary && (
        <Card>
          <CardHeader>
            <CardTitle>{committed ? "Import Results" : "Preview Results"}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="mb-4 flex flex-wrap gap-4 text-sm">
              <span>Total rows: {summary.totalRows}</span>
              <span className="flex items-center gap-1 text-green-600">
                <CheckCircle2 className="size-4" /> Valid: {summary.validRows}
              </span>
              <span className="flex items-center gap-1 text-destructive">
                <XCircle className="size-4" /> Invalid: {summary.invalidRows}
              </span>
              {committed && <span>Created: {summary.createdCount}</span>}
            </div>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Row</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Employee Code</TableHead>
                  <TableHead>Errors</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {summary.rows.map((row) => (
                  <TableRow key={row.rowNumber}>
                    <TableCell>{row.rowNumber}</TableCell>
                    <TableCell>
                      <Badge variant={row.isValid ? "default" : "destructive"}>
                        {row.isValid ? "Valid" : "Invalid"}
                      </Badge>
                    </TableCell>
                    <TableCell>{row.employeeCode ?? "-"}</TableCell>
                    <TableCell className="text-sm text-muted-foreground">
                      {row.errors.length > 0 ? row.errors.join("; ") : "-"}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      )}
    </div>
  )
}
