import { apiClient } from "@/lib/api-client"
import type { ApplyResult, MyReferral, OpenJobOption, ReferralPayout, ReferralSubmissionRequest } from "./types"

export async function getOpenJobs(): Promise<OpenJobOption[]> {
  const { data } = await apiClient.get<OpenJobOption[]>("/referrals/jobs")
  return data
}

export async function submitReferral(
  jobSlug: string, request: ReferralSubmissionRequest, resume: File | null,
): Promise<ApplyResult> {
  const formData = new FormData()
  formData.append("firstName", request.firstName)
  formData.append("lastName", request.lastName)
  formData.append("email", request.email)
  formData.append("phone", request.phone)
  if (resume) formData.append("resume", resume)

  const { data } = await apiClient.post<ApplyResult>(`/referrals/jobs/${jobSlug}`, formData, {
    headers: { "Content-Type": "multipart/form-data" },
  })
  return data
}

export async function getMyReferrals(): Promise<MyReferral[]> {
  const { data } = await apiClient.get<MyReferral[]>("/referrals/mine")
  return data
}

export async function getReferralPayouts(): Promise<ReferralPayout[]> {
  const { data } = await apiClient.get<ReferralPayout[]>("/referrals/payouts")
  return data
}

export async function markReferralBonusPaid(candidateId: string): Promise<void> {
  await apiClient.post(`/referrals/payouts/${candidateId}/mark-paid`)
}
