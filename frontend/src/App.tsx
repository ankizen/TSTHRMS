import { useEffect } from "react"
import { Navigate, Route, Routes } from "react-router-dom"
import { AppShell } from "@/components/app-shell"
import { ProtectedRoute } from "@/components/protected-route"
import { EmployeeFormPage } from "@/features/employees/employee-form-page"
import { EmployeeListPage } from "@/features/employees/employee-list-page"
import { OrgChartPage } from "@/features/org-chart/org-chart-page"
import { refreshAccessToken } from "@/lib/api-client"
import { DashboardPage } from "@/pages/dashboard-page"
import { LoginPage } from "@/pages/login-page"

export function App() {
  // Try to silently restore a session from the HttpOnly refresh cookie on first load.
  useEffect(() => {
    void refreshAccessToken()
  }, [])

  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        element={
          <ProtectedRoute>
            <AppShell />
          </ProtectedRoute>
        }
      >
        <Route path="/" element={<DashboardPage />} />
        <Route path="/employees" element={<EmployeeListPage />} />
        <Route path="/employees/new" element={<EmployeeFormPage />} />
        <Route path="/employees/:id" element={<EmployeeFormPage />} />
        <Route path="/org-chart" element={<OrgChartPage />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
