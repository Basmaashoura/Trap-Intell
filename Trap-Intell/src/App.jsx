import { BrowserRouter, Route, Routes, Navigate } from "react-router-dom";
import AppLayout from "./components/AppLayout";
import HomePage from "./pages/HomePage";
import PageNotFound from "./pages/PageNotFound";
import Dashboard from "./pages/Dashboard";

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
import AlertsPage from "./pages/AlertsPage";
import AttacksPage from "./pages/AttacksPage";
import ProtectedRoute from "./components/ProtectedRoute";
import { AuthProvider } from "./context/AuthContext";

function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          {/* public auth routes */}
          <Route path="/login" element={<LoginPage />} />
          <Route path="/signup" element={<SignUpPage />} />
          <Route path="/create-org" element={<CreateOrganization />} />
          {/* Invitation route */}
          <Route path="/join-org/:token" element={<JoinOrganization />} />
          {/* Password reset */}
          <Route path="/forgot-password" element={<ForgotPasswordPage />} />
          <Route path="/reset-password" element={<SetPasswordPage />} />
          {/* Email verification */}
          <Route path="/check-email" element={<CheckEmailPage />} />
          <Route path="/verify-code" element={<VerifyCodePage />} />
          {/* Status screens */}
          <Route path="/pending-approval" element={<PendingApprovalPage />} />
          <Route path="/account-suspended" element={<AccountSuspendedPage />} />
          <Route path="/setup-complete" element={<SetupComplete />} />
          {/* Protected app routes */}{" "}
          <Route
            element={
              <ProtectedRoute>
                <AppLayout />
              </ProtectedRoute>
            }
          >
            <Route path="/" element={<HomePage />} />
            <Route path="/dashboard" element={<Dashboard />} />
            {/* <Route path="/honeypots" element={<HoneypotsPage />} /> */}
            <Route path="/attacks" element={<AttacksPage />} />
            <Route path="/alerts" element={<AlertsPage />} />
          </Route>
          {/* Default redirects */}
          <Route path="/" element={<Navigate to="/login" replace />} />
          {/* Catch-all redirect */}
          <Route path="*" element={<Navigate to="/login" replace />} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  );
}

export default App;
