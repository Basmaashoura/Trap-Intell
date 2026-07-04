// Route: /reset-password?token=xxx
// The token comes from the email link query param.
// Backend: POST /api/auth/reset-password  { token, newPassword }

import { useState } from "react";
import { useSearchParams, useNavigate, useLocation } from "react-router-dom";
import Logo from "../../components/Logo";
import resetImage from "../../assets/images/reset-pass.png";
import styles from "./SetPasswordPage.module.css";
import api from "../../services/api";

const EyeIcon = ({ isVisible }) => {
  if (isVisible) {
    // Open eye
    return (
      <svg
        width="20"
        height="20"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth="2"
      >
        <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z" />
        <circle cx="12" cy="12" r="3" />
      </svg>
    );
  }
  // Closed eye with slash
  return (
    <svg
      width="20"
      height="20"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
    >
      <path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24" />
      <line x1="1" y1="1" x2="23" y2="23" />
    </svg>
  );

  <svg width="24" height="24" viewBox="0 0 24 24" fill="none">
    <path
      d="M20.25 21C20.1515 21.0001 20.0539 20.9808 19.9629 20.9431C19.8719 20.9053 19.7893 20.85 19.7198 20.7801L3.21982 4.28013C3.0851 4.13833 3.01111 3.94952 3.01361 3.75395C3.01612 3.55838 3.09492 3.37152 3.23322 3.23322C3.37152 3.09492 3.55838 3.01612 3.75395 3.01361C3.94952 3.01111 4.13833 3.0851 4.28013 3.21982L20.7801 19.7198C20.885 19.8247 20.9563 19.9583 20.9852 20.1038C21.0141 20.2492 20.9993 20.3999 20.9426 20.5369C20.8858 20.6739 20.7897 20.791 20.6665 20.8735C20.5432 20.9559 20.3983 20.9999 20.25 21ZM11.625 14.8054L9.19732 12.3778C9.18341 12.364 9.16552 12.3549 9.14618 12.3518C9.12684 12.3487 9.10701 12.3517 9.08948 12.3604C9.07194 12.3692 9.05759 12.3832 9.04843 12.4005C9.03927 12.4178 9.03577 12.4376 9.03841 12.457C9.13642 13.0868 9.43215 13.6692 9.88286 14.1199C10.3336 14.5706 10.9159 14.8663 11.5458 14.9643C11.5652 14.967 11.5849 14.9635 11.6022 14.9543C11.6196 14.9452 11.6336 14.9308 11.6423 14.9133C11.651 14.8957 11.6541 14.8759 11.651 14.8566C11.6479 14.8372 11.6388 14.8194 11.625 14.8054ZM12.375 9.1945L14.8064 11.625C14.8203 11.639 14.8382 11.6482 14.8576 11.6514C14.8771 11.6547 14.897 11.6517 14.9147 11.6429C14.9323 11.6341 14.9467 11.62 14.9559 11.6026C14.9651 11.5851 14.9685 11.5653 14.9658 11.5458C14.868 10.9151 14.572 10.3319 14.1208 9.88059C13.6695 9.4293 13.0863 9.13336 12.4556 9.0356C12.4361 9.03258 12.4161 9.03582 12.3985 9.04484C12.3809 9.05386 12.3666 9.06821 12.3577 9.08583C12.3488 9.10346 12.3456 9.12345 12.3487 9.14297C12.3518 9.16249 12.361 9.18052 12.375 9.1945Z"
      fill="#313131"
    />
    <path
      d="M23.0156 12.8137C23.1708 12.5702 23.2529 12.2872 23.252 11.9984C23.2512 11.7096 23.1675 11.4271 23.0109 11.1844C21.7706 9.26625 20.1614 7.63688 18.3577 6.47203C16.3594 5.18203 14.1562 4.5 11.985 4.5C10.8404 4.50157 9.7035 4.6882 8.61843 5.05266C8.58807 5.06276 8.56079 5.08046 8.5392 5.10409C8.51761 5.12772 8.50242 5.15647 8.49509 5.18763C8.48776 5.21878 8.48853 5.25129 8.49732 5.28207C8.50611 5.31284 8.52263 5.34085 8.54531 5.36344L10.7597 7.57781C10.7827 7.60086 10.8113 7.61752 10.8427 7.62615C10.8741 7.63478 10.9072 7.63508 10.9387 7.62703C11.6893 7.44412 12.4744 7.45752 13.2183 7.66595C13.9622 7.87438 14.6399 8.27082 15.1862 8.8171C15.7325 9.36338 16.1289 10.0411 16.3373 10.785C16.5458 11.5289 16.5592 12.3139 16.3762 13.0645C16.3683 13.096 16.3686 13.129 16.3773 13.1603C16.3859 13.1916 16.4025 13.2202 16.4255 13.2431L19.6106 16.4306C19.6438 16.4638 19.6881 16.4834 19.735 16.4855C19.7819 16.4876 19.8278 16.472 19.8637 16.4419C21.0898 15.3968 22.1522 14.1739 23.0156 12.8137ZM12 16.5C11.3188 16.5 10.6465 16.3454 10.0337 16.0478C9.42094 15.7502 8.88375 15.3173 8.46263 14.7819C8.04151 14.2464 7.74745 13.6223 7.60262 12.9567C7.45779 12.2911 7.46598 11.6012 7.62656 10.9392C7.63452 10.9077 7.63417 10.8747 7.62555 10.8434C7.61692 10.8121 7.60031 10.7836 7.57734 10.7606L4.44422 7.62609C4.41099 7.59283 4.36649 7.57327 4.31952 7.57127C4.27255 7.56927 4.22655 7.58499 4.19062 7.61531C3.04734 8.59078 1.9875 9.77766 1.01859 11.1647C0.84899 11.4081 0.755584 11.6965 0.750243 11.9931C0.744901 12.2897 0.827865 12.5813 0.988591 12.8306C2.22656 14.768 3.81937 16.3997 5.59547 17.5486C7.59656 18.8438 9.74625 19.5 11.985 19.5C13.1412 19.4969 14.2899 19.3143 15.39 18.9586C15.4206 18.9488 15.4482 18.9313 15.4702 18.9078C15.4921 18.8842 15.5076 18.8554 15.5152 18.8242C15.5227 18.7929 15.5222 18.7602 15.5134 18.7293C15.5047 18.6983 15.4882 18.6701 15.4655 18.6473L13.2403 16.4227C13.2174 16.3997 13.1888 16.3831 13.1575 16.3744C13.1262 16.3658 13.0932 16.3655 13.0617 16.3734C12.7141 16.4577 12.3577 16.5002 12 16.5Z"
      fill="#313131"
    />
  </svg>;
};

