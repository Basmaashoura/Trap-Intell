import { useState, useEffect, useCallback } from "react";
import { useAuth } from "../context/AuthContext";
import { api } from "../services/api";
import styles from "./ReportsPage.module.css";

// ── Helpers ────────────────────────────────────────────────────
function timeAgo(dateStr) {
  if (!dateStr) return "—";
  const diff = Math.floor((Date.now() - new Date(dateStr)) / 1000);
  if (diff < 60) return `${diff}s ago`;
  if (diff < 3600) return `${Math.floor(diff / 60)}m ago`;
  if (diff < 86400) return `${Math.floor(diff / 3600)}h ago`;
  return `${Math.floor(diff / 86400)}d ago`;
}

function SeverityBadge({ severity }) {
  const cfg = {
    Info: {
      bg: "rgba(52,152,219,0.08)",
      color: "#1a6fa8",
      border: "rgba(52,152,219,0.25)",
      dot: "#3498db",
    },
    Warning: {
      bg: "rgba(243,156,18,0.08)",
      color: "#b7770d",
      border: "rgba(243,156,18,0.25)",
      dot: "#f39c12",
    },
    Error: {
      bg: "rgba(231,76,60,0.08)",
      color: "#c0392b",
      border: "rgba(231,76,60,0.25)",
      dot: "#e74c3c",
    },
    Critical: {
      bg: "rgba(231,76,60,0.12)",
      color: "#922b21",
      border: "rgba(231,76,60,0.35)",
      dot: "#c0392b",
    },
  }[severity] ?? {
    bg: "#f4f5fa",
    color: "#aab0c6",
    border: "#e8eaf0",
    dot: "#aab0c6",
  };

  const ACTION_MAP = {
    0: "View",
    1: "Create",
    2: "Update",
    3: "Delete",
    View: "View",
    Create: "Create",
    Update: "Update",
    Delete: "Delete",
  };

  const SEVERITY_MAP = {
    0: "Info",
    1: "Warning",
    2: "Error",
    3: "Critical",
    Info: "Info",
    Warning: "Warning",
    Error: "Error",
    Critical: "Critical",
  };

  function resolveAction(val) {
    return ACTION_MAP[val] ?? String(val);
  }
  function resolveSeverity(val) {
    return SEVERITY_MAP[val] ?? String(val);
  }

  return (
    <span
      style={{
        display: "inline-flex",
        alignItems: "center",
        gap: 5,
        padding: "3px 10px",
        borderRadius: 50,
        fontSize: "0.68rem",
        fontWeight: 800,
        letterSpacing: "0.5px",
        textTransform: "uppercase",
        background: cfg.bg,
        color: cfg.color,
        border: `1px solid ${cfg.border}`,
      }}
    >
      <span
        style={{
          width: 5,
          height: 5,
          borderRadius: "50%",
          background: cfg.dot,
          flexShrink: 0,
        }}
      />
      {severity}
    </span>
  );
}

function ActionBadge({ action }) {
  const cfg = {
    Create: {
      color: "#1a8a40",
      bg: "rgba(42,206,95,0.08)",
      border: "rgba(42,206,95,0.25)",
    },
    Update: {
      color: "#1a6fa8",
      bg: "rgba(52,152,219,0.08)",
      border: "rgba(52,152,219,0.25)",
    },
    Delete: {
      color: "#c0392b",
      bg: "rgba(231,76,60,0.08)",
      border: "rgba(231,76,60,0.25)",
    },
    View: {
      color: "#6b7280",
      bg: "rgba(107,114,128,0.08)",
      border: "rgba(107,114,128,0.2)",
    },
  }[action] ?? { color: "#6b7280", bg: "#f4f5fa", border: "#e8eaf0" };

  return (
    <span
      style={{
        display: "inline-flex",
        alignItems: "center",
        padding: "3px 10px",
        borderRadius: 6,
        fontSize: "0.72rem",
        fontWeight: 700,
        background: cfg.bg,
        color: cfg.color,
        border: `1px solid ${cfg.border}`,
      }}
    >
      {action}
    </span>
  );
}

function Sk({ w = "100%", h = 14, r = 4 }) {
  return (
    <div
      style={{
        width: w,
        height: h,
        borderRadius: r,
        flexShrink: 0,
        background:
          "linear-gradient(90deg,#f0f0f0 25%,#e4e4e4 50%,#f0f0f0 75%)",
        backgroundSize: "200% 100%",
        animation: "shimmer 1.4s infinite",
      }}
    />
  );
}

