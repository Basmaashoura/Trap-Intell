// src/components/Topbar.jsx
import { useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import NotificationBell from "./NotificationBell";
import styles from "./Topbar.module.css";

function getInitials(firstName, lastName) {
  return ((firstName?.[0] ?? "") + (lastName?.[0] ?? "")).toUpperCase() || "?";
}

export default function Topbar({ onMenuClick }) {
  const navigate = useNavigate();
  const { user } = useAuth();

  const firstName = user?.firstName ?? "";
  const lastName = user?.lastName ?? "";
  const fullName = [firstName, lastName].filter(Boolean).join(" ") || "User";
  const initials = getInitials(firstName, lastName);
  const role = user?.role ?? "";

  // Read org name from localStorage (set during login via orgId lookup)
  // For now we derive it from email domain as a fallback
  const orgName = (() => {
    try {
      const u = JSON.parse(localStorage.getItem("user") ?? "{}");
      // The backend doesn't return org name in JWT — use email domain as display
      const email = u.email ?? "";
      const domain = email.split("@")[1]?.split(".")[0] ?? "";
      return domain
        ? domain.charAt(0).toUpperCase() + domain.slice(1)
        : "Organization";
    } catch {
      return "Organization";
    }
  })();

  return (
    <header className={styles.topbar}>
      {/* hamburger — mobile only */}
      <button
        className={styles.hamburger}
        onClick={onMenuClick}
        aria-label="Menu"
      >
        <span />
        <span />
        <span />
      </button>

      {/* search */}
      <div className={styles.search}>
        <svg
          width="16"
          height="16"
          viewBox="0 0 24 24"
          fill="none"
          stroke="#9098b1"
          strokeWidth="2"
          strokeLinecap="round"
          strokeLinejoin="round"
        >
          <circle cx="11" cy="11" r="8" />
          <line x1="21" y1="21" x2="16.65" y2="16.65" />
        </svg>
        <input type="text" placeholder="Search IPs, Threats, or Honeypots..." />
      </div>

      <div className={styles.right}>
        {/* org indicator */}
        <div className={styles.orgIndicator}>
          <span className={styles.orgDot} />
          {orgName}
        </div>

        {/* <button
          className={styles.createOrgBtn}
          onClick={() => navigate("/create-org")}
        >
          + Create Organization
        </button> */}

        <NotificationBell />

        <div className={styles.divider} />

        {/* profile */}
        <div className={styles.profile}>
          <div className={styles.profileInfo}>
            <span className={styles.profileName}>{fullName}</span>
            <span className={styles.profileRole}>{role}</span>
          </div>
          <div className={styles.profileAvatar}>{initials}</div>
        </div>
      </div>
    </header>
  );
}
