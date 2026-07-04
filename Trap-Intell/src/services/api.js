const BASE_URL = import.meta.env.VITE_API_BASE_URL || "http://localhost:5000";

// ── Token helpers ──────────────────────────────────────────────
export const tokenStorage = {
  getAccess: () => localStorage.getItem("access_token"),
  getRefresh: () => localStorage.getItem("refresh_token"),
  setTokens: (access, refresh) => {
    localStorage.setItem("access_token", access);
    if (refresh) localStorage.setItem("refresh_token", refresh);
  },
  clear: () => {
    localStorage.removeItem("access_token");
    localStorage.removeItem("refresh_token");
    localStorage.removeItem("org_id");
    localStorage.removeItem("user");
    localStorage.removeItem("permissions");
  },
};

// ── Refresh token logic (runs once even if multiple calls fail) ─
let refreshPromise = null;

async function refreshAccessToken() {
  if (refreshPromise) return refreshPromise;

  refreshPromise = fetch(`${BASE_URL}/api/auth/refresh`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ refreshToken: tokenStorage.getRefresh() }),
  })
    .then((res) => {
      if (!res.ok) throw new Error("Refresh failed");
      return res.json();
    })
    .then((data) => {
      tokenStorage.setTokens(data.accessToken, data.refreshToken);
      return data.accessToken;
    })
    .catch((err) => {
      tokenStorage.clear();
      window.location.href = "/login";
      throw err;
    })
    .finally(() => {
      refreshPromise = null;
    });

  return refreshPromise;
}

// Auth endpoints that should NEVER trigger a token refresh on 401
// (401 on these means bad credentials, not expired token)
const NO_REFRESH_ENDPOINTS = [
  "/api/auth/login",
  "/api/auth/register",
  "/api/auth/refresh",
  "/api/auth/forgot-password",
  "/api/auth/reset-password",
  "/api/auth/verify-email",
  "/api/organizations",
  "/api/plans",
];

function isNoRefreshEndpoint(endpoint) {
  return NO_REFRESH_ENDPOINTS.some((e) => endpoint.includes(e));
}

// ── Core request function ──────────────────────────────────────
async function request(endpoint, options = {}, retry = true) {
  const token = tokenStorage.getAccess();

  const headers = {
    "Content-Type": "application/json",
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
    ...options.headers,
  };

  const response = await fetch(`${BASE_URL}${endpoint}`, {
    ...options,
    headers,
  });

  // Token expired — try refresh once
  // BUT skip refresh for auth endpoints (401 there = bad credentials)
  if (response.status === 401 && retry && !isNoRefreshEndpoint(endpoint)) {
    try {
      await refreshAccessToken();
      return request(endpoint, options, false);
    } catch {
      return response;
    }
  }

  return response;
}

// ── Response parser ────────────────────────────────────────────
async function parseResponse(response) {
  const contentType = response.headers.get("content-type");
  const isJson =
    contentType &&
    (contentType.includes("application/json") ||
      contentType.includes("application/problem+json"));

  if (!response.ok) {
    let errorMessage = `HTTP ${response.status}`;
    if (isJson) {
      const errorData = await response.json();
      // RFC 7807 Problem Details: use detail → title → message → fallback
      errorMessage =
        errorData.detail ||
        errorData.title ||
        errorData.message ||
        errorMessage;
    }
    const error = new Error(errorMessage);
    error.status = response.status;
    throw error;
  }

  if (response.status === 204 || !isJson) return null;
  return response.json();
}

// ── Public API methods ─────────────────────────────────────────
export const api = {
  get: async (endpoint, params) => {
    let url = endpoint;
    if (params) {
      const query = new URLSearchParams(
        Object.entries(params).filter(
          ([, v]) => v !== undefined && v !== null && v !== "",
        ),
      ).toString();
      if (query) url += `?${query}`;
    }
    const res = await request(url, { method: "GET" });
    return parseResponse(res);
  },

  post: async (endpoint, body) => {
    const res = await request(endpoint, {
      method: "POST",
      body: JSON.stringify(body),
    });
    return parseResponse(res);
  },

  put: async (endpoint, body) => {
    const res = await request(endpoint, {
      method: "PUT",
      body: body !== undefined ? JSON.stringify(body) : undefined,
    });
    return parseResponse(res);
  },

  delete: async (endpoint) => {
    const res = await request(endpoint, { method: "DELETE" });
    return parseResponse(res);
  },

  // For file downloads (PDF, CSV exports)
  download: async (endpoint, filename) => {
    const token = tokenStorage.getAccess();
    const res = await fetch(`${BASE_URL}${endpoint}`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) throw new Error(`Download failed: ${res.status}`);
    const blob = await res.blob();
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = filename || "export";
    a.click();
    URL.revokeObjectURL(url);
  },
};

export default api;
