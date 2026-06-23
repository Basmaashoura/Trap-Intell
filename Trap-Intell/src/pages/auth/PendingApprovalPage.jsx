// Shown when: organization.status === "PendingApproval"
// No API calls needed here — it's a pure informational screen.

import { useNavigate } from "react-router-dom";
import Logo from "../../components/Logo";
import styles from "./StatusPage.module.css";

export function PendingApprovalPage() {
  const navigate = useNavigate();

  return (
    <>
      <div className={styles.binaryBg} aria-hidden="true" />
      <header className={styles.header}>
        <Logo />
      </header>
      <main className={styles.page}>
        <div className={styles.card}>
          <div className={styles.iconWrap}>
            <svg width="64" height="64" viewBox="0 0 64 64" fill="none">
              <circle cx="32" cy="32" r="32" fill="#4044e4" fillOpacity="0.1" />
              {/* Clock face */}
              <circle
                cx="32"
                cy="32"
                r="18"
                stroke="#4044e4"
                strokeWidth="2.5"
                fill="none"
              />
              {/* Hour hand */}
              <line
                x1="32"
                y1="32"
                x2="32"
                y2="20"
                stroke="#4044e4"
                strokeWidth="2.5"
                strokeLinecap="round"
              />
              {/* Minute hand */}
              <line
                x1="32"
                y1="32"
                x2="40"
                y2="36"
                stroke="#4044e4"
                strokeWidth="2.5"
                strokeLinecap="round"
              />
            </svg>
          </div>
          <h1 className={styles.title}>Pending Approval</h1>
          <p className={styles.message}>
            Your organization registration is under review. Our team will verify
            your details and approve your account within 1–2 business days.
          </p>
          <p className={styles.message}>
            You'll receive an email at your registered address once approved.
          </p>
          <div className={styles.actions}>
            <button
              type="button"
              className={styles.btnSecondary}
              onClick={() => navigate("/login")}
            >
              Back to Login
            </button>
            <a
              href="mailto:support@trap-intell.com"
              className={styles.btnPrimary}
            >
              Contact Support
            </a>
          </div>
        </div>
      </main>
    </>
  );
}

export default PendingApprovalPage;
