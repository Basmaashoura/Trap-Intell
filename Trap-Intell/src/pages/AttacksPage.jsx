// src/pages/AttacksPage.jsx
// Backend confirmed endpoints:
//   POST /api/organizations/{orgId}/honeypots/{honeypotId}/attacks/events  (ingestion)
//   No GET /attacks endpoint confirmed — using static data + alert feed fallback

import { useState, useEffect, useCallback } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import { api } from "../services/api";
import styles from "./AttacksPage.module.css";

// ── Static chart data ──────────────────────────────────────────
const CHART_DATA = {
  quarterly: [32, 28, 38, 25, 42, 35, 90, 55, 48, 38, 22, 18],
  monthly: [45, 38, 55, 42, 60, 52, 78, 65, 58, 50, 35, 28],
  weekly: [20, 35, 28, 42, 38, 55, 48, 60, 44, 32, 28, 22],
};
const MONTHS = [
  "Jan",
  "Feb",
  "Mar",
  "Apr",
  "May",
  "Jun",
  "Jul",
  "Aug",
  "Sep",
  "Oct",
  "Nov",
  "Dec",
];

const ACTORS = [
  {
    initials: "AP",
    name: "APT-28",
    category: "State",
    pct: "95%",
    color: "red",
  },
  {
    initials: "La",
    name: "Lazarus",
    category: "Finance",
    pct: "88%",
    color: "dark",
  },
  {
    initials: "Oi",
    name: "OilRig",
    category: "Espionage",
    pct: "60%",
    color: "slate",
  },
];

const STATUS_CLASS = {
  Blocked: styles.blocked,
  Detected: styles.detected,
  Contained: styles.contained,
};

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

// ── Time helper ────────────────────────────────────────────────
function timeAgo(d) {
  if (!d) return "—";
  const diff = Math.floor((Date.now() - new Date(d)) / 1000);
  if (diff < 60) return `${diff}s ago`;
  if (diff < 3600) return `${Math.floor(diff / 60)}m ago`;
  if (diff < 86400) return `${Math.floor(diff / 3600)}h ago`;
  return `${Math.floor(diff / 86400)}d ago`;
}

// ── Static fallback events ─────────────────────────────────────
const STATIC_EVENTS = [
  {
    id: "ATK-001",
    status: "Blocked",
    severity: "High",
    attackType: "BruteForce",
    targetTrap: "SSH-Honeypot-DMZ-01",
    sourceIP: "185.220.101.42",
    country: "Russia",
    time: "07:19",
  },
  {
    id: "ATK-002",
    status: "Detected",
    severity: "Low",
    attackType: "Reconnaissance",
    targetTrap: "HTTP-Honeypot-Web-01",
    sourceIP: "23.45.67.89",
    country: "United States",
    time: "21:19",
  },
  {
    id: "ATK-003",
    status: "Blocked",
    severity: "High",
    attackType: "LateralMovement",
    targetTrap: "SMB-Honeypot-Internal-01",
    sourceIP: "185.56.89.123",
    country: "Ukraine",
    time: "01:19",
  },
  {
    id: "ATK-004",
    status: "Blocked",
    severity: "Critical",
    attackType: "SQLInjection",
    targetTrap: "MySQL-Honeypot-DB-01",
    sourceIP: "103.21.244.5",
    country: "China",
    time: "14:32",
  },
  {
    id: "ATK-005",
    status: "Contained",
    severity: "Medium",
    attackType: "PortScan",
    targetTrap: "HTTP-Honeypot-Web-01",
    sourceIP: "45.33.22.11",
    country: "Germany",
    time: "09:45",
  },
];

const PAGE_SIZE = 10;

