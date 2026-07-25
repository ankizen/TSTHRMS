import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { Plus, Trash2 } from "lucide-react"
import { useState } from "react"
import { toast } from "sonner"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { createUser, deleteUser, getUsers } from "./api"
import { CreateUserDialog } from "./create-user-dialog"
import type { CreateUserRequest } from "./types"

export function UsersPage() {
  const queryClient = useQueryClient()
  const [dialogOpen, setDialogOpen] = useState(false)
  const queryKey = ["users"]

  const { data: users = [], isLoading } = useQuery({ queryKey, queryFn: getUsers })

  const invalidate = () => queryClient.invalidateQueries({ queryKey })

  const createMutation = useMutation({
    mutationFn: (request: CreateUserRequest) => createUser(request),
    onSuccess: async () => {
      toast.success("Login created.")
      setDialogOpen(false)
      await invalidate()
    },
    onError: () => toast.error("Couldn't create the login. Check the email/password requirements."),
  })

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteUser(id),
    onSuccess: async () => {
      toast.success("Login removed.")
      await invalidate()
    },
    onError: () => toast.error("Couldn't remove the login."),
  })

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Users</h1>
          <p className="text-muted-foreground">{users.length} login{users.length === 1 ? "" : "s"}</p>
        </div>
        <Button onClick={() => setDialogOpen(true)}>
          <Plus />
          Create login
        </Button>
      </div>

      <div className="rounded-md border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Email</TableHead>
              <TableHead>Employee</TableHead>
              <TableHead>Roles</TableHead>
              <TableHead>Scope</TableHead>
              <TableHead className="text-right">Actions</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              <TableRow>
                <TableCell colSpan={5} className="h-24 text-center text-muted-foreground">
                  Loading...
                </TableCell>
              </TableRow>
            ) : users.length === 0 ? (
              <TableRow>
                <TableCell colSpan={5} className="h-24 text-center text-muted-foreground">
                  No logins yet.
                </TableCell>
              </TableRow>
            ) : (
              users.map((user) => (
                <TableRow key={user.id}>
                  <TableCell>{user.email}</TableCell>
                  <TableCell>{user.employeeName ?? "-"}</TableCell>
                  <TableCell>
                    <div className="flex gap-1">
                      {user.roles.map((role) => (
                        <Badge key={role} variant="secondary">{role}</Badge>
                      ))}
                    </div>
                  </TableCell>
                  <TableCell className="text-muted-foreground">
                    {user.assignedLegalEntityName ?? user.assignedProductName
                      ? [user.assignedLegalEntityName, user.assignedProductName].filter(Boolean).join(" · ")
                      : "-"}
                  </TableCell>
                  <TableCell className="text-right">
                    <Button
                      type="button"
                      variant="ghost"
                      size="icon"
                      onClick={() => deleteMutation.mutate(user.id)}
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

      <CreateUserDialog
        open={dialogOpen}
        onOpenChange={setDialogOpen}
        onSubmit={(request) => createMutation.mutate(request)}
        isSubmitting={createMutation.isPending}
      />
    </div>
  )
}
