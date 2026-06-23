// Shown when: user.status === "Suspended" (after login check)

import { useNavigate } from "react-router-dom";
import Logo from "../../components/Logo";
import styles from "./StatusPage.module.css";

export function AccountSuspendedPage() {
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
              <circle cx="32" cy="32" r="32" fill="#e53e3e" fillOpacity="0.1" />
              {/* Lock body */}
              <rect
                x="20"
                y="30"
                width="24"
                height="18"
                rx="3"
                stroke="#e53e3e"
                strokeWidth="2.5"
                fill="none"
              />
              {/* Lock shackle */}
              <path
                d="M24 30v-6a8 8 0 0 1 16 0v6"
                stroke="#e53e3e"
                strokeWidth="2.5"
                strokeLinecap="round"
                fill="none"
              />
              {/* Keyhole */}
              <circle cx="32" cy="39" r="2.5" fill="#e53e3e" />
              <line
                x1="32"
                y1="41"
                x2="32"
                y2="44"
                stroke="#e53e3e"
                strokeWidth="2"
                strokeLinecap="round"
              />
            </svg>
          </div>
          <h1 className={styles.title} style={{ color: "#c53030" }}>
            Account Suspended
          </h1>
          <p className={styles.message}>
            Your account has been temporarily suspended. This may be due to
            multiple failed login attempts or an action taken by your
            organization administrator.
          </p>
          <p className={styles.message}>
            Please contact your administrator or our support team to restore
            access.
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
              className={styles.btnDanger}
            >
              Contact Support
            </a>
          </div>
        </div>
      </main>
    </>
  );
}

export default AccountSuspendedPage;
