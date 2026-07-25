import { useQuery } from "@tanstack/react-query"
import { flexRender, getCoreRowModel, useReactTable, type ColumnDef } from "@tanstack/react-table"
import { Download, Plus, Search, Upload } from "lucide-react"
import { useState } from "react"
import { toast } from "sonner"
import { Link, useNavigate } from "react-router-dom"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { exportEmployees, getEmployees, getLegalEntities, getProducts } from "./api"
import { EMPLOYEE_STATUS_BADGE_VARIANT, EMPLOYEE_STATUS_OPTIONS } from "./constants"
import type { EmployeeListFilter, EmployeeListItem, EmployeeStatus } from "./types"

const columns: ColumnDef<EmployeeListItem>[] = [
  { accessorKey: "employeeCode", header: "Code" },
  {
    id: "name",
    header: "Name",
    cell: ({ row }) => `${row.original.firstName} ${row.original.lastName}`,
  },
  { accessorKey: "legalEntityName", header: "Entity" },
  { accessorKey: "productName", header: "Product" },
  { accessorKey: "department", header: "Department" },
  { accessorKey: "designation", header: "Designation" },
  { accessorKey: "workLocation", header: "Work Location" },
  {
    accessorKey: "status",
    header: "Status",
    cell: ({ getValue }) => {
      const status = getValue<EmployeeStatus>()
      return <Badge variant={EMPLOYEE_STATUS_BADGE_VARIANT[status]}>{status}</Badge>
    },
  },
]

const PAGE_SIZE = 20

