import { useQuery } from "@tanstack/react-query"
import { useState } from "react"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { getLegalEntities, getProducts } from "@/features/employees/api"
import { getOrgChart } from "./api"
import { buildOrgTree } from "./build-tree"
import "./org-chart.css"
import { OrgChartNode } from "./org-chart-node"

const ALL = "all"

export function OrgChartPage() {
  const [legalEntityId, setLegalEntityId] = useState(ALL)
  const [productId, setProductId] = useState(ALL)

  const { data: legalEntities = [] } = useQuery({ queryKey: ["legal-entities"], queryFn: getLegalEntities })
  const { data: products = [] } = useQuery({ queryKey: ["products"], queryFn: getProducts })

  const { data: nodes = [], isLoading } = useQuery({
    queryKey: ["org-chart", legalEntityId, productId],
    queryFn: () =>
      getOrgChart({
        legalEntityId: legalEntityId === ALL ? undefined : legalEntityId,
        productId: productId === ALL ? undefined : productId,
      }),
  })

  const roots = buildOrgTree(nodes)

  return (
    <div className="flex flex-col gap-4">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Org Chart</h1>
        <p className="text-muted-foreground">{nodes.length} employees shown</p>
      </div>

      <div className="flex items-center gap-2">
        <Select value={legalEntityId} onValueChange={setLegalEntityId}>
          <SelectTrigger className="w-[200px]">
            <SelectValue placeholder="All entities" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value={ALL}>All entities</SelectItem>
            {legalEntities.map((entity) => (
              <SelectItem key={entity.id} value={entity.id}>
                {entity.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Select value={productId} onValueChange={setProductId}>
          <SelectTrigger className="w-[200px]">
            <SelectValue placeholder="All products" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value={ALL}>All products</SelectItem>
            {products.map((product) => (
              <SelectItem key={product.id} value={product.id}>
                {product.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      <div className="overflow-x-auto rounded-md border p-6">
        {isLoading ? (
          <p className="text-center text-muted-foreground">Loading...</p>
        ) : roots.length === 0 ? (
          <p className="text-center text-muted-foreground">No employees match this filter.</p>
        ) : (
          <ul className="org-tree">
            {roots.map((root) => (
              <OrgChartNode key={root.id} node={root} />
            ))}
          </ul>
        )}
      </div>
    </div>
  )
}
