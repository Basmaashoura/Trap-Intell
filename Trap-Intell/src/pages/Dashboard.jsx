import { useState, useEffect, useCallback } from "react";
import { MapContainer, TileLayer, CircleMarker, Tooltip } from "react-leaflet";
import { useNavigate } from "react-router-dom";
import "leaflet/dist/leaflet.css";
import { useAuth } from "../context/AuthContext";
import { api } from "../services/api";
import styles from "./Dashboard.module.css";

// ── Helpers ────────────────────────────────────────────────────
function timeAgo(dateStr) {
  if (!dateStr) return "—";
  const diff = Math.floor((Date.now() - new Date(dateStr)) / 1000);
  if (diff < 60) return `${diff}s ago`;
  if (diff < 3600) return `${Math.floor(diff / 60)}m ago`;
  if (diff < 86400) return `${Math.floor(diff / 3600)}h ago`;
  return `${Math.floor(diff / 86400)}d ago`;
}

// severity from real API is numeric: 0=Info,1=Low,2=Medium,3=High,4=Critical
const SEV_NUM = { 0: "info", 1: "low", 2: "medium", 3: "high", 4: "critical" };
function severityClass(s) {
  if (typeof s === "number") s = SEV_NUM[s] ?? "low";
  if (!s || typeof s !== "string") return styles.low;
  const v = s.toLowerCase();
  if (v === "critical") return styles.critical;
  if (v === "high") return styles.high;
  if (v === "medium") return styles.medium;
  return styles.low;
}
function severityLabel(s) {
  if (typeof s === "number")
    return (
      (SEV_NUM[s] ?? "low").charAt(0).toUpperCase() +
      (SEV_NUM[s] ?? "low").slice(1)
    );
  return String(s ?? "");
}

