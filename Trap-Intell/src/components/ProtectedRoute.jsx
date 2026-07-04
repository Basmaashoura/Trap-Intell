import { Navigate, useLocation } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

export default function ProtectedRoute({ children }) {
  const { isAuthenticated, subscriptionActive } = useAuth();

  const location = useLocation();

  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  const allowed = location.pathname.startsWith("/settings");

  if (!subscriptionActive && !allowed) {
    return <Navigate to="/settings?tab=billing" replace />;
  }

  return children;
}
