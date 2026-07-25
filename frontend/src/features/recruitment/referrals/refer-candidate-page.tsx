import { useMutation, useQuery } from "@tanstack/react-query"
import { useState } from "react"
import { toast } from "sonner"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { getOpenJobs, submitReferral } from "./api"

export function ReferCandidatePage() {
  const { data: jobs = [] } = useQuery({ queryKey: ["referrals", "open-jobs"], queryFn: getOpenJobs })

  const [jobSlug, setJobSlug] = useState("")
  const [firstName, setFirstName] = useState("")
  const [lastName, setLastName] = useState("")
  const [email, setEmail] = useState("")
  const [phone, setPhone] = useState("")
  const [resume, setResume] = useState<File | null>(null)

  const submitMutation = useMutation({
    mutationFn: () => submitReferral(jobSlug, { firstName, lastName, email, phone }, resume),
    onSuccess: (result) => {
      if (result.succeeded) {
        toast.success("Referral submitted - thanks for helping us hire!")
        setFirstName(""); setLastName(""); setEmail(""); setPhone(""); setResume(null); setJobSlug("")
      } else {
        toast.error(result.error ?? "Couldn't submit the referral.")
      }
    },
    onError: () => toast.error("Couldn't submit the referral."),
  })

  const isValid = Boolean(jobSlug && firstName && lastName && email && phone)

  return (
    <div className="flex flex-col gap-4">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Refer a Candidate</h1>
        <p className="text-muted-foreground">Know someone great for one of our open roles? Refer them here.</p>
      </div>

      <form
        onSubmit={(event) => {
          event.preventDefault()
          if (isValid) submitMutation.mutate()
        }}
        className="flex max-w-lg flex-col gap-4 rounded-xl border p-5"
      >
        <div className="flex flex-col gap-2">
          <Label>Job opening</Label>
          <Select value={jobSlug} onValueChange={setJobSlug}>
            <SelectTrigger><SelectValue placeholder="Select an open role" /></SelectTrigger>
            <SelectContent>
              {jobs.map((job) => (
                <SelectItem key={job.slug} value={job.slug}>
                  {job.title}{job.department ? ` - ${job.department}` : ""}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div className="flex flex-col gap-2">
            <Label htmlFor="referralFirstName">First name</Label>
            <Input id="referralFirstName" value={firstName} onChange={(e) => setFirstName(e.target.value)} />
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="referralLastName">Last name</Label>
            <Input id="referralLastName" value={lastName} onChange={(e) => setLastName(e.target.value)} />
          </div>
        </div>

        <div className="flex flex-col gap-2">
          <Label htmlFor="referralEmail">Email</Label>
          <Input id="referralEmail" type="email" value={email} onChange={(e) => setEmail(e.target.value)} />
        </div>

        <div className="flex flex-col gap-2">
          <Label htmlFor="referralPhone">Phone</Label>
          <Input id="referralPhone" value={phone} onChange={(e) => setPhone(e.target.value)} />
        </div>

        <div className="flex flex-col gap-2">
          <Label htmlFor="referralResume">Resume (optional)</Label>
          <Input
            id="referralResume"
            type="file"
            accept="application/pdf,image/jpeg,image/png"
            onChange={(e) => setResume(e.target.files?.[0] ?? null)}
          />
        </div>

        <Button type="submit" disabled={!isValid || submitMutation.isPending} className="w-fit">
          {submitMutation.isPending ? "Submitting..." : "Submit referral"}
        </Button>
      </form>
    </div>
  )
}
