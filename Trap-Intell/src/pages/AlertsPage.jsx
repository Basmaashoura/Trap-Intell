import { useState, useEffect, useCallback } from "react";
import { useAuth } from "../context/AuthContext";
import { api } from "../services/api";
import styles from "./AlertsPage.module.css";

// ── Severity helpers ───────────────────────────────────────────
const SEVERITY_LABEL = ["Info", "Low", "Medium", "High", "Critical"];
const SEVERITY_CLASS = {
  low: styles.badgeLow,
  medium: styles.badgeMedium,
  high: styles.badgeHigh,
  critical: styles.badgeCritical,
  info: styles.badgeInfo,
};

function severityLabel(s) {
  if (typeof s === "number") return SEVERITY_LABEL[s] ?? "Unknown";
  return String(s ?? "");
}

function severityClass(s) {
  const label = severityLabel(s).toLowerCase();
  return SEVERITY_CLASS[label] ?? "";
}

// ── Status helpers ─────────────────────────────────────────────
const STATUS_LABEL = [
  "New",
  "Acknowledged",
  "InProgress",
  "Escalated",
  "Snoozed",
  "Resolved",
  "FalsePositive",
  "Expired",
];

function statusLabel(s) {
  if (typeof s === "number") return STATUS_LABEL[s] ?? "Unknown";
  return String(s ?? "");
}

// ── Time helper ────────────────────────────────────────────────
function timeAgo(dateStr) {
  if (!dateStr) return "—";
  const diff = Math.floor((Date.now() - new Date(dateStr)) / 1000);
  if (diff < 60) return `${diff}s ago`;
  if (diff < 3600) return `${Math.floor(diff / 60)}m ago`;
  if (diff < 86400) return `${Math.floor(diff / 3600)}h ago`;
  return `${Math.floor(diff / 86400)}d ago`;
}

