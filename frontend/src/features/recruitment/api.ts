import { apiClient } from "@/lib/api-client"
import type {
  ApplicantListItem,
  AssessmentDetail,
  Interview,
  InterviewerCandidate,
  InterviewScorecard,
  JobRequisition,
  JobRequisitionListItem,
  JobRequisitionWriteRequest,
  MoveApplicationStageRequest,
  MyInterview,
  PublishJobPostingRequest,
  RequisitionStatus,
  RescheduleInterviewRequest,
  ScheduleInterviewRequest,
  ScoreAssessmentRequest,
  SendAssessmentResult,
  SubmitScorecardRequest,
  TalentPoolCandidate,
  TestConfiguration,
  TestConfigurationRequest,
  UpdateInterviewStatusRequest,
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
