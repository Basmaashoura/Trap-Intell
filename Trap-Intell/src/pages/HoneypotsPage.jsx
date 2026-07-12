import { useState, useCallback, useEffect } from "react";
import { useAuth } from "../context/AuthContext";
import { api } from "../services/api";
import styles from "./HoneypotsPage.module.css";

// ── Real data from DB (replace when GET endpoint exists) ───────
const STATIC_HONEYPOTS = [
  {
    id: "dddd1111-1111-1111-1111-111111111111",
    name: "SSH-Honeypot-DMZ-01",
    type: "SSH",
    ip: "—",
    logs: "1,250",
    criticalEvents: 45,
    status: "active",
    health: "Healthy",
  },
  {
    id: "dddd1111-1111-1111-1111-222222222222",
    name: "HTTP-Honeypot-Web-01",
    type: "HTTP",
    ip: "—",
    logs: "3,500",
    criticalEvents: 89,
    status: "active",
    health: "Healthy",
  },
  {
    id: "dddd1111-1111-1111-1111-333333333333",
    name: "SMB-Honeypot-Internal-01",
    type: "Samba",
    ip: "—",
    logs: "450",
    criticalEvents: 25,
    status: "active",
    health: "Healthy",
  },
];

// Type enum mapping
const TYPE_MAP = {
  SSH: 0,
  HTTP: 1,
  FTP: 2,
  RDP: 3,
  MySQL: 4,
  Samba: 5,
};

// Location enum mapping
const LOCATION_MAP = {
  DMZ: 0,
  Internal: 1,
  Cloud: 2,
  Public: 3,
};