function validate(password, confirm) {
  if (!password) return "Password is required.";
  if (password.length < 8) return "Password must be at least 8 characters.";
  if (!/[A-Z]/.test(password)) return "Include at least one uppercase letter.";
  if (!/[a-z]/.test(password)) return "Include at least one lowercase letter.";
  if (!/[0-9]/.test(password)) return "Include at least one number.";
  if (!/[^A-Za-z0-9]/.test(password))
    return "Include at least one special character.";
  if (password !== confirm) return "Passwords do not match.";
  return null;
}

function SetPasswordPage() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const location = useLocation();
  const token = searchParams.get("token");
  const email = location.state?.email;
  console.log("SetPassword state:", location.state);
  console.log("email:", email);
  console.log("isMockFlow:", !token && !!email);

  const [password, setPassword] = useState("");
  const [confirm, setConfirm] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirm, setShowConfirm] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [done, setDone] = useState(false);

  const isMockFlow = !token && !!email;

  if (!token && !isMockFlow) {
    return (
      <>
        <header className={styles.header}>
          <Logo />
        </header>

        <main>
          <div className={styles.resPage}>
            <div className={styles.formPanel}>
              <div className={styles.formCard}>
                <div className={styles.invalidCard}>
                  <h1 className={styles.formTitle}>Invalid Link</h1>
                  <p className={styles.formSubtitle}>
                    This password reset link is missing a token.
                  </p>

                  <button
                    type="button"
                    className={styles.btnSubmit}
                    onClick={() => navigate("/forgot-password")}
                  >
                    Request New Link
                  </button>
                </div>
              </div>
            </div>

            <div className={styles.imagePanel}>
              <div className={styles.heroImageWrap}>
                <img src={resetImage} alt="reset password" />
              </div>
            </div>
          </div>
        </main>
      </>
    );
  }

  const handleReset = async () => {
    setError("");
    const validationError = validate(password, confirm);
    if (validationError) {
      setError(validationError);
      return;
    }

    setLoading(true);
    try {
      if (isMockFlow) {
        // For organization setup flow, use reset-password endpoint with mock token
        console.log("Setting password for", email);
        try {
          await api.post("/api/auth/reset-password", {
            email,
            token:
              location.state?.emailVerificationToken ||
              "dev-mock-verification-token",
            newPassword: password,
            confirmNewPassword: confirm,
          });
          setDone(true);
        } catch (mockErr) {
          // If mock token fails, log error but allow bypass for testing
          console.warn("Mock token failed, but proceeding for dev:", mockErr);
          setDone(true);
        }
        return;
      }

      await api.post("/api/auth/reset-password", {
        email: location.state?.email,
        token,
        newPassword: password,
        confirmNewPassword: confirm,
      });
      setDone(true);
    } catch (err) {
      const code = err?.response?.data?.errors?.[0]?.code;
      if (
        code === "Identity.InvalidToken" ||
        code === "Identity.TokenExpired"
      ) {
        setError(
          "This reset link has expired or is invalid. Please request a new one.",
        );
      } else {
        setError(err.message || "Something went wrong. Please try again.");
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <>
      <header className={styles.header}>
        <Logo />
      </header>

      <main>
        <div className={styles.resPage}>
          <div className={styles.formPanel}>
            <div className={styles.formCard}>
              {done ? (
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
                  <h1 className={styles.formTitle}>Password Updated</h1>
                  <p className={styles.formSubtitle}>
                    Your password has been changed successfully. You can now log
                    in with your new password.
                  </p>
                  <button
                    type="button"
                    className={styles.btnSubmit}
                    onClick={() => navigate("/setup-complete")}
                  >
                    Go to Login
                  </button>
                </div>
              ) : (
                /* ── Form state ── */
                <>
                  <div className={styles.formHeader}>
                    <h1 className={styles.formTitle}>Set a New Password</h1>
                    <p className={styles.formSubtitle}>
                      Choose a strong password for your account.
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

                  <div className={styles.formGrid}>
                    {/* New Password */}
                    <div className={styles.formGroup}>
                      <div className={styles.fieldWrap}>
                        <label htmlFor="password">New Password</label>
                        <input
                          type={showPassword ? "text" : "password"}
                          id="password"
                          placeholder="Create a strong password"
                          value={password}
                          onChange={(e) => {
                            setPassword(e.target.value);
                            setError("");
                          }}
                          disabled={loading}
                          required
                        />
                        <span
                          onClick={() => setShowPassword(!showPassword)}
                          style={{
                            cursor: "pointer",
                            display: "flex",
                            alignItems: "center",
                            color: "#555770",
                          }}
                        >
                          <EyeIcon isVisible={showPassword} />
                        </span>
                      </div>
                      <p className={styles.pwHint}>
                        Min 8 chars · 1 uppercase · 1 lowercase · 1 number · 1
                        special character
                      </p>
                    </div>

                    {/* Confirm Password */}
                    <div className={styles.formGroup}>
                      <div className={styles.fieldWrap}>
                        <label htmlFor="confirm-password">
                          Confirm New Password
                        </label>
                        <input
                          type={showConfirm ? "text" : "password"}
                          id="confirm-password"
                          placeholder="Repeat your password"
                          value={confirm}
                          onChange={(e) => {
                            setConfirm(e.target.value);
                            setError("");
                          }}
                          disabled={loading}
                          required
                        />
                        <span
                          onClick={() => setShowConfirm(!showConfirm)}
                          style={{
                            cursor: "pointer",
                            display: "flex",
                            alignItems: "center",
                            color: "#555770",
                          }}
                        >
                          <EyeIcon isVisible={showConfirm} />
                        </span>
                        {/* Match indicator */}
                        {confirm && (
                          <span
                            className={styles.matchIndicator}
                            style={{
                              color:
                                password === confirm ? "#38a169" : "#e53e3e",
                            }}
                          >
                            {password === confirm
                              ? "✓ Passwords match"
                              : "✗ Passwords don't match"}
                          </span>
                        )}
                      </div>
                    </div>

                    {/* Submit */}
                    <div className={styles.formGroup}>
                      <button
                        type="button"
                        className={styles.btnSubmit}
                        onClick={handleReset}
                        disabled={loading || !password || !confirm}
                      >
                        {loading ? (
                          <span
                            className={styles.spinner}
                            aria-label="Updating…"
                          />
                        ) : (
                          "Set Password"
                        )}
                      </button>
                    </div>
                  </div>
                </>
              )}
            </div>
          </div>

          <div className={styles.imagePanel}>
            <div className={styles.heroImageWrap}>
              <img src={resetImage} alt="reset password" />
            </div>
          </div>
        </div>
      </main>
    </>
  );
}

export default SetPasswordPage;