function fmt(val) {
  if (val == null) return "—";
  if (typeof val === "number") return val.toLocaleString();
  return String(val);
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

// ── Static chart data ──────────────────────────────────────────
const CHART_DATA = {
  "24h": [
    38, 52, 28, 70, 55, 90, 45, 62, 48, 85, 72, 58, 40, 66, 78, 55, 90, 44, 68,
    52, 75, 82, 60, 48,
  ],
  "7d": [220, 310, 280, 450, 390, 520, 410],
  "30d": [
    3200, 4100, 3800, 4800, 4200, 5100, 3900, 4600, 4400, 5200, 4100, 4700,
    3600, 4300, 4900, 5300, 4000, 4800, 4500, 5000, 3700, 4400, 4100, 4900,
    5200, 4300, 4700, 4000, 4600, 5100,
  ],
};
const CHART_LABELS = {
  "24h": Array.from({ length: 24 }, (_, i) =>
    i % 6 === 0 ? `${String(i).padStart(2, "0")}:00` : "",
  ),
  "7d": ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"],
  "30d": Array.from({ length: 30 }, (_, i) => (i % 7 === 0 ? `D${i + 1}` : "")),
};

// Fallback static data while API loads
const FALLBACK_ACTORS = [
  {
    id: 1,
    name: "APT-28",
    category: "State-Sponsored",
    riskScore: 95,
    color: "#e74c3c",
  },
  {
    id: 2,
    name: "Lazarus",
    category: "Financial",
    riskScore: 88,
    color: "#8a1bfa",
  },
  {
    id: 3,
    name: "OilRig",
    category: "Espionage",
    riskScore: 74,
    color: "#f39c12",
  },
  {
    id: 4,
    name: "Sandworm",
    category: "Infrastructure",
    riskScore: 61,
    color: "#4044e4",
  },
];
const FALLBACK_MAP = [
  {
    lat: 55.7558,
    lng: 37.6173,
    type: "attack",
    label: "Moscow — SSH Brute Force",
  },
  {
    lat: 39.9042,
    lng: 116.4074,
    type: "attack",
    label: "Beijing — SQL Injection",
  },
  { lat: 37.5665, lng: 126.978, type: "attack", label: "Seoul — DDoS" },
  { lat: 52.52, lng: 13.405, type: "attack", label: "Berlin — Port Scan" },
  {
    lat: 40.7128,
    lng: -74.006,
    type: "honeypot",
    label: "New York — Honeypot DB-01",
  },
  {
    lat: 51.5074,
    lng: -0.1278,
    type: "honeypot",
    label: "London — Honeypot WEB-03",
  },
  {
    lat: 1.3521,
    lng: 103.8198,
    type: "honeypot",
    label: "Singapore — Honeypot API-05",
  },
];

// ── Threat Map ─────────────────────────────────────────────────
function ThreatMap({ points }) {
  return (
    <MapContainer
      center={[20, 10]}
      zoom={2}
      minZoom={2}
      maxZoom={6}
      scrollWheelZoom={false}
      style={{ height: "260px", width: "100%", borderRadius: "8px" }}
      className={styles.leafletMap}
    >
      <TileLayer
        attribution='&copy; <a href="https://www.openstreetmap.org/">OSM</a>'
        url="https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png"
      />
      {points.map((p, i) => (
        <CircleMarker
          key={i}
          center={[p.lat, p.lng]}
          radius={p.type === "attack" ? 7 : 6}
          pathOptions={{
            color: p.type === "attack" ? "#e74c3c" : "#4044e4",
            fillColor: p.type === "attack" ? "#e74c3c" : "#4044e4",
            fillOpacity: 0.85,
            weight: 2,
          }}
        >
          <Tooltip direction="top" offset={[0, -6]} opacity={0.95}>
            <span style={{ fontSize: "0.75rem", fontWeight: 600 }}>
              {p.label}
            </span>
          </Tooltip>
        </CircleMarker>
      ))}
    </MapContainer>
  );
}

// ── Main Dashboard ─────────────────────────────────────────────
export default function Dashboard() {
  const { orgId } = useAuth();
  const navigate = useNavigate();
  const [range, setRange] = useState("24h");

  // Dashboard data state
  const [alertsDash, setAlertsDash] = useState(null); // alerts/dashboard
  const [ownerDash, setOwnerDash] = useState(null); // dashboard/owner
  const [recentAlerts, setRecentAlerts] = useState(null);
  const [loading, setLoading] = useState(true);

  // ── Current endpoints ────────────────────────────────────────
  // GET /api/organizations/{orgId}/alerts/dashboard   → SOC overview metrics
  // GET /api/organizations/{orgId}/dashboard/owner    → owner-focused metrics
  // GET /api/organizations/{orgId}/alerts             → recent alert list
  const load = useCallback(() => {
    if (!orgId) return;
    setLoading(true);

    Promise.allSettled([
      // dashboard/owner contains: organizationName, quota, alerts, auditing
      api.get(`/api/organizations/${orgId}/dashboard/owner`),
      // recent alerts list for the table
      api.get(`/api/organizations/${orgId}/alerts`, {
        pageNumber: 1,
        pageSize: 5,
        sortBy: "createdAt",
        order: "desc",
      }),
    ])
      .then(([ownerRes, alertsRes]) => {
        if (ownerRes.status === "fulfilled" && ownerRes.value) {
          setOwnerDash(ownerRes.value);
          // alerts summary is nested inside owner response
          if (ownerRes.value.alerts) setAlertsDash(ownerRes.value.alerts);
        }
        if (alertsRes.status === "fulfilled" && alertsRes.value) {
          const d = alertsRes.value;
          setRecentAlerts(Array.isArray(d) ? d : (d.items ?? d.data ?? []));
        } else {
          setRecentAlerts([]);
        }
      })
      .finally(() => setLoading(false));
  }, [orgId]);

  useEffect(() => {
    load();
  }, [load]);

  // ── KPI cards — map from real response fields ──────────────────
  // alerts/dashboard likely returns: totalOpen, critical, acknowledged, resolved, avgResponseTime
  // dashboard/owner likely returns: activeHoneypots, totalHoneypots, totalAttacks, activeThreatActors
  // ── KPI cards — field names confirmed from real dashboard/owner response ──
  // dashboard/owner returns: { organizationName, quota, alerts, auditing }
  // quota.currentHoneypots / quota.maxHoneypots (both 0 — not seeded yet)
  // alerts.totalActiveAlerts, alerts.criticalUnresolvedAlerts
  // NO threat actor count or attack count in this endpoint yet
  const kpiCards = [
    {
      label: "Active Threat Actors",
      // Not available in dashboard/owner yet — show placeholder
      value: loading ? null : "—",
      sub: "Backend pending",
      color: "purple",
    },
    {
      label: "Total Attacks (24h)",
      // Not available in dashboard/owner yet — show audit events
      value: loading ? null : fmt(ownerDash?.auditing?.totalEvents ?? "—"),
      sub: ownerDash?.auditing?.totalEvents != null ? "audit events" : null,
      color: "green",
    },
    {
      label: "Active Honeypots",
      // 0/0 until honeypots are seeded
      value: loading
        ? null
        : ownerDash?.quota != null
          ? `${ownerDash.quota.currentHoneypots}/${ownerDash.quota.maxHoneypots}`
          : "—",
      sub:
        ownerDash?.quota?.honeypotUsagePercent != null
          ? `${ownerDash.quota.honeypotUsagePercent}% used`
          : null,
      color: "orange",
    },
    {
      label: "Open Alerts",
      // Confirmed field: alerts.totalActiveAlerts (returns 3)
      value: loading
        ? null
        : fmt(
            ownerDash?.alerts?.totalActiveAlerts ??
              alertsDash?.totalActiveAlerts ??
              "—",
          ),
      sub:
        (ownerDash?.alerts?.criticalUnresolvedAlerts ??
          alertsDash?.criticalUnresolvedAlerts) != null
          ? `${ownerDash?.alerts?.criticalUnresolvedAlerts ?? alertsDash?.criticalUnresolvedAlerts} critical`
          : null,
      color: "red",
    },
  ];

  const actorColors = ["#e74c3c", "#8a1bfa", "#f39c12", "#4044e4"];
  const chartData = CHART_DATA[range];
  const maxVal = Math.max(...chartData);
  const peakIdx = chartData.indexOf(maxVal);

  return (
    <div className={styles.page}>
      {/* Page header */}
      <div className={styles.pageHeader}>
        <div>
          <h1 className={styles.pageTitle}>Dashboard</h1>
          <p className={styles.pageSubtitle}>
            Overview of current threat landscape & honeypot status.
          </p>
        </div>
        <div className={styles.pageActions}>
          <button className={styles.btnLive} onClick={load}>
            <span className={styles.livePulse} />
            Live Monitoring
          </button>
          <button className={styles.btnExport}>Export Report</button>
        </div>
      </div>

      {/* KPI stat cards */}
      <div className={styles.statsGrid}>
        {kpiCards.map((s) => (
          <div key={s.label} className={styles.statCard}>
            <div className={styles.statHeader}>
              <span className={styles.statLabel}>{s.label}</span>
              <div className={`${styles.statIcon} ${styles[s.color]}`} />
            </div>
            <div className={styles.statValue}>
              {s.value == null ? <Sk w={64} h={34} r={4} /> : s.value}
            </div>
            {s.sub && !loading && (
              <div className={styles.statFooter}>
                <span className={styles.statMeta}>{s.sub}</span>
              </div>
            )}
          </div>
        ))}
      </div>

      {/* Charts row */}
      <div className={styles.chartsRow}>
        {/* Attack trend */}
        <div className={styles.trendCard}>
          <div className={styles.cardHeader}>
            <h2 className={styles.cardTitle}>Attack Trend</h2>
            <div className={styles.timeToggle}>
              {["24h", "7d", "30d"].map((r) => (
                <button
                  key={r}
                  className={`${styles.timeBtn} ${range === r ? styles.active : ""}`}
                  onClick={() => setRange(r)}
                >
                  {r.toUpperCase()}
                </button>
              ))}
            </div>
          </div>
          <div className={styles.barChart}>
            {chartData.map((val, i) => (
              <div key={i} className={styles.barGroup}>
                <div
                  className={`${styles.bar} ${i === peakIdx ? styles.peakBar : ""}`}
                  style={{ height: `${(val / maxVal) * 100}%` }}
                />
                <span className={styles.barLabel}>
                  {CHART_LABELS[range][i]}
                </span>
              </div>
            ))}
          </div>
        </div>

        {/* Right column */}
        <div className={styles.rightCol}>
          {/* Top threat actors — static fallback (no endpoint exists) */}
          <div className={styles.actorsCard}>
            <h2 className={styles.cardTitle}>Top Threat Actors</h2>
            <div className={styles.actorList}>
              {FALLBACK_ACTORS.map((actor, idx) => (
                <div key={actor.id} className={styles.actorItem}>
                  <div
                    className={styles.actorIcon}
                    style={{
                      background: actorColors[idx % actorColors.length],
                      color: "#fff",
                    }}
                  >
                    {actor.name.substring(0, 2).toUpperCase()}
                  </div>
                  <div className={styles.actorDetails}>
                    <span className={styles.actorName}>{actor.name}</span>
                    <span className={styles.actorCategory}>
                      {actor.category}
                    </span>
                  </div>
                  <span className={styles.actorLevel}>{actor.riskScore}%</span>
                </div>
              ))}
            </div>
            <button
              className={styles.btnViewAll}
              onClick={() => navigate("/threat-actors")}
            >
              View All Actors
            </button>
          </div>

          {/* Recent alerts — real data */}
          <div className={styles.alertsCard}>
            <h2 className={styles.cardTitle}>Recent Alerts</h2>
            <table className={styles.alertsTable}>
              <thead>
                <tr>
                  <th>Event</th>
                  <th>Time</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                {loading || recentAlerts === null ? (
                  Array.from({ length: 3 }).map((_, i) => (
                    <tr key={i}>
                      <td>
                        <Sk w="80%" h={12} />
                      </td>
                      <td>
                        <Sk w={40} h={12} />
                      </td>
                      <td>
                        <Sk w={50} h={18} r={4} />
                      </td>
                    </tr>
                  ))
                ) : recentAlerts.length > 0 ? (
                  recentAlerts.slice(0, 5).map((alert, i) => (
                    <tr key={alert.id ?? i}>
                      <td>{alert.title ?? alert.name ?? "Alert"}</td>
                      <td>{timeAgo(alert.createdAt ?? alert.triggeredAt)}</td>
                      <td>
                        <span
                          className={`${styles.badge} ${severityClass(alert.severity)}`}
                        >
                          {severityLabel(alert.severity).toUpperCase() || "—"}
                        </span>
                      </td>
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td
                      colSpan={3}
                      style={{
                        textAlign: "center",
                        color: "#9098b1",
                        padding: "20px 0",
                      }}
                    >
                      No recent alerts
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
            <button
              className={styles.btnViewAll}
              style={{ marginTop: 10 }}
              onClick={() => navigate("/alerts")}
            >
              View All Alerts
            </button>
          </div>
        </div>
      </div>

      {/* Global Threat Map */}
      <div className={styles.mapCard}>
        <h2 className={styles.cardTitle}>Global Threat Map</h2>
        <ThreatMap points={FALLBACK_MAP} />
        <div className={styles.mapLegend}>
          <div className={styles.legendItem}>
            <span className={`${styles.legendDot} ${styles.red}`} /> Active
            Attack
          </div>
          <div className={styles.legendItem}>
            <span className={`${styles.legendDot} ${styles.blue}`} /> Honeypot
          </div>
        </div>
      </div>
    </div>
  );
}
