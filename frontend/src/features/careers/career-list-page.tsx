import { useQuery } from "@tanstack/react-query"
import { Briefcase, MapPin, Search } from "lucide-react"
import { useState } from "react"
import { Link, useParams } from "react-router-dom"
import { EmptyState } from "@/components/empty-state"
import { Badge } from "@/components/ui/badge"
import { Input } from "@/components/ui/input"
import { Skeleton } from "@/components/ui/skeleton"
import { getPublicJobs } from "./api"
import { EMPLOYMENT_TYPE_LABELS } from "./constants"

export function CareerListPage() {
  const { tenantSlug = "" } = useParams()
  const [search, setSearch] = useState("")

  const { data: jobs = [], isLoading } = useQuery({
    queryKey: ["careers", tenantSlug, "jobs"],
    queryFn: () => getPublicJobs(tenantSlug, {}),
    enabled: Boolean(tenantSlug),
  })

  const filtered = jobs.filter((job) => {
    if (!search) return true
    const haystack = `${job.title} ${job.department ?? ""} ${job.location ?? ""}`.toLowerCase()
    return haystack.includes(search.toLowerCase())
  })

  return (
    <div className="flex flex-col gap-8">
      <div className="flex flex-col gap-3">
        <h1 className="font-heading text-3xl font-semibold tracking-tight text-balance sm:text-4xl">
          Join our team
        </h1>
        <p className="max-w-xl text-muted-foreground">
          We're building something ambitious - explore our open roles below and find where you fit in.
        </p>
      </div>

      <div className="relative max-w-sm">
        <Search className="absolute top-2.5 left-3 size-4 text-muted-foreground" />
        <Input
          placeholder="Search by title, department, or location..."
          className="h-10 pl-9"
          value={search}
          onChange={(event) => setSearch(event.target.value)}
        />
      </div>

      {isLoading ? (
        <div className="flex flex-col gap-3">
          {Array.from({ length: 4 }).map((_, index) => (
            <Skeleton key={index} className="h-24 w-full rounded-xl" />
          ))}
        </div>
      ) : filtered.length === 0 ? (
        <EmptyState
          icon={Briefcase}
          title={jobs.length === 0 ? "No open positions right now" : "No roles match your search"}
          description={
            jobs.length === 0
              ? "Check back soon - we're always growing."
              : "Try a different search term."
          }
        />
      ) : (
        <div className="flex flex-col gap-3">
          {filtered.map((job) => (
            <Link
              key={job.slug}
              to={`/careers/${tenantSlug}/${job.slug}`}
              className="group flex flex-col gap-3 rounded-xl border p-5 transition-all hover:border-primary/40 hover:shadow-md sm:flex-row sm:items-center sm:justify-between"
            >
              <div className="flex flex-col gap-1.5">
                <h2 className="font-heading text-lg font-semibold tracking-tight transition-colors group-hover:text-primary">
                  {job.title}
                </h2>
                <div className="flex flex-wrap items-center gap-x-3 gap-y-1 text-sm text-muted-foreground">
                  {job.department && <span>{job.department}</span>}
                  {job.location && (
                    <span className="flex items-center gap-1">
                      <MapPin className="size-3.5" />
                      {job.location}
                    </span>
                  )}
                  <span>{job.legalEntityName} · {job.productName}</span>
                </div>
              </div>
              <Badge variant="secondary" className="w-fit shrink-0">
                {EMPLOYMENT_TYPE_LABELS[job.employmentType]}
              </Badge>
            </Link>
          ))}
        </div>
      )}
    </div>
  )
}
