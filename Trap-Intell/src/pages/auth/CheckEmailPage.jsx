// src/pages/auth/CheckEmailPage.jsx
import { useEffect } from "react";
import { Link, useNavigate, useLocation } from "react-router-dom";
import Logo from "../../components/Logo";
import toast from "react-hot-toast";
import styles from "./CheckEmailPage.module.css";

function CheckEmailPage() {
  const navigate = useNavigate();
  const location = useLocation();

  useEffect(() => {
    toast.success("Verification email sent successfully!");
  }, []);

  return (
    <>
      <div className={styles.binaryBg} aria-hidden="true"></div>

      <header className={styles.header}>
        <Logo />
      </header>

      <main>
        <div className={styles.page}>
          <div className={styles.card}>
            {/* Back link */}
            <Link to="/login" className={styles.backLink}>
              <svg width="9" height="15" viewBox="0 0 9 15" fill="none">
                <path
                  d="M7.5 14.25L0.75 7.5L7.5 0.75"
                  stroke="#313131"
                  strokeWidth="1.5"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                />
              </svg>
              Back to login
            </Link>

            {/* Title */}
            <div className={styles.cardHeader}>
              <h1 className={styles.cardTitle}>Check Your Email</h1>
              <p className={styles.cardSubtitle}>
                Password reset instructions sent
              </p>
            </div>

            {/* Check icon */}
            <div className={styles.checkCircle}>
              <svg width="56" height="56" viewBox="0 0 56 56" fill="none">
                <circle
                  cx="28"
                  cy="28"
                  r="28"
                  fill="#4044e4"
                  fillOpacity="0.12"
                />
                <path
                  d="M18 28.5L24.5 35.5L38 21"
                  stroke="#4044e4"
                  strokeWidth="3"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                />
                <path
                  d="M28 14C20.268 14 14 20.268 14 28C14 35.732 20.268 42 28 42C35.732 42 42 35.732 42 28C42 20.268 35.732 14 28 14Z"
                  stroke="#4044e4"
                  strokeWidth="2.5"
                />
              </svg>
            </div>

            {/* Info */}
            <p className={styles.cardInfo}>
              If an account exists with{" "}
              <strong>{location.state?.email || "your email"}</strong>, you will
              receive a password reset link shortly.
            </p>

            {/* Spam note */}
            <p className={styles.cardSpam}>
              Please check your email inbox and spam folder.
            </p>

            {/* Enter code */}
            <Link to="/verify-code" className={styles.btnEnterCode}>
              Enter code
            </Link>

            {/* DEV: Bypass to set password */}
            {process.env.NODE_ENV === "development" && (
              <button
                onClick={() =>
                  navigate("/set-password", {
                    state: {
                      email: location.state?.email || "test@company.com",
                      emailVerificationToken: "dev-mock-token",
                    },
                  })
                }
                style={{
                  marginTop: 12,
                  padding: "10px 20px",
                  background: "#ff6b6b",
                  color: "white",
                  border: "none",
                  borderRadius: "6px",
                  cursor: "pointer",
                  fontSize: "0.85rem",
                  fontWeight: 600,
                  width: "100%",
                }}
              >
                🔧 DEV: Skip to Set Password
              </button>
            )}
          </div>
        </div>
      </main>
    </>
  );
}

export default CheckEmailPage;
