import { useQuery } from "@tanstack/react-query"
import { Bell } from "lucide-react"
import { Link } from "react-router-dom"
import { getPendingEditRequests } from "@/features/admin/edit-requests/api"

/** Real data, not a placeholder - the badge count is however many employee edit requests are
 * actually waiting on HR review right now. No general notification system exists in this app,
 * so this only ever surfaces the one thing there's real data for. */
export function NotificationBell() {
  const { data: pending = [] } = useQuery({
    queryKey: ["edit-requests", "pending", "count"],
    queryFn: getPendingEditRequests,
    refetchInterval: 60_000,
  })

  return (
    <Link
      to="/admin/edit-requests"
      title={pending.length > 0 ? `${pending.length} pending edit request${pending.length === 1 ? "" : "s"}` : "No pending edit requests"}
      className="relative flex size-8 shrink-0 items-center justify-center rounded-lg text-muted-foreground transition-colors hover:bg-muted hover:text-foreground"
    >
      <Bell className="size-4.5" />
      {pending.length > 0 && (
        <span className="absolute top-1 right-1 flex size-4 items-center justify-center rounded-full bg-destructive text-[10px] font-medium text-destructive-foreground">
          {pending.length > 9 ? "9+" : pending.length}
        </span>
      )}
    </Link>
  )
}