export function EmployeeListPage() {
  const navigate = useNavigate()
  const [search, setSearch] = useState("")
  const [status, setStatus] = useState<EmployeeStatus | "all">("all")
  const [legalEntityId, setLegalEntityId] = useState<string | "all">("all")
  const [productId, setProductId] = useState<string | "all">("all")
  const [department, setDepartment] = useState("")
  const [designation, setDesignation] = useState("")
  const [workLocation, setWorkLocation] = useState("")
  const [page, setPage] = useState(1)
  const [isExporting, setIsExporting] = useState(false)

  const { data: legalEntities = [] } = useQuery({ queryKey: ["legal-entities"], queryFn: getLegalEntities })
  const { data: products = [] } = useQuery({ queryKey: ["products"], queryFn: getProducts })

  const filter: Partial<EmployeeListFilter> = {
    search: search || undefined,
    status: status === "all" ? undefined : status,
    legalEntityId: legalEntityId === "all" ? undefined : legalEntityId,
    productId: productId === "all" ? undefined : productId,
    department: department || undefined,
    designation: designation || undefined,
    workLocation: workLocation || undefined,
  }

  const { data, isLoading } = useQuery({
    queryKey: ["employees", { ...filter, page }],
    queryFn: () => getEmployees({ ...filter, page, pageSize: PAGE_SIZE }),
    placeholderData: (previous) => previous,
  })

  const handleExport = async () => {
    setIsExporting(true)
    try {
      await exportEmployees(filter)
    } catch {
      toast.error("Couldn't export employees to Excel.")
    } finally {
      setIsExporting(false)
    }
  }

  const table = useReactTable({
    data: data?.items ?? [],
    columns,
    getCoreRowModel: getCoreRowModel(),
  })

  const totalPages = data ? Math.max(1, Math.ceil(data.totalCount / PAGE_SIZE)) : 1

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Employees</h1>
          <p className="text-muted-foreground">{data?.totalCount ?? 0} total</p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" onClick={handleExport} disabled={isExporting}>
            <Download />
            {isExporting ? "Exporting..." : "Export to Excel"}
          </Button>
          <Button asChild variant="outline">
            <Link to="/employees/bulk-import">
              <Upload />
              Bulk Import
            </Link>
          </Button>
          <Button asChild>
            <Link to="/employees/new">
              <Plus />
              New Employee
            </Link>
          </Button>
        </div>
      </div>

      <div className="flex flex-wrap items-center gap-2">
        <div className="relative w-full max-w-sm">
          <Search className="absolute top-2.5 left-2.5 size-4 text-muted-foreground" />
          <Input
            placeholder="Search by name, code, or email..."
            className="pl-8"
            value={search}
            onChange={(event) => {
              setSearch(event.target.value)
              setPage(1)
            }}
          />
        </div>
        <Select
          value={status}
          onValueChange={(value) => {
            setStatus(value as EmployeeStatus | "all")
            setPage(1)
          }}
        >
          <SelectTrigger className="w-[160px]">
            <SelectValue placeholder="Status" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All statuses</SelectItem>
            {EMPLOYEE_STATUS_OPTIONS.map((option) => (
              <SelectItem key={option.value} value={option.value}>
                {option.label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Select
          value={legalEntityId}
          onValueChange={(value) => {
            setLegalEntityId(value)
            setPage(1)
          }}
        >
          <SelectTrigger className="w-[160px]">
            <SelectValue placeholder="Legal Entity" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All entities</SelectItem>
            {legalEntities.map((entity) => (
              <SelectItem key={entity.id} value={entity.id}>
                {entity.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Select
          value={productId}
          onValueChange={(value) => {
            setProductId(value)
            setPage(1)
          }}
        >
          <SelectTrigger className="w-[160px]">
            <SelectValue placeholder="Product" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All products</SelectItem>
            {products.map((product) => (
              <SelectItem key={product.id} value={product.id}>
                {product.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Input
          placeholder="Department"
          className="w-[160px]"
          value={department}
          onChange={(event) => {
            setDepartment(event.target.value)
            setPage(1)
          }}
        />
        <Input
          placeholder="Designation"
          className="w-[160px]"
          value={designation}
          onChange={(event) => {
            setDesignation(event.target.value)
            setPage(1)
          }}
        />
        <Input
          placeholder="Work Location"
          className="w-[160px]"
          value={workLocation}
          onChange={(event) => {
            setWorkLocation(event.target.value)
            setPage(1)
          }}
        />
      </div>

      <div className="rounded-md border">
        <Table>
          <TableHeader>
            {table.getHeaderGroups().map((headerGroup) => (
              <TableRow key={headerGroup.id}>
                {headerGroup.headers.map((header) => (
                  <TableHead key={header.id}>
                    {header.isPlaceholder
                      ? null
                      : flexRender(header.column.columnDef.header, header.getContext())}
                  </TableHead>
                ))}
              </TableRow>
            ))}
          </TableHeader>
          <TableBody>
            {isLoading ? (
              <TableRow>
                <TableCell colSpan={columns.length} className="h-24 text-center text-muted-foreground">
                  Loading...
                </TableCell>
              </TableRow>
            ) : table.getRowModel().rows.length === 0 ? (
              <TableRow>
                <TableCell colSpan={columns.length} className="h-24 text-center text-muted-foreground">
                  No employees found.
                </TableCell>
              </TableRow>
            ) : (
              table.getRowModel().rows.map((row) => (
                <TableRow
                  key={row.id}
                  className="cursor-pointer"
                  onClick={() => navigate(`/employees/${row.original.id}`)}
                >
                  {row.getVisibleCells().map((cell) => (
                    <TableCell key={cell.id}>
                      {flexRender(cell.column.columnDef.cell, cell.getContext())}
                    </TableCell>
                  ))}
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>

      <div className="flex items-center justify-between">
        <p className="text-sm text-muted-foreground">
          Page {data?.page ?? page} of {totalPages}
        </p>
        <div className="flex gap-2">
          <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>
            Previous
          </Button>
          <Button
            variant="outline"
            size="sm"
            disabled={page >= totalPages}
            onClick={() => setPage((p) => p + 1)}
          >
            Next
          </Button>
        </div>
      </div>
    </div>
  )
}
