import { createContext, useContext, useState, useCallback } from "react";
import { api, tokenStorage } from "../services/api";

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [user, setUser] = useState(() => {
    try {
      const stored = localStorage.getItem("user");
      return stored ? JSON.parse(stored) : null;
    } catch {
      return null;
    }
  });

  const [subscriptionActive, setSubscriptionActive] = useState(
    localStorage.getItem("subscription_active") === "true",
  );
  const checkSubscription = useCallback(async () => {
    const organizationId =
      localStorage.getItem("org_id") || user?.organizationId;

    if (!organizationId) {
      setSubscriptionActive(false);
      return false;
    }

    try {
      const subscription = await api.get(
        `/api/organizations/${organizationId}/subscriptions/current`,
      );

      const active =
        subscription &&
        subscription.status !== "Cancelled" &&
        subscription.status !== "Suspended";

      setSubscriptionActive(active);
      localStorage.setItem("subscription_active", active);

      return active;
    } catch (err) {
      if (err.status === 404) {
        setSubscriptionActive(false);
        localStorage.setItem("subscription_active", false);
        return false;
      }

      throw err;
    }
  }, [user]);

  const [orgId, setOrgId] = useState(
    () => localStorage.getItem("org_id") || null,
  );

  // ── Login ──────────────────────────────────────────────────────────────────
  // Confirmed response shape from backend:
  // {
  //   accessToken, refreshToken, expiresIn, tokenType,
  //   user: {
  //     id, email, userName, firstName, lastName, fullName,
  //     roleId, role, organizationId, emailConfirmed,
  //     twoFactorEnabled, permissions: string[]
  //   }
  // }
  // No top-level "organization" object. No "status" field on user.
  // Role is a string name e.g. "OrganizationAdmin", "SecurityAnalyst"
  const login = useCallback(async (email, password, rememberMe = false) => {
    // Throws on 4xx/5xx — LoginPage catches and shows the error banner
    const data = await api.post("/api/auth/login", {
      email,
      password,
      rememberMe,
    });

    // Store tokens
    tokenStorage.setTokens(data.accessToken, data.refreshToken ?? null);

    // Persist user + permissions
    const userData = data.user;
    localStorage.setItem("user", JSON.stringify(userData));
    localStorage.setItem("org_id", userData.organizationId ?? "");
    localStorage.setItem(
      "permissions",
      JSON.stringify(userData.permissions ?? []),
    );

    setUser(userData);
    setOrgId(userData.organizationId ?? null);

    await checkSubscription();

    return data;
  }, []);

  // ── Logout ─────────────────────────────────────────────────────────────────
  const logout = useCallback(async () => {
    try {
      await api.post("/api/auth/logout", {});
    } catch {
      // Ignore errors — clear state regardless
    } finally {
      tokenStorage.clear();
      localStorage.removeItem("permissions");
      setUser(null);
      setOrgId(null);
      window.location.href = "/login";
    }
  }, []);

  // ── Permission helpers ─────────────────────────────────────────────────────
  const hasPermission = useCallback(
    (permission) => {
      const perms = user?.permissions ?? [];
      return perms.includes(permission);
    },
    [user],
  );

  const hasRole = useCallback(
    (role) => {
      if (!user) return false;
      if (Array.isArray(role)) return role.includes(user.role);
      return user.role === role;
    },
    [user],
  );

  const isAuthenticated = !!user && !!tokenStorage.getAccess();

  const value = {
    user,
    orgId,
    isAuthenticated,
    subscriptionActive,
    checkSubscription,
    login,
    logout,
    hasRole,
    hasPermission,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used inside <AuthProvider>");
  return ctx;
}

export default AuthContext;
