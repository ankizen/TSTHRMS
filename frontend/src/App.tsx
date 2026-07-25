import { useEffect } from "react"
import { Navigate, Route, Routes } from "react-router-dom"
import { AppShell } from "@/components/app-shell"
import { ProtectedRoute } from "@/components/protected-route"
import { CustomFieldsPage } from "@/features/admin/custom-fields/custom-fields-page"
import { EditRequestsPage } from "@/features/admin/edit-requests/edit-requests-page"
import { UsersPage } from "@/features/admin/users/users-page"
import { BulkImportPage } from "@/features/employees/bulk-import/bulk-import-page"
import { EmployeeFormPage } from "@/features/employees/employee-form-page"
import { EmployeeListPage } from "@/features/employees/employee-list-page"
import { MyProfilePage } from "@/features/my/my-profile-page"
import { MyTeamPage } from "@/features/my/my-team-page"
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
        <Route path="/employees/bulk-import" element={<BulkImportPage />} />
        <Route path="/employees/:id" element={<EmployeeFormPage />} />
        <Route path="/org-chart" element={<OrgChartPage />} />
        <Route path="/my/profile" element={<MyProfilePage />} />
        <Route path="/my/team" element={<MyTeamPage />} />
        <Route path="/admin/users" element={<UsersPage />} />
        <Route path="/admin/edit-requests" element={<EditRequestsPage />} />
        <Route path="/admin/custom-fields" element={<CustomFieldsPage />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
