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

// ── Auth endpoints that skip token refresh on 401 ──────────────
// 401 on these = bad credentials, not expired token
const NO_REFRESH_ENDPOINTS = [
  "/api/auth/login",
  "/api/auth/register",
  "/api/auth/refresh",
  "/api/auth/forgot-password",
  "/api/auth/reset-password",
];

function isNoRefreshEndpoint(endpoint) {
  return NO_REFRESH_ENDPOINTS.some((e) => endpoint.includes(e));
}

// ── Refresh token logic ────────────────────────────────────────
// Runs once even if multiple parallel calls fail simultaneously
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

// ── Core request ───────────────────────────────────────────────
async function request(endpoint, options = {}, retry = true) {
  const token = tokenStorage.getAccess();

  const headers = {
    "Content-Type": "application/json",
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
    ...options.headers,
  };

  let response;
  try {
    response = await fetch(`${BASE_URL}${endpoint}`, { ...options, headers });
  } catch (err) {
    // Network failure (no internet, server down, CORS preflight blocked)
    const error = new Error("Network error — server unreachable");
    error.status = 0;
    error.isNetworkError = true;
    throw error;
  }

  // Token expired → refresh once and retry
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
  const contentType = response.headers.get("content-type") ?? "";
  const isJson =
    contentType.includes("application/json") ||
    contentType.includes("application/problem+json");

  if (!response.ok) {
    let errorMessage = `HTTP ${response.status}`;
    if (isJson) {
      try {
        const errorData = await response.json();
        // RFC 7807 Problem Details: detail → title → message → fallback
        errorMessage =
          errorData.detail ??
          errorData.title ??
          errorData.message ??
          errorData.error ??
          errorMessage;
      } catch {
        // Body wasn't valid JSON despite content-type header
      }
    }
    const error = new Error(errorMessage);
    error.status = response.status;
    throw error;
  }

  // 204 No Content or non-JSON response
  if (response.status === 204 || !isJson) return null;

  try {
    return await response.json();
  } catch {
    return null;
  }
}

// ── Public API ─────────────────────────────────────────────────
export const api = {
  get: async (endpoint, params) => {
    let url = endpoint;
    if (params && Object.keys(params).length > 0) {
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
      body: body !== undefined ? JSON.stringify(body) : undefined,
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

  patch: async (endpoint, body) => {
    const res = await request(endpoint, {
      method: "PATCH",
      body: body !== undefined ? JSON.stringify(body) : undefined,
    });
    return parseResponse(res);
  },

  delete: async (endpoint) => {
    const res = await request(endpoint, { method: "DELETE" });
    return parseResponse(res);
  },

  // File downloads (PDF, CSV exports)
  download: async (endpoint, filename) => {
    const token = tokenStorage.getAccess();
    let res;
    try {
      res = await fetch(`${BASE_URL}${endpoint}`, {
        headers: { Authorization: `Bearer ${token}` },
      });
    } catch {
      throw new Error("Download failed — network error");
    }
    if (!res.ok) throw new Error(`Download failed: HTTP ${res.status}`);
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
