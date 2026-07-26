import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { ArrowLeft } from "lucide-react"
import { toast } from "sonner"
import { Link, useParams } from "react-router-dom"
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
import { Skeleton } from "@/components/ui/skeleton"
import { getEmployee } from "@/features/employees/api"
import {
  completeOnboardingItem, getInterviewerCandidates, getOnboardingChecklist, updateOnboardingItem,
} from "./api"

const TASK_LABELS: Record<string, string> = {
  ItSetup: "IT Setup",
  AccessProvisioning: "Access Provisioning",
  InductionSession: "Induction Session",
  PolicyAcknowledgement: "Policy Acknowledgement (POSH, Code of Conduct)",
  BuddyAssignment: "Buddy Assignment",
}

export function OnboardingChecklistPage() {
  const { employeeId = "" } = useParams()
  const queryClient = useQueryClient()
  const queryKey = ["recruitment", "employees", employeeId, "onboarding-checklist"]

  const { data: employee, isLoading: isLoadingEmployee } = useQuery({
    queryKey: ["employees", employeeId],
    queryFn: () => getEmployee(employeeId),
    enabled: Boolean(employeeId),
  })

  const { data: checklist, isLoading } = useQuery({
    queryKey,
    queryFn: () => getOnboardingChecklist(employeeId),
    enabled: Boolean(employeeId),
  })

  const { data: candidates = [] } = useQuery({
    queryKey: ["recruitment", "interviewer-candidates"],
    queryFn: getInterviewerCandidates,
  })

  const invalidate = () => queryClient.invalidateQueries({ queryKey })

  const assignMutation = useMutation({
    mutationFn: ({ itemId, ownerUserId }: { itemId: string; ownerUserId: string }) =>
      updateOnboardingItem(itemId, { ownerUserId, dueDate: null }),
    onSuccess: async () => { toast.success("Owner assigned."); await invalidate() },
    onError: () => toast.error("Couldn't assign an owner."),
  })

  const dueDateMutation = useMutation({
    mutationFn: ({ itemId, dueDate }: { itemId: string; dueDate: string }) =>
      updateOnboardingItem(itemId, { ownerUserId: null, dueDate }),
    onSuccess: async () => { toast.success("Due date updated."); await invalidate() },
    onError: () => toast.error("Couldn't update the due date."),
  })

  const completeMutation = useMutation({
    mutationFn: (itemId: string) => completeOnboardingItem(itemId),
    onSuccess: async () => { toast.success("Marked complete."); await invalidate() },
    onError: () => toast.error("Couldn't mark this complete."),
  })

  return (
    <div className="flex flex-col gap-4">
      <Button variant="ghost" size="sm" className="w-fit" asChild>
        <Link to="/recruitment/requisitions">
          <ArrowLeft />
          Back to requisitions
        </Link>
      </Button>

      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Onboarding Checklist</h1>
        {isLoadingEmployee ? (
          <Skeleton className="mt-1 h-5 w-48" />
        ) : employee ? (
          <p className="text-muted-foreground">
            {employee.firstName} {employee.lastName} ({employee.employeeCode}) &middot;{" "}
            <Link to={`/employees/${employee.id}`} className="text-primary hover:underline">
              View Core HR profile
            </Link>
          </p>
        ) : null}
      </div>

      <div className="flex flex-col gap-3">
        {isLoading ? (
          Array.from({ length: 5 }).map((_, index) => <Skeleton key={index} className="h-20 w-full rounded-xl" />)
        ) : (
          checklist?.map((item) => (
            <div key={item.id} className="flex flex-col gap-2 rounded-xl border p-4 sm:flex-row sm:items-center sm:justify-between">
              <div>
                <p className="font-medium">{TASK_LABELS[item.taskType] ?? item.taskType}</p>
                <div className="flex flex-wrap items-center gap-2 text-xs text-muted-foreground">
                  <span>Due {new Date(item.dueDate).toLocaleDateString()}</span>
                  {item.isOverdue && <Badge variant="destructive">Overdue</Badge>}
                  {item.ownerDisplayName && <span>&middot; Owner: {item.ownerDisplayName}</span>}
                </div>
              </div>

              <div className="flex flex-wrap items-center gap-2">
                <Badge variant={item.status === "Completed" ? "default" : "outline"}>
                  {item.status === "Completed" ? "Completed" : "Pending"}
                </Badge>

                {item.status === "Pending" && (
                  <>
                    <Select
                      value={item.ownerUserId ?? ""}
                      onValueChange={(value) => assignMutation.mutate({ itemId: item.id, ownerUserId: value })}
                    >
                      <SelectTrigger className="h-8 w-40 text-xs">
                        <SelectValue placeholder="Assign owner" />
                      </SelectTrigger>
                      <SelectContent>
                        {candidates.map((candidate) => (
                          <SelectItem key={candidate.userId} value={candidate.userId}>
                            {candidate.employeeName ?? candidate.email}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                    <Input
                      type="date"
                      className="h-8 w-36 text-xs"
                      defaultValue={item.dueDate.slice(0, 10)}
                      onChange={(e) => e.target.value && dueDateMutation.mutate({ itemId: item.id, dueDate: e.target.value })}
                    />
                    <Button size="sm" onClick={() => completeMutation.mutate(item.id)} disabled={completeMutation.isPending}>
                      Mark done
                    </Button>
                  </>
                )}
              </div>
            </div>
          ))
        )}
      </div>
    </div>
  )
}
