import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { Pencil, Plus, Trash2 } from "lucide-react"
import { useState } from "react"
import { toast } from "sonner"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import {
  createFamilyMember,
  deleteFamilyMember,
  getFamilyMembers,
  updateFamilyMember,
} from "./api"
import { FAMILY_RELATION_LABEL } from "./constants"
import { FamilyFormDialog } from "./family-form-dialog"
import type { FamilyFormValues } from "./schema"
import type { FamilyMember } from "./types"

export function FamilySection({ employeeId }: { employeeId: string }) {
  const queryClient = useQueryClient()
  const queryKey = ["employees", employeeId, "family"]
  const [dialogOpen, setDialogOpen] = useState(false)
  const [editingMember, setEditingMember] = useState<FamilyMember | undefined>(undefined)

  const { data: members = [], isLoading } = useQuery({
    queryKey,
    queryFn: () => getFamilyMembers(employeeId),
  })

  const invalidate = () => queryClient.invalidateQueries({ queryKey })

  const saveMutation = useMutation({
    mutationFn: (values: FamilyFormValues) => {
      const request = { ...values, dateOfBirth: values.dateOfBirth || null }
      return editingMember
        ? updateFamilyMember(employeeId, editingMember.id, request)
        : createFamilyMember(employeeId, request)
    },
    onSuccess: async () => {
      toast.success(editingMember ? "Family member updated" : "Family member added")
      setDialogOpen(false)
      await invalidate()
    },
    onError: () => toast.error("Couldn't save the family member."),
  })

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteFamilyMember(employeeId, id),
    onSuccess: async () => {
      toast.success("Family member removed")
      await invalidate()
    },
    onError: () => toast.error("Couldn't remove the family member."),
  })

  const openCreateDialog = () => {
    setEditingMember(undefined)
    setDialogOpen(true)
  }

  const openEditDialog = (member: FamilyMember) => {
    setEditingMember(member)
    setDialogOpen(true)
  }

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between">
        <CardTitle>Family</CardTitle>
        <Button type="button" variant="outline" size="sm" onClick={openCreateDialog}>
          <Plus />
          Add family member
        </Button>
      </CardHeader>
      <CardContent>
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Relation</TableHead>
              <TableHead>Name</TableHead>
              <TableHead>Date of birth</TableHead>
              <TableHead>Dependent</TableHead>
              <TableHead>Differently-abled</TableHead>
              <TableHead className="text-right">Actions</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              <TableRow>
                <TableCell colSpan={6} className="h-20 text-center text-muted-foreground">
                  Loading...
                </TableCell>
              </TableRow>
            ) : members.length === 0 ? (
              <TableRow>
                <TableCell colSpan={6} className="h-20 text-center text-muted-foreground">
                  No family members added yet.
                </TableCell>
              </TableRow>
            ) : (
              members.map((member) => (
                <TableRow key={member.id}>
                  <TableCell>{FAMILY_RELATION_LABEL[member.relation]}</TableCell>
                  <TableCell>{member.name}</TableCell>
                  <TableCell>{member.dateOfBirth ?? "-"}</TableCell>
                  <TableCell>
                    {member.isDependent && <Badge variant="secondary">Dependent</Badge>}
                  </TableCell>
                  <TableCell>
                    {member.isDifferentlyAbled && <Badge variant="secondary">Yes</Badge>}
                  </TableCell>
                  <TableCell className="text-right">
                    <Button type="button" variant="ghost" size="icon" onClick={() => openEditDialog(member)}>
                      <Pencil />
                    </Button>
                    <Button
                      type="button"
                      variant="ghost"
                      size="icon"
                      onClick={() => deleteMutation.mutate(member.id)}
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

      <FamilyFormDialog
        open={dialogOpen}
        onOpenChange={setDialogOpen}
        member={editingMember}
        onSubmit={(values) => saveMutation.mutate(values)}
        isSubmitting={saveMutation.isPending}
      />
    </Card>
  )
}
