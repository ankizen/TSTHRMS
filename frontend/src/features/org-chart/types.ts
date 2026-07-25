export interface OrgChartNode {
  id: string
  fullName: string
  designation: string | null
  department: string | null
  reportingManagerId: string | null
  status: "Active" | "OnLeave" | "NoticePeriod" | "Exited"
}

export interface OrgChartTreeNode extends OrgChartNode {
  children: OrgChartTreeNode[]
}
