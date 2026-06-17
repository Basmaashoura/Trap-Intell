import { useState, useEffect } from "react";
import { MapContainer, TileLayer, CircleMarker, Tooltip } from "react-leaflet";
import "leaflet/dist/leaflet.css";
import styles from "./Dashboard.module.css";

/* ─────────────────────────────────────────
   MOCK DATA — replace with API calls
   TODO (backend team): fetch from
   GET /api/organizations/{orgId}/alerts/dashboard
───────────────────────────────────────── */
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

const LABELS = {
  "24h": Array.from({ length: 24 }, (_, i) =>
    i % 6 === 0 ? `${String(i).padStart(2, "0")}:00` : "",
  ),
  "7d": ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"],
  "30d": Array.from({ length: 30 }, (_, i) => (i % 7 === 0 ? `D${i + 1}` : "")),
};

/*
 * TODO (backend team): replace STATS values with live data from
 * GET /api/organizations/{orgId}/alerts/dashboard
 * Fields: totalOpenAlerts, activeHoneypots, totalAttacks24h, activeThreatActors
 */
const STATS = [
  {
    label: "Active Threat Actors",
    value: "8",
    color: "purple",
    trend: "+1",
    meta: "3 New Detected",
  },
  { label: "Total Attacks (24h)", value: "1,245", color: "green" },
  { label: "Active Honeypots", value: "15/15", color: "orange" },
  { label: "Open Alerts", value: "5", color: "red" },
];

/*
 * TODO (backend team): replace with real threat actors from
 * GET /api/organizations/{orgId}/threat-actors (top 4 by activity)
 * Fields per actor: initials, name, category, threatLevel (%)
 */
const TOP_THREAT_ACTORS = [
  {
    initials: "AP",
    name: "APT-28",
    category: "State-Sponsored",
    level: "95%",
    color: "#e74c3c",
  },
  {
    initials: "La",
    name: "Lazarus",
    category: "Financial",
    level: "88%",
    color: "#8a1bfa",
  },
  {
    initials: "Oi",
    name: "OilRig",
    category: "Espionage",
    level: "74%",
    color: "#f39c12",
  },
  {
    initials: "Sw",
    name: "Sandworm",
    category: "Infrastructure",
    level: "61%",
    color: "#4044e4",
  },
];

/*
 * TODO (backend team): replace with live recent alerts from
 * GET /api/organizations/{orgId}/alerts?pageSize=5&sortBy=createdAt&order=desc
 * Fields: title, createdAt (relative), severity
 */
const RECENT_ALERTS = [
  { event: "Malware Attack", time: "2 mins", severity: "high" },
  { event: "DDoS Attempt", time: "5 mins", severity: "critical" },
  { event: "Port Scan Detect", time: "12 mins", severity: "medium" },
];

/*
 * TODO (backend team): replace with live attack origin data from
 * GET /api/organizations/{orgId}/alerts/dashboard (geoData field)
 * Each point: { lat, lng, type: "attack" | "honeypot", label }
 */
const MAP_POINTS = [
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
  { lat: 35.6762, lng: 139.6503, type: "attack", label: "Tokyo — Ransomware" },
  { lat: -23.5505, lng: -46.6333, type: "attack", label: "São Paulo — MitM" },
  {
    lat: 30.0444,
    lng: 31.2357,
    type: "honeypot",
    label: "Cairo — Honeypot SSH-04",
  },
];

/* ── Leaflet map component ── */
function ThreatMap() {
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
        attribution='&copy; <a href="https://www.openstreetmap.org/">OpenStreetMap</a>'
        url="https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png"
      />

      {MAP_POINTS.map((point, i) => (
        <CircleMarker
          key={i}
          center={[point.lat, point.lng]}
          radius={point.type === "attack" ? 7 : 6}
          pathOptions={{
            color: point.type === "attack" ? "#e74c3c" : "#4044e4",
            fillColor: point.type === "attack" ? "#e74c3c" : "#4044e4",
            fillOpacity: 0.85,
            weight: 2,
          }}
        >
          <Tooltip direction="top" offset={[0, -6]} opacity={0.95}>
            <span style={{ fontSize: "0.75rem", fontWeight: 600 }}>
              {point.label}
            </span>
          </Tooltip>
        </CircleMarker>
      ))}
    </MapContainer>
  );
}

