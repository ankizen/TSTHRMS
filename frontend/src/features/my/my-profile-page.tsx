import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { useState } from "react"
import { toast } from "sonner"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { getMyEditRequests, getMyProfile, submitMyEditRequests } from "./api"
import { EDIT_REQUEST_STATUS_BADGE_VARIANT, EDITABLE_FIELD_LABELS } from "./constants"
import type { EditableEmployeeField } from "./types"

const EDITABLE_FIELDS: EditableEmployeeField[] = [
  "PersonalEmail",
  "PersonalPhone",
  "CurrentAddress",
  "PermanentAddress",
  "EmergencyContactName",
  "EmergencyContactRelation",
  "EmergencyContactPhone",
]

export function MyProfilePage() {
  const queryClient = useQueryClient()
  const [draft, setDraft] = useState<Partial<Record<EditableEmployeeField, string>>>({})

  const { data: profile, isLoading } = useQuery({ queryKey: ["my-profile"], queryFn: getMyProfile })
  const { data: requests = [] } = useQuery({ queryKey: ["my-edit-requests"], queryFn: getMyEditRequests })

  const submitMutation = useMutation({
    mutationFn: (changes: { field: EditableEmployeeField; newValue: string | null }[]) => submitMyEditRequests(changes),
    onSuccess: async () => {
      toast.success("Submitted for HR review.")
      setDraft({})
      await queryClient.invalidateQueries({ queryKey: ["my-edit-requests"] })
    },
    onError: () => toast.error("Couldn't submit your changes."),
  })

  if (isLoading || !profile) {
    return <p className="text-muted-foreground">Loading...</p>
  }

  const currentValue = (field: EditableEmployeeField): string => {
    switch (field) {
      case "PersonalEmail":
        return profile.personalEmail ?? ""
      case "PersonalPhone":
        return profile.personalPhone ?? ""
      case "CurrentAddress":
        return profile.currentAddress ?? ""
      case "PermanentAddress":
        return profile.permanentAddress ?? ""
      case "EmergencyContactName":
        return profile.emergencyContactName ?? ""
      case "EmergencyContactRelation":
        return profile.emergencyContactRelation ?? ""
      case "EmergencyContactPhone":
        return profile.emergencyContactPhone ?? ""
      default:
        return ""
    }
  }

  const handleSubmit = () => {
    const changes = EDITABLE_FIELDS
      .filter((field) => draft[field] !== undefined && draft[field] !== currentValue(field))
      .map((field) => ({ field, newValue: draft[field] ?? null }))

    if (changes.length === 0) {
      toast.info("No changes to submit.")
      return
    }

    submitMutation.mutate(changes)
  }

  return (
    <div className="flex flex-col gap-4">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">My Profile</h1>
        <p className="text-muted-foreground">
          {profile.employeeCode} · {profile.designation ?? "-"}
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Employment Details</CardTitle>
        </CardHeader>
        <CardContent className="grid grid-cols-2 gap-4 text-sm sm:grid-cols-3">
          <div>
            <p className="text-muted-foreground">Name</p>
            <p>{profile.firstName} {profile.lastName}</p>
          </div>
          <div>
            <p className="text-muted-foreground">Department</p>
            <p>{profile.department ?? "-"}</p>
          </div>
          <div>
            <p className="text-muted-foreground">Work Location</p>
            <p>{profile.workLocation ?? "-"}</p>
          </div>
          <div>
            <p className="text-muted-foreground">Date of Joining</p>
            <p>{profile.dateOfJoining}</p>
          </div>
          <div>
            <p className="text-muted-foreground">Status</p>
            <p>{profile.status}</p>
          </div>
          <div>
            <p className="text-muted-foreground">Reporting Manager</p>
            <p>{profile.reportingManagerName ?? "-"}</p>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Contact & Emergency Details</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          <p className="text-xs text-muted-foreground">
            These fields are read-only until HR approves your requested change. Edit a value below
            and submit - HR will review before it takes effect.
          </p>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            {EDITABLE_FIELDS.map((field) => (
              <div key={field} className="flex flex-col gap-2">
                <Label htmlFor={field}>{EDITABLE_FIELD_LABELS[field]}</Label>
                <Input
                  id={field}
                  value={draft[field] ?? currentValue(field)}
                  onChange={(event) => setDraft((prev) => ({ ...prev, [field]: event.target.value }))}
                />
              </div>
            ))}
          </div>
          <Button type="button" className="w-fit" onClick={handleSubmit} disabled={submitMutation.isPending}>
            {submitMutation.isPending ? "Submitting..." : "Submit changes for review"}
          </Button>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>My Requests</CardTitle>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Field</TableHead>
                <TableHead>Old Value</TableHead>
                <TableHead>New Value</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Note</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {requests.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={5} className="h-20 text-center text-muted-foreground">
                    No requests submitted yet.
                  </TableCell>
                </TableRow>
              ) : (
                requests.map((request) => (
                  <TableRow key={request.id}>
                    <TableCell>{EDITABLE_FIELD_LABELS[request.field]}</TableCell>
                    <TableCell className="text-muted-foreground">{request.oldValue ?? "-"}</TableCell>
                    <TableCell>{request.newValue}</TableCell>
                    <TableCell>
                      <Badge variant={EDIT_REQUEST_STATUS_BADGE_VARIANT[request.status]}>{request.status}</Badge>
                    </TableCell>
                    <TableCell className="text-muted-foreground">{request.reviewNote ?? "-"}</TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    </div>
  )
}
