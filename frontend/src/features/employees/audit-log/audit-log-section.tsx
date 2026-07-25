import { useMutation, useQuery } from "@tanstack/react-query"
import { ChevronDown, ChevronRight, Eye } from "lucide-react"
import { Fragment, useState } from "react"
import { toast } from "sonner"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { getEmployeeAuditLog, revealAuditLogEntry } from "./api"
import { AUDIT_ACTION_BADGE_VARIANT, AUDIT_ENTITY_LABELS } from "./constants"
import type { AuditLogEntry } from "./types"

export function AuditLogSection({ employeeId }: { employeeId: string }) {
  const [expandedIds, setExpandedIds] = useState<Set<string>>(new Set())
  const [revealedEntries, setRevealedEntries] = useState<Record<string, AuditLogEntry>>({})

  const { data: entries = [], isLoading } = useQuery({
    queryKey: ["employees", employeeId, "audit-log"],
    queryFn: () => getEmployeeAuditLog(employeeId),
  })

  const revealMutation = useMutation({
    mutationFn: (auditLogId: string) => revealAuditLogEntry(employeeId, auditLogId),
    onSuccess: (entry) => {
      setRevealedEntries((prev) => ({ ...prev, [entry.id]: entry }))
    },
    onError: () => toast.error("Couldn't reveal this entry's sensitive fields."),
  })

  const toggleExpanded = (id: string) => {
    setExpandedIds((prev) => {
      const next = new Set(prev)
      if (next.has(id)) {
        next.delete(id)
      } else {
        next.add(id)
      }
      return next
    })
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>Change History</CardTitle>
      </CardHeader>
      <CardContent>
        <p className="mb-3 text-xs text-muted-foreground">
          Every recorded change to this employee's record, including education, family, previous
          employment, identity documents, and nominees. Sensitive fields are masked by default -
          click Reveal to view the real value; every reveal is itself logged.
        </p>
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead className="w-8" />
              <TableHead>When</TableHead>
              <TableHead>Section</TableHead>
              <TableHead>Action</TableHead>
              <TableHead>Changed By</TableHead>
              <TableHead>Fields</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              <TableRow>
                <TableCell colSpan={6} className="h-20 text-center text-muted-foreground">
                  Loading...
                </TableCell>
              </TableRow>
            ) : entries.length === 0 ? (
              <TableRow>
                <TableCell colSpan={6} className="h-20 text-center text-muted-foreground">
                  No changes recorded yet.
                </TableCell>
              </TableRow>
            ) : (
              entries.map((entry) => {
                const isExpanded = expandedIds.has(entry.id)
                const displayEntry = revealedEntries[entry.id] ?? entry
                const hasMaskedFields =
                  displayEntry.changes.some((c) => c.isSensitive) && !revealedEntries[entry.id]

                return (
                  <Fragment key={entry.id}>
                    <TableRow
                      className="cursor-pointer"
                      onClick={() => toggleExpanded(entry.id)}
                    >
                      <TableCell>
                        {isExpanded ? <ChevronDown className="size-4" /> : <ChevronRight className="size-4" />}
                      </TableCell>
                      <TableCell>{new Date(entry.changedAt).toLocaleString()}</TableCell>
                      <TableCell>{AUDIT_ENTITY_LABELS[entry.entityName] ?? entry.entityName}</TableCell>
                      <TableCell>
                        <Badge variant={AUDIT_ACTION_BADGE_VARIANT[entry.action]}>{entry.action}</Badge>
                      </TableCell>
                      <TableCell>{entry.changedByDisplayName ?? "System"}</TableCell>
                      <TableCell>{entry.changes.length}</TableCell>
                    </TableRow>
                    {isExpanded && (
                      <TableRow>
                        <TableCell colSpan={6} className="bg-muted/30">
                          <div className="flex flex-col gap-2 py-2">
                            {displayEntry.changes.map((change) => (
                              <div key={change.propertyName} className="grid grid-cols-3 gap-2 text-sm">
                                <span className="font-medium">{change.propertyName}</span>
                                <span className="text-muted-foreground">{change.oldValue ?? "-"}</span>
                                <span>{change.newValue ?? "-"}</span>
                              </div>
                            ))}
                            {hasMaskedFields && (
                              <Button
                                type="button"
                                variant="outline"
                                size="sm"
                                className="w-fit"
                                disabled={revealMutation.isPending}
                                onClick={(event) => {
                                  event.stopPropagation()
                                  revealMutation.mutate(entry.id)
                                }}
                              >
                                <Eye />
                                Reveal sensitive fields
                              </Button>
                            )}
                          </div>
                        </TableCell>
                      </TableRow>
                    )}
                  </Fragment>
                )
              })
            )}
          </TableBody>
        </Table>
      </CardContent>
    </Card>
  )
}
