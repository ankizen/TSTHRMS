import { publicApiClient } from "@/lib/public-api-client"
import type {
  ApplyResult,
  PublicApplicationRequest,
  PublicAssessment,
  PublicCompany,
  PublicJobDetail,
  PublicJobFilter,
  PublicJobListItem,
} from "./types"

export async function getCompany(tenantSlug: string): Promise<PublicCompany> {
  const { data } = await publicApiClient.get<PublicCompany>(`/public/${tenantSlug}/company`)
  return data
}

export async function getPublicJobs(
  tenantSlug: string, filter: PublicJobFilter,
): Promise<PublicJobListItem[]> {
  const { data } = await publicApiClient.get<PublicJobListItem[]>(`/public/${tenantSlug}/jobs`, { params: filter })
  return data
}

export async function getPublicJobBySlug(tenantSlug: string, jobSlug: string): Promise<PublicJobDetail> {
  const { data } = await publicApiClient.get<PublicJobDetail>(`/public/${tenantSlug}/jobs/${jobSlug}`)
  return data
}

export async function submitApplication(
  tenantSlug: string,
  jobSlug: string,
  request: PublicApplicationRequest,
  resume: File,
): Promise<ApplyResult> {
  const formData = new FormData()
  formData.append("firstName", request.firstName)
  formData.append("lastName", request.lastName)
  formData.append("email", request.email)
  formData.append("phone", request.phone)
  if (request.currentCtc !== null) formData.append("currentCtc", String(request.currentCtc))
  if (request.expectedCtc !== null) formData.append("expectedCtc", String(request.expectedCtc))
  if (request.noticePeriodDays !== null) formData.append("noticePeriodDays", String(request.noticePeriodDays))
  formData.append("consentGiven", String(request.consentGiven))
  formData.append("resume", resume)

  const { data } = await publicApiClient.post<ApplyResult>(
    `/public/${tenantSlug}/jobs/${jobSlug}/apply`, formData,
    { headers: { "Content-Type": "multipart/form-data" } },
  )
  return data
}

export async function getPublicAssessment(tenantSlug: string, token: string): Promise<PublicAssessment> {
  const { data } = await publicApiClient.get<PublicAssessment>(`/public/${tenantSlug}/assessments/${token}`)
  return data
}

export async function submitPublicAssessment(tenantSlug: string, token: string, submissionText: string): Promise<void> {
  await publicApiClient.post(`/public/${tenantSlug}/assessments/${token}/submit`, { submissionText })
}
