import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { CalendarClock, LogOut, ShieldAlert } from "lucide-react"
import { useEffect } from "react"
import { useNavigate, useParams } from "react-router-dom"
import { toast } from "sonner"
import { EmptyState } from "@/components/empty-state"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Skeleton } from "@/components/ui/skeleton"
import { getMyApplications, getMyDataDeletionRequest, requestDataDeletion } from "./api"
import { useCandidateAuthStore } from "./candidate-auth-store"
import { APPLICATION_STAGE_LABELS, ASSESSMENT_TYPE_LABELS, INTERVIEW_STATUS_LABELS, OFFER_STATUS_LABELS } from "./constants"
import { PreboardingTasks } from "./preboarding-tasks"

export function CandidatePortalPage() {
  const { tenantSlug = "" } = useParams()
  const navigate = useNavigate()
  const accessToken = useCandidateAuthStore((s) => s.accessToken)
  const candidateName = useCandidateAuthStore((s) => s.candidateName)
  const clear = useCandidateAuthStore((s) => s.clear)

  useEffect(() => {
    if (!accessToken) {
      navigate(`/careers/${tenantSlug}/portal/login`, { replace: true })
    }
  }, [accessToken, tenantSlug, navigate])

  const { data: applications, isLoading } = useQuery({
    queryKey: ["careers", "candidate-portal", "applications"],
    queryFn: getMyApplications,
    enabled: Boolean(accessToken),
  })

  const queryClient = useQueryClient()
  const deletionRequestQueryKey = ["careers", "candidate-portal", "data-deletion-request"]
  const { data: deletionRequest } = useQuery({
    queryKey: deletionRequestQueryKey,
    queryFn: getMyDataDeletionRequest,
    enabled: Boolean(accessToken),
  })

  const requestDeletionMutation = useMutation({
    mutationFn: requestDataDeletion,
    onSuccess: async (result) => {
      if (result.succeeded) {
        toast.success("Deletion request submitted - HR will review it shortly.")
        await queryClient.invalidateQueries({ queryKey: deletionRequestQueryKey })
      } else {
        toast.error(result.error ?? "Couldn't submit a deletion request.")
      }
    },
    onError: () => toast.error("Couldn't submit a deletion request."),
  })

  if (!accessToken) {
    return null
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="font-heading text-2xl font-semibold tracking-tight">
            Welcome{candidateName ? `, ${candidateName}` : ""}
          </h1>
          <p className="text-sm text-muted-foreground">Your applications and their current status</p>
        </div>
        <Button
          variant="ghost"
          size="sm"
          onClick={() => {
            clear()
            navigate(`/careers/${tenantSlug}`)
          }}
        >
          <LogOut className="size-3.5" />
          Sign out
        </Button>
      </div>

      {isLoading ? (
        <div className="flex flex-col gap-3">
          {Array.from({ length: 2 }).map((_, index) => (
            <Skeleton key={index} className="h-32 w-full rounded-xl" />
          ))}
        </div>
      ) : !applications || applications.length === 0 ? (
        <EmptyState icon={CalendarClock} title="No applications yet" description="Applications you submit will show up here." />
      ) : (
        <div className="flex flex-col gap-4">
          {applications.map((application) => (
            <div key={application.applicationId} className="flex flex-col gap-3 rounded-xl border p-5">
              <div className="flex flex-wrap items-center justify-between gap-2">
                <p className="font-heading font-semibold">{application.jobPostingTitle}</p>
                <Badge>{APPLICATION_STAGE_LABELS[application.stage]}</Badge>
              </div>
              <p className="text-xs text-muted-foreground">
                Applied {new Date(application.appliedAt).toLocaleDateString()}
              </p>

              {application.assessment && (
                <div className="rounded-lg bg-muted/40 p-3 text-sm">
                  <p className="font-medium">{ASSESSMENT_TYPE_LABELS[application.assessment.type]}</p>
                  <p className="text-muted-foreground">
                    {application.assessment.submitted
                      ? "Submitted - awaiting review"
                      : `Due by ${new Date(application.assessment.dueAt).toLocaleString()}`}
                  </p>
                </div>
              )}

              {application.interviews.length > 0 && (
                <div className="flex flex-col gap-2">
                  <p className="text-xs font-medium text-muted-foreground">Interviews</p>
                  {application.interviews.map((interview) => (
                    <div key={interview.interviewId} className="rounded-lg border p-2 text-sm">
                      <div className="flex items-center justify-between">
                        <span>{new Date(interview.scheduledAt).toLocaleString()}</span>
                        <Badge variant="secondary">{INTERVIEW_STATUS_LABELS[interview.status]}</Badge>
                      </div>
                      {interview.videoLink && (
                        <a href={interview.videoLink} target="_blank" rel="noreferrer" className="text-primary hover:underline">
                          Join video call
                        </a>
                      )}
                    </div>
                  ))}
                </div>
              )}

              {application.offer && (
                <div className="rounded-lg bg-muted/40 p-3 text-sm">
                  <p className="font-medium">Offer: {OFFER_STATUS_LABELS[application.offer.status]}</p>
                  {application.offer.offerToken && (
                    <a
                      href={`/careers/${tenantSlug}/offer/${application.offer.offerToken}`}
                      className="text-primary hover:underline"
                    >
                      View and respond to your offer
                    </a>
                  )}
                </div>
              )}

              {(application.stage === "OfferAccepted" || application.stage === "Hired") && (
                <PreboardingTasks applicationId={application.applicationId} />
              )}
            </div>
          ))}
        </div>
      )}

      <div className="flex flex-col gap-2 rounded-xl border p-5">
        <div className="flex items-center gap-2">
          <ShieldAlert className="size-4 text-muted-foreground" />
          <p className="font-heading font-semibold">Your data</p>
        </div>
        {deletionRequest ? (
          <p className="text-sm text-muted-foreground">
            {deletionRequest.status === "Pending" && "Your request to delete your data is pending HR review."}
            {deletionRequest.status === "Approved" && "Your data has been deleted, as requested."}
            {deletionRequest.status === "Rejected" && "Your deletion request was reviewed and not approved."}
          </p>
        ) : (
          <>
            <p className="text-sm text-muted-foreground">
              You can request that we delete your personal data from our records at any time.
            </p>
            <Button
              variant="outline"
              size="sm"
              className="w-fit"
              onClick={() => requestDeletionMutation.mutate()}
              disabled={requestDeletionMutation.isPending}
            >
              Request deletion of my data
            </Button>
          </>
        )}
      </div>
    </div>
  )
}
