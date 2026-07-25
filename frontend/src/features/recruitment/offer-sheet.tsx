import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { useState } from "react"
import { toast } from "sonner"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet"
import {
  approveOffer, createOffer, getOffer, reviseOffer, sendOffer, submitOffer,
} from "./api"
import { OFFER_STATUS_BADGE_VARIANT, OFFER_STATUS_LABELS } from "./constants"
import { DecisionDialog } from "./decision-dialog"
import { OfferForm } from "./offer-form"
import type { CreateOrReviseOfferRequest } from "./types"

interface OfferSheetProps {
  applicationId: string | null
  candidateName: string | null
  onOpenChange: (open: boolean) => void
}

export function OfferSheet({ applicationId, candidateName, onOpenChange }: OfferSheetProps) {
  const queryClient = useQueryClient()
  const [showForm, setShowForm] = useState(false)
  const [approveOpen, setApproveOpen] = useState(false)
  const [responseWindowDays, setResponseWindowDays] = useState(7)
  const queryKey = ["recruitment", "applications", applicationId, "offer"]

  const { data: offer, isLoading } = useQuery({
    queryKey,
    queryFn: () => getOffer(applicationId!),
    enabled: Boolean(applicationId),
  })

  const invalidate = () => queryClient.invalidateQueries({ queryKey })

  const createMutation = useMutation({
    mutationFn: (request: CreateOrReviseOfferRequest) => createOffer(applicationId!, request),
    onSuccess: async () => { toast.success("Offer created."); setShowForm(false); await invalidate() },
    onError: () => toast.error("Couldn't create the offer."),
  })

  const reviseMutation = useMutation({
    mutationFn: (request: CreateOrReviseOfferRequest) => reviseOffer(offer!.id, request),
    onSuccess: async () => { toast.success("Offer revised."); setShowForm(false); await invalidate() },
    onError: () => toast.error("Couldn't revise the offer."),
  })

  const submitMutation = useMutation({
    mutationFn: () => submitOffer(offer!.id),
    onSuccess: async () => { toast.success("Submitted for approval."); await invalidate() },
    onError: () => toast.error("Couldn't submit the offer."),
  })

  const approveMutation = useMutation({
    mutationFn: (comment: string | null) => approveOffer(offer!.id, comment),
    onSuccess: async () => { toast.success("Offer approved."); setApproveOpen(false); await invalidate() },
    onError: () => toast.error("Couldn't approve the offer."),
  })

  const sendMutation = useMutation({
    mutationFn: () => sendOffer(offer!.id, { responseWindowDays }),
    onSuccess: async () => { toast.success("Offer sent to the candidate."); await invalidate() },
    onError: () => toast.error("Couldn't send the offer."),
  })

  const latestVersion = offer?.versions.at(-1)
  const canRevise = offer && offer.status !== "Accepted" && offer.status !== "Declined"

  return (
    <Sheet open={Boolean(applicationId)} onOpenChange={onOpenChange}>
      <SheetContent className="sm:max-w-md">
        <SheetHeader>
          <SheetTitle>Offer{candidateName ? ` - ${candidateName}` : ""}</SheetTitle>
        </SheetHeader>

        <div className="flex flex-col gap-3 overflow-y-auto px-4 pb-4">
          {isLoading ? (
            <p className="text-sm text-muted-foreground">Loading...</p>
          ) : !offer && !showForm ? (
            <Button variant="outline" size="sm" className="w-fit" onClick={() => setShowForm(true)}>
              Create offer
            </Button>
          ) : null}

          {offer && (
            <div className="flex flex-col gap-3">
              <div className="flex items-center justify-between">
                <Badge variant={OFFER_STATUS_BADGE_VARIANT[offer.status]}>{OFFER_STATUS_LABELS[offer.status]}</Badge>
                {offer.expiresAt && offer.status === "Sent" && (
                  <span className="text-xs text-muted-foreground">
                    Expires {new Date(offer.expiresAt).toLocaleDateString()}
                  </span>
                )}
              </div>

              {latestVersion && (
                <div className="rounded-xl border p-3 text-sm">
                  <p className="font-medium">{latestVersion.designation || "Role"}</p>
                  <p className="text-muted-foreground">
                    Joining {new Date(latestVersion.dateOfJoining).toLocaleDateString()} &middot; CTC{" "}
                    {latestVersion.annualCtc.toLocaleString()}
                  </p>
                  {(latestVersion.fixedComponent || latestVersion.variableComponent || latestVersion.joiningBonus) && (
                    <p className="mt-1 text-xs text-muted-foreground">
                      Fixed {latestVersion.fixedComponent ?? "-"} &middot; Variable {latestVersion.variableComponent ?? "-"}
                      {" "}&middot; Bonus {latestVersion.joiningBonus ?? "-"}
                    </p>
                  )}
                </div>
              )}

              {offer.status === "Declined" && offer.declineReason && (
                <p className="text-sm text-muted-foreground">Decline reason: {offer.declineReason}</p>
              )}

              <div className="flex flex-wrap gap-2">
                {offer.status === "Draft" && (
                  <Button size="sm" onClick={() => submitMutation.mutate()} disabled={submitMutation.isPending}>
                    Submit for approval
                  </Button>
                )}
                {offer.status === "PendingApproval" && (
                  <Button size="sm" onClick={() => setApproveOpen(true)}>Approve</Button>
                )}
                {offer.status === "Approved" && (
                  <div className="flex items-center gap-2">
                    <Input
                      type="number"
                      min={1}
                      max={30}
                      className="h-8 w-20 text-xs"
                      value={responseWindowDays}
                      onChange={(e) => setResponseWindowDays(Number(e.target.value) || 7)}
                    />
                    <span className="text-xs text-muted-foreground">days to respond</span>
                    <Button size="sm" onClick={() => sendMutation.mutate()} disabled={sendMutation.isPending}>
                      Send offer
                    </Button>
                  </div>
                )}
                {canRevise && !showForm && (
                  <Button size="sm" variant="outline" onClick={() => setShowForm(true)}>Revise</Button>
                )}
              </div>
            </div>
          )}

          {showForm && (
            <OfferForm
              onSubmit={(request) => (offer ? reviseMutation.mutate(request) : createMutation.mutate(request))}
              onCancel={() => setShowForm(false)}
              isSubmitting={createMutation.isPending || reviseMutation.isPending}
              isRevision={Boolean(offer)}
              latestVersion={latestVersion}
            />
          )}

          {offer && offer.versions.length > 1 && (
            <div className="flex flex-col gap-2">
              <p className="text-xs font-medium text-muted-foreground">Negotiation history</p>
              {[...offer.versions].reverse().map((version) => (
                <div key={version.versionNumber} className="rounded-lg border p-2 text-xs">
                  <p className="font-medium">v{version.versionNumber} - CTC {version.annualCtc.toLocaleString()}</p>
                  {version.revisionReason && <p className="text-muted-foreground">{version.revisionReason}</p>}
                </div>
              ))}
            </div>
          )}
        </div>
      </SheetContent>

      <DecisionDialog
        open={approveOpen}
        onOpenChange={setApproveOpen}
        onSubmit={(comment) => approveMutation.mutate(comment)}
        isSubmitting={approveMutation.isPending}
        title="Approve offer"
        actionLabel="Approve"
      />
    </Sheet>
  )
}
