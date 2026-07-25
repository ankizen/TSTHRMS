import { useQuery } from "@tanstack/react-query"
import { useState } from "react"
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
import { getEmployees, getLegalEntities, getProducts } from "@/features/employees/api"
import { ROLE_OPTIONS } from "./constants"
import type { CreateUserRequest } from "./types"

interface CreateUserDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onSubmit: (request: CreateUserRequest) => void
  isSubmitting: boolean
}

const empty = { employeeId: "", email: "", password: "", role: "Employee" }

export function CreateUserDialog({ open, onOpenChange, onSubmit, isSubmitting }: CreateUserDialogProps) {
  const [form, setForm] = useState(empty)
  const [assignedLegalEntityId, setAssignedLegalEntityId] = useState("")
  const [assignedProductId, setAssignedProductId] = useState("")

  const { data: employees } = useQuery({
    queryKey: ["employees", "picker"],
    queryFn: () => getEmployees({ page: 1, pageSize: 200 }),
    enabled: open,
  })
  const { data: legalEntities = [] } = useQuery({ queryKey: ["legal-entities"], queryFn: getLegalEntities, enabled: open })
  const { data: products = [] } = useQuery({ queryKey: ["products"], queryFn: getProducts, enabled: open })

  const reset = () => {
    setForm(empty)
    setAssignedLegalEntityId("")
    setAssignedProductId("")
  }

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault()
    if (!form.employeeId || !form.email || !form.password) return

    onSubmit({
      employeeId: form.employeeId,
      email: form.email,
      password: form.password,
      role: form.role,
      assignedLegalEntityId: form.role === "HRBP" && assignedLegalEntityId ? assignedLegalEntityId : null,
      assignedProductId: form.role === "HRBP" && assignedProductId ? assignedProductId : null,
    })
  }

  return (
    <Dialog
      open={open}
      onOpenChange={(next) => {
        if (!next) reset()
        onOpenChange(next)
      }}
    >
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Create login</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          <div className="flex flex-col gap-2">
            <Label>Employee</Label>
            <Select value={form.employeeId} onValueChange={(value) => setForm((f) => ({ ...f, employeeId: value }))}>
              <SelectTrigger>
                <SelectValue placeholder="Select an employee" />
              </SelectTrigger>
              <SelectContent>
                {employees?.items.map((employee) => (
                  <SelectItem key={employee.id} value={employee.id}>
                    {employee.firstName} {employee.lastName} ({employee.employeeCode})
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="userEmail">Email</Label>
            <Input
              id="userEmail"
              type="email"
              value={form.email}
              onChange={(event) => setForm((f) => ({ ...f, email: event.target.value }))}
            />
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="userPassword">Temporary password</Label>
            <Input
              id="userPassword"
              type="password"
              value={form.password}
              onChange={(event) => setForm((f) => ({ ...f, password: event.target.value }))}
            />
          </div>
          <div className="flex flex-col gap-2">
            <Label>Role</Label>
            <Select value={form.role} onValueChange={(value) => setForm((f) => ({ ...f, role: value }))}>
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {ROLE_OPTIONS.map((option) => (
                  <SelectItem key={option.value} value={option.value}>
                    {option.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          {form.role === "HRBP" && (
            <>
              <div className="flex flex-col gap-2">
                <Label>Assigned Legal Entity (optional)</Label>
                <Select value={assignedLegalEntityId} onValueChange={setAssignedLegalEntityId}>
                  <SelectTrigger>
                    <SelectValue placeholder="Unrestricted" />
                  </SelectTrigger>
                  <SelectContent>
                    {legalEntities.map((entity) => (
                      <SelectItem key={entity.id} value={entity.id}>
                        {entity.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div className="flex flex-col gap-2">
                <Label>Assigned Product (optional)</Label>
                <Select value={assignedProductId} onValueChange={setAssignedProductId}>
                  <SelectTrigger>
                    <SelectValue placeholder="Unrestricted" />
                  </SelectTrigger>
                  <SelectContent>
                    {products.map((product) => (
                      <SelectItem key={product.id} value={product.id}>
                        {product.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            </>
          )}
          <DialogFooter>
            <Button type="submit" disabled={!form.employeeId || !form.email || !form.password || isSubmitting}>
              {isSubmitting ? "Creating..." : "Create login"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
