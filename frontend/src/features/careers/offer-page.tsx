import { useMutation, useQuery } from "@tanstack/react-query"
import { CheckCircle2, XCircle } from "lucide-react"
import { useState } from "react"
import { useParams } from "react-router-dom"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Skeleton } from "@/components/ui/skeleton"
import { getPublicOffer, respondToPublicOffer } from "./api"

export function OfferPage() {
  const { tenantSlug = "", token = "" } = useParams()
  const [declining, setDeclining] = useState(false)
  const [declineReason, setDeclineReason] = useState("")

  const { data: offer, isLoading, error } = useQuery({
    queryKey: ["careers", tenantSlug, "offer", token],
    queryFn: () => getPublicOffer(tenantSlug, token),
    enabled: Boolean(tenantSlug && token),
    retry: false,
  })

  const respondMutation = useMutation({
    mutationFn: (accepted: boolean) => respondToPublicOffer(tenantSlug, token, accepted, declineReason || null),
  })

  if (isLoading) {
    return (
      <div className="flex flex-col gap-4">
        <Skeleton className="h-8 w-2/3" />
        <Skeleton className="h-48 w-full" />
      </div>
    )
  }

  if (error || !offer) {
    return (
      <div className="flex flex-col items-center gap-3 py-16 text-center">
        <h1 className="font-heading text-2xl font-semibold">This link isn't valid</h1>
        <p className="text-muted-foreground">It may have expired or the link was copied incorrectly.</p>
      </div>
    )
  }

  if (respondMutation.isSuccess || offer.status === "Accepted" || offer.status === "Declined") {
    const accepted = respondMutation.isSuccess ? respondMutation.variables : offer.status === "Accepted"
    return (
      <div className="flex flex-col items-center gap-3 py-16 text-center">
        <CheckCircle2 className="size-10 text-emerald-500" />
        <h1 className="font-heading text-2xl font-semibold">
          {accepted ? "Offer accepted" : "Response recorded"}
        </h1>
        <p className="max-w-sm text-muted-foreground">
          {accepted
            ? `Welcome aboard! Our team will follow up with next steps for joining ${offer.jobTitle}.`
            : "Thanks for letting us know. We wish you the best in your search."}
        </p>
      </div>
    )
  }

  if (offer.isExpired || offer.status === "Expired") {
    return (
      <div className="flex flex-col items-center gap-3 py-16 text-center">
        <h1 className="font-heading text-2xl font-semibold">This offer has expired</h1>
        <p className="max-w-sm text-muted-foreground">
          The response window for {offer.jobTitle} has closed. Reach out to the hiring team if you'd still like to
          discuss this role.
        </p>
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-2">
        <h1 className="font-heading text-2xl font-semibold tracking-tight">{offer.jobTitle}</h1>
        {offer.designation && <p className="text-sm text-muted-foreground">{offer.designation}</p>}
        <p className="text-sm text-muted-foreground">
          Joining {new Date(offer.dateOfJoining).toLocaleDateString()} &middot; Respond by{" "}
          {new Date(offer.expiresAt).toLocaleDateString()}
        </p>
      </div>

      <div className="rounded-xl border p-5">
        <p className="text-sm text-muted-foreground">Annual CTC</p>
        <p className="font-heading text-2xl font-semibold">{offer.annualCtc.toLocaleString()}</p>
        {(offer.fixedComponent || offer.variableComponent || offer.joiningBonus) && (
          <p className="mt-2 text-sm text-muted-foreground">
            Fixed {offer.fixedComponent ?? "-"} &middot; Variable {offer.variableComponent ?? "-"} &middot; Joining
            bonus {offer.joiningBonus ?? "-"}
          </p>
        )}
      </div>

      {offer.offerLetterText && (
        <div className="rounded-xl border bg-muted/30 p-4 leading-relaxed whitespace-pre-wrap">
          {offer.offerLetterText}
        </div>
      )}

      {declining ? (
        <div className="flex flex-col gap-3 rounded-xl border p-4">
          <Label htmlFor="declineReason">Would you like to share why? (optional)</Label>
          <Input id="declineReason" value={declineReason} onChange={(e) => setDeclineReason(e.target.value)} />
          <div className="flex gap-2">
            <Button variant="ghost" onClick={() => setDeclining(false)}>Back</Button>
            <Button variant="destructive" onClick={() => respondMutation.mutate(false)} disabled={respondMutation.isPending}>
              Confirm decline
            </Button>
          </div>
        </div>
      ) : (
        <div className="flex gap-3">
          <Button onClick={() => respondMutation.mutate(true)} disabled={respondMutation.isPending}>
            <CheckCircle2 />
            Accept offer
          </Button>
          <Button variant="outline" onClick={() => setDeclining(true)} disabled={respondMutation.isPending}>
            <XCircle />
            Decline
          </Button>
        </div>
      )}
    </div>
  )
}
