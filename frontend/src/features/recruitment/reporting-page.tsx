import { useQuery } from "@tanstack/react-query"
import { Clock3, Percent, Send, Timer, UserCheck2, Users } from "lucide-react"
import { Bar, BarChart, CartesianGrid, XAxis, YAxis } from "recharts"
import { Badge } from "@/components/ui/badge"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { ChartContainer, ChartTooltip, ChartTooltipContent, type ChartConfig } from "@/components/ui/chart"
import { Skeleton } from "@/components/ui/skeleton"
import { CANDIDATE_SOURCE_LABELS, REQUISITION_STATUS_BADGE_VARIANT, REQUISITION_STATUS_LABELS } from "./constants"
import { getRecruitmentReport } from "./api"

const tintClasses: Record<string, string> = {
  blue: "bg-blue-500/10 text-blue-600",
  emerald: "bg-emerald-500/10 text-emerald-600",
  indigo: "bg-indigo-500/10 text-indigo-600",
  amber: "bg-amber-500/10 text-amber-600",
  violet: "bg-violet-500/10 text-violet-600",
  rose: "bg-rose-500/10 text-rose-600",
}

const barChartConfig = { value: { label: "Value", color: "var(--chart-1)" } } satisfies ChartConfig

export function RecruitmentReportingPage() {
  const { data, isLoading } = useQuery({
    queryKey: ["recruitment", "reports"],
    queryFn: getRecruitmentReport,
  })

  const tiles = data
    ? [
        { label: "Open Requisitions", value: data.summary.openRequisitions, icon: Users, tint: "blue" },
        { label: "Active Applications", value: data.summary.activeApplications, icon: UserCheck2, tint: "indigo" },
        { label: "Hires (Last 30 Days)", value: data.summary.hiresLast30Days, icon: UserCheck2, tint: "emerald" },
        {
          label: "Avg. Time-to-Hire",
          value: data.summary.averageTimeToHireDays !== null ? `${data.summary.averageTimeToHireDays}d` : "-",
          icon: Timer,
          tint: "amber",
        },
        {
          label: "Offer Acceptance Rate",
          value: data.summary.offerAcceptanceRatePercent !== null ? `${data.summary.offerAcceptanceRatePercent}%` : "-",
          icon: Percent,
          tint: "violet",
        },
        {
          label: "Offer-to-Joining Rate",
          value: data.summary.offerToJoiningRatePercent !== null ? `${data.summary.offerToJoiningRatePercent}%` : "-",
          icon: Send,
          tint: "rose",
        },
      ]
    : []

  const sourceChartData = data?.sourceEffectiveness.map((s) => ({
    source: CANDIDATE_SOURCE_LABELS[s.source] ?? s.source,
    value: s.conversionRatePercent,
    applications: s.applications,
    hires: s.hires,
  })) ?? []

  const timeToHireChartData = data?.timeToHireByPosting.map((t) => ({
    source: t.title,
    value: t.averageTimeToHireDays,
    hires: t.hires,
  })) ?? []

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Recruitment Reports</h1>
        <p className="text-muted-foreground">
          Time-to-hire, source effectiveness, offer-to-joining ratio, and requisition ageing.
        </p>
      </div>

      {isLoading ? (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {Array.from({ length: 6 }).map((_, index) => (
            <Skeleton key={index} className="h-24 w-full rounded-xl" />
          ))}
        </div>
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {tiles.map((tile) => (
            <Card key={tile.label} className="cursor-default">
              <CardContent className="flex items-center gap-3.5">
                <div className={`flex size-11 shrink-0 items-center justify-center rounded-xl ${tintClasses[tile.tint]}`}>
                  <tile.icon className="size-5" />
                </div>
                <div>
                  <p className="font-heading text-2xl font-semibold tracking-tight">{tile.value}</p>
                  <p className="text-xs text-muted-foreground">{tile.label}</p>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      <div className="grid gap-4 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle className="text-sm font-semibold">Source Effectiveness (Conversion Rate)</CardTitle>
          </CardHeader>
          <CardContent>
            {isLoading ? (
              <Skeleton className="h-64 w-full rounded-xl" />
            ) : sourceChartData.length === 0 ? (
              <p className="py-12 text-center text-sm text-muted-foreground">No applications yet.</p>
            ) : (
              <ChartContainer config={barChartConfig} className="aspect-auto h-64 w-full">
                <BarChart data={sourceChartData} layout="vertical" margin={{ left: 8 }}>
                  <CartesianGrid horizontal={false} />
                  <XAxis type="number" domain={[0, 100]} tickFormatter={(v) => `${v}%`} />
                  <YAxis type="category" dataKey="source" width={90} tickLine={false} axisLine={false} />
                  <ChartTooltip
                    content={
                      <ChartTooltipContent
                        formatter={(value, _name, item) => (
                          <div className="flex w-full flex-col gap-0.5">
                            <div className="flex items-center justify-between gap-4">
                              <span className="text-muted-foreground">Conversion rate</span>
                              <span className="font-mono font-medium text-foreground">{`${value}%`}</span>
                            </div>
                            <div className="flex items-center justify-between gap-4">
                              <span className="text-muted-foreground">Applications</span>
                              <span className="font-mono font-medium text-foreground">{item.payload.applications}</span>
                            </div>
                            <div className="flex items-center justify-between gap-4">
                              <span className="text-muted-foreground">Hires</span>
                              <span className="font-mono font-medium text-foreground">{item.payload.hires}</span>
                            </div>
                          </div>
                        )}
                      />
                    }
                  />
                  <Bar dataKey="value" fill="var(--color-value)" radius={4} />
                </BarChart>
              </ChartContainer>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-sm font-semibold">Time-to-Hire by Posting (Days)</CardTitle>
          </CardHeader>
          <CardContent>
            {isLoading ? (
              <Skeleton className="h-64 w-full rounded-xl" />
            ) : timeToHireChartData.length === 0 ? (
              <p className="py-12 text-center text-sm text-muted-foreground">No hires recorded yet.</p>
            ) : (
              <ChartContainer config={barChartConfig} className="aspect-auto h-64 w-full">
                <BarChart data={timeToHireChartData} layout="vertical" margin={{ left: 8 }}>
                  <CartesianGrid horizontal={false} />
                  <XAxis type="number" tickFormatter={(v) => `${v}d`} />
                  <YAxis type="category" dataKey="source" width={110} tickLine={false} axisLine={false} />
                  <ChartTooltip
                    content={
                      <ChartTooltipContent
                        formatter={(value, _name, item) => (
                          <div className="flex w-full flex-col gap-0.5">
                            <div className="flex items-center justify-between gap-4">
                              <span className="text-muted-foreground">Avg. time-to-hire</span>
                              <span className="font-mono font-medium text-foreground">{`${value} days`}</span>
                            </div>
                            <div className="flex items-center justify-between gap-4">
                              <span className="text-muted-foreground">Hires</span>
                              <span className="font-mono font-medium text-foreground">{item.payload.hires}</span>
                            </div>
                          </div>
                        )}
                      />
                    }
                  />
                  <Bar dataKey="value" fill="var(--color-value)" radius={4} />
                </BarChart>
              </ChartContainer>
            )}
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-sm font-semibold">Requisition Ageing</CardTitle>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <Skeleton className="h-40 w-full rounded-xl" />
          ) : data?.requisitionAgeing.length === 0 ? (
            <p className="py-8 text-center text-sm text-muted-foreground">No open requisitions right now.</p>
          ) : (
            <div className="flex flex-col divide-y divide-border">
              {data?.requisitionAgeing.map((req) => (
                <div key={req.requisitionId} className="flex flex-wrap items-center justify-between gap-2 py-2.5">
                  <div className="min-w-0">
                    <p className="truncate text-sm font-medium">
                      {req.requisitionCode} &middot; {req.title}
                    </p>
                    <p className="text-xs text-muted-foreground">{req.openings} opening(s)</p>
                  </div>
                  <div className="flex shrink-0 items-center gap-2">
                    <Badge variant={REQUISITION_STATUS_BADGE_VARIANT[req.status]}>
                      {REQUISITION_STATUS_LABELS[req.status]}
                    </Badge>
                    <span className="flex items-center gap-1 text-xs text-muted-foreground">
                      <Clock3 className="size-3.5" />
                      {req.ageInDays}d
                    </span>
                    {req.isStale && <Badge variant="destructive">Ageing</Badge>}
                  </div>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
