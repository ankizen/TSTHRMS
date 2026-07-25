import { apiClient } from "@/lib/api-client"
import type {
  ApplicantListItem,
  JobRequisition,
  JobRequisitionListItem,
  JobRequisitionWriteRequest,
  MoveApplicationStageRequest,
  PublishJobPostingRequest,
  RequisitionStatus,
  TalentPoolCandidate,
} from "./types"

export async function getRequisitions(status?: RequisitionStatus): Promise<JobRequisitionListItem[]> {
  const { data } = await apiClient.get<JobRequisitionListItem[]>("/recruitment/requisitions", { params: { status } })
  return data
}

export async function getRequisition(id: string): Promise<JobRequisition> {
  const { data } = await apiClient.get<JobRequisition>(`/recruitment/requisitions/${id}`)
  return data
}

export async function createRequisition(request: JobRequisitionWriteRequest): Promise<JobRequisition> {
  const { data } = await apiClient.post<JobRequisition>("/recruitment/requisitions", request)
  return data
}

export async function updateRequisition(id: string, request: JobRequisitionWriteRequest): Promise<JobRequisition> {
  const { data } = await apiClient.put<JobRequisition>(`/recruitment/requisitions/${id}`, request)
  return data
}

export async function submitRequisition(id: string): Promise<JobRequisition> {
  const { data } = await apiClient.post<JobRequisition>(`/recruitment/requisitions/${id}/submit`)
  return data
}

export async function approveRequisition(id: string, comment: string | null): Promise<JobRequisition> {
  const { data } = await apiClient.post<JobRequisition>(`/recruitment/requisitions/${id}/approve`, { comment })
  return data
}

export async function rejectRequisition(id: string, comment: string | null): Promise<JobRequisition> {
  const { data } = await apiClient.post<JobRequisition>(`/recruitment/requisitions/${id}/reject`, { comment })
  return data
}

export async function holdRequisition(id: string): Promise<JobRequisition> {
  const { data } = await apiClient.post<JobRequisition>(`/recruitment/requisitions/${id}/hold`)
  return data
}

export async function resumeRequisition(id: string): Promise<JobRequisition> {
  const { data } = await apiClient.post<JobRequisition>(`/recruitment/requisitions/${id}/resume`)
  return data
}

export async function closeRequisition(id: string): Promise<JobRequisition> {
  const { data } = await apiClient.post<JobRequisition>(`/recruitment/requisitions/${id}/close`)
  return data
}

export async function publishRequisition(id: string, request: PublishJobPostingRequest): Promise<JobRequisition> {
  const { data } = await apiClient.post<JobRequisition>(`/recruitment/requisitions/${id}/publish`, request)
  return data
}

export async function getApplicants(jobPostingId: string): Promise<ApplicantListItem[]> {
  const { data } = await apiClient.get<ApplicantListItem[]>(`/recruitment/job-postings/${jobPostingId}/applicants`)
  return data
}

export async function moveApplicationStage(
  applicationId: string, request: MoveApplicationStageRequest,
): Promise<ApplicantListItem> {
  const { data } = await apiClient.patch<ApplicantListItem>(
    `/recruitment/applications/${applicationId}/stage`, request,
  )
  return data
}

export async function setTalentPool(candidateId: string, isInTalentPool: boolean): Promise<void> {
  await apiClient.post(`/recruitment/candidates/${candidateId}/talent-pool`, isInTalentPool)
}

export async function getTalentPool(): Promise<TalentPoolCandidate[]> {
  const { data } = await apiClient.get<TalentPoolCandidate[]>("/recruitment/candidates/talent-pool")
  return data
}

export async function getCurrentTenant(): Promise<{ id: string; name: string; slug: string }> {
  const { data } = await apiClient.get<{ id: string; name: string; slug: string }>("/tenant/current")
  return data
}
