import { useState } from "react";
import styles from "./AlertsPage.module.css";

const INITIAL_RULES = [
  { id: 1, name: "Root Access Attempt", enabled: true },
  { id: 2, name: "Ransomware Pattern Detection", enabled: true },
  { id: 3, name: "Data Exfiltration > 100MB", enabled: true },
  { id: 4, name: "Known Malicious IP Access", enabled: true },
  { id: 5, name: "Port Scanning Activity", enabled: false },
  { id: 6, name: "Suspicious PowerShell Execution", enabled: false },
];

const INITIAL_ALERTS = [
  {
    id: 1,
    title: "Suspicious SSH Activity",
    target: "Server-01",
    severity: "critical",
    time: "2m ago",
  },
  {
    id: 2,
    title: "Suspicious SSH Activity",
    target: "Server-02",
    severity: "critical",
    time: "5m ago",
  },
  {
    id: 3,
    title: "Suspicious SSH Activity",
    target: "Server-03",
    severity: "critical",
    time: "8m ago",
  },
];

// maps severity string → CSS module class
const BADGE_CLASS = {
  critical: styles.badgeCritical,
  high: styles.badgeHigh,
  medium: styles.badgeMedium,
};

export default function AlertsPage() {
  const [rules, setRules] = useState(INITIAL_RULES);
  const [alerts, setAlerts] = useState(INITIAL_ALERTS);
  const [showModal, setShowModal] = useState(false);

  const dismissAlert = (id) =>
    setAlerts((prev) => prev.filter((a) => a.id !== id));

  const toggleRule = (id) =>
    setRules((prev) =>
      prev.map((r) => (r.id === id ? { ...r, enabled: !r.enabled } : r)),
    );

  return (
    <div>
      {/* header */}
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

      {/* main grid */}
      <div className={styles.alertsMainRow}>
        {/* alert cards */}
        <div className={styles.alertsList}>
          {alerts.map((alert) => (
            <div key={alert.id} className={styles.alertCard}>
              <div className={styles.alertCardTop}>
                <div className={styles.alertIconWrap}>⚠</div>
                <div className={styles.alertCardInfo}>
                  <div className={styles.alertTitle}>{alert.title}</div>
                  <div className={styles.alertTarget}>
                    Target: {alert.target}
                  </div>
                </div>
                <span
                  className={`${styles.alertBadge} ${BADGE_CLASS[alert.severity] ?? ""}`}
                >
                  {alert.severity}
                </span>
              </div>

              <div className={styles.alertCardFooter}>
                <span className={styles.alertTime}>{alert.time}</span>
                <div className={styles.alertActions}>
                  <button
                    className={styles.btnDismiss}
                    onClick={() => dismissAlert(alert.id)}
                  >
                    Dismiss
                  </button>
                  <button className={styles.btnInvestigate}>Investigate</button>
                </div>
              </div>
            </div>
          ))}
        </div>

        {/* active rules panel */}
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

      {/* configure rules modal */}
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
                aria-label="Close"
              >
                ✕
              </button>
            </div>

            <div className={styles.rulesModalList}>
              {rules.map((rule) => (
                <div key={rule.id} className={styles.ruleRow}>
                  {/* circle indicator */}
                  <div
                    className={`${styles.ruleIndicator} ${rule.enabled ? styles.on : ""}`}
                  >
                    <span className={styles.ruleIndicatorDot} />
                  </div>

                  <span className={styles.ruleRowName}>{rule.name}</span>

                  {/* iOS toggle */}
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
