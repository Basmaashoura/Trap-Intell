import { BrowserRouter, Route, Routes } from "react-router-dom";
import AppLayout from "./components/AppLayout";
import HomePage from "./pages/HomePage";
import PageNotFound from "./pages/PageNotFound";
import Dashboard from "./pages/Dashboard";

// auth pages — no layout wrapper
import LoginPage from "./pages/auth/LoginPage";
import SignUpPage from "./pages/auth/SignUpPage";
import ForgotPasswordPage from "./pages/auth/ForgotPasswordPage";
import CheckEmailPage from "./pages/auth/CheckEmailPage";
import VerifyCodePage from "./pages/auth/VerifyCodePage";
import SetPasswordPage from "./pages/auth/SetPasswordPage";
import SetupComplete from "./pages/auth/SetupComplete";
import JoinOrganization from "./pages/auth/JoinOrganization";
import CreateOrganization from "./pages/auth/CreateOrganization";

import AlertsPage from "./pages/AlertsPage";
import AttacksPage from "./pages/AttacksPage";
import ProtectedRoute from "./components/ProtectedRoute";

function App() {
  return (
    <BrowserRouter>
      <Routes>
        {/* Auth routes — standalone, no sidebar/navbar */}
        <Route path="/login" element={<LoginPage />} />
        <Route path="/signup" element={<SignUpPage />} />
        <Route path="/create-org" element={<CreateOrganization />} />
        <Route path="/join-org" element={<JoinOrganization />} />
        <Route path="/forgot-password" element={<ForgotPasswordPage />} />
        <Route path="/check-email" element={<CheckEmailPage />} />
        <Route path="/verify-code" element={<VerifyCodePage />} />
        <Route path="/set-password" element={<SetPasswordPage />} />
        <Route path="/setup-complete" element={<SetupComplete />} />

        {/* App routes — with sidebar/navbar via AppLayout */}
        <ProtectedRoute>
          <Route element={<AppLayout />}>
            <Route path="/" element={<HomePage />} />
            <Route path="/dashboard" element={<Dashboard />} />
            {/* add more app pages here as you build them */}
            {/* <Route path="/honeypots" element={<HoneypotsPage />} /> */}
            <Route path="/attacks" element={<AttacksPage />} />
            <Route path="/alerts" element={<AlertsPage />} />
          </Route>
        </ProtectedRoute>

        <Route path="*" element={<PageNotFound />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
