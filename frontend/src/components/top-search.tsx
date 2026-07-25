import { useQuery } from "@tanstack/react-query"
import { Loader2, Search } from "lucide-react"
import { useEffect, useState } from "react"
import { useNavigate } from "react-router-dom"
import {
  Popover,
  PopoverAnchor,
  PopoverContent,
} from "@/components/ui/popover"
import { getEmployees } from "@/features/employees/api"

/** HRAdmin/HRBP only - the employee list API this searches is itself restricted to those roles
 * (Section 14 access control), so there's nothing to show anyone else. */
export function TopSearch() {
  const navigate = useNavigate()
  const [query, setQuery] = useState("")
  const [debounced, setDebounced] = useState("")
  const [open, setOpen] = useState(false)

  useEffect(() => {
    const timeout = setTimeout(() => setDebounced(query.trim()), 250)
    return () => clearTimeout(timeout)
  }, [query])

  const { data, isFetching } = useQuery({
    queryKey: ["employees", "quick-search", debounced],
    queryFn: () => getEmployees({ search: debounced, page: 1, pageSize: 6 }),
    enabled: debounced.length > 1,
  })

  const results = debounced.length > 1 ? (data?.items ?? []) : []

  return (
    <Popover open={open && results.length > 0}>
      <PopoverAnchor asChild>
        <div className="relative w-full max-w-xs">
          <Search className="pointer-events-none absolute top-1/2 left-2.5 size-4 -translate-y-1/2 text-muted-foreground" />
          <input
            value={query}
            onChange={(event) => {
              setQuery(event.target.value)
              setOpen(true)
            }}
            onFocus={() => setOpen(true)}
            onBlur={() => setTimeout(() => setOpen(false), 150)}
            placeholder="Search employees..."
            className="h-9 w-full rounded-lg border border-input bg-muted/40 py-1 pr-8 pl-8 text-sm outline-none transition-all duration-200 placeholder:text-muted-foreground hover:border-foreground/20 focus-visible:border-ring focus-visible:bg-background focus-visible:ring-4 focus-visible:ring-ring/15"
          />
          {isFetching && (
            <Loader2 className="absolute top-1/2 right-2.5 size-3.5 -translate-y-1/2 animate-spin text-muted-foreground" />
          )}
        </div>
      </PopoverAnchor>
      <PopoverContent
        align="start"
        onOpenAutoFocus={(event) => event.preventDefault()}
        className="w-[22rem] p-1.5"
      >
        {results.map((employee) => (
          <button
            key={employee.id}
            type="button"
            onMouseDown={(event) => event.preventDefault()}
            onClick={() => {
              navigate(`/employees/${employee.id}`)
              setQuery("")
              setOpen(false)
            }}
            className="flex w-full flex-col items-start rounded-md px-2.5 py-2 text-left transition-colors hover:bg-muted"
          >
            <span className="text-sm font-medium">
              {employee.firstName} {employee.lastName}
            </span>
            <span className="text-xs text-muted-foreground">
              {employee.employeeCode}
              {employee.designation ? ` · ${employee.designation}` : ""}
            </span>
          </button>
        ))}
      </PopoverContent>
    </Popover>
  )
}
