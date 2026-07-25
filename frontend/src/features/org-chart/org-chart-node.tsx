import { Badge } from "@/components/ui/badge"
import type { OrgChartTreeNode } from "./types"

export function OrgChartNode({ node }: { node: OrgChartTreeNode }) {
  return (
    <li>
      <div className="inline-flex min-w-[160px] flex-col items-center gap-0.5 rounded-md border bg-card px-3 py-2 text-center shadow-sm">
        <span className="text-sm font-medium">{node.fullName}</span>
        {node.designation && <span className="text-xs text-muted-foreground">{node.designation}</span>}
        {node.status !== "Active" && (
          <Badge variant="secondary" className="mt-1 text-[10px]">
            {node.status}
          </Badge>
        )}
      </div>
      {node.children.length > 0 && (
        <ul>
          {node.children.map((child) => (
            <OrgChartNode key={child.id} node={child} />
          ))}
        </ul>
      )}
    </li>
  )
}
