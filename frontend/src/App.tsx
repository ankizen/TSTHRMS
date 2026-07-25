import { useEffect } from "react"
import { Navigate, Route, Routes } from "react-router-dom"
import { AppShell } from "@/components/app-shell"
import { ProtectedRoute } from "@/components/protected-route"
import { CustomFieldsPage } from "@/features/admin/custom-fields/custom-fields-page"
import { EditRequestsPage } from "@/features/admin/edit-requests/edit-requests-page"
import { UsersPage } from "@/features/admin/users/users-page"
import { CareerDetailPage } from "@/features/careers/career-detail-page"
import { CareerListPage } from "@/features/careers/career-list-page"
import { CareersLayout } from "@/features/careers/careers-layout"
import { BulkImportPage } from "@/features/employees/bulk-import/bulk-import-page"
import { EmployeeFormPage } from "@/features/employees/employee-form-page"
import { EmployeeListPage } from "@/features/employees/employee-list-page"
import { MyProfilePage } from "@/features/my/my-profile-page"
import { MyTeamPage } from "@/features/my/my-team-page"
import { OrgChartPage } from "@/features/org-chart/org-chart-page"
import { ApplicantsPage } from "@/features/recruitment/applicants-page"
import { RequisitionDetailPage } from "@/features/recruitment/requisition-detail-page"
import { RequisitionsListPage } from "@/features/recruitment/requisitions-list-page"
import { TalentPoolPage } from "@/features/recruitment/talent-pool-page"
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
      <Route path="/careers/:tenantSlug" element={<CareersLayout />}>
        <Route index element={<CareerListPage />} />
        <Route path=":jobSlug" element={<CareerDetailPage />} />
      </Route>
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
        <Route path="/recruitment/requisitions" element={<RequisitionsListPage />} />
        <Route path="/recruitment/requisitions/:id" element={<RequisitionDetailPage />} />
        <Route path="/recruitment/postings/:jobPostingId/applicants" element={<ApplicantsPage />} />
        <Route path="/recruitment/talent-pool" element={<TalentPoolPage />} />
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
