import { NavLink, useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import styles from "./Sidebar.module.css";

const NAV_ITEMS = [
  {
    label: "Dashboard",
    to: "/dashboard",
    icon: (
      <svg
        width="15"
        height="15"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
      >
        <rect x="3" y="3" width="7" height="7" />
        <rect x="14" y="3" width="7" height="7" />
        <rect x="14" y="14" width="7" height="7" />
        <rect x="3" y="14" width="7" height="7" />
      </svg>
    ),
  },
  {
    label: "Honeypots",
    to: "/honeypots",
    icon: (
      <svg
        width="15"
        height="15"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
      >
        <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z" />
      </svg>
    ),
  },
  {
    label: "Attacks",
    to: "/attacks",
    icon: (
      <svg
        width="15"
        height="15"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
      >
        <path d="M13 2L3 14h9l-1 8 10-12h-9l1-8z" />
      </svg>
    ),
  },
  {
    label: "Alerts",
    to: "/alerts",
    icon: (
      <svg
        width="15"
        height="15"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
      >
        <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9" />
        <path d="M13.73 21a2 2 0 0 1-3.46 0" />
      </svg>
    ),
  },
  {
    label: "Threat Actors",
    to: "/threat-actors",
    icon: (
      <svg
        width="15"
        height="15"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
      >
        <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" />
        <circle cx="12" cy="7" r="4" />
      </svg>
    ),
  },
  {
    label: "Reports",
    to: "/reports",
    icon: (
      <svg
        width="15"
        height="15"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
      >
        <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
        <polyline points="14,2 14,8 20,8" />
        <line x1="16" y1="13" x2="8" y2="13" />
        <line x1="16" y1="17" x2="8" y2="17" />
      </svg>
    ),
  },
  {
    label: "Settings",
    to: "/settings",
    icon: (
      <svg
        width="15"
        height="15"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
      >
        <circle cx="12" cy="12" r="3" />
        <path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83-2.83l.06-.06A1.65 1.65 0 0 0 4.68 15a1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 2.83-2.83l.06.06A1.65 1.65 0 0 0 9 4.68a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 2.83l-.06.06A1.65 1.65 0 0 0 19.4 9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z" />
      </svg>
    ),
  },
];

function getInitials(firstName, lastName) {
  return ((firstName?.[0] ?? "") + (lastName?.[0] ?? "")).toUpperCase() || "?";
}

export default function Sidebar({ open }) {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const firstName = user?.firstName ?? "";
  const lastName = user?.lastName ?? "";
  const fullName =
    [firstName, lastName].filter(Boolean).join(" ") || user?.email || "User";
  const initials = getInitials(firstName, lastName);
  const role = user?.role ?? "";

  return (
    <aside className={`${styles.sidebar} ${open ? styles.open : ""}`}>
      {/* Logo */}
      <a href="/" className={styles.logo}>
        <svg width="28" height="38" viewBox="0 0 39 53" fill="none">
          <path
            d="M29.8865 43.0497L27.0449 45.5729V37.4788C27.0449 37.1731 26.8972 36.8852 26.6475 36.7082L20.4142 32.2555V18.1751L23.1832 16.7919C24.8886 18.5279 28.0486 17.2135 27.9925 14.749C27.8499 10.7675 21.9668 11.1331 22.3451 15.0929L19.0451 16.7435C18.7241 16.904 18.5216 17.2314 18.5216 17.5905V32.7446C18.5216 33.049 18.6693 33.3368 18.919 33.5151L25.1523 37.9666V47.2554L22.3107 49.7785V38.4264C22.3107 37.9042 21.8866 37.48 21.3631 37.48H15.6813V33.4285C17.3333 32.5255 16.6429 29.8674 14.7337 29.903C12.8245 29.8674 12.1342 32.5268 13.7861 33.4285V38.4264C13.7861 38.9498 14.2102 39.374 14.7337 39.374H20.4168V51.4597L19.4692 52.3003L16.6276 49.7772V44.1082C16.6837 43.5719 15.9144 43.051 15.6125 42.7008C15.6507 42.5505 15.6762 42.39 15.6813 42.2142C15.6176 39.7166 11.9559 39.7153 11.8922 42.2142C11.9291 43.6764 13.199 44.2763 14.2586 44.0254L14.7337 44.5004V48.096C14.3593 47.7406 6.34411 40.7088 6.34411 40.5776L11.6158 35.3059C11.7928 35.1288 11.8934 34.8881 11.8934 34.6359V29.4267L15.303 26.8692C15.5412 26.6908 15.6813 26.4106 15.6813 26.1113V24.0442C18.6451 22.987 17.9179 18.5611 14.735 18.5356C11.5521 18.5611 10.8236 22.987 13.7874 24.0442V25.6388L10.3778 28.1963C10.1396 28.3746 9.99951 28.6548 9.99951 28.9541V34.2449L5.12903 39.1154C3.55861 36.9973 2.65304 34.4372 2.39958 31.7957H4.52404L4.71127 32.5446C3.74711 33.7125 4.68071 35.6319 6.21037 35.5835C8.53608 35.5619 8.76916 32.1663 6.48294 31.8224C6.27023 31.1385 6.25877 29.8826 5.26404 29.9005H2.14612L1.76784 27.0589H7.15798C7.68145 27.0589 8.10558 26.6348 8.10558 26.1113V20.0971L16.0672 16.5589C17.1932 16.0367 16.4455 14.3402 15.2967 14.828L6.77333 18.6158C6.43072 18.7687 6.21037 19.1075 6.21037 19.4806V25.1637H1.51566L0.999824 21.2969L2.88484 19.4119C5.64996 20.0232 6.20018 15.8278 3.37011 15.6928C2.14994 15.6737 1.20106 16.8989 1.54622 18.072L0.68523 18.933L0 13.7988H12.8411C13.3645 13.7988 13.7874 13.3747 13.7874 12.8512V8.11577C13.767 6.87268 11.9138 6.87013 11.8934 8.11577V11.9049H2.88866C6.81791 9.90144 10.6121 7.41908 14.3389 4.42979L16.6289 7.48531V12.8512C16.6493 14.0943 18.5025 14.0982 18.5229 12.8512V7.16944C18.5229 6.96438 18.4566 6.76441 18.3331 6.60139L15.8049 3.22746C17.0148 2.20853 18.2184 1.13101 19.4182 0C20.8104 1.55259 22.27 3.00202 23.7984 4.34318L20.6944 7.4471C20.5174 7.62414 20.4168 7.86486 20.4168 8.11577V10.9573C20.4384 12.2017 22.2903 12.2029 22.312 10.9573V8.50806L25.2465 5.57227C26.4387 6.53898 27.6741 7.43309 28.9414 8.2737V17.8516L23.9066 19.5303C23.5207 19.659 23.2596 20.0207 23.2596 20.4283V28.9516C23.2596 29.2917 23.4417 29.6063 23.7372 29.7744L29.889 33.291V43.0485L29.8865 43.0497Z"
            fill="#3D42DF"
          />
        </svg>
        <span>Trap-intell</span>
      </a>

      {/* Nav */}
      <nav className={styles.nav}>
        {NAV_ITEMS.map(({ label, to, icon }) => (
          <NavLink
            key={to}
            to={to}
            className={({ isActive }) =>
              `${styles.navItem} ${isActive ? styles.active : ""}`
            }
          >
            <span className={styles.navIcon}>{icon}</span>
            {label}
          </NavLink>
        ))}
      </nav>

      {/* Upgrade */}
      <div className={styles.upgrade}>
        <button className={styles.btnUpgrade}>Upgrade for Free</button>
      </div>

      {/* User + Logout */}
      <div className={styles.userSection}>
        <div className={styles.user}>
          <div className={styles.avatar}>{initials}</div>
          <div className={styles.userInfo}>
            <span className={styles.greeting}>Welcome back 👋</span>
            <span className={styles.name} title={fullName}>
              {fullName}
            </span>
            {role && <span className={styles.roleTag}>{role}</span>}
          </div>
        </div>
        <button
          className={styles.btnLogout}
          onClick={() => logout()}
          title="Sign out"
          aria-label="Sign out"
        >
          <svg
            width="15"
            height="15"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            strokeWidth="2"
            strokeLinecap="round"
            strokeLinejoin="round"
          >
            <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
            <polyline points="16,17 21,12 16,7" />
            <line x1="21" y1="12" x2="9" y2="12" />
          </svg>
        </button>
      </div>
    </aside>
  );
}
