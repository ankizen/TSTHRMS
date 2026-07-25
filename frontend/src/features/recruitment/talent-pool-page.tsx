import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { Star, UsersRound } from "lucide-react"
import { toast } from "sonner"
import { EmptyState } from "@/components/empty-state"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { TableSkeletonRows } from "@/components/table-skeleton-rows"
import { getTalentPool, setTalentPool } from "./api"
import { APPLICATION_STAGE_LABELS } from "./constants"

export function TalentPoolPage() {
  const queryClient = useQueryClient()
  const queryKey = ["recruitment", "talent-pool"]

  const { data: candidates = [], isLoading } = useQuery({ queryKey, queryFn: getTalentPool })

  const removeMutation = useMutation({
    mutationFn: (candidateId: string) => setTalentPool(candidateId, false),
    onSuccess: async () => {
      toast.success("Removed from talent pool.")
      await queryClient.invalidateQueries({ queryKey })
    },
    onError: () => toast.error("Couldn't update the talent pool tag."),
  })

  return (
    <div className="flex flex-col gap-4">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Talent Pool</h1>
        <p className="text-muted-foreground">
          {candidates.length} candidate{candidates.length === 1 ? "" : "s"} kept in mind for future openings
        </p>
      </div>

      <div className="rounded-xl border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Candidate</TableHead>
              <TableHead>Contact</TableHead>
              <TableHead>Most recent application</TableHead>
              <TableHead className="text-right">Actions</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              <TableSkeletonRows columns={4} />
            ) : candidates.length === 0 ? (
              <TableRow>
                <TableCell colSpan={4}>
                  <EmptyState
                    icon={UsersRound}
                    title="No one in the talent pool yet"
                    description="Tag rejected-but-good candidates from an applicants board so they're easy to find for future openings."
                  />
                </TableCell>
              </TableRow>
            ) : (
              candidates.map((candidate) => (
                <TableRow key={candidate.candidateId}>
                  <TableCell>
                    <div className="flex flex-col">
                      <span className="font-medium">{candidate.firstName} {candidate.lastName}</span>
                      {candidate.resumeDocumentId && (
                        <a
                          href={`${import.meta.env.VITE_API_URL || "/api"}/documents/${candidate.resumeDocumentId}`}
                          target="_blank"
                          rel="noreferrer"
                          className="text-xs text-primary hover:underline"
                        >
                          View resume
                        </a>
                      )}
                    </div>
                  </TableCell>
                  <TableCell className="text-sm text-muted-foreground">
                    <div>{candidate.email}</div>
                    <div>{candidate.phone}</div>
                  </TableCell>
                  <TableCell>
                    {candidate.mostRecentJobPostingTitle ? (
                      <div className="flex flex-col gap-1">
                        <span>{candidate.mostRecentJobPostingTitle}</span>
                        {candidate.mostRecentStage && (
                          <Badge variant="secondary" className="w-fit">
                            {APPLICATION_STAGE_LABELS[candidate.mostRecentStage]}
                          </Badge>
                        )}
                      </div>
                    ) : (
                      <span className="text-muted-foreground">-</span>
                    )}
                  </TableCell>
                  <TableCell className="text-right">
                    <Button
                      type="button"
                      variant="ghost"
                      size="icon"
                      onClick={() => removeMutation.mutate(candidate.candidateId)}
                      disabled={removeMutation.isPending}
                    >
                      <Star className="fill-amber-400 text-amber-400" />
                    </Button>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>
    </div>
  )
}