// ── Skeleton ───────────────────────────────────────────────────
function Sk({ w = "100%", h = 20, r = 6 }) {
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

// ── Static rules (no backend endpoint for rules yet) ──────────
const INITIAL_RULES = [
  { id: 1, name: "Root Access Attempt", enabled: true },
  { id: 2, name: "Ransomware Pattern Detection", enabled: true },
  { id: 3, name: "Data Exfiltration > 100MB", enabled: true },
  { id: 4, name: "Known Malicious IP Access", enabled: true },
  { id: 5, name: "Port Scanning Activity", enabled: false },
  { id: 6, name: "Suspicious PowerShell Execution", enabled: false },
];

// ── Main Component ─────────────────────────────────────────────
export default function AlertsPage() {
  const { orgId } = useAuth();
  const [alerts, setAlerts] = useState(null); // null = loading
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [actionLoading, setActionLoading] = useState({}); // { [alertId]: true }
  const [rules, setRules] = useState(INITIAL_RULES);
  const [showModal, setShowModal] = useState(false);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const PAGE_SIZE = 10;

  // ── Fetch alerts ─────────────────────────────────────────────
  const loadAlerts = useCallback(async () => {
    if (!orgId) return;
    setLoading(true);
    setError(null);
    try {
      const data = await api.get(`/api/organizations/${orgId}/alerts`, {
        pageNumber: page,
        pageSize: PAGE_SIZE,
      });
      if (data) {
        const items = Array.isArray(data)
          ? data
          : (data.items ?? data.data ?? []);
        setAlerts(items);
        setTotalCount(data.totalCount ?? data.total ?? items.length);
      } else {
        setAlerts([]);
      }
    } catch (err) {
      setError(err.message ?? "Failed to load alerts");
      setAlerts([]);
    } finally {
      setLoading(false);
    }
  }, [orgId, page]);

  useEffect(() => {
    loadAlerts();
  }, [loadAlerts]);

  // ── Actions ───────────────────────────────────────────────────
  const setAlertLoading = (id, val) =>
    setActionLoading((prev) => ({ ...prev, [id]: val }));

  const acknowledge = async (alertId) => {
    setAlertLoading(alertId, true);
    try {
      await api.put(
        `/api/organizations/${orgId}/alerts/${alertId}/acknowledge`,
      );
      await loadAlerts();
    } catch (err) {
      console.error("Acknowledge failed:", err.message);
    } finally {
      setAlertLoading(alertId, false);
    }
  };

  const resolve = async (alertId) => {
    setAlertLoading(alertId, true);
    try {
      await api.put(`/api/organizations/${orgId}/alerts/${alertId}/resolve`);
      await loadAlerts();
    } catch (err) {
      console.error("Resolve failed:", err.message);
    } finally {
      setAlertLoading(alertId, false);
    }
  };

  const toggleRule = (id) =>
    setRules((prev) =>
      prev.map((r) => (r.id === id ? { ...r, enabled: !r.enabled } : r)),
    );

  const totalPages = Math.ceil(totalCount / PAGE_SIZE);

  return (
    <div>
      {/* Header */}
      <div className={styles.pageHeader}>
        <div>
          <h1>Alerts Center</h1>
          <p>Manage, triage and resolve high-fidelity incidents.</p>
        </div>
        <button
          className={styles.configureBtn}
          onClick={() => setShowModal(true)}
        >
          Configure Rules
        </button>
      </div>

      {/* Error banner */}
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
          <button
            onClick={loadAlerts}
            style={{ color: "#c00", fontWeight: 600 }}
          >
            Retry
          </button>
        </div>
      )}

      {/* Main grid */}
      <div className={styles.alertsMainRow}>
        {/* Alert cards */}
        <div className={styles.alertsList}>
          {loading || alerts === null ? (
            Array.from({ length: 3 }).map((_, i) => (
              <div key={i} className={styles.alertCard}>
                <div className={styles.alertCardTop}>
                  <Sk w={32} h={32} r={8} />
                  <div
                    style={{
                      flex: 1,
                      display: "flex",
                      flexDirection: "column",
                      gap: 6,
                    }}
                  >
                    <Sk w="60%" h={14} />
                    <Sk w="40%" h={12} />
                  </div>
                  <Sk w={60} h={22} r={4} />
                </div>
                <div className={styles.alertCardFooter}>
                  <Sk w={60} h={12} />
                  <Sk w={140} h={30} r={6} />
                </div>
              </div>
            ))
          ) : alerts.length === 0 ? (
            <div style={{ padding: 40, textAlign: "center", color: "#888" }}>
              No alerts found.
            </div>
          ) : (
            alerts.map((alert) => {
              const id = alert.id;
              const title = alert.title ?? alert.name ?? "Alert";
              const target =
                alert.sourceName ?? alert.source?.honeypotId ?? "—";

              const sev = alert.severity;
              const status = statusLabel(alert.status);
              const isLoading = !!actionLoading[id];
              const isResolved =
                status === "Resolved" || status === "FalsePositive";

              return (
                <div key={id} className={styles.alertCard}>
                  <div className={styles.alertCardTop}>
                    <div className={styles.alertIconWrap}>⚠</div>
                    <div className={styles.alertCardInfo}>
                      <div className={styles.alertTitle}>{title}</div>
                      <div className={styles.alertTarget}>
                        Target: {target} · {status}
                      </div>
                    </div>
                    <span
                      className={`${styles.alertBadge} ${severityClass(sev)}`}
                    >
                      {severityLabel(sev)}
                    </span>
                  </div>

                  <div className={styles.alertCardFooter}>
                    <span className={styles.alertTime}>
                      {timeAgo(alert.createdAt ?? alert.triggeredAt)}
                    </span>
                    <div className={styles.alertActions}>
                      {!isResolved && (
                        <>
                          <button
                            className={styles.btnDismiss}
                            onClick={() => acknowledge(id)}
                            disabled={isLoading || status === "Acknowledged"}
                          >
                            {status === "Acknowledged"
                              ? "Acknowledged"
                              : "Acknowledge"}
                          </button>
                          <button
                            className={styles.btnInvestigate}
                            onClick={() => resolve(id)}
                            disabled={isLoading}
                          >
                            {isLoading ? "..." : "Resolve"}
                          </button>
                        </>
                      )}
                      {isResolved && (
                        <span
                          style={{
                            color: "#22c55e",
                            fontSize: 13,
                            fontWeight: 600,
                          }}
                        >
                          ✓ {status}
                        </span>
                      )}
                    </div>
                  </div>
                </div>
              );
            })
          )}

          {/* Pagination */}
          {!loading && totalPages > 1 && (
            <div
              style={{
                display: "flex",
                gap: 8,
                justifyContent: "center",
                marginTop: 16,
              }}
            >
              <button
                onClick={() => setPage((p) => p - 1)}
                disabled={page === 1}
                className={styles.btnDismiss}
              >
                ‹ Prev
              </button>
              <span style={{ lineHeight: "32px", fontSize: 13, color: "#666" }}>
                Page {page} of {totalPages}
              </span>
              <button
                onClick={() => setPage((p) => p + 1)}
                disabled={page === totalPages}
                className={styles.btnDismiss}
              >
                Next ›
              </button>
            </div>
          )}
        </div>

        {/* Active rules panel */}
        <div className={styles.activeRulesPanel}>
          <h3 className={styles.rulesPanelTitle}>Active Rules</h3>
          {rules
            .filter((r) => r.enabled)
            .map((rule) => (
              <div key={rule.id} className={styles.activeRuleItem}>
                <span className={styles.ruleDot} />
                <span className={styles.ruleName}>{rule.name}</span>
              </div>
            ))}
        </div>
      </div>

      {/* Configure rules modal */}
      {showModal && (
        <div
          className={styles.modalOverlay}
          onClick={() => setShowModal(false)}
        >
          <div
            className={styles.rulesModalCard}
            onClick={(e) => e.stopPropagation()}
          >
            <div className={styles.rulesModalHeader}>
              <h3 className={styles.rulesModalTitle}>
                Detection Rules Configuration
              </h3>
              <button
                className={styles.rulesModalClose}
                onClick={() => setShowModal(false)}
              >
                ✕
              </button>
            </div>
            <div className={styles.rulesModalList}>
              {rules.map((rule) => (
                <div key={rule.id} className={styles.ruleRow}>
                  <div
                    className={`${styles.ruleIndicator} ${rule.enabled ? styles.on : ""}`}
                  >
                    <span className={styles.ruleIndicatorDot} />
                  </div>
                  <span className={styles.ruleRowName}>{rule.name}</span>
                  <label className={styles.toggleSwitch}>
                    <input
                      type="checkbox"
                      checked={rule.enabled}
                      onChange={() => toggleRule(rule.id)}
                    />
                    <span className={styles.toggleTrack} />
                    <span className={styles.toggleThumb} />
                  </label>
                </div>
              ))}
            </div>
            <button
              className={styles.btnSaveConfig}
              onClick={() => setShowModal(false)}
            >
              Save Configuration
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
