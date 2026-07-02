//   GET  /api/organizations/{orgId}/alerts
//   GET  /api/organizations/{orgId}/alerts/dashboard
//   PUT  /api/organizations/{orgId}/alerts/{id}/acknowledge
//   PUT  /api/organizations/{orgId}/alerts/{id}/resolve
//   PUT  /api/organizations/{orgId}/alerts/{id}/snooze
//   PUT  /api/organizations/{orgId}/alerts/{id}/unsnooze
//   PUT  /api/organizations/{orgId}/alerts/{id}/assign

import { useState, useEffect, useCallback } from "react";
import { useAuth } from "../context/AuthContext";
import { api } from "../services/api";
import styles from "./AlertsPage.module.css";

// ── Severity helpers ───────────────────────────────────────────
const SEV_LABEL = {
  0: "Info",
  1: "Low",
  2: "Medium",
  3: "High",
  4: "Critical",
};
const SEV_CLASS = {
  info: styles.badgeInfo,
  low: styles.badgeLow,
  medium: styles.badgeMedium,
  high: styles.badgeHigh,
  critical: styles.badgeCritical,
};

function sevLabel(s) {
  if (typeof s === "number") return SEV_LABEL[s] ?? "Unknown";
  return String(s ?? "");
}
function sevClass(s) {
  return SEV_CLASS[sevLabel(s).toLowerCase()] ?? "";
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

// ── Time helpers ───────────────────────────────────────────────
function timeAgo(d) {
  if (!d) return "—";
  const diff = Math.floor((Date.now() - new Date(d)) / 1000);
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

// ── Filter tabs ────────────────────────────────────────────────
const QUICK_FILTERS = [
  { label: "All", status: null },
  { label: "Open", status: "New,Acknowledged,InProgress" },
  { label: "Critical", severity: "Critical" },
  { label: "Snoozed", status: "Snoozed" },
  { label: "Resolved", status: "Resolved,FalsePositive" },
];

// ── Static rules (no backend endpoint) ────────────────────────
const STATIC_RULES = [
  { id: 1, name: "Root Access Attempt", enabled: true },
  { id: 2, name: "Ransomware Pattern Detection", enabled: true },
  { id: 3, name: "Data Exfiltration > 100MB", enabled: true },
  { id: 4, name: "Known Malicious IP Access", enabled: true },
  { id: 5, name: "Port Scanning Activity", enabled: false },
  { id: 6, name: "Suspicious PowerShell Execution", enabled: false },
];

// ── Snooze options ─────────────────────────────────────────────
const SNOOZE_OPTS = [
  { label: "1 hour", minutes: 60 },
  { label: "4 hours", minutes: 240 },
  { label: "24 hours", minutes: 1440 },
  { label: "3 days", minutes: 4320 },
];

export default function AlertsPage() {
  const { orgId } = useAuth();

  const [alerts, setAlerts] = useState(null);
  const [summary, setSummary] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [actionBusy, setActionBusy] = useState({});
  const [rules, setRules] = useState(STATIC_RULES);
  const [showRules, setShowRules] = useState(false);
  const [activeFilter, setActiveFilter] = useState(0);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);

  // Snooze modal state
  const [snoozeAlert, setSnoozeAlert] = useState(null);
  const [snoozeMinutes, setSnoozeMinutes] = useState(60);
  const [snoozeLoading, setSnoozeLoading] = useState(false);

  const PAGE_SIZE = 10;

  // ── Load summary ───────────────────────────────────────────
  const loadSummary = useCallback(async () => {
    if (!orgId) return;
    try {
      const data = await api.get(
        `/api/organizations/${orgId}/alerts/dashboard`,
      );
      if (data) setSummary(data);
    } catch {
      /* non-critical */
    }
  }, [orgId]);

  // ── Load alerts ────────────────────────────────────────────
  const loadAlerts = useCallback(async () => {
    if (!orgId) return;
    setLoading(true);
    setError("");

    const filter = QUICK_FILTERS[activeFilter];
    const params = { pageNumber: page, pageSize: PAGE_SIZE };
    if (filter.status) params.status = filter.status;
    if (filter.severity) params.severity = filter.severity;

    try {
      const data = await api.get(`/api/organizations/${orgId}/alerts`, params);
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
  }, [orgId, page, activeFilter]);

  useEffect(() => {
    loadSummary();
  }, [loadSummary]);
  useEffect(() => {
    setPage(1);
  }, [activeFilter]);
  useEffect(() => {
    loadAlerts();
  }, [loadAlerts]);

  // ── Action helper ──────────────────────────────────────────
  const setBusy = (id, val) => setActionBusy((p) => ({ ...p, [id]: val }));

  const acknowledge = async (id) => {
    setBusy(id, "ack");
    try {
      await api.put(`/api/organizations/${orgId}/alerts/${id}/acknowledge`);
      await loadAlerts();
    } catch (err) {
      console.error("Ack failed:", err.message);
    } finally {
      setBusy(id, null);
    }
  };

  const resolve = async (id) => {
    // Phase 5 spec: PUT /api/organizations/{orgId}/alerts/{alertId}/resolve
    // Body: { resolution: string } — required field
    setBusy(id, "resolve");
    try {
      await api.put(`/api/organizations/${orgId}/alerts/${id}/resolve`, {
        resolution: "ThreatMitigated", // default resolution
        notes: "Resolved via dashboard",
      });
      await loadAlerts();
    } catch (err) {
      console.error("Resolve failed:", err.message);
    } finally {
      setBusy(id, null);
    }
  };

  const unsnooze = async (id) => {
    setBusy(id, "unsnooze");
    try {
      await api.put(`/api/organizations/${orgId}/alerts/${id}/unsnooze`);
      await loadAlerts();
    } catch (err) {
      console.error("Unsnooze failed:", err.message);
    } finally {
      setBusy(id, null);
    }
  };

  const submitSnooze = async () => {
    if (!snoozeAlert) return;
    setSnoozeLoading(true);
    try {
      await api.put(
        `/api/organizations/${orgId}/alerts/${snoozeAlert.id}/snooze`,
        {
          duration: snoozeMinutes,
        },
      );
      setSnoozeAlert(null);
      await loadAlerts();
    } catch (err) {
      console.error("Snooze failed:", err.message);
    } finally {
      setSnoozeLoading(false);
    }
  };

  const totalPages = Math.ceil(totalCount / PAGE_SIZE);

  // ── Summary stat pills ─────────────────────────────────────
  const summaryPills = [
    {
      label: "Critical",
      value: summary?.critical ?? summary?.criticalCount ?? "—",
      color: "#e74c3c",
    },
    {
      label: "High",
      value: summary?.high ?? summary?.highCount ?? "—",
      color: "#f39c12",
    },
    {
      label: "Medium",
      value: summary?.medium ?? summary?.mediumCount ?? "—",
      color: "#3498db",
    },
    {
      label: "Open",
      value: summary?.totalOpen ?? summary?.openAlerts ?? summary?.total ?? "—",
      color: "#9098b1",
    },
  ];

  return (
    <div>
      {/* Header */}
      <div className={styles.pageHeader}>
        <div>
          <h1>Alerts Center</h1>
          <p>Manage, triage and resolve high-fidelity security incidents.</p>
        </div>
        <button
          className={styles.configureBtn}
          onClick={() => setShowRules(true)}
        >
          Configure Rules
        </button>
      </div>

      {/* Summary pills */}
      {summary && (
        <div
          style={{
            display: "flex",
            gap: 12,
            marginBottom: 20,
            flexWrap: "wrap",
          }}
        >
          {summaryPills.map((p) => (
            <div
              key={p.label}
              style={{
                display: "flex",
                alignItems: "center",
                gap: 6,
                padding: "6px 14px",
                background: "#fff",
                border: "1.5px solid #e8eaf0",
                borderRadius: 20,
                fontSize: "0.82rem",
                fontWeight: 600,
                color: "#111326",
              }}
            >
              <span
                style={{
                  width: 8,
                  height: 8,
                  borderRadius: "50%",
                  background: p.color,
                  flexShrink: 0,
                }}
              />
              <span style={{ color: "#555770" }}>{p.label}:</span>
              <span>{p.value}</span>
            </div>
          ))}
        </div>
      )}

      {/* Quick filter tabs */}
      <div
        style={{ display: "flex", gap: 8, marginBottom: 20, flexWrap: "wrap" }}
      >
        {QUICK_FILTERS.map((f, i) => (
          <button
            key={f.label}
            onClick={() => setActiveFilter(i)}
            style={{
              padding: "7px 16px",
              borderRadius: 20,
              border: "1.5px solid",
              borderColor: activeFilter === i ? "#4044e4" : "#e8eaf0",
              background: activeFilter === i ? "#4044e4" : "#fff",
              color: activeFilter === i ? "#fff" : "#555770",
              fontSize: "0.82rem",
              fontWeight: 600,
              cursor: "pointer",
              transition: "all 0.15s",
            }}
          >
            {f.label}
          </button>
        ))}
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
                  <Sk w={36} h={36} r={10} />
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
            <div
              style={{
                padding: 40,
                textAlign: "center",
                color: "#9098b1",
                background: "#fff",
                borderRadius: 16,
                border: "1.5px solid #e8eaf0",
              }}
            >
              No alerts found for this filter.
            </div>
          ) : (
            alerts.map((alert) => {
              const id = alert.id;
              const title = alert.title ?? alert.name ?? "Alert";
              const target =
                alert.sourceName ?? alert.source?.honeypotId ?? "—";
              const sev = alert.severity;
              const status = statusLabel(alert.status);
              const busy = actionBusy[id];
              const isResolved =
                status === "Resolved" ||
                status === "FalsePositive" ||
                status === "Expired";
              const isSnoozed = status === "Snoozed";
              const isAcked =
                status === "Acknowledged" || status === "InProgress";

              return (
                <div key={id} className={styles.alertCard}>
                  <div className={styles.alertCardTop}>
                    <div className={styles.alertIconWrap}>⚠</div>
                    <div className={styles.alertCardInfo}>
                      <div className={styles.alertTitle}>{title}</div>
                      <div className={styles.alertTarget}>
                        {target !== "—" ? `Target: ${target} · ` : ""}
                        {status}
                      </div>
                    </div>
                    <span className={`${styles.alertBadge} ${sevClass(sev)}`}>
                      {sevLabel(sev)}
                    </span>
                  </div>

                  <div className={styles.alertCardFooter}>
                    <span className={styles.alertTime}>
                      {timeAgo(alert.createdAt ?? alert.triggeredAt)}
                    </span>
                    <div className={styles.alertActions}>
                      {isResolved ? (
                        <span
                          style={{
                            color: "#22c55e",
                            fontSize: 13,
                            fontWeight: 600,
                          }}
                        >
                          ✓ {status}
                        </span>
                      ) : isSnoozed ? (
                        <>
                          <span style={{ color: "#9098b1", fontSize: 13 }}>
                            😴 Snoozed
                          </span>
                          <button
                            className={styles.btnInvestigate}
                            onClick={() => unsnooze(id)}
                            disabled={!!busy}
                          >
                            {busy === "unsnooze" ? "…" : "Wake Up"}
                          </button>
                        </>
                      ) : (
                        <>
                          {!isAcked && (
                            <button
                              className={styles.btnDismiss}
                              onClick={() => acknowledge(id)}
                              disabled={!!busy}
                            >
                              {busy === "ack" ? "…" : "Acknowledge"}
                            </button>
                          )}
                          <button
                            className={styles.btnDismiss}
                            onClick={() => setSnoozeAlert(alert)}
                            disabled={!!busy}
                            style={{ color: "#9098b1" }}
                          >
                            Snooze
                          </button>
                          <button
                            className={styles.btnInvestigate}
                            onClick={() => resolve(id)}
                            disabled={!!busy}
                          >
                            {busy === "resolve" ? "…" : "Resolve"}
                          </button>
                        </>
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
          {rules.filter((r) => r.enabled).length === 0 && (
            <p style={{ color: "#9098b1", fontSize: "0.82rem" }}>
              No active rules.
            </p>
          )}
        </div>
      </div>

      {/* ── Snooze modal ── */}
      {snoozeAlert && (
        <div
          className={styles.modalOverlay}
          onClick={() => setSnoozeAlert(null)}
        >
          <div
            className={styles.rulesModalCard}
            onClick={(e) => e.stopPropagation()}
          >
            <div className={styles.rulesModalHeader}>
              <h3 className={styles.rulesModalTitle}>Snooze Alert</h3>
              <button
                className={styles.rulesModalClose}
                onClick={() => setSnoozeAlert(null)}
              >
                ✕
              </button>
            </div>
            <p
              style={{
                color: "#555770",
                fontSize: "0.88rem",
                marginBottom: 16,
              }}
            >
              Snooze "<strong>{snoozeAlert.title ?? "Alert"}</strong>" until:
            </p>
            <div
              style={{
                display: "flex",
                flexDirection: "column",
                gap: 8,
                marginBottom: 24,
              }}
            >
              {SNOOZE_OPTS.map((opt) => (
                <label
                  key={opt.minutes}
                  style={{
                    display: "flex",
                    alignItems: "center",
                    gap: 10,
                    cursor: "pointer",
                    fontSize: "0.88rem",
                    color: "#111326",
                  }}
                >
                  <input
                    type="radio"
                    name="snooze"
                    value={opt.minutes}
                    checked={snoozeMinutes === opt.minutes}
                    onChange={() => setSnoozeMinutes(opt.minutes)}
                  />
                  {opt.label}
                </label>
              ))}
            </div>
            <button
              className={styles.btnSaveConfig}
              onClick={submitSnooze}
              disabled={snoozeLoading}
            >
              {snoozeLoading ? "Snoozing…" : "Snooze Alert"}
            </button>
          </div>
        </div>
      )}

      {/* ── Configure rules modal ── */}
      {showRules && (
        <div
          className={styles.modalOverlay}
          onClick={() => setShowRules(false)}
        >
          <div
            className={styles.rulesModalCard}
            onClick={(e) => e.stopPropagation()}
          >
            <div className={styles.rulesModalHeader}>
              <h3 className={styles.rulesModalTitle}>Detection Rules</h3>
              <button
                className={styles.rulesModalClose}
                onClick={() => setShowRules(false)}
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
                      onChange={() =>
                        setRules((prev) =>
                          prev.map((r) =>
                            r.id === rule.id
                              ? { ...r, enabled: !r.enabled }
                              : r,
                          ),
                        )
                      }
                    />
                    <span className={styles.toggleTrack} />
                    <span className={styles.toggleThumb} />
                  </label>
                </div>
              ))}
            </div>
            <button
              className={styles.btnSaveConfig}
              onClick={() => setShowRules(false)}
            >
              Save Configuration
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
