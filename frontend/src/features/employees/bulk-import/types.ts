export interface BulkImportRowResult {
  rowNumber: number
  isValid: boolean
  employeeCode: string | null
  errors: string[]
}

export interface BulkImportSummary {
  totalRows: number
  validRows: number
  invalidRows: number
  createdCount: number
  rows: BulkImportRowResult[]
}
