import { useState, useEffect, useRef } from "react";
import styles from "./AttacksPage.module.css";

/* ── static data ── */
const MONTHLY_DATA = {
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

const ATTACK_TYPES = [
  "Brute Force",
  "SQL Injection",
  "XSS Attack",
  "DDoS",
  "Port Scan",
  "Phishing",
  "Ransomware",
  "MitM",
];
const TARGET_TRAPS = ["DB-01", "DB-02", "WEB-03", "SSH-04", "API-05"];
const SOURCE_IPS = [
  "192.168.1.1",
  "45.33.22.11",
  "103.21.244.0",
  "185.220.101.5",
  "198.51.100.23",
];
const STATUSES = ["Blocked", "Blocked", "Blocked", "Detected", "Contained"];
const SEVERITIES = ["DB-01", "DB-02", "WEB-03"];

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

const PAGE_SIZE = 10;

/* ── helpers ── */
const randItem = (arr) => arr[Math.floor(Math.random() * arr.length)];
const randId = () => `ATK-${900 + Math.floor(Math.random() * 99)}`;
const randTime = () => {
  const h = String(Math.floor(Math.random() * 24)).padStart(2, "0");
  const m = String(Math.floor(Math.random() * 60)).padStart(2, "0");
  return `${h}:${m}`;
};
const makeEvent = () => ({
  id: randId(),
  status: randItem(STATUSES),
  severity: randItem(SEVERITIES),
  attackType: randItem(ATTACK_TYPES),
  targetTrap: randItem(TARGET_TRAPS),
  sourceIP: randItem(SOURCE_IPS),
  time: randTime(),
});

const STATUS_CLASS = {
  Blocked: styles.blocked,
  Detected: styles.detected,
  Contained: styles.contained,
};

/* ── component ── */
export default function AttacksPage() {
  const [period, setPeriod] = useState("quarterly");
  const [liveFeed, setLiveFeed] = useState(true);
  const [events, setEvents] = useState(() =>
    Array.from({ length: 48 }, makeEvent),
  );
  const [query, setQuery] = useState("");
  const [page, setPage] = useState(1);
  const liveRef = useRef(null);

  /* live feed */
  useEffect(() => {
    if (!liveFeed) {
      clearInterval(liveRef.current);
      return;
    }
    liveRef.current = setInterval(() => {
      setEvents((prev) => [makeEvent(), ...prev]);
    }, 5000);
    return () => clearInterval(liveRef.current);
  }, [liveFeed]);

  /* reset page on search */
  useEffect(() => {
    setPage(1);
  }, [query]);

  /* derived */
  const filtered = query
    ? events.filter(
        (e) =>
          e.sourceIP.includes(query) ||
          e.attackType.toLowerCase().includes(query) ||
          e.id.toLowerCase().includes(query) ||
          e.status.toLowerCase().includes(query) ||
          e.targetTrap.toLowerCase().includes(query),
      )
    : events;

  const totalPages = Math.ceil(filtered.length / PAGE_SIZE);
  const slice = filtered.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);
  const startNum = (page - 1) * PAGE_SIZE + 1;
  const endNum = Math.min(page * PAGE_SIZE, filtered.length);

  /* bar chart */
  const data = MONTHLY_DATA[period];
  const maxVal = Math.max(...data);
  const peakIdx = data.indexOf(maxVal);

  /* pagination range */
  const pageRange = [];
  for (let p = 1; p <= totalPages; p++) {
    if (p === 1 || p === totalPages || (p >= page - 1 && p <= page + 1))
      pageRange.push(p);
    else if (pageRange[pageRange.length - 1] !== "…") pageRange.push("…");
  }

  return (
    <div>
      {/* page header */}
      <div className={styles.pageHeader}>
        <div>
          <h1>Attack Analysis</h1>
          <p>Real-time inspection of incoming threats and signatures.</p>
        </div>
        <button
          className={styles.btnLiveFeed}
          onClick={() => setLiveFeed((f) => !f)}
        >
          {liveFeed ? (
            <>
              <span className={styles.liveFeedDot} />
              Live Feed On
            </>
          ) : (
            <>
              <span className={styles.liveFeedDotOff} />
              Live Feed Off
            </>
          )}
        </button>
      </div>

      {/* search */}
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
          placeholder="Filter by IP, Attack Type, or Hash..."
          value={query}
          onChange={(e) => setQuery(e.target.value.toLowerCase())}
        />
      </div>

      {/* main row */}
      <div className={styles.mainRow}>
        {/* traffic volume card */}
        <div className={styles.trafficCard}>
          <div className={styles.trafficHeader}>
            <div>
              <div className={styles.trafficTitle}>Traffic Volume (Pps)</div>
              <div className={styles.trafficSubtitle}>Monthly Earning</div>
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

        {/* threat actors panel */}
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
          <button className={styles.btnViewActors}>View All Actors</button>
        </div>
      </div>

      {/* event log */}
      <div className={styles.eventLogSection}>
        <h2 className={styles.sectionHeading}>Detailed Event Log</h2>
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
              {slice.length === 0 ? (
                <tr>
                  <td colSpan={6} className={styles.emptyRow}>
                    No events match your search.
                  </td>
                </tr>
              ) : (
                slice.map((ev, i) => (
                  <tr key={`${ev.id}-${i}`}>
                    <td>
                      <span
                        className={`${styles.statusPill} ${STATUS_CLASS[ev.status] ?? ""}`}
                      >
                        {ev.status}
                      </span>
                    </td>
                    <td>
                      <span className={styles.severityChip}>{ev.severity}</span>
                    </td>
                    <td className={styles.attackTypeCell}>{ev.attackType}</td>
                    <td>{ev.targetTrap}</td>
                    <td className={styles.ipCell}>{ev.sourceIP}</td>
                    <td className={styles.idCell}>
                      {ev.id}&nbsp;
                      <span className={styles.timeLabel}>{ev.time}</span>
                    </td>
                  </tr>
                ))
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
                  <span key={`dots-${i}`} className={styles.pageDots}>
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
