import { useState } from "react";
import { Link } from "react-router-dom";
import Logo from "../../components/Logo";
import forgotImage from "../../assets/Images/passwordf.png";
import styles from "./ForgotPasswordPage.module.css";
import api from "../../services/api";

function ForgotPasswordPage() {
  const [email, setEmail] = useState("");
  const [loading, setLoading] = useState(false);
  const [submitted, setSubmitted] = useState(false);
  const [error, setError] = useState("");

  const handleSubmit = async () => {
    setError("");
    if (!email) {
      setError("Please enter your email address.");
      return;
    }

    setLoading(true);
    try {
      await api.post("/auth/forgot-password", { email });
      // Always show success — backend never reveals if account exists
      setSubmitted(true);
    } catch {
      // Still show success to avoid account enumeration
      setSubmitted(true);
    } finally {
      setLoading(false);
    }
  };

  const handleKeyDown = (e) => {
    if (e.key === "Enter") handleSubmit();
  };

  return (
    <>
      <header className={styles.header}>
        <Logo />
      </header>

      <main>
        <div className={styles.page}>
          <div className={styles.formPanel}>
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

            <div className={styles.formCard}>
              {submitted ? (
                /* ── Success state ── */
                <div className={styles.successCard}>
                  <div className={styles.successIcon}>
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
                  <h1 className={styles.formTitle}>Check Your Email</h1>
                  <p className={styles.formSubtitle}>
                    If an account exists for <strong>{email}</strong>, you'll
                    receive a password reset link shortly. Please check your
                    inbox and spam folder.
                  </p>
                  <Link
                    to="/login"
                    className={styles.btnSubmit}
                    style={{
                      display: "block",
                      textAlign: "center",
                      textDecoration: "none",
                    }}
                  >
                    Back to Login
                  </Link>
                </div>
              ) : (
                /* ── Form state ── */
                <>
                  <div className={styles.formHeader}>
                    <h1 className={styles.formTitle}>Forgot your password?</h1>
                    <p className={styles.formSubtitle}>
                      No worries. Enter your email below and we'll send you a
                      reset link.
                    </p>
                  </div>

                  {error && (
                    <div className={styles.errorBanner} role="alert">
                      <svg
                        width="16"
                        height="16"
                        viewBox="0 0 24 24"
                        fill="none"
                      >
                        <circle
                          cx="12"
                          cy="12"
                          r="10"
                          stroke="#e53e3e"
                          strokeWidth="2"
                        />
                        <path
                          d="M12 8v4M12 16h.01"
                          stroke="#e53e3e"
                          strokeWidth="2"
                          strokeLinecap="round"
                        />
                      </svg>
                      {error}
                    </div>
                  )}

                  <div className={styles.formGroup}>
                    <div className={styles.fieldWrap}>
                      <label htmlFor="email">Email Address</label>
                      <input
                        type="email"
                        id="email"
                        placeholder="Enter your email"
                        value={email}
                        onChange={(e) => setEmail(e.target.value)}
                        onKeyDown={handleKeyDown}
                        disabled={loading}
                        required
                      />
                    </div>
                  </div>

                  <div className={styles.formGroup}>
                    <button
                      type="button"
                      className={styles.btnSubmit}
                      onClick={handleSubmit}
                      disabled={loading || !email}
                    >
                      {loading ? (
                        <span
                          className={styles.spinner}
                          aria-label="Sending…"
                        />
                      ) : (
                        "Send Reset Link"
                      )}
                    </button>
                  </div>

                  <div className={styles.formGroup}>
                    <div className={styles.divider}>
                      <p>Or login with</p>
                    </div>
                  </div>

                  <div
                    className={`${styles.formGroup} ${styles.socialOptions}`}
                  >
                    <button
                      type="button"
                      className={styles.btnSocial}
                      title="Facebook"
                    >
                      <svg
                        width="24"
                        height="24"
                        viewBox="0 0 24 24"
                        fill="none"
                      >
                        <path
                          d="M24 12.0733C24 5.40546 18.6274 0 12 0C5.37262 0 0 5.40536 0 12.0733C0 18.0994 4.38825 23.0943 10.125 24V15.5633H7.07812V12.0733H10.125V9.41343C10.125 6.38755 11.9166 4.71615 14.6575 4.71615C15.9705 4.71615 17.3438 4.95195 17.3438 4.95195V7.92313H15.8306C14.3398 7.92313 13.875 8.85381 13.875 9.80864V12.0733H17.2031L16.6711 15.5633H13.875V24C19.6117 23.0943 24 18.0995 24 12.0733Z"
                          fill="#1877F2"
                        />
                      </svg>
                    </button>
                    <button
                      type="button"
                      className={styles.btnSocial}
                      title="Google"
                    >
                      <svg
                        width="24"
                        height="24"
                        viewBox="0 0 24 24"
                        fill="none"
                      >
                        <path
                          d="M21.8055 10.0415H21V10H12V14H17.6515C16.827 16.3285 14.6115 18 12 18C8.6865 18 6 15.3135 6 12C6 8.6865 8.6865 6 12 6C13.5295 6 14.921 6.577 15.9805 7.5195L18.809 4.691C17.023 3.0265 14.634 2 12 2C6.4775 2 2 6.4775 2 12C2 17.5225 6.4775 22 12 22C17.5225 22 22 17.5225 22 12C22 11.3295 21.931 10.675 21.8055 10.0415Z"
                          fill="#FFC107"
                        />
                        <path
                          d="M3.15302 7.3455L6.43851 9.755C7.32752 7.554 9.48052 6 12 6C13.5295 6 14.921 6.577 15.9805 7.5195L18.809 4.691C17.023 3.0265 14.634 2 12 2C8.15902 2 4.82802 4.1685 3.15302 7.3455Z"
                          fill="#FF3D00"
                        />
                        <path
                          d="M12 22C14.583 22 16.93 21.0115 18.7045 19.404L15.6095 16.785C14.5717 17.5742 13.3037 18.001 12 18C9.39897 18 7.19047 16.3415 6.35847 14.027L3.09747 16.5395C4.75247 19.778 8.11347 22 12 22Z"
                          fill="#4CAF50"
                        />
                        <path
                          d="M21.8055 10.0415H21V10H12V14H17.6515C17.2571 15.1082 16.5467 16.0766 15.608 16.7855L15.6095 16.7845L18.7045 19.4035C18.4855 19.6025 22 17 22 12C22 11.3295 21.931 10.675 21.8055 10.0415Z"
                          fill="#1976D2"
                        />
                      </svg>
                    </button>
                    <button
                      type="button"
                      className={styles.btnSocial}
                      title="Apple"
                    >
                      <svg
                        width="24"
                        height="24"
                        viewBox="0 0 24 24"
                        fill="none"
                      >
                        <path
                          d="M17.5172 12.5555C17.5078 10.957 18.232 9.75234 19.6945 8.86406C18.8766 7.69219 17.6391 7.04766 16.0078 6.92344C14.4633 6.80156 12.7734 7.82344 12.1547 7.82344C11.5008 7.82344 10.0055 6.96563 8.82891 6.96563C6.40078 7.00313 3.82031 8.90156 3.82031 12.7641C3.82031 13.9055 4.02891 15.0844 4.44609 16.2984C5.00391 17.8969 7.01484 21.8133 9.1125 21.75C10.2094 21.7242 10.9852 20.9719 12.4125 20.9719C13.7977 20.9719 14.5148 21.75 15.7383 21.75C17.8547 21.7195 19.6734 18.1594 20.2031 16.5563C17.3648 15.218 17.5172 12.6375 17.5172 12.5555ZM15.0539 5.40703C16.2422 3.99609 16.1344 2.71172 16.0992 2.25C15.0492 2.31094 13.8352 2.96484 13.1437 3.76875C12.382 4.63125 11.9344 5.69766 12.0305 6.9C13.1648 6.98672 14.2008 6.40313 15.0539 5.40703Z"
                          fill="#313131"
                        />
                      </svg>
                    </button>
                  </div>
                </>
              )}
            </div>
          </div>

          <div className={styles.imagePanel}>
            <div className={styles.heroImageWrap}>
              <img src={forgotImage} alt="forgot password" />
            </div>
          </div>
        </div>
      </main>
    </>
  );
}

export default ForgotPasswordPage;
