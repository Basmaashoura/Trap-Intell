// src/App.jsx
import { BrowserRouter, Route, Routes, Navigate } from "react-router-dom";
import { AuthProvider, useAuth } from "./context/AuthContext";
import AppLayout from "./components/AppLayout";
import ProtectedRoute from "./components/ProtectedRoute";
import PageNotFound from "./pages/PageNotFound";

// Auth pages
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

// App pages
import HomePage from "./pages/HomePage";
import Dashboard from "./pages/Dashboard";
import AlertsPage from "./pages/AlertsPage";
import AttacksPage from "./pages/AttacksPage";
import HoneypotsPage from "./pages/HoneypotsPage";
import ThreatActorsPage from "./pages/ThreatActorsPage";
import ReportsPage from "./pages/ReportsPage";
import SettingsPage from "./pages/SettingsPage";

// ── Root redirect: landing if logged out, dashboard if logged in ──
function RootRedirect() {
  const { isAuthenticated } = useAuth();
  return isAuthenticated ? <Navigate to="/dashboard" replace /> : <HomePage />;
}

function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          {/* ── Landing ── */}
          <Route path="/" element={<RootRedirect />} />

          {/* ── Public auth routes ── */}
          <Route path="/login" element={<LoginPage />} />
          <Route path="/signup" element={<SignUpPage />} />
          <Route path="/join/:token" element={<JoinOrganization />} />
          <Route path="/forgot-password" element={<ForgotPasswordPage />} />
          <Route path="/set-password" element={<SetPasswordPage />} />
          <Route path="/check-email" element={<CheckEmailPage />} />
          <Route path="/verify-code" element={<VerifyCodePage />} />
          <Route path="/pending-approval" element={<PendingApprovalPage />} />
          <Route path="/account-suspended" element={<AccountSuspendedPage />} />
          <Route path="/setup-complete" element={<SetupComplete />} />

          {/* ── Protected app routes ── */}
          <Route
            element={
              <ProtectedRoute>
                <AppLayout />
              </ProtectedRoute>
            }
          >
            <Route path="/dashboard" element={<Dashboard />} />
            <Route path="/honeypots" element={<HoneypotsPage />} />
            <Route path="/attacks" element={<AttacksPage />} />
            <Route path="/alerts" element={<AlertsPage />} />
            <Route path="/threat-actors" element={<ThreatActorsPage />} />
            <Route path="/reports" element={<ReportsPage />} />
            <Route path="/settings" element={<SettingsPage />} />
          </Route>

          {/* ── Protected auth route (must be logged in) ── */}
          <Route
            path="/create-org"
            element={
              <ProtectedRoute>
                <CreateOrganization />
              </ProtectedRoute>
            }
          />

          {/* Root → dashboard */}
          {/* <Route path="/" element={<Navigate to="/dashboard" replace />} /> */}

          {/* Catch-all */}
          <Route path="*" element={<PageNotFound />} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  );
}

function ComingSoon({ page }) {
  const { subscriptionActive } = useAuth();
  const navigate = useNavigate();

  if (!subscriptionActive) {
    return (
      <div
        style={{
          display: "flex",
          flexDirection: "column",
          alignItems: "center",
          justifyContent: "center",
          height: "60vh",
          gap: 12,
          color: "#555770",
        }}
      >
        <svg
          width="48"
          height="48"
          viewBox="0 0 24 24"
          fill="none"
          stroke="#c8cce0"
          strokeWidth="1.5"
          strokeLinecap="round"
          strokeLinejoin="round"
        >
          <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-2h2v2zm0-4h-2V7h2v6z" />
        </svg>
        <h2 style={{ margin: 0, color: "#111326", fontWeight: 800 }}>
          Subscription Required
        </h2>
        <p style={{ margin: 0, fontSize: "0.9rem" }}>
          Activate a subscription to access this section.
        </p>
        <button
          onClick={() => navigate("/settings?tab=billing")}
          style={{
            marginTop: 12,
            padding: "10px 24px",
            backgroundColor: "#4f46e5",
            color: "#fff",
            border: "none",
            borderRadius: "6px",
            cursor: "pointer",
            fontSize: "0.9rem",
            fontWeight: 600,
          }}
        >
          Go To Billing
        </button>
      </div>
    );
  }

  return (
    <div
      style={{
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        justifyContent: "center",
        height: "60vh",
        gap: 12,
        color: "#555770",
      }}
    >
      <svg
        width="48"
        height="48"
        viewBox="0 0 24 24"
        fill="none"
        stroke="#c8cce0"
        strokeWidth="1.5"
        strokeLinecap="round"
        strokeLinejoin="round"
      >
        <circle cx="12" cy="12" r="10" />
        <line x1="12" y1="8" x2="12" y2="12" />
        <line x1="12" y1="16" x2="12.01" y2="16" />
      </svg>
      <h2 style={{ margin: 0, color: "#111326", fontWeight: 800 }}>{page}</h2>
      <p style={{ margin: 0, fontSize: "0.9rem" }}>
        This section is coming soon.
      </p>
    </div>
  );
}

export { ComingSoon };
export default App;