export default function AttacksPage() {
  const { orgId } = useAuth();
  const navigate = useNavigate();

  const [period, setPeriod] = useState("quarterly");
  const [events, setEvents] = useState(null); // null = loading
  const [loading, setLoading] = useState(true);
  const [query, setQuery] = useState("");
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);

  // ── Try to fetch real attack events ────────────────────────
  // Backend may not have GET /attacks yet — gracefully fall back
  const loadEvents = useCallback(async () => {
    if (!orgId) return;
    setLoading(true);
    try {
      // Try org-scoped attacks endpoint
      const data = await api.get(`/api/organizations/${orgId}/attacks`, {
        pageNumber: 1,
        pageSize: 50,
      });
      if (data) {
        const items = Array.isArray(data)
          ? data
          : (data.items ?? data.data ?? []);
        if (items.length > 0) {
          setEvents(items);
          setTotalCount(data.totalCount ?? items.length);
          return;
        }
      }
    } catch {
      // Endpoint doesn't exist — use static data silently
    }
    // Fallback to static data
    setEvents(STATIC_EVENTS);
    setTotalCount(STATIC_EVENTS.length);
    setLoading(false);
  }, [orgId]);

  useEffect(() => {
    loadEvents();
  }, [loadEvents]);
  useEffect(() => {
    setPage(1);
  }, [query]);

  // ── Resolve field names from real API or static data ───────
  function getField(ev, ...keys) {
    for (const k of keys) {
      if (ev[k] != null) return String(ev[k]);
    }
    return "—";
  }

  const eventsToShow = events ?? [];
  const filtered = query
    ? eventsToShow.filter((e) =>
        Object.values(e).some((v) =>
          String(v ?? "")
            .toLowerCase()
            .includes(query.toLowerCase()),
        ),
      )
    : eventsToShow;

  const totalPages = Math.ceil(filtered.length / PAGE_SIZE);
  const slice = filtered.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);
  const startNum = (page - 1) * PAGE_SIZE + 1;
  const endNum = Math.min(page * PAGE_SIZE, filtered.length);

  // ── Chart ──────────────────────────────────────────────────
  const data = CHART_DATA[period];
  const maxVal = Math.max(...data);
  const peakIdx = data.indexOf(maxVal);

  // Pagination range
  const pageRange = [];
  for (let p = 1; p <= totalPages; p++) {
    if (p === 1 || p === totalPages || (p >= page - 1 && p <= page + 1))
      pageRange.push(p);
    else if (pageRange[pageRange.length - 1] !== "…") pageRange.push("…");
  }

  return (
    <div>
      {/* Header */}
      <div className={styles.pageHeader}>
        <div>
          <h1>Attack Analysis</h1>
          <p>Real-time inspection of incoming threats and signatures.</p>
        </div>
      </div>

      {/* Search */}
      <div className={styles.searchWrap}>
        <svg
          className={styles.searchIcon}
          width="16"
          height="16"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2.2"
          strokeLinecap="round"
          strokeLinejoin="round"
        >
          <circle cx="11" cy="11" r="8" />
          <line x1="21" y1="21" x2="16.65" y2="16.65" />
        </svg>
        <input
          className={styles.searchInput}
          type="text"
          placeholder="Filter by IP, Attack Type, ID, or Status..."
          value={query}
          onChange={(e) => setQuery(e.target.value)}
        />
      </div>

      {/* Main row */}
      <div className={styles.mainRow}>
        {/* Traffic volume chart */}
        <div className={styles.trafficCard}>
          <div className={styles.trafficHeader}>
            <div>
              <div className={styles.trafficTitle}>Traffic Volume (Pps)</div>
              <div className={styles.trafficSubtitle}>
                Attack frequency over time
              </div>
            </div>
            <div className={styles.periodWrap}>
              <select
                value={period}
                onChange={(e) => setPeriod(e.target.value)}
              >
                <option value="quarterly">Quarterly</option>
                <option value="monthly">Monthly</option>
                <option value="weekly">Weekly</option>
              </select>
              <svg
                width="14"
                height="14"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2.5"
                strokeLinecap="round"
                strokeLinejoin="round"
              >
                <polyline points="6 9 12 15 18 9" />
              </svg>
            </div>
          </div>
          <div className={styles.chartArea}>
            <div className={styles.bars}>
              {data.map((val, i) => {
                const isPeak = i === peakIdx;
                return (
                  <div key={i} className={styles.barGroup}>
                    <div
                      className={`${styles.bar} ${isPeak ? styles.barPeak : ""}`}
                      style={{ height: `${(val / maxVal) * 100}%` }}
                    >
                      {isPeak && (
                        <div className={styles.barTooltip}>
                          <span className={styles.tooltipArrow}>▲</span>
                          {Math.round((val / maxVal) * 100)}%
                        </div>
                      )}
                    </div>
                    <span
                      className={`${styles.barLabel} ${isPeak ? styles.barLabelPeak : ""}`}
                    >
                      {MONTHS[i]}
                    </span>
                  </div>
                );
              })}
            </div>
          </div>
        </div>

        {/* Threat actors panel */}
        <div className={styles.actorsPanel}>
          <div className={styles.panelTitle}>Top Threat Actors</div>
          <div className={styles.panelDivider} />
          {ACTORS.map((a) => (
            <div key={a.name} className={styles.actorRow}>
              <div className={`${styles.actorAvatar} ${styles[a.color]}`}>
                {a.initials}
              </div>
              <div className={styles.actorInfo}>
                <div className={styles.actorName}>{a.name}</div>
                <div className={styles.actorCategory}>{a.category}</div>
              </div>
              <span className={styles.actorPct}>{a.pct}</span>
            </div>
          ))}
          <button
            className={styles.btnViewActors}
            onClick={() => navigate("/threat-actors")}
          >
            View All Actors
          </button>
        </div>
      </div>

      {/* Event log */}
      <div className={styles.eventLogSection}>
        <h2 className={styles.sectionHeading}>
          Detailed Event Log
          {events === STATIC_EVENTS && (
            <span
              style={{
                marginLeft: 10,
                fontSize: "0.72rem",
                color: "#9098b1",
                fontWeight: 400,
                background: "#f4f5f9",
                padding: "3px 8px",
                borderRadius: 20,
                border: "1px solid #e8eaf0",
              }}
            >
              Sample data — live feed pending
            </span>
          )}
        </h2>
        <div className={styles.tableWrap}>
          <table className={styles.eventTable}>
            <thead>
              <tr>
                <th>Status</th>
                <th>Severity</th>
                <th>Attack Type</th>
                <th>Target Trap</th>
                <th>Source IP</th>
                <th>ID &amp; Time</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                Array.from({ length: 5 }).map((_, i) => (
                  <tr key={i}>
                    {Array.from({ length: 6 }).map((_, j) => (
                      <td key={j}>
                        <Sk h={14} />
                      </td>
                    ))}
                  </tr>
                ))
              ) : slice.length === 0 ? (
                <tr>
                  <td colSpan={6} className={styles.emptyRow}>
                    No events match your search.
                  </td>
                </tr>
              ) : (
                slice.map((ev, i) => {
                  const id = getField(ev, "id", "externalEventId");
                  const status = getField(ev, "status", "attackStatus");
                  const sev = getField(ev, "severity", "attackSeverity");
                  const type = getField(ev, "attackType", "type", "eventType");
                  const target = getField(
                    ev,
                    "targetTrap",
                    "honeypotName",
                    "honeypotId",
                  );
                  const ip = getField(
                    ev,
                    "sourceIP",
                    "sourceIp",
                    "ipAddress",
                    "source.ipAddress",
                  );
                  const time =
                    ev.time ??
                    (ev.createdAt
                      ? new Date(ev.createdAt).toLocaleTimeString()
                      : "—");

                  return (
                    <tr key={`${id}-${i}`}>
                      <td>
                        <span
                          className={`${styles.statusPill} ${STATUS_CLASS[status] ?? ""}`}
                        >
                          {status}
                        </span>
                      </td>
                      <td>
                        <span className={styles.severityChip}>{sev}</span>
                      </td>
                      <td className={styles.attackTypeCell}>{type}</td>
                      <td>{target}</td>
                      <td className={styles.ipCell}>{ip}</td>
                      <td className={styles.idCell}>
                        {id} <span className={styles.timeLabel}>{time}</span>
                      </td>
                    </tr>
                  );
                })
              )}
            </tbody>
          </table>

          <div className={styles.tableFooter}>
            <span className={styles.tableInfo}>
              {filtered.length
                ? `Showing ${startNum}–${endNum} of ${filtered.length} events`
                : "No events found"}
            </span>
            <div className={styles.pagination}>
              <button
                className={styles.pageBtn}
                onClick={() => setPage((p) => p - 1)}
                disabled={page === 1}
              >
                ‹
              </button>
              {pageRange.map((p, i) =>
                p === "…" ? (
                  <span key={`d-${i}`} className={styles.pageDots}>
                    …
                  </span>
                ) : (
                  <button
                    key={p}
                    className={`${styles.pageBtn} ${page === p ? styles.pageBtnActive : ""}`}
                    onClick={() => setPage(p)}
                  >
                    {p}
                  </button>
                ),
              )}
              <button
                className={styles.pageBtn}
                onClick={() => setPage((p) => p + 1)}
                disabled={page === totalPages || totalPages === 0}
              >
                ›
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
