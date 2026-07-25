import { useQuery } from "@tanstack/react-query"
import { Building2 } from "lucide-react"
import { Link, Outlet, useParams } from "react-router-dom"
import { getCompany } from "./api"

export function CareersLayout() {
  const { tenantSlug = "" } = useParams()
  const { data: company } = useQuery({
    queryKey: ["careers", tenantSlug, "company"],
    queryFn: () => getCompany(tenantSlug),
    enabled: Boolean(tenantSlug),
    retry: false,
  })

  return (
    <div className="flex min-h-svh flex-col bg-background">
      <header className="sticky top-0 z-10 border-b bg-background/80 backdrop-blur-sm">
        <div className="mx-auto flex max-w-5xl items-center justify-between gap-2.5 px-6 py-4">
          <div className="flex items-center gap-2.5">
            <div className="flex size-9 shrink-0 items-center justify-center rounded-xl bg-gradient-to-br from-blue-600 to-indigo-600 text-white shadow-sm">
              <Building2 className="size-4.5" />
            </div>
            <div>
              <p className="font-heading text-sm leading-tight font-semibold tracking-tight">
                {company?.name ?? "Careers"}
              </p>
              <p className="text-xs leading-tight text-muted-foreground">Open positions</p>
            </div>
          </div>
          <Link
            to={`/careers/${tenantSlug}/portal/login`}
            className="text-sm font-medium text-muted-foreground transition-colors hover:text-foreground"
          >
            Track application
          </Link>
        </div>
      </header>

      <main className="mx-auto w-full max-w-5xl flex-1 px-6 py-10">
        <Outlet />
      </main>

      <footer className="border-t px-6 py-6 text-center text-xs text-muted-foreground">
        Powered by TSTHRMS
      </footer>
    </div>
  )
}
