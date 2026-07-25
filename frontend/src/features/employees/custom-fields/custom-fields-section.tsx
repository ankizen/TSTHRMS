import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { useEffect, useState } from "react"
import { toast } from "sonner"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Checkbox } from "@/components/ui/checkbox"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { getEmployeeCustomFieldValues, setEmployeeCustomFieldValues } from "./api"

export function CustomFieldsSection({ employeeId }: { employeeId: string }) {
  const queryClient = useQueryClient()
  const queryKey = ["employees", employeeId, "custom-fields"]
  const [draft, setDraft] = useState<Record<string, string | null>>({})

  const { data: values = [], isLoading } = useQuery({
    queryKey,
    queryFn: () => getEmployeeCustomFieldValues(employeeId),
  })

  useEffect(() => {
    setDraft(Object.fromEntries(values.map((v) => [v.definitionId, v.value])))
  }, [values])

  const saveMutation = useMutation({
    mutationFn: () =>
      setEmployeeCustomFieldValues(
        employeeId,
        values.map((v) => ({ definitionId: v.definitionId, value: draft[v.definitionId] ?? null })),
      ),
    onSuccess: async () => {
      toast.success("Custom fields saved.")
      await queryClient.invalidateQueries({ queryKey })
    },
    onError: () => toast.error("Couldn't save custom fields."),
  })

  if (isLoading) {
    return null
  }

  if (values.length === 0) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Custom Fields</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-muted-foreground">
            No custom fields configured yet - an HR Admin can add some under Custom Fields.
          </p>
        </CardContent>
      </Card>
    )
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>Custom Fields</CardTitle>
      </CardHeader>
      <CardContent className="flex flex-col gap-4">
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          {values.map((field) => (
            <div key={field.definitionId} className="flex flex-col gap-2">
              <Label htmlFor={field.definitionId}>
                {field.label}
                {field.isRequired && " *"}
              </Label>
              {field.fieldType === "Boolean" ? (
                <div className="flex items-center gap-2">
                  <Checkbox
                    id={field.definitionId}
                    checked={draft[field.definitionId] === "true"}
                    onCheckedChange={(checked) =>
                      setDraft((prev) => ({ ...prev, [field.definitionId]: checked === true ? "true" : "false" }))
                    }
                  />
                </div>
              ) : field.fieldType === "Select" ? (
                <Select
                  value={draft[field.definitionId] ?? ""}
                  onValueChange={(value) => setDraft((prev) => ({ ...prev, [field.definitionId]: value }))}
                >
                  <SelectTrigger>
                    <SelectValue placeholder="Select..." />
                  </SelectTrigger>
                  <SelectContent>
                    {(field.options ?? []).map((option) => (
                      <SelectItem key={option} value={option}>
                        {option}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              ) : (
                <Input
                  id={field.definitionId}
                  type={field.fieldType === "Number" ? "number" : field.fieldType === "Date" ? "date" : "text"}
                  value={draft[field.definitionId] ?? ""}
                  onChange={(event) =>
                    setDraft((prev) => ({ ...prev, [field.definitionId]: event.target.value }))
                  }
                />
              )}
            </div>
          ))}
        </div>
        <Button type="button" className="w-fit" onClick={() => saveMutation.mutate()} disabled={saveMutation.isPending}>
          {saveMutation.isPending ? "Saving..." : "Save Custom Fields"}
        </Button>
      </CardContent>
    </Card>
  )
}
