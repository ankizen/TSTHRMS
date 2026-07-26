import { apiClient } from "@/lib/api-client"
import type {
  ApplicantListItem,
  AssessmentDetail,
  Bgv,
  CandidateDataDeletionRequest,
  CandidateDataDeletionRequestStatus,
  ConvertToEmployeeResult,
  CreateOrReviseOfferRequest,
  DecideDeletionRequestRequest,
  DecideDeletionRequestResult,
  InitiateBgvRequest,
  Interview,
  InterviewerCandidate,
  InterviewScorecard,
  JobRequisition,
  JobRequisitionListItem,
  JobRequisitionWriteRequest,
  MoveApplicationStageRequest,
  MyInterview,
  Offer,
  OnboardingChecklistItem,
  PreboardingChecklistItem,
  PublishJobPostingRequest,
  RecruitmentReport,
  RequisitionStatus,
  RescheduleInterviewRequest,
  ScheduleInterviewRequest,
  ScoreAssessmentRequest,
  SendAssessmentResult,
  SendOfferRequest,
  SubmitScorecardRequest,
  TalentPoolCandidate,
  TenantSettings,
  TestConfiguration,
  TestConfigurationRequest,
  UpdateBgvStatusRequest,
  UpdateInterviewStatusRequest,
  UpdateOnboardingItemRequest,
  UpdateTenantSettingsRequest,
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

export async function getInterviews(applicationId: string): Promise<Interview[]> {
  const { data } = await apiClient.get<Interview[]>(`/recruitment/applications/${applicationId}/interviews`)
  return data
}

export async function scheduleInterview(applicationId: string, request: ScheduleInterviewRequest): Promise<Interview> {
  const { data } = await apiClient.post<Interview>(`/recruitment/applications/${applicationId}/interviews`, request)
  return data
}

export async function rescheduleInterview(interviewId: string, request: RescheduleInterviewRequest): Promise<Interview> {
  const { data } = await apiClient.post<Interview>(`/recruitment/interviews/${interviewId}/reschedule`, request)
  return data
}

export async function updateInterviewStatus(interviewId: string, request: UpdateInterviewStatusRequest): Promise<Interview> {
  const { data } = await apiClient.post<Interview>(`/recruitment/interviews/${interviewId}/status`, request)
  return data
}

export async function getInterviewerCandidates(): Promise<InterviewerCandidate[]> {
  const { data } = await apiClient.get<InterviewerCandidate[]>("/recruitment/interviewer-candidates")
  return data
}

export async function getMyInterviews(): Promise<MyInterview[]> {
  const { data } = await apiClient.get<MyInterview[]>("/recruitment/my-interviews")
  return data
}

export async function submitScorecard(interviewId: string, request: SubmitScorecardRequest): Promise<InterviewScorecard> {
  const { data } = await apiClient.post<InterviewScorecard>(`/recruitment/my-interviews/${interviewId}/scorecard`, request)
  return data
}

export async function getTestConfiguration(jobPostingId: string): Promise<TestConfiguration> {
  const { data } = await apiClient.get<TestConfiguration>(`/recruitment/job-postings/${jobPostingId}/test-configuration`)
  return data
}

export async function configureTest(jobPostingId: string, request: TestConfigurationRequest): Promise<TestConfiguration> {
  const { data } = await apiClient.put<TestConfiguration>(
    `/recruitment/job-postings/${jobPostingId}/test-configuration`, request,
  )
  return data
}

export async function sendAssessment(applicationId: string): Promise<SendAssessmentResult> {
  const { data } = await apiClient.post<SendAssessmentResult>(`/recruitment/applications/${applicationId}/assessment`)
  return data
}

export async function getAssessmentDetail(assessmentSubmissionId: string): Promise<AssessmentDetail> {
  const { data } = await apiClient.get<AssessmentDetail>(`/recruitment/assessments/${assessmentSubmissionId}`)
  return data
}

export async function scoreAssessment(
  assessmentSubmissionId: string, request: ScoreAssessmentRequest,
): Promise<AssessmentDetail> {
  const { data } = await apiClient.post<AssessmentDetail>(
    `/recruitment/assessments/${assessmentSubmissionId}/score`, request,
  )
  return data
}

export async function getOffer(applicationId: string): Promise<Offer | null> {
  try {
    const { data } = await apiClient.get<Offer>(`/recruitment/applications/${applicationId}/offer`)
    return data
  } catch {
    return null
  }
}

export async function createOffer(applicationId: string, request: CreateOrReviseOfferRequest): Promise<Offer> {
  const { data } = await apiClient.post<Offer>(`/recruitment/applications/${applicationId}/offer`, request)
  return data
}

export async function reviseOffer(offerId: string, request: CreateOrReviseOfferRequest): Promise<Offer> {
  const { data } = await apiClient.put<Offer>(`/recruitment/offers/${offerId}`, request)
  return data
}

export async function submitOffer(offerId: string): Promise<Offer> {
  const { data } = await apiClient.post<Offer>(`/recruitment/offers/${offerId}/submit`)
  return data
}

export async function approveOffer(offerId: string, comment: string | null): Promise<Offer> {
  const { data } = await apiClient.post<Offer>(`/recruitment/offers/${offerId}/approve`, { comment })
  return data
}

export async function sendOffer(offerId: string, request: SendOfferRequest): Promise<Offer> {
  const { data } = await apiClient.post<Offer>(`/recruitment/offers/${offerId}/send`, request)
  return data
}

export async function getBgv(applicationId: string): Promise<Bgv> {
  const { data } = await apiClient.get<Bgv>(`/recruitment/applications/${applicationId}/bgv`)
  return data
}

export async function initiateBgv(applicationId: string, request: InitiateBgvRequest): Promise<Bgv> {
  const { data } = await apiClient.post<Bgv>(`/recruitment/applications/${applicationId}/bgv/initiate`, request)
  return data
}

export async function updateBgvStatus(applicationId: string, request: UpdateBgvStatusRequest): Promise<Bgv> {
  const { data } = await apiClient.post<Bgv>(`/recruitment/applications/${applicationId}/bgv/status`, request)
  return data
}

export async function getPreboardingChecklist(applicationId: string): Promise<PreboardingChecklistItem[]> {
  const { data } = await apiClient.get<PreboardingChecklistItem[]>(
    `/recruitment/applications/${applicationId}/preboarding`,
  )
  return data
}

export async function completeItAssetTask(applicationId: string): Promise<PreboardingChecklistItem> {
  const { data } = await apiClient.post<PreboardingChecklistItem>(
    `/recruitment/applications/${applicationId}/preboarding/it-asset-request/complete`,
  )
  return data
}

export async function convertToEmployee(applicationId: string): Promise<ConvertToEmployeeResult> {
  const { data } = await apiClient.post<ConvertToEmployeeResult>(
    `/recruitment/applications/${applicationId}/convert-to-employee`,
  )
  return data
}

export async function getOnboardingChecklist(employeeId: string): Promise<OnboardingChecklistItem[]> {
  const { data } = await apiClient.get<OnboardingChecklistItem[]>(
    `/recruitment/employees/${employeeId}/onboarding-checklist`,
  )
  return data
}

export async function updateOnboardingItem(
  itemId: string, request: UpdateOnboardingItemRequest,
): Promise<OnboardingChecklistItem> {
  const { data } = await apiClient.put<OnboardingChecklistItem>(`/recruitment/onboarding-checklist/${itemId}`, request)
  return data
}

export async function completeOnboardingItem(itemId: string): Promise<OnboardingChecklistItem> {
  const { data } = await apiClient.post<OnboardingChecklistItem>(`/recruitment/onboarding-checklist/${itemId}/complete`)
  return data
}

export async function getRecruitmentReport(): Promise<RecruitmentReport> {
  const { data } = await apiClient.get<RecruitmentReport>("/recruitment/reports")
  return data
}

export async function getTenantSettings(): Promise<TenantSettings> {
  const { data } = await apiClient.get<TenantSettings>("/recruitment/settings")
  return data
}

export async function updateTenantSettings(request: UpdateTenantSettingsRequest): Promise<TenantSettings> {
  const { data } = await apiClient.put<TenantSettings>("/recruitment/settings", request)
  return data
}

export async function getDeletionRequests(
  status?: CandidateDataDeletionRequestStatus,
): Promise<CandidateDataDeletionRequest[]> {
  const { data } = await apiClient.get<CandidateDataDeletionRequest[]>(
    "/recruitment/data-privacy/deletion-requests", { params: { status } },
  )
  return data
}

export async function decideDeletionRequest(
  requestId: string, request: DecideDeletionRequestRequest,
): Promise<DecideDeletionRequestResult> {
  const { data } = await apiClient.post<DecideDeletionRequestResult>(
    `/recruitment/data-privacy/deletion-requests/${requestId}/decide`, request,
  )
  return data
}

export async function runRetentionSweep(): Promise<number> {
  const { data } = await apiClient.post<number>("/recruitment/data-privacy/run-retention-sweep")
  return data
}