export default function HoneypotsPage() {
  const { orgId } = useAuth();
  const [honeypots, setHoneypots] = useState(STATIC_HONEYPOTS);
  const [search, setSearch] = useState("");
  const [showDeploy, setShowDeploy] = useState(false);
  const [showLogs, setShowLogs] = useState(false);
  const [selectedHoneypot, setSelectedHoneypot] = useState(null);
  const [actionLoading, setActionLoading] = useState({});
  const [form, setForm] = useState({ name: "", type: "SSH", location: "DMZ" });
  const [sub, setSub] = useState(null);

  useEffect(() => {
    if (!orgId) return;
    api
      .get(`/api/organizations/${orgId}/subscriptions/current`)
      .then((data) => setSub(data))
      .catch(() => {});
  }, [orgId]);

  // ── Search filter ──────────────────────────────────────────
  const filtered = honeypots.filter(
    (hp) =>
      hp.name.toLowerCase().includes(search.toLowerCase()) ||
      hp.ip.toLowerCase().includes(search.toLowerCase()) ||
      hp.type.toLowerCase().includes(search.toLowerCase()),
  );

  // ── Action helper ──────────────────────────────────────────
  const setLoading = (id, val) =>
    setActionLoading((prev) => ({ ...prev, [id]: val }));

  // ── Pause ──────────────────────────────────────────────────
  const pause = useCallback(
    async (hp) => {
      if (!orgId) return;
      setLoading(hp.id, "pause");
      try {
        await api.put(`/api/organizations/${orgId}/honeypots/${hp.id}/pause`);
        setHoneypots((prev) =>
          prev.map((h) => (h.id === hp.id ? { ...h, status: "paused" } : h)),
        );
      } catch (err) {
        console.error("Pause failed:", err.message);
      } finally {
        setLoading(hp.id, null);
      }
    },
    [orgId],
  );

  // ── Resume ─────────────────────────────────────────────────
  const resume = useCallback(
    async (hp) => {
      if (!orgId) return;
      setLoading(hp.id, "resume");
      try {
        await api.put(`/api/organizations/${orgId}/honeypots/${hp.id}/resume`);
        setHoneypots((prev) =>
          prev.map((h) => (h.id === hp.id ? { ...h, status: "active" } : h)),
        );
      } catch (err) {
        console.error("Resume failed:", err.message);
      } finally {
        setLoading(hp.id, null);
      }
    },
    [orgId],
  );

  // ── Terminate ──────────────────────────────────────────────
  const terminate = useCallback(
    async (hp) => {
      if (!orgId) return;
      if (!window.confirm(`Permanently terminate ${hp.name}?`)) return;
      setLoading(hp.id, "terminate");
      try {
        await api.put(
          `/api/organizations/${orgId}/honeypots/${hp.id}/terminate`,
        );
        setHoneypots((prev) => prev.filter((h) => h.id !== hp.id));
      } catch (err) {
        console.error("Terminate failed:", err.message);
      } finally {
        setLoading(hp.id, null);
      }
    },
    [orgId],
  );

  // ── Deploy (POST — endpoint exists) ───────────────────────
  const deploy = useCallback(async () => {
    if (!form.name.trim() || !form.type) {
      alert("Please fill all fields");
      return;
    }
    if (!orgId) return;
    try {
      await api.post(`/api/organizations/${orgId}/honeypots`, {
        subscriptionId: sub?.id ?? null, // ← need current subscription id
        name: form.name.trim(),
        type: TYPE_MAP[form.type] ?? 0,
        location: LOCATION_MAP[form.location] ?? 0,
        configTemplateBase64: null,
      });
      // optimistic add
      setHoneypots((prev) => [
        {
          id: crypto.randomUUID(),
          name: form.name,
          type: form.type,
          ip: "—",
          logs: "0",
          criticalEvents: 0,
          status: "active",
          health: "Healthy",
        },
        ...prev,
      ]);
      setForm({ name: "", type: "SSH", location: "DMZ" });
      setShowDeploy(false);
    } catch (err) {
      alert(`Deploy failed: ${err.message}`);
    }
  }, [orgId, form, sub]);

  // ── Logs modal ─────────────────────────────────────────────
  const openLogs = (hp) => {
    setSelectedHoneypot(hp);
    setShowLogs(true);
  };

  const logs = selectedHoneypot
    ? [
        "System initialized.",
        `Listening on port (${selectedHoneypot.type})...`,
        `Connection attempt detected from 45.33.22.11`,
        "Honeypot trigger: /admin access attempt",
        "Payload saved successfully",
      ]
    : [];

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
          <h1 style={{ margin: 0 }}>Honeypots Management</h1>
          <p style={{ margin: "6px 0 0", color: "#6b7280" }}>
            Manage, deploy, and monitor your deception nodes.
          </p>
        </div>
        <button
          className={styles.btnDeploy}
          onClick={() => setShowDeploy(true)}
        >
          + Deploy New Trap
        </button>
      </div>

      {/* Search */}
      <div className={styles.honeypotSearchWrap}>
        <input
          className={styles.honeypotSearchInput}
          placeholder="Search by name, IP or type..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
      </div>

      {/* Cards */}
      <div className={styles.honeypotsGrid} style={{ marginTop: 20 }}>
        {filtered.length === 0 ? (
          <p>No honeypots found</p>
        ) : (
          filtered.map((hp) => {
            const busy = actionLoading[hp.id];
            const isPaused = hp.status === "paused";
            const isTerminated = hp.status === "terminated";

            return (
              <div key={hp.id} className={styles.honeypotCard}>
                {/* Top row */}
                <div className={styles.honeypotCardTop}>
                  <span className={styles.honeypotName}>{hp.name}</span>
                  <span
                    className={`${styles.badgeStatus} ${
                      hp.status === "active"
                        ? styles.badgeActive
                        : hp.status === "paused"
                          ? styles.badgeWarning
                          : styles.badgeInactive
                    }`}
                  >
                    {hp.status}
                  </span>
                </div>

                <span className={styles.honeypotType}>{hp.type}</span>
                <div className={styles.honeypotDivider} />

                {/* Meta */}
                <div className={styles.honeypotMeta}>
                  <div className={styles.metaItem}>
                    <span className={styles.metaLabel}>Total Events</span>
                    <span className={styles.metaValue}>{hp.logs}</span>
                  </div>
                  <div className={styles.metaItem}>
                    <span className={styles.metaLabel}>Critical</span>
                    <span
                      className={styles.metaValue}
                      style={{ color: "#e74c3c" }}
                    >
                      {hp.criticalEvents}
                    </span>
                  </div>
                  <div className={styles.metaItem}>
                    <span className={styles.metaLabel}>Type</span>
                    <span className={styles.metaValue}>{hp.type}</span>
                  </div>
                  <div className={styles.metaItem}>
                    <span className={styles.metaLabel}>Health</span>
                    <span
                      className={styles.metaValue}
                      style={{ color: "#22c55e" }}
                    >
                      {hp.health}
                    </span>
                  </div>
                </div>

                {/* Action buttons */}
                <div style={{ display: "flex", gap: 8 }}>
                  <button
                    className={styles.btnViewLogs}
                    style={{ flex: 1 }}
                    onClick={() => openLogs(hp)}
                  >
                    View Logs
                  </button>

                  {!isTerminated && (
                    <>
                      <button
                        onClick={() => (isPaused ? resume(hp) : pause(hp))}
                        disabled={!!busy}
                        style={{
                          padding: "11px 14px",
                          borderRadius: 50,
                          border: "1.5px solid #e8eaf0",
                          background: "#fff",
                          cursor: "pointer",
                          fontSize: "0.8rem",
                          fontWeight: 700,
                          color: isPaused ? "#22c55e" : "#f39c12",
                          whiteSpace: "nowrap",
                        }}
                      >
                        {busy === "pause" || busy === "resume"
                          ? "..."
                          : isPaused
                            ? "Resume"
                            : "Pause"}
                      </button>

                      <button
                        onClick={() => terminate(hp)}
                        disabled={!!busy}
                        style={{
                          padding: "11px 14px",
                          borderRadius: 50,
                          border: "1.5px solid #fde8e8",
                          background: "#fff",
                          cursor: "pointer",
                          fontSize: "0.8rem",
                          fontWeight: 700,
                          color: "#e74c3c",
                          whiteSpace: "nowrap",
                        }}
                      >
                        {busy === "terminate" ? "..." : "Terminate"}
                      </button>
                    </>
                  )}
                </div>
              </div>
            );
          })
        )}
      </div>

      {/* Deploy Modal */}
      {showDeploy && (
        <div
          className={styles.modalOverlay}
          onClick={() => setShowDeploy(false)}
        >
          <div
            className={styles.modalCard}
            onClick={(e) => e.stopPropagation()}
          >
            <div className={styles.modalHeader}>
              <h2 className={styles.modalTitle}>Deploy New Honeypot</h2>
              <button
                className={styles.modalClose}
                onClick={() => setShowDeploy(false)}
              >
                ✕
              </button>
            </div>
            <div className={styles.modalBody}>
              <div className={styles.fieldWrap}>
                <label className={styles.fieldLabel}>Honeypot Name</label>
                <input
                  className={styles.fieldInput}
                  placeholder="e.g., SSH-Honeypot-DMZ-02"
                  value={form.name}
                  onChange={(e) => setForm({ ...form, name: e.target.value })}
                />
              </div>

              <div className={styles.fieldWrap}>
                <label className={styles.fieldLabel}>Type</label>
                <select
                  className={styles.fieldSelect}
                  value={form.type}
                  onChange={(e) => setForm({ ...form, type: e.target.value })}
                >
                  {Object.keys(TYPE_MAP).map((t) => (
                    <option key={t} value={t}>
                      {t}
                    </option>
                  ))}
                </select>
              </div>

              <div className={styles.fieldWrap}>
                <label className={styles.fieldLabel}>Location</label>
                <select
                  className={styles.fieldSelect}
                  value={form.location}
                  onChange={(e) =>
                    setForm({ ...form, location: e.target.value })
                  }
                >
                  {Object.keys(LOCATION_MAP).map((l) => (
                    <option key={l} value={l}>
                      {l}
                    </option>
                  ))}
                </select>
              </div>
            </div>
            <div className={styles.modalFooter}>
              <button
                className={styles.btnCancel}
                onClick={() => setShowDeploy(false)}
              >
                Cancel
              </button>
              <button
                className={styles.btnDeploySubmit}
                onClick={deploy}
                disabled={!form.name.trim()}
              >
                Deploy Honeypot
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Logs Modal */}
      {showLogs && selectedHoneypot && (
        <div className={styles.modalOverlay} onClick={() => setShowLogs(false)}>
          <div
            className={styles.modalCard}
            onClick={(e) => e.stopPropagation()}
          >
            <div className={styles.modalHeader}>
              <h2 className={styles.modalTitle}>
                System Logs — {selectedHoneypot.name}
              </h2>
              <button
                className={styles.modalClose}
                onClick={() => setShowLogs(false)}
              >
                ✕
              </button>
            </div>
            <div className={styles.logsBody}>
              {logs.map((log, i) => (
                <div key={i} className={styles.logLine}>
                  <span className={styles.logTime}>
                    {String(i).padStart(2, "0")}
                  </span>
                  <span className={styles.logMsg}>{log}</span>
                </div>
              ))}
            </div>
            <div className={styles.modalFooter}>
              <button
                className={styles.btnCancel}
                style={{ flex: 1 }}
                onClick={() => setShowLogs(false)}
              >
                Close
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
