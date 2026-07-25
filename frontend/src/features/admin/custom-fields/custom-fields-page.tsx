import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { Pencil, Plus, Trash2 } from "lucide-react"
import { useState } from "react"
import { toast } from "sonner"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import {
  createCustomFieldDefinition,
  deleteCustomFieldDefinition,
  getCustomFieldDefinitions,
  updateCustomFieldDefinition,
} from "./api"
import { CustomFieldDialog } from "./custom-field-dialog"
import type { CustomFieldDefinition, CustomFieldDefinitionWriteRequest } from "./types"

export function CustomFieldsPage() {
  const queryClient = useQueryClient()
  const queryKey = ["custom-field-definitions"]
  const [dialogOpen, setDialogOpen] = useState(false)
  const [editing, setEditing] = useState<CustomFieldDefinition | null>(null)

  const { data: definitions = [], isLoading } = useQuery({ queryKey, queryFn: getCustomFieldDefinitions })

  const invalidate = () => queryClient.invalidateQueries({ queryKey })

  const createMutation = useMutation({
    mutationFn: (request: CustomFieldDefinitionWriteRequest) => createCustomFieldDefinition(request),
    onSuccess: async () => {
      toast.success("Custom field created.")
      setDialogOpen(false)
      await invalidate()
    },
    onError: () => toast.error("Couldn't create the field - the name may already be in use."),
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, request }: { id: string; request: CustomFieldDefinitionWriteRequest }) =>
      updateCustomFieldDefinition(id, request),
    onSuccess: async () => {
      toast.success("Custom field updated.")
      setDialogOpen(false)
      setEditing(null)
      await invalidate()
    },
    onError: () => toast.error("Couldn't update the field - the name may already be in use."),
  })

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteCustomFieldDefinition(id),
    onSuccess: async () => {
      toast.success("Custom field removed.")
      await invalidate()
    },
    onError: () => toast.error("Couldn't remove the field."),
  })

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Custom Fields</h1>
          <p className="text-muted-foreground">Add employee fields without needing a code change.</p>
        </div>
        <Button
          onClick={() => {
            setEditing(null)
            setDialogOpen(true)
          }}
        >
          <Plus />
          New field
        </Button>
      </div>

      <div className="rounded-md border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Name</TableHead>
              <TableHead>Label</TableHead>
              <TableHead>Type</TableHead>
              <TableHead>Required</TableHead>
              <TableHead>Order</TableHead>
              <TableHead className="text-right">Actions</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              <TableRow>
                <TableCell colSpan={6} className="h-24 text-center text-muted-foreground">
                  Loading...
                </TableCell>
              </TableRow>
            ) : definitions.length === 0 ? (
              <TableRow>
                <TableCell colSpan={6} className="h-24 text-center text-muted-foreground">
                  No custom fields yet.
                </TableCell>
              </TableRow>
            ) : (
              definitions.map((definition) => (
                <TableRow key={definition.id}>
                  <TableCell className="font-mono text-sm">{definition.name}</TableCell>
                  <TableCell>{definition.label}</TableCell>
                  <TableCell>{definition.fieldType}</TableCell>
                  <TableCell>{definition.isRequired && <Badge variant="secondary">Required</Badge>}</TableCell>
                  <TableCell>{definition.displayOrder}</TableCell>
                  <TableCell className="text-right">
                    <Button
                      type="button"
                      variant="ghost"
                      size="icon"
                      onClick={() => {
                        setEditing(definition)
                        setDialogOpen(true)
                      }}
                    >
                      <Pencil />
                    </Button>
                    <Button
                      type="button"
                      variant="ghost"
                      size="icon"
                      onClick={() => deleteMutation.mutate(definition.id)}
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

      <CustomFieldDialog
        open={dialogOpen}
        onOpenChange={setDialogOpen}
        editing={editing}
        isSubmitting={createMutation.isPending || updateMutation.isPending}
        onSubmit={(request) => {
          if (editing) {
            updateMutation.mutate({ id: editing.id, request })
          } else {
            createMutation.mutate(request)
          }
        }}
      />
    </div>
  )
}
