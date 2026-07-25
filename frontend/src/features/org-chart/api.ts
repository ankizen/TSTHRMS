import { apiClient } from "@/lib/api-client"
import type { OrgChartNode } from "./types"

export interface OrgChartParams {
  legalEntityId?: string
  productId?: string
}

export async function getOrgChart(params: OrgChartParams): Promise<OrgChartNode[]> {
  const { data } = await apiClient.get<OrgChartNode[]>("/employees/org-chart", { params })
  return data
}
