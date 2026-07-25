import type { OrgChartNode, OrgChartTreeNode } from "./types"

/** Any node whose manager isn't in the (possibly filtered) node set renders as its own root -
 * simpler than trying to reach past a manager the chart can't show. */
export function buildOrgTree(nodes: OrgChartNode[]): OrgChartTreeNode[] {
  const byId = new Map<string, OrgChartTreeNode>()
  nodes.forEach((node) => byId.set(node.id, { ...node, children: [] }))

  const roots: OrgChartTreeNode[] = []

  byId.forEach((node) => {
    const parent = node.reportingManagerId ? byId.get(node.reportingManagerId) : undefined
    if (parent) {
      parent.children.push(node)
    } else {
      roots.push(node)
    }
  })

  return roots
}
