import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet"
import { completeItAssetTask, getPreboardingChecklist } from "./api"
import { PREBOARDING_TASK_LABELS } from "./constants"

interface PreboardingSheetProps {
  applicationId: string | null
  candidateName: string | null
  onOpenChange: (open: boolean) => void
}

export function PreboardingSheet({ applicationId, candidateName, onOpenChange }: PreboardingSheetProps) {
  const queryClient = useQueryClient()
  const queryKey = ["recruitment", "applications", applicationId, "preboarding"]

  const { data: checklist, isLoading } = useQuery({
    queryKey,
    queryFn: () => getPreboardingChecklist(applicationId!),
    enabled: Boolean(applicationId),
  })

  const completeItMutation = useMutation({
    mutationFn: () => completeItAssetTask(applicationId!),
    onSuccess: async () => {
      toast.success("IT asset request marked complete.")
      await queryClient.invalidateQueries({ queryKey })
    },
    onError: () => toast.error("Couldn't update the task."),
  })

  return (
    <Sheet open={Boolean(applicationId)} onOpenChange={onOpenChange}>
      <SheetContent className="sm:max-w-md">
        <SheetHeader>
          <SheetTitle>Pre-boarding{candidateName ? ` - ${candidateName}` : ""}</SheetTitle>
        </SheetHeader>

        <div className="flex flex-col gap-2 overflow-y-auto px-4 pb-4">
          {isLoading ? (
            <p className="text-sm text-muted-foreground">Loading...</p>
          ) : !checklist || checklist.length === 0 ? (
            <p className="text-sm text-muted-foreground">
              No checklist yet - one is created automatically once the offer is accepted.
            </p>
          ) : (
            checklist.map((item) => (
              <div key={item.id} className="flex items-center justify-between gap-2 rounded-lg border p-3 text-sm">
                <div>
                  <p className="font-medium">{PREBOARDING_TASK_LABELS[item.taskType]}</p>
                  {item.bankAccountNumberMasked && (
                    <p className="text-xs text-muted-foreground">
                      {item.bankAccountNumberMasked} &middot; {item.bankIfscCode}
                    </p>
                  )}
                  {item.documentId && (
                    <a
                      href={`${import.meta.env.VITE_API_URL || "/api"}/documents/${item.documentId}`}
                      target="_blank"
                      rel="noreferrer"
                      className="text-xs text-primary hover:underline"
                    >
                      View document
                    </a>
                  )}
                </div>
                <div className="flex items-center gap-2">
                  <Badge variant={item.status === "Completed" ? "default" : "outline"}>
                    {item.status === "Completed" ? "Completed" : "Pending"}
                  </Badge>
                  {item.taskType === "ItAssetRequest" && item.status === "Pending" && (
                    <Button size="sm" variant="outline" onClick={() => completeItMutation.mutate()} disabled={completeItMutation.isPending}>
                      Mark done
                    </Button>
                  )}
                </div>
              </div>
            ))
          )}
        </div>
      </SheetContent>
    </Sheet>
  )
}
