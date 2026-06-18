import {
  createContext,
  useContext,
  useState,
  useEffect,
  useCallback,
} from "react";
import { tokenStorage } from "../services/api";
import api from "../services/api";

// ── Context ────────────────────────────────────────────────────
const AuthContext = createContext(null);

// ── Provider ───────────────────────────────────────────────────
export function AuthProvider({ children }) {
  const [user, setUser] = useState(() => {
    try {
      const stored = localStorage.getItem("user");
      return stored ? JSON.parse(stored) : null;
    } catch {
      return null;
    }
  });

  const [orgId, setOrgId] = useState(
    () => localStorage.getItem("org_id") || null,
  );

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  // ── Login ────────────────────────────────────────────────────
  const login = useCallback(async (email, password) => {
    setLoading(true);
    setError(null);

    try {
      const data = await api.post("/api/auth/login", { email, password });

      // Store tokens — adjust key names to match your API response
      tokenStorage.setTokens(data.accessToken, data.refreshToken);

      // Store user info
      const userData = data.user || data;
      localStorage.setItem("user", JSON.stringify(userData));
      localStorage.setItem("org_id", userData.organizationId || "");

      setUser(userData);
      setOrgId(userData.organizationId || null);

      return { success: true, data };
    } catch (err) {
      setError(err.message);
      return { success: false, error: err.message };
    } finally {
      setLoading(false);
    }
  }, []);

  // ── Logout ───────────────────────────────────────────────────
  const logout = useCallback(async () => {
    try {
      // Tell the server to revoke the refresh token
      await api.post("/api/auth/logout", {});
    } catch {
      // Even if the request fails, clear local state
    } finally {
      tokenStorage.clear();
      setUser(null);
      setOrgId(null);
      window.location.href = "/login";
    }
  }, []);

  // ── Derived helpers ──────────────────────────────────────────
  const isAuthenticated = !!user && !!tokenStorage.getAccess();

  const hasRole = useCallback(
    (role) => {
      if (!user) return false;
      if (Array.isArray(role)) return role.includes(user.role);
      return user.role === role;
    },
    [user],
  );

  // ── Clear error when user navigates ─────────────────────────
  const clearError = useCallback(() => setError(null), []);

  const value = {
    user,
    orgId,
    loading,
    error,
    isAuthenticated,
    login,
    logout,
    hasRole,
    clearError,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

// ── Hook ───────────────────────────────────────────────────────
export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used inside <AuthProvider>");
  return ctx;
}

export default AuthContext;