const PAGE_SIZE = 20;

export default function ReportsPage() {
  const { orgId } = useAuth();

  // ── Data state ─────────────────────────────────────────────
  const [logs, setLogs] = useState(null);
  const [summary, setSummary] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);

  // ── Filters ────────────────────────────────────────────────
  const [search, setSearch] = useState("");
  const [severityFilter, setSeverityFilter] = useState("all");
  const [actionFilter, setActionFilter] = useState("all");

  // ── Detail panel ───────────────────────────────────────────
  const [selected, setSelected] = useState(null);

  // ── Fetch logs ─────────────────────────────────────────────
  const loadLogs = useCallback(async () => {
    if (!orgId) return;
    setLoading(true);
    setError(null);
    try {
      const [logsData, summaryData] = await Promise.allSettled([
        api.get(`/api/organizations/${orgId}/auditlogs`, {
          pageNumber: page,
          pageSize: PAGE_SIZE,
        }),
        api.get(`/api/organizations/${orgId}/auditlogs/summary`),
      ]);

      if (logsData.status === "fulfilled" && logsData.value) {
        const d = logsData.value;
        const items = Array.isArray(d) ? d : (d.items ?? d.data ?? []);
        setLogs(items);
        setTotalCount(d.totalCount ?? d.total ?? items.length);
      } else {
        setLogs([]);
      }

      if (summaryData.status === "fulfilled" && summaryData.value) {
        setSummary(summaryData.value);
      }
    } catch (err) {
      setError(err.message ?? "Failed to load audit logs");
      setLogs([]);
    } finally {
      setLoading(false);
    }
  }, [orgId, page]);

  useEffect(() => {
    loadLogs();
  }, [loadLogs]);

  // ── Acknowledge ────────────────────────────────────────────
  const acknowledge = async (log) => {
    try {
      await api.post(
        `/api/organizations/${orgId}/auditlogs/${log.id}/acknowledge`,
      );
      setLogs((prev) =>
        prev.map((l) => (l.id === log.id ? { ...l, isAcknowledged: true } : l)),
      );
      if (selected?.id === log.id)
        setSelected((prev) => ({ ...prev, isAcknowledged: true }));
    } catch (err) {
      console.error("Acknowledge failed:", err.message);
    }
  };

  // ── Export ─────────────────────────────────────────────────
  const exportLogs = async () => {
    try {
      await api.download(
        `/api/organizations/${orgId}/auditlogs/export`,
        "audit-logs.csv",
      );
    } catch (err) {
      console.error("Export failed:", err.message);
    }
  };

  // ── Client-side filter (on top of server pagination) ──────
  const filtered = (logs ?? []).filter((log) => {
    const matchSeverity =
      severityFilter === "all" || log.severity === severityFilter;
    const matchAction = actionFilter === "all" || log.action === actionFilter;
    const matchSearch =
      !search ||
      log.reason?.toLowerCase().includes(search.toLowerCase()) ||
      log.resourceType?.toLowerCase().includes(search.toLowerCase()) ||
      log.action?.toLowerCase().includes(search.toLowerCase()) ||
      log.ipAddress?.toLowerCase().includes(search.toLowerCase());
    return matchSeverity && matchAction && matchSearch;
  });

  const totalPages = Math.ceil(totalCount / PAGE_SIZE);

  return (
    <div>
      {/* Header */}
      <div
        style={{
          display: "flex",
          alignItems: "flex-start",
          justifyContent: "space-between",
          marginBottom: 24,
        }}
      >
        <div>
          <h1 style={{ margin: 0 }}>Audit Logs</h1>
          <p style={{ margin: "6px 0 0", color: "#6b7280" }}>
            Track all actions and changes within your organization.
          </p>
        </div>
        <div style={{ display: "flex", gap: 10 }}>
          <button
            onClick={exportLogs}
            style={{
              padding: "10px 20px",
              background: "#fff",
              border: "1.5px solid #e8eaf0",
              borderRadius: 50,
              fontSize: "0.87rem",
              fontWeight: 700,
              cursor: "pointer",
              color: "#4044e4",
            }}
          >
            ↓ Export CSV
          </button>
          <button
            onClick={loadLogs}
            style={{
              padding: "10px 20px",
              background: "#4044e4",
              color: "#fff",
              border: "none",
              borderRadius: 50,
              fontSize: "0.87rem",
              fontWeight: 700,
              cursor: "pointer",
            }}
          >
            Refresh
          </button>
        </div>
      </div>

      {/* Summary KPI cards */}
      <div
        style={{
          display: "grid",
          gridTemplateColumns: "repeat(4,1fr)",
          gap: 16,
          marginBottom: 24,
        }}
      >
        {[
          {
            label: "Total Logs",
            value: summary?.totalCount ?? totalCount ?? "—",
            color: "#4044e4",
          },
          {
            label: "Critical",
            value: summary?.criticalCount ?? "—",
            color: "#e74c3c",
          },
          {
            label: "Warnings",
            value: summary?.warningCount ?? "—",
            color: "#f39c12",
          },
          {
            label: "Unacknowledged",
            value: summary?.unacknowledgedCount ?? "—",
            color: "#8a1bfa",
          },
        ].map((s) => (
          <div
            key={s.label}
            style={{
              background: "#fff",
              borderRadius: 14,
              border: "1.5px solid #e8eaf0",
              padding: "18px 20px",
            }}
          >
            <div
              style={{
                fontSize: "0.7rem",
                fontWeight: 700,
                color: "#aab0c6",
                letterSpacing: "0.6px",
                textTransform: "uppercase",
                marginBottom: 8,
              }}
            >
              {s.label}
            </div>
            <div
              style={{
                fontSize: "1.6rem",
                fontWeight: 800,
                color: s.color,
                letterSpacing: "-0.5px",
              }}
            >
              {s.value}
            </div>
          </div>
        ))}
      </div>

      {/* Error */}
      {error && (
        <div
          style={{
            background: "#fff0f0",
            border: "1px solid #fcc",
            borderRadius: 8,
            padding: "12px 16px",
            marginBottom: 16,
            color: "#c00",
            fontSize: 14,
          }}
        >
          {error} —{" "}
          <button onClick={loadLogs} style={{ color: "#c00", fontWeight: 600 }}>
            Retry
          </button>
        </div>
      )}

      {/* Filters */}
      <div
        style={{ display: "flex", gap: 12, marginBottom: 20, flexWrap: "wrap" }}
      >
        <input
          style={{
            flex: 1,
            minWidth: 220,
            background: "#fff",
            border: "1.5px solid #e8eaf0",
            borderRadius: 10,
            padding: "11px 14px",
            fontSize: "0.87rem",
            outline: "none",
          }}
          placeholder="Search by action, resource, reason, IP..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        <select
          value={severityFilter}
          onChange={(e) => setSeverityFilter(e.target.value)}
          style={{
            padding: "11px 14px",
            border: "1.5px solid #e8eaf0",
            borderRadius: 10,
            fontSize: "0.85rem",
            fontWeight: 600,
            outline: "none",
            cursor: "pointer",
          }}
        >
          <option value="all">All Severities</option>
          <option value="Info">Info</option>
          <option value="Warning">Warning</option>
          <option value="Error">Error</option>
          <option value="Critical">Critical</option>
        </select>
        <select
          value={actionFilter}
          onChange={(e) => setActionFilter(e.target.value)}
          style={{
            padding: "11px 14px",
            border: "1.5px solid #e8eaf0",
            borderRadius: 10,
            fontSize: "0.85rem",
            fontWeight: 600,
            outline: "none",
            cursor: "pointer",
          }}
        >
          <option value="all">All Actions</option>
          <option value="Create">Create</option>
          <option value="Update">Update</option>
          <option value="Delete">Delete</option>
          <option value="View">View</option>
        </select>
      </div>

      {/* Table */}
      <div
        style={{
          background: "#fff",
          borderRadius: 16,
          border: "1.5px solid #e8eaf0",
          overflow: "hidden",
        }}
      >
        <table style={{ width: "100%", borderCollapse: "collapse" }}>
          <thead>
            <tr
              style={{
                background: "#f8f9fc",
                borderBottom: "1.5px solid #e8eaf0",
              }}
            >
              {[
                "Action",
                "Resource",
                "Reason",
                "Severity",
                "IP Address",
                "Time",
                "",
              ].map((h) => (
                <th
                  key={h}
                  style={{
                    padding: "12px 16px",
                    textAlign: "left",
                    fontSize: "0.7rem",
                    fontWeight: 700,
                    color: "#aab0c6",
                    letterSpacing: "0.6px",
                    textTransform: "uppercase",
                    whiteSpace: "nowrap",
                  }}
                >
                  {h}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {loading || logs === null ? (
              Array.from({ length: 5 }).map((_, i) => (
                <tr key={i} style={{ borderBottom: "1px solid #f4f5fa" }}>
                  {Array.from({ length: 7 }).map((_, j) => (
                    <td key={j} style={{ padding: "14px 16px" }}>
                      <Sk w={j === 2 ? "80%" : j === 6 ? 80 : "60%"} />
                    </td>
                  ))}
                </tr>
              ))
            ) : filtered.length === 0 ? (
              <tr>
                <td
                  colSpan={7}
                  style={{
                    padding: 48,
                    textAlign: "center",
                    color: "#aab0c6",
                    fontSize: "0.88rem",
                    fontWeight: 600,
                  }}
                >
                  No audit logs found.
                </td>
              </tr>
            ) : (
              filtered.map((log, i) => (
                <tr
                  key={log.id}
                  onClick={() => setSelected(log)}
                  style={{
                    borderBottom:
                      i < filtered.length - 1 ? "1px solid #f4f5fa" : "none",
                    cursor: "pointer",
                    transition: "background 0.15s",
                  }}
                  onMouseEnter={(e) =>
                    (e.currentTarget.style.background = "#f8f9fc")
                  }
                  onMouseLeave={(e) => (e.currentTarget.style.background = "")}
                >
                  <td style={{ padding: "14px 16px" }}>
                    <ActionBadge action={resolveAction(log.action)} />
                  </td>
                  <td style={{ padding: "14px 16px" }}>
                    <div
                      style={{
                        fontWeight: 700,
                        fontSize: "0.85rem",
                        color: "#1a1f36",
                      }}
                    >
                      {log.resourceType ?? "—"}
                    </div>
                  </td>
                  <td style={{ padding: "14px 16px", maxWidth: 280 }}>
                    <div
                      style={{
                        fontSize: "0.83rem",
                        color: "#4a5568",
                        overflow: "hidden",
                        textOverflow: "ellipsis",
                        whiteSpace: "nowrap",
                      }}
                    >
                      {log.reason ?? "—"}
                    </div>
                  </td>
                  <td style={{ padding: "14px 16px" }}>
                    <SeverityBadge severity={resolveSeverity(log.severity)} />
                  </td>
                  <td
                    style={{
                      padding: "14px 16px",
                      fontSize: "0.82rem",
                      color: "#6b7280",
                      fontFamily: "monospace",
                    }}
                  >
                    {log.ipAddress ?? "—"}
                  </td>
                  <td
                    style={{
                      padding: "14px 16px",
                      fontSize: "0.8rem",
                      color: "#aab0c6",
                      whiteSpace: "nowrap",
                    }}
                  >
                    {timeAgo(log.timestamp)}
                  </td>
                  <td style={{ padding: "14px 16px" }}>
                    {!log.isAcknowledged ? (
                      <button
                        onClick={(e) => {
                          e.stopPropagation();
                          acknowledge(log);
                        }}
                        style={{
                          padding: "5px 12px",
                          background: "none",
                          border: "1.5px solid #e8eaf0",
                          borderRadius: 6,
                          fontSize: "0.75rem",
                          fontWeight: 700,
                          color: "#4044e4",
                          cursor: "pointer",
                        }}
                      >
                        Acknowledge
                      </button>
                    ) : (
                      <span
                        style={{
                          fontSize: "0.75rem",
                          color: "#22c55e",
                          fontWeight: 700,
                        }}
                      >
                        ✓ Acked
                      </span>
                    )}
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>

        {/* Footer */}
        <div
          style={{
            display: "flex",
            alignItems: "center",
            justifyContent: "space-between",
            padding: "12px 16px",
            borderTop: "1px solid #f0f1f7",
            background: "#fafbff",
            flexWrap: "wrap",
            gap: 10,
          }}
        >
          <span
            style={{ fontSize: "0.78rem", color: "#aab0c6", fontWeight: 500 }}
          >
            {loading
              ? "Loading..."
              : `${totalCount.toLocaleString()} total logs · Page ${page} of ${totalPages || 1}`}
          </span>
          <div style={{ display: "flex", gap: 6 }}>
            <button
              onClick={() => setPage((p) => p - 1)}
              disabled={page === 1}
              style={{
                padding: "6px 14px",
                border: "1px solid #e8eaf0",
                borderRadius: 7,
                background: "#fff",
                cursor: "pointer",
                fontSize: "0.82rem",
                fontWeight: 600,
                color: "#6b7280",
                opacity: page === 1 ? 0.4 : 1,
              }}
            >
              ‹ Prev
            </button>
            <button
              onClick={() => setPage((p) => p + 1)}
              disabled={page >= totalPages || totalPages === 0}
              style={{
                padding: "6px 14px",
                border: "1px solid #e8eaf0",
                borderRadius: 7,
                background: "#fff",
                cursor: "pointer",
                fontSize: "0.82rem",
                fontWeight: 600,
                color: "#6b7280",
                opacity: page >= totalPages ? 0.4 : 1,
              }}
            >
              Next ›
            </button>
          </div>
        </div>
      </div>

      {/* Detail side panel */}
      {selected && (
        <div
          onClick={() => setSelected(null)}
          style={{
            position: "fixed",
            inset: 0,
            background: "rgba(0,0,0,0.35)",
            zIndex: 200,
            display: "flex",
            alignItems: "center",
            justifyContent: "flex-end",
          }}
        >
          <div
            onClick={(e) => e.stopPropagation()}
            style={{
              width: 420,
              height: "100%",
              background: "#fff",
              padding: 28,
              overflowY: "auto",
              boxSizing: "border-box",
            }}
          >
            {/* Header */}
            <div
              style={{
                display: "flex",
                alignItems: "center",
                justifyContent: "space-between",
                marginBottom: 24,
              }}
            >
              <div>
                <ActionBadge action={selected.action} />
                <div
                  style={{
                    marginTop: 8,
                    fontWeight: 800,
                    fontSize: "1rem",
                    color: "#1a1f36",
                  }}
                >
                  {selected.resourceType}
                </div>
              </div>
              <button
                onClick={() => setSelected(null)}
                style={{
                  background: "none",
                  border: "none",
                  cursor: "pointer",
                  fontSize: 20,
                  color: "#aab0c6",
                }}
              >
                ✕
              </button>
            </div>

            {/* Fields */}
            {[
              ["Severity", <SeverityBadge severity={selected.severity} />],
              ["Reason", selected.reason ?? "—"],
              ["IP Address", selected.ipAddress ?? "—"],
              ["Timestamp", new Date(selected.timestamp).toLocaleString()],
              ["Resource ID", selected.resourceId ?? "—"],
              ["Acknowledged", selected.isAcknowledged ? "✓ Yes" : "No"],
              [
                "Compliance",
                (selected.complianceStandards ?? []).join(", ") || "—",
              ],
            ].map(([label, val]) => (
              <div
                key={label}
                style={{
                  display: "flex",
                  justifyContent: "space-between",
                  alignItems: "center",
                  padding: "12px 0",
                  borderBottom: "1px solid #f0f1f7",
                }}
              >
                <span style={{ fontSize: "0.82rem", color: "#6b7280" }}>
                  {label}
                </span>
                <span
                  style={{
                    fontSize: "0.85rem",
                    fontWeight: 600,
                    color: "#1a1f36",
                    textAlign: "right",
                    maxWidth: "60%",
                  }}
                >
                  {val}
                </span>
              </div>
            ))}

            {/* Changes */}
            {selected.changes && selected.changes.length > 0 && (
              <div style={{ marginTop: 16 }}>
                <div
                  style={{
                    fontSize: "0.7rem",
                    fontWeight: 700,
                    color: "#aab0c6",
                    letterSpacing: "0.6px",
                    textTransform: "uppercase",
                    marginBottom: 10,
                  }}
                >
                  Changes
                </div>
                <pre
                  style={{
                    background: "#f8f9fc",
                    borderRadius: 8,
                    padding: 12,
                    fontSize: "0.75rem",
                    overflow: "auto",
                    color: "#1a1f36",
                  }}
                >
                  {JSON.stringify(selected.changes, null, 2)}
                </pre>
              </div>
            )}

            {/* Acknowledge button */}
            {!selected.isAcknowledged && (
              <button
                onClick={() => acknowledge(selected)}
                style={{
                  marginTop: 20,
                  width: "100%",
                  padding: "12px",
                  background: "#4044e4",
                  color: "#fff",
                  border: "none",
                  borderRadius: 50,
                  fontSize: "0.88rem",
                  fontWeight: 700,
                  cursor: "pointer",
                }}
              >
                Acknowledge Log
              </button>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
