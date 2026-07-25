import { zodResolver } from "@hookform/resolvers/zod"
import { useMutation, useQuery } from "@tanstack/react-query"
import axios from "axios"
import { ArrowLeft, CheckCircle2, MapPin } from "lucide-react"
import { useState } from "react"
import { Controller, useForm } from "react-hook-form"
import { Link, useParams } from "react-router-dom"
import { z } from "zod"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Checkbox } from "@/components/ui/checkbox"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Skeleton } from "@/components/ui/skeleton"
import { getPublicJobBySlug, submitApplication } from "./api"
import { EMPLOYMENT_TYPE_LABELS } from "./constants"
import type { ApplyResult } from "./types"

const applySchema = z.object({
  firstName: z.string().min(1, "First name is required"),
  lastName: z.string().min(1, "Last name is required"),
  email: z.string().min(1, "Email is required").email("Enter a valid email address"),
  phone: z.string().min(1, "Phone number is required"),
  currentCtc: z.string(),
  expectedCtc: z.string(),
  noticePeriodDays: z.string(),
  consentGiven: z.boolean().refine((v) => v, "Please provide consent to continue"),
})

type ApplyFormValues = z.infer<typeof applySchema>

export function CareerDetailPage() {
  const { tenantSlug = "", jobSlug = "" } = useParams()
  const [resumeFile, setResumeFile] = useState<File | null>(null)
  const [resumeTouched, setResumeTouched] = useState(false)

  const { data: job, isLoading } = useQuery({
    queryKey: ["careers", tenantSlug, "jobs", jobSlug],
    queryFn: () => getPublicJobBySlug(tenantSlug, jobSlug),
    enabled: Boolean(tenantSlug && jobSlug),
    retry: false,
  })

  const {
    register,
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<ApplyFormValues>({
    resolver: zodResolver(applySchema),
    defaultValues: {
      firstName: "", lastName: "", email: "", phone: "",
      currentCtc: "", expectedCtc: "", noticePeriodDays: "", consentGiven: false,
    },
  })

  const applyMutation = useMutation({
    mutationFn: ({ values, resume }: { values: ApplyFormValues; resume: File }) =>
      submitApplication(
        tenantSlug, jobSlug,
        {
          firstName: values.firstName,
          lastName: values.lastName,
          email: values.email,
          phone: values.phone,
          currentCtc: values.currentCtc ? Number(values.currentCtc) : null,
          expectedCtc: values.expectedCtc ? Number(values.expectedCtc) : null,
          noticePeriodDays: values.noticePeriodDays ? Number(values.noticePeriodDays) : null,
          consentGiven: values.consentGiven,
        },
        resume,
      ),
  })

  const onSubmit = handleSubmit((values) => {
    setResumeTouched(true)
    if (!resumeFile) return
    applyMutation.mutate({ values, resume: resumeFile })
  })

  if (isLoading) {
    return (
      <div className="flex flex-col gap-4">
        <Skeleton className="h-8 w-2/3" />
        <Skeleton className="h-4 w-1/3" />
        <Skeleton className="mt-4 h-64 w-full" />
      </div>
    )
  }

  if (!job) {
    return (
      <div className="flex flex-col items-center gap-4 py-16 text-center">
        <h1 className="font-heading text-2xl font-semibold">This job posting isn't available</h1>
        <p className="text-muted-foreground">It may have been closed or the link is incorrect.</p>
        <Button asChild variant="outline">
          <Link to={`/careers/${tenantSlug}`}>
            <ArrowLeft />
            Back to all openings
          </Link>
        </Button>
      </div>
    )
  }

  const applyError = applyMutation.error
  const applyErrorMessage = axios.isAxiosError<{ error?: string }>(applyError)
    ? applyError.response?.data?.error ?? "Something went wrong. Please try again."
    : applyError
      ? "Something went wrong. Please try again."
      : null

  const result = applyMutation.data as ApplyResult | undefined

  return (
    <div className="flex flex-col gap-8">
      <Link
        to={`/careers/${tenantSlug}`}
        className="flex w-fit items-center gap-1.5 text-sm text-muted-foreground transition-colors hover:text-foreground"
      >
        <ArrowLeft className="size-3.5" />
        Back to all openings
      </Link>

      <div className="flex flex-col gap-3">
        <div className="flex flex-wrap items-center gap-3">
          <h1 className="font-heading text-3xl font-semibold tracking-tight text-balance">{job.title}</h1>
          <Badge variant="secondary">{EMPLOYMENT_TYPE_LABELS[job.employmentType]}</Badge>
        </div>
        <div className="flex flex-wrap items-center gap-x-3 gap-y-1 text-sm text-muted-foreground">
          {job.department && <span>{job.department}</span>}
          {job.location && (
            <span className="flex items-center gap-1">
              <MapPin className="size-3.5" />
              {job.location}
            </span>
          )}
          <span>{job.legalEntityName} · {job.productName}</span>
        </div>
      </div>

      <div className="max-w-none leading-relaxed whitespace-pre-wrap text-foreground/90">
        {job.description}
      </div>

      <div className="rounded-xl border p-6 sm:p-8">
        {result?.succeeded ? (
          <div className="flex flex-col items-center gap-3 py-8 text-center">
            <CheckCircle2 className="size-10 text-emerald-500" />
            <h2 className="font-heading text-xl font-semibold">Application received</h2>
            <p className="max-w-sm text-muted-foreground">
              Thanks for applying - we've emailed you a confirmation. Our hiring team will reach out if your
              profile is a fit for the next step.
            </p>
          </div>
        ) : (
          <form onSubmit={onSubmit} className="flex flex-col gap-5">
            <h2 className="font-heading text-xl font-semibold tracking-tight">Apply for this role</h2>

            <div className="grid gap-4 sm:grid-cols-2">
              <div className="flex flex-col gap-2">
                <Label htmlFor="firstName">First name</Label>
                <Input id="firstName" aria-invalid={Boolean(errors.firstName)} {...register("firstName")} />
                {errors.firstName && <p className="text-sm text-destructive">{errors.firstName.message}</p>}
              </div>
              <div className="flex flex-col gap-2">
                <Label htmlFor="lastName">Last name</Label>
                <Input id="lastName" aria-invalid={Boolean(errors.lastName)} {...register("lastName")} />
                {errors.lastName && <p className="text-sm text-destructive">{errors.lastName.message}</p>}
              </div>
              <div className="flex flex-col gap-2">
                <Label htmlFor="email">Email</Label>
                <Input id="email" type="email" aria-invalid={Boolean(errors.email)} {...register("email")} />
                {errors.email && <p className="text-sm text-destructive">{errors.email.message}</p>}
              </div>
              <div className="flex flex-col gap-2">
                <Label htmlFor="phone">Phone</Label>
                <Input id="phone" aria-invalid={Boolean(errors.phone)} {...register("phone")} />
                {errors.phone && <p className="text-sm text-destructive">{errors.phone.message}</p>}
              </div>
              <div className="flex flex-col gap-2">
                <Label htmlFor="currentCtc">Current CTC (optional)</Label>
                <Input id="currentCtc" type="number" min={0} {...register("currentCtc")} />
              </div>
              <div className="flex flex-col gap-2">
                <Label htmlFor="expectedCtc">Expected CTC (optional)</Label>
                <Input id="expectedCtc" type="number" min={0} {...register("expectedCtc")} />
              </div>
              <div className="flex flex-col gap-2">
                <Label htmlFor="noticePeriodDays">Notice period (days, optional)</Label>
                <Input id="noticePeriodDays" type="number" min={0} {...register("noticePeriodDays")} />
              </div>
              <div className="flex flex-col gap-2">
                <Label htmlFor="resume">Resume (PDF, JPG, or PNG, up to 10MB)</Label>
                <Input
                  id="resume"
                  type="file"
                  accept="application/pdf,image/jpeg,image/png"
                  onChange={(event) => setResumeFile(event.target.files?.[0] ?? null)}
                />
                {resumeTouched && !resumeFile && (
                  <p className="text-sm text-destructive">A resume file is required.</p>
                )}
              </div>
            </div>

            <div className="flex items-start gap-2">
              <Controller
                control={control}
                name="consentGiven"
                render={({ field }) => (
                  <Checkbox
                    id="consentGiven"
                    checked={field.value}
                    onCheckedChange={(checked) => field.onChange(checked === true)}
                    className="mt-0.5"
                  />
                )}
              />
              <Label htmlFor="consentGiven" className="cursor-pointer font-normal text-muted-foreground">
                I consent to this company storing and processing my application data for hiring purposes.
              </Label>
            </div>
            {errors.consentGiven && <p className="text-sm text-destructive">{errors.consentGiven.message}</p>}

            {applyErrorMessage && (
              <p className="rounded-lg bg-destructive/10 px-3 py-2 text-sm text-destructive">{applyErrorMessage}</p>
            )}

            <Button type="submit" isLoading={applyMutation.isPending} className="mt-1 w-fit">
              {applyMutation.isPending ? "Submitting..." : "Submit application"}
            </Button>
          </form>
        )}
      </div>
    </div>
  )
}
