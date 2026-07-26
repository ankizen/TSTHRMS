import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { useState } from "react"
import { toast } from "sonner"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import {
  getMyPreboardingChecklist, submitPreboardingBankDetails, submitPreboardingDocument,
} from "./api"
import { PREBOARDING_TASK_LABELS } from "./constants"
import type { PreboardingTaskType } from "./types"

const DOCUMENT_TASK_TYPES: PreboardingTaskType[] = ["EducationCertificate", "IdentityProof", "PreviousEmploymentRelievingLetter"]

export function PreboardingTasks({ applicationId }: { applicationId: string }) {
  const queryClient = useQueryClient()
  const queryKey = ["careers", "candidate-portal", "preboarding", applicationId]

  const { data: tasks } = useQuery({
    queryKey,
    queryFn: () => getMyPreboardingChecklist(applicationId),
  })

  const invalidate = () => queryClient.invalidateQueries({ queryKey })

  const documentMutation = useMutation({
    mutationFn: ({ taskType, file }: { taskType: PreboardingTaskType; file: File }) =>
      submitPreboardingDocument(applicationId, taskType, file),
    onSuccess: async () => { toast.success("Document submitted."); await invalidate() },
    onError: () => toast.error("Couldn't submit that document."),
  })

  const [bankAccountNumber, setBankAccountNumber] = useState("")
  const [bankIfscCode, setBankIfscCode] = useState("")

  const bankDetailsMutation = useMutation({
    mutationFn: () => submitPreboardingBankDetails(applicationId, { bankAccountNumber, bankIfscCode }),
    onSuccess: async () => { toast.success("Bank details submitted."); await invalidate() },
    onError: () => toast.error("Couldn't submit bank details."),
  })

  if (!tasks || tasks.length === 0) {
    return null
  }

  return (
    <div className="flex flex-col gap-2 rounded-lg bg-muted/40 p-3">
      <p className="text-xs font-medium text-muted-foreground">Pre-boarding checklist</p>
      {tasks
        .filter((task) => task.taskType !== "WelcomeCommunication" && task.taskType !== "ItAssetRequest")
        .map((task) => (
          <div key={task.taskType} className="flex flex-col gap-1.5 text-sm">
            <div className="flex items-center justify-between">
              <span>{PREBOARDING_TASK_LABELS[task.taskType]}</span>
              <Badge variant={task.status === "Completed" ? "default" : "outline"}>
                {task.status === "Completed" ? "Submitted" : "Pending"}
              </Badge>
            </div>

            {task.status === "Pending" && DOCUMENT_TASK_TYPES.includes(task.taskType) && (
              <Input
                type="file"
                accept="application/pdf,image/jpeg,image/png"
                className="h-9 text-xs"
                onChange={(event) => {
                  const file = event.target.files?.[0]
                  if (file) documentMutation.mutate({ taskType: task.taskType, file })
                }}
              />
            )}

            {task.status === "Pending" && task.taskType === "BankDetails" && (
              <div className="flex flex-col gap-2 sm:flex-row">
                <Input
                  placeholder="Account number"
                  className="h-9 text-xs"
                  value={bankAccountNumber}
                  onChange={(e) => setBankAccountNumber(e.target.value)}
                />
                <Input
                  placeholder="IFSC code"
                  className="h-9 text-xs"
                  value={bankIfscCode}
                  onChange={(e) => setBankIfscCode(e.target.value)}
                />
                <Button
                  size="sm"
                  disabled={!bankAccountNumber || !bankIfscCode || bankDetailsMutation.isPending}
                  onClick={() => bankDetailsMutation.mutate()}
                >
                  Submit
                </Button>
              </div>
            )}
          </div>
        ))}
    </div>
  )
}