/* ── Main Dashboard ── */
export default function Dashboard() {
  const [range, setRange] = useState("24h");
  const data = CHART_DATA[range];
  const maxVal = Math.max(...data);
  const peakIdx = data.indexOf(maxVal);

  return (
    <div className={styles.page}>
      {/* ── Page header ── */}
      <div className={styles.pageHeader}>
        <div>
          <h1 className={styles.pageTitle}>Dashboard</h1>
          <p className={styles.pageSubtitle}>
            Overview of current threat landscape & honeypot status.
          </p>
        </div>
        <div className={styles.pageActions}>
          {/* TODO (backend team): connect to WebSocket for live updates */}
          <button className={styles.btnLive}>
            <span className={styles.livePulse} />
            Live Monitoring
          </button>
          {/* TODO (backend team): call GET /api/organizations/{orgId}/reports/export */}
          <button className={styles.btnExport}>Export Report</button>
        </div>
      </div>

      {/* ── Stat cards ── */}
      {/* TODO (backend team): replace STATS values with getAlertDashboard() response */}
      <div className={styles.statsGrid}>
        {STATS.map((s) => (
          <div key={s.label} className={styles.statCard}>
            <div className={styles.statHeader}>
              <span className={styles.statLabel}>{s.label}</span>
              <div className={`${styles.statIcon} ${styles[s.color]}`} />
            </div>
            <div className={styles.statValue}>{s.value}</div>
            {s.trend && (
              <div className={styles.statFooter}>
                <span className={`${styles.statTrend} ${styles.up}`}>
                  {s.trend}
                </span>
                <span className={styles.statMeta}>{s.meta}</span>
              </div>
            )}
          </div>
        ))}
      </div>

      {/* ── Charts row ── */}
      <div className={styles.chartsRow}>
        {/* Attack trend bar chart */}
        <div className={styles.trendCard}>
          <div className={styles.cardHeader}>
            <h2 className={styles.cardTitle}>Attack Trend</h2>
            {/* TODO (backend team): fetch chart data per range from getAlertDashboard(lastNDays) */}
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
            {data.map((val, i) => (
              <div key={i} className={styles.barGroup}>
                <div
                  className={`${styles.bar} ${i === peakIdx ? styles.peakBar : ""}`}
                  style={{ height: `${(val / maxVal) * 100}%` }}
                />
                <span className={styles.barLabel}>{LABELS[range][i]}</span>
              </div>
            ))}
          </div>
        </div>

        {/* Right column */}
        <div className={styles.rightCol}>
          {/* Top threat actors */}
          <div className={styles.actorsCard}>
            <h2 className={styles.cardTitle}>Top Threat Actors</h2>
            {/* TODO (backend team): replace TOP_THREAT_ACTORS with real data */}
            <div className={styles.actorList}>
              {TOP_THREAT_ACTORS.map((actor) => (
                <div key={actor.name} className={styles.actorItem}>
                  <div
                    className={styles.actorIcon}
                    style={{ background: actor.color, color: "#fff" }}
                  >
                    {actor.initials}
                  </div>
                  <div className={styles.actorDetails}>
                    <span className={styles.actorName}>{actor.name}</span>
                    <span className={styles.actorCategory}>
                      {actor.category}
                    </span>
                  </div>
                  <span className={styles.actorLevel}>{actor.level}</span>
                </div>
              ))}
            </div>
            <button className={styles.btnViewAll}>View All Actors</button>
          </div>

          {/* Recent alerts */}
          <div className={styles.alertsCard}>
            <h2 className={styles.cardTitle}>Recent Alerts</h2>
            {/* TODO (backend team): replace RECENT_ALERTS with getAlerts({ pageSize: 5 }) */}
            <table className={styles.alertsTable}>
              <thead>
                <tr>
                  <th>Event</th>
                  <th>Time</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                {RECENT_ALERTS.map((alert, i) => (
                  <tr key={i}>
                    <td>{alert.event}</td>
                    <td>{alert.time}</td>
                    <td>
                      <span
                        className={`${styles.badge} ${styles[alert.severity]}`}
                      >
                        {alert.severity.toUpperCase()}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </div>

      {/* ── Global Threat Map (Leaflet) ── */}
      <div className={styles.mapCard}>
        <h2 className={styles.cardTitle}>Global Threat Map</h2>
        {/* TODO (backend team): replace MAP_POINTS with geoData from getAlertDashboard() */}
        <ThreatMap />
        <div className={styles.mapLegend}>
          <div className={styles.legendItem}>
            <span className={`${styles.legendDot} ${styles.red}`} />
            Active Attack
          </div>
          <div className={styles.legendItem}>
            <span className={`${styles.legendDot} ${styles.blue}`} />
            Honeypot
          </div>
        </div>
      </div>
    </div>
  );
}

// import { useState } from "react";
// import styles from "./Dashboard.module.css";

// const CHART_DATA = {
//   "24h": [
//     38, 52, 28, 70, 55, 90, 45, 62, 48, 85, 72, 58, 40, 66, 78, 55, 90, 44, 68,
//     52, 75, 82, 60, 48,
//   ],
//   "7d": [220, 310, 280, 450, 390, 520, 410],
//   "30d": [
//     3200, 4100, 3800, 4800, 4200, 5100, 3900, 4600, 4400, 5200, 4100, 4700,
//     3600, 4300, 4900, 5300, 4000, 4800, 4500, 5000, 3700, 4400, 4100, 4900,
//     5200, 4300, 4700, 4000, 4600, 5100,
//   ],
// };

// const LABELS = {
//   "24h": Array.from({ length: 24 }, (_, i) =>
//     i % 6 === 0 ? `${String(i).padStart(2, "0")}:00` : "",
//   ),
//   "7d": ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"],
//   "30d": Array.from({ length: 30 }, (_, i) => (i % 7 === 0 ? `D${i + 1}` : "")),
// };

// const STATS = [
//   {
//     label: "Active Threat Actors",
//     value: "8",
//     color: "purple",
//     trend: "+1",
//     meta: "3 New Detected",
//   },
//   { label: "Total Attacks (24h)", value: "1,245", color: "green" },
//   { label: "Active Honeypots", value: "15/15", color: "orange" },
//   { label: "Open Alerts", value: "5", color: "red" },
// ];

// export default function Dashboard() {
//   const [range, setRange] = useState("24h");
//   const data = CHART_DATA[range];
//   const maxVal = Math.max(...data);
//   const peakIdx = data.indexOf(maxVal);

//   return (

//     // <>
//     // <div className={styles.page}>
//     //   {/* page header */}
//     //   <div className={styles.pageHeader}>
//     //     <div>
//     //       <h1 className={styles.pageTitle}>Dashboard</h1>
//     //       <p className={styles.pageSubtitle}>
//     //         Overview of current threat landscape & honeypot status.
//     //       </p>
//     //     </div>
//     //     <div className={styles.pageActions}>
//     //       <button className={styles.btnLive}>
//     //         <span className={styles.livePulse} />
//     //         Live Monitoring
//     //       </button>
//     //       <button className={styles.btnExport}>Export Report</button>
//     //     </div>
//     //   </div>

//     //   {/* stat cards */}
//     //   <div className={styles.statsGrid}>
//     //     {STATS.map((s) => (
//     //       <div key={s.label} className={styles.statCard}>
//     //         <div className={styles.statHeader}>
//     //           <span className={styles.statLabel}>{s.label}</span>
//     //           <div className={`${styles.statIcon} ${styles[s.color]}`} />
//     //         </div>
//     //         <div className={styles.statValue}>{s.value}</div>
//     //         {s.trend && (
//     //           <div className={styles.statFooter}>
//     //             <span className={`${styles.statTrend} ${styles.up}`}>
//     //               {s.trend}
//     //             </span>
//     //             <span className={styles.statMeta}>{s.meta}</span>
//     //           </div>
//     //         )}
//     //       </div>
//     //     ))}
//     //   </div>

//     //   {/* charts row */}
//     //   <div className={styles.chartsRow}>
//     //     {/* attack trend */}
//     //     <div className={styles.trendCard}>
//     //       <div className={styles.cardHeader}>
//     //         <h2 className={styles.cardTitle}>Attack Trend</h2>
//     //         <div className={styles.timeToggle}>
//     //           {["24h", "7d", "30d"].map((r) => (
//     //             <button
//     //               key={r}
//     //               className={`${styles.timeBtn} ${range === r ? styles.active : ""}`}
//     //               onClick={() => setRange(r)}
//     //             >
//     //               {r.toUpperCase()}
//     //             </button>
//     //           ))}
//     //         </div>
//     //       </div>
//     //       <div className={styles.barChart}>
//     //         {data.map((val, i) => (
//     //           <div key={i} className={styles.barGroup}>
//     //             <div
//     //               className={`${styles.bar} ${i === peakIdx ? styles.peakBar : ""}`}
//     //               style={{ height: `${(val / maxVal) * 100}%` }}
//     //             />
//     //             <span className={styles.barLabel}>{LABELS[range][i]}</span>
//     //           </div>
//     //         ))}
//     //       </div>
//     //     </div>

//     //     {/* right col */}
//     //     <div className={styles.rightCol}>
//     //       {/* top threat actors */}
//     //       <div className={styles.actorsCard}>
//     //         <h2 className={styles.cardTitle}>Top Threat Actors</h2>
//     //         <div className={styles.actorList}>
//     //           {[
//     //             ["BTC", "99%", "down"],
//     //             ["BCH", "90%", "up"],
//     //             ["ETH", "85%", "up"],
//     //             ["LTC", "78%", "down"],
//     //           ].map(([name, pct, dir]) => (
//     //             <div key={name} className={styles.actorItem}>
//     //               <div className={styles.actorIcon}>{name}</div>
//     //               <span className={styles.actorName}>{name}</span>
//     //               <span className={`${styles.actorPct} ${styles[dir]}`}>
//     //                 {pct}
//     //               </span>
//     //             </div>
//     //           ))}
//     //         </div>
//     //         <button className={styles.btnViewAll}>View All Actors</button>
//     //       </div>

//     //       {/* recent alerts */}
//     //       <div className={styles.alertsCard}>
//     //         <h2 className={styles.cardTitle}>Recent Alerts</h2>
//     //         <table className={styles.alertsTable}>
//     //           <thead>
//     //             <tr>
//     //               <th>Event</th>
//     //               <th>Time</th>
//     //               <th>Status</th>
//     //             </tr>
//     //           </thead>
//     //           <tbody>
//     //             <tr>
//     //               <td>Malware Attack</td>
//     //               <td>2 mins</td>
//     //               <td>
//     //                 <span className={`${styles.badge} ${styles.high}`}>
//     //                   HIGH
//     //                 </span>
//     //               </td>
//     //             </tr>
//     //             <tr>
//     //               <td>DDoS Attempt</td>
//     //               <td>5 mins</td>
//     //               <td>
//     //                 <span className={`${styles.badge} ${styles.critical}`}>
//     //                   CRITICAL
//     //                 </span>
//     //               </td>
//     //             </tr>
//     //           </tbody>
//     //         </table>
//     //       </div>
//     //     </div>
//     //   </div>

//     //   {/* threat map */}
//     //   <div className={styles.mapCard}>
//     //     <h2 className={styles.cardTitle}>Global Threat Map</h2>
//     //     <div className={styles.mapContainer}>
//     //       <svg viewBox="0 0 900 440" xmlns="http://www.w3.org/2000/svg">
//     //         <rect width="900" height="440" fill="#c9d8ea" />
//     //         <circle cx="230" cy="330" r="5" fill="#e74c3c" />
//     //         <circle cx="770" cy="320" r="5" fill="#4044e4" />
//     //       </svg>
//     //     </div>
//     //     <div className={styles.mapLegend}>
//     //       <div className={styles.legendItem}>
//     //         <span className={`${styles.legendDot} ${styles.red}`} />
//     //         Active Attack
//     //       </div>
//     //       <div className={styles.legendItem}>
//     //         <span className={`${styles.legendDot} ${styles.blue}`} />
//     //         Honeypot
//     //       </div>
//     //     </div>
//     //   </div>
//     // </div>
//     // </>
//   );
// }
