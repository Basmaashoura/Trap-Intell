import { BrowserRouter, Route, Routes, Navigate } from "react-router-dom";
import { AuthProvider } from "./context/AuthContext";
import AppLayout from "./components/AppLayout";
import ProtectedRoute from "./components/ProtectedRoute";
import HomePage from "./pages/HomePage";
import PageNotFound from "./pages/PageNotFound";

// auth pages
import LoginPage from "./pages/auth/LoginPage";
import SignUpPage from "./pages/auth/SignUpPage";

import CreateOrganization from "./pages/auth/CreateOrganization";
import JoinOrganization from "./pages/auth/JoinOrganization";

import ForgotPasswordPage from "./pages/auth/ForgotPasswordPage";
import SetPasswordPage from "./pages/auth/SetPasswordPage";
import CheckEmailPage from "./pages/auth/CheckEmailPage";
import VerifyCodePage from "./pages/auth/VerifyCodePage";
import SetupComplete from "./pages/auth/SetupComplete";

import PendingApprovalPage from "./pages/auth/PendingApprovalPage";
import AccountSuspendedPage from "./pages/auth/AccountSuspendedPage";

// app pages
import Dashboard from "./pages/Dashboard";
import AlertsPage from "./pages/AlertsPage";
import AttacksPage from "./pages/AttacksPage";
import HoneypotsPage from "./pages/HoneypotsPage";

function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          {/* Public auth routes */}
          <Route path="/login" element={<LoginPage />} />
          <Route path="/signup" element={<SignUpPage />} />
          <Route path="/create-org" element={<CreateOrganization />} />
          <Route path="/join-org/:token" element={<JoinOrganization />} />
          <Route path="/forgot-password" element={<ForgotPasswordPage />} />
          <Route path="/reset-password" element={<SetPasswordPage />} />
          <Route path="/check-email" element={<CheckEmailPage />} />
          <Route path="/verify-code" element={<VerifyCodePage />} />
          <Route path="/pending-approval" element={<PendingApprovalPage />} />
          <Route path="/account-suspended" element={<AccountSuspendedPage />} />
          <Route path="/setup-complete" element={<SetupComplete />} />

          {/* Protected app routes */}
          <Route
            element={
              <ProtectedRoute>
                <AppLayout />
              </ProtectedRoute>
            }
          >
            <Route path="/" element={<HomePage />} />
            <Route path="/dashboard" element={<Dashboard />} />
            <Route path="/honeypots" element={<HoneypotsPage />} />
            <Route path="/attacks" element={<AttacksPage />} />
            <Route path="/alerts" element={<AlertsPage />} />
          </Route>

          {/* Catch-all → 404 */}
          <Route path="*" element={<PageNotFound />} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  );
}

export default App;
