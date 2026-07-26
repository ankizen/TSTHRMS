import { publicApiClient } from "@/lib/public-api-client"
import { candidateApiClient } from "./candidate-api-client"
import type {
  ApplyResult,
  CandidateLoginResult,
  MyApplication,
  MyPreboardingTask,
  PreboardingTaskType,
  PublicApplicationRequest,
  PublicAssessment,
  PublicCompany,
  PublicJobDetail,
  PublicJobFilter,
  PublicJobListItem,
  PublicOffer,
  SubmitBankDetailsRequest,
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

export async function getPublicOffer(tenantSlug: string, token: string): Promise<PublicOffer> {
  const { data } = await publicApiClient.get<PublicOffer>(`/public/${tenantSlug}/offers/${token}`)
  return data
}

export async function respondToPublicOffer(
  tenantSlug: string, token: string, accepted: boolean, declineReason: string | null,
): Promise<void> {
  await publicApiClient.post(`/public/${tenantSlug}/offers/${token}/respond`, { accepted, declineReason })
}

export async function requestCandidateOtp(tenantSlug: string, email: string): Promise<void> {
  await publicApiClient.post(`/public/${tenantSlug}/candidate-auth/request-otp`, { email })
}

export async function verifyCandidateOtp(
  tenantSlug: string, email: string, code: string,
): Promise<CandidateLoginResult> {
  const { data } = await publicApiClient.post<CandidateLoginResult>(
    `/public/${tenantSlug}/candidate-auth/verify-otp`, { email, code },
  )
  return data
}

export async function getMyApplications(): Promise<MyApplication[]> {
  const { data } = await candidateApiClient.get<MyApplication[]>("/candidate-portal/applications")
  return data
}

export async function getMyPreboardingChecklist(applicationId: string): Promise<MyPreboardingTask[]> {
  const { data } = await candidateApiClient.get<MyPreboardingTask[]>(
    `/candidate-portal/applications/${applicationId}/preboarding`,
  )
  return data
}

export async function submitPreboardingDocument(
  applicationId: string, taskType: PreboardingTaskType, file: File,
): Promise<void> {
  const formData = new FormData()
  formData.append("file", file)
  await candidateApiClient.post(
    `/candidate-portal/applications/${applicationId}/preboarding/${taskType}/document`, formData,
    { headers: { "Content-Type": "multipart/form-data" } },
  )
}

export async function submitPreboardingBankDetails(
  applicationId: string, request: SubmitBankDetailsRequest,
): Promise<void> {
  await candidateApiClient.post(`/candidate-portal/applications/${applicationId}/preboarding/bank-details`, request)
}
