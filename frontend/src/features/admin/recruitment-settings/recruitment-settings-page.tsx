import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { useEffect, useState } from "react"
import { toast } from "sonner"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { Skeleton } from "@/components/ui/skeleton"
import { getTenantSettings, updateTenantSettings } from "@/features/recruitment/api"

export function RecruitmentSettingsPage() {
  const queryClient = useQueryClient()
  const queryKey = ["recruitment", "settings"]
  const { data: settings, isLoading } = useQuery({ queryKey, queryFn: getTenantSettings })

  const [retentionDays, setRetentionDays] = useState(180)
  const [bonusAmount, setBonusAmount] = useState("")
  const [offerTemplate, setOfferTemplate] = useState("")

  useEffect(() => {
    if (settings) {
      setRetentionDays(settings.rejectedCandidateRetentionDays)
      setBonusAmount(settings.referralBonusAmount?.toString() ?? "")
      setOfferTemplate(settings.offerLetterTemplate ?? "")
    }
  }, [settings])

  const updateMutation = useMutation({
    mutationFn: updateTenantSettings,
    onSuccess: async () => {
      toast.success("Settings saved.")
      await queryClient.invalidateQueries({ queryKey })
    },
    onError: () => toast.error("Couldn't save settings."),
  })

  const saveRetention = () =>
    updateMutation.mutate({
      rejectedCandidateRetentionDays: retentionDays,
      referralBonusAmount: settings?.referralBonusAmount ?? null,
      offerLetterTemplate: settings?.offerLetterTemplate ?? null,
    })

  const saveBonus = () =>
    updateMutation.mutate({
      rejectedCandidateRetentionDays: settings?.rejectedCandidateRetentionDays ?? 180,
      referralBonusAmount: bonusAmount.trim() === "" ? null : Number(bonusAmount),
      offerLetterTemplate: settings?.offerLetterTemplate ?? null,
    })

  const saveTemplate = () =>
    updateMutation.mutate({
      rejectedCandidateRetentionDays: settings?.rejectedCandidateRetentionDays ?? 180,
      referralBonusAmount: settings?.referralBonusAmount ?? null,
      offerLetterTemplate: offerTemplate.trim() === "" ? null : offerTemplate,
    })

  if (isLoading) {
    return (
      <div className="flex flex-col gap-4">
        <Skeleton className="h-8 w-64" />
        {Array.from({ length: 3 }).map((_, index) => (
          <Skeleton key={index} className="h-40 w-full rounded-xl" />
        ))}
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Recruitment Settings</h1>
        <p className="text-muted-foreground">Tenant-wide config for candidate retention, referral bonuses, and offer letters.</p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-sm font-semibold">Candidate Data Retention</CardTitle>
          <CardDescription>
            Rejected candidates (not in the Talent Pool) are automatically anonymized after this many days.
          </CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col gap-3">
          <div className="flex items-end gap-2">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="retention-days">Retention period (days)</Label>
              <Input
                id="retention-days"
                type="number"
                min={1}
                className="w-40"
                value={retentionDays}
                onChange={(e) => setRetentionDays(Number(e.target.value))}
              />
            </div>
            <Button onClick={saveRetention} disabled={updateMutation.isPending}>Save</Button>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-sm font-semibold">Referral Bonus</CardTitle>
          <CardDescription>
            Amount paid to an employee whose referral is hired. Leave blank to disable referral bonuses.
          </CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col gap-3">
          <div className="flex items-end gap-2">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="bonus-amount">Bonus amount (₹)</Label>
              <Input
                id="bonus-amount"
                type="number"
                min={0}
                className="w-40"
                placeholder="Not configured"
                value={bonusAmount}
                onChange={(e) => setBonusAmount(e.target.value)}
              />
            </div>
            <Button onClick={saveBonus} disabled={updateMutation.isPending}>Save</Button>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-sm font-semibold">Offer Letter Template</CardTitle>
          <CardDescription>
            Merge variables: {"{{CandidateName}}"}, {"{{Designation}}"}, {"{{CompanyName}}"}, {"{{AnnualCtc}}"},{" "}
            {"{{FixedComponent}}"}, {"{{VariableComponent}}"}, {"{{JoiningBonus}}"}, {"{{DateOfJoining}}"}. Leave blank
            to use the default letter text.
          </CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col gap-3">
          <Textarea
            rows={10}
            placeholder="Dear {{CandidateName}}, we are pleased to offer you the position of {{Designation}}..."
            value={offerTemplate}
            onChange={(e) => setOfferTemplate(e.target.value)}
          />
          <Button className="w-fit" onClick={saveTemplate} disabled={updateMutation.isPending}>Save</Button>
        </CardContent>
      </Card>
    </div>
  )
}
