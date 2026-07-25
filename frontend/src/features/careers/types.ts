export type PublicEmploymentType = "FullTime" | "Contract" | "Intern"

export interface PublicCompany {
  name: string
}

export interface PublicJobListItem {
  slug: string
  title: string
  department: string | null
  location: string | null
  employmentType: PublicEmploymentType
  legalEntityName: string
  productName: string
  publishedAt: string
}

export interface PublicJobDetail {
  slug: string
  title: string
  description: string
  department: string | null
  location: string | null
  employmentType: PublicEmploymentType
  legalEntityName: string
  productName: string
  publishedAt: string
}

export interface PublicJobFilter {
  legalEntityId?: string
  productId?: string
  location?: string
  department?: string
}

export interface PublicApplicationRequest {
  firstName: string
  lastName: string
  email: string
  phone: string
  currentCtc: number | null
  expectedCtc: number | null
  noticePeriodDays: number | null
  consentGiven: boolean
}

export interface ApplyResult {
  succeeded: boolean
  error: string | null
  applicationId: string | null
}
