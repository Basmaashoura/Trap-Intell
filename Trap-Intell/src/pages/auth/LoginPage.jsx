// src/pages/auth/LoginPage.jsx
import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import Logo from "../../components/Logo";
import lockImage from "../../assets/images/lock.png";
import styles from "./LoginPage.module.css";
import { useAuth } from "../../context/AuthContext";
import "../../assets/styles/utilities.css";
import "../../assets/styles/normalize.css";

// Backend returns RFC 7807: { detail: "...", title: "...", status: 4xx }
// api.js already extracts errorData.detail → error.message
// So error.message IS the human-readable string from the backend.
function parseApiError(error) {
  const msg = error?.message || "";
  // Map known backend messages to friendlier UI text if needed
  if (msg.includes("Invalid email or password"))
    return "Invalid email or password.";
  if (msg.includes("suspended"))
    return "Your account has been suspended. Please contact support.";
  if (msg.includes("locked") || msg.includes("Too many"))
    return "Too many failed attempts. Please try again later.";
  if (msg.includes("not active") || msg.includes("verify"))
    return "Please verify your email address before logging in.";
  return msg || "Something went wrong. Please try again.";
}

function LoginPage() {
  const [showPassword, setShowPassword] = useState(false);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [rememberMe, setRememberMe] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const { login } = useAuth();
  const navigate = useNavigate();

  const handleLogin = async () => {
    if (!email || !password) {
      setError("Please enter your email and password.");
      return;
    }

    setError("");
    setLoading(true);

    try {
      const result = await login(email, password, rememberMe);

      // Confirmed response: { accessToken, user: { role, emailConfirmed, ... } }
      // No "organization" object, no "status" field
      const user = result?.user;

      if (!user?.emailConfirmed) {
        navigate("/check-email");
        return;
      }
      if (user?.role === "SuperAdmin") {
        navigate("/admin");
        return;
      }

      navigate("/dashboard");
    } catch (err) {
      setError(parseApiError(err));
    } finally {
      setLoading(false);
    }
  };

  const handleKeyDown = (e) => {
    if (e.key === "Enter") handleLogin();
  };

  return (
    <>
      <div className={styles.binaryBg} aria-hidden="true" />

      <header className={styles.header}>
        <Logo />
      </header>

      <main>
        <div className={styles.page}>
          {/* Form panel */}
          <div className={styles.formPanel}>
            <div className={styles.formCard}>
              <div className={styles.formHeader}>
                <h1 className={styles.formTitle}>Welcome back!</h1>
                <p className={styles.formSubtitle}>
                  Please enter your credentials to access the secure portal.
                </p>
              </div>

              <div className={styles.formFlex}>
                {/* API error banner */}
                {error && (
                  <div className={styles.errorBanner} role="alert">
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none">
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

                {/* Email */}
                <div className={styles.formGroup}>
                  <div className={styles.fieldWrap}>
                    <label htmlFor="username">Email</label>
                    <input
                      type="email"
                      id="username"
                      name="username"
                      placeholder="Enter your email address"
                      value={email}
                      onChange={(e) => setEmail(e.target.value)}
                      onKeyDown={handleKeyDown}
                      disabled={loading}
                      required
                    />
                  </div>
                </div>

                {/* Password */}
                <div className={styles.formGroup}>
                  <div className={styles.fieldWrap}>
                    <label htmlFor="password">Password</label>
                    <input
                      type={showPassword ? "text" : "password"}
                      id="password"
                      name="password"
                      placeholder="Enter your password"
                      value={password}
                      onChange={(e) => setPassword(e.target.value)}
                      onKeyDown={handleKeyDown}
                      disabled={loading}
                      required
                    />
                    <svg
                      width="24"
                      height="24"
                      viewBox="0 0 24 24"
                      fill="none"
                      onClick={() => setShowPassword(!showPassword)}
                      style={{ cursor: "pointer" }}
                      aria-label={
                        showPassword ? "Hide password" : "Show password"
                      }
                    >
                      <path
                        d="M20.25 21C20.1515 21.0001 20.0539 20.9808 19.9629 20.9431C19.8719 20.9053 19.7893 20.85 19.7198 20.7801L3.21982 4.28013C3.0851 4.13833 3.01111 3.94952 3.01361 3.75395C3.01612 3.55838 3.09492 3.37152 3.23322 3.23322C3.37152 3.09492 3.55838 3.01612 3.75395 3.01361C3.94952 3.01111 4.13833 3.0851 4.28013 3.21982L20.7801 19.7198C20.885 19.8247 20.9563 19.9583 20.9852 20.1038C21.0141 20.2492 20.9993 20.3999 20.9426 20.5369C20.8858 20.6739 20.7897 20.791 20.6665 20.8735C20.5432 20.9559 20.3983 20.9999 20.25 21ZM11.625 14.8054L9.19732 12.3778C9.18341 12.364 9.16552 12.3549 9.14618 12.3518C9.12684 12.3487 9.10701 12.3517 9.08948 12.3604C9.07194 12.3692 9.05759 12.3832 9.04843 12.4005C9.03927 12.4178 9.03577 12.4376 9.03841 12.457C9.13642 13.0868 9.43215 13.6692 9.88286 14.1199C10.3336 14.5706 10.9159 14.8663 11.5458 14.9643C11.5652 14.967 11.5849 14.9635 11.6022 14.9543C11.6196 14.9452 11.6336 14.9308 11.6423 14.9133C11.651 14.8957 11.6541 14.8759 11.651 14.8566C11.6479 14.8372 11.6388 14.8194 11.625 14.8054ZM12.375 9.1945L14.8064 11.625C14.8203 11.639 14.8382 11.6482 14.8576 11.6514C14.8771 11.6547 14.897 11.6517 14.9147 11.6429C14.9323 11.6341 14.9467 11.62 14.9559 11.6026C14.9651 11.5851 14.9685 11.5653 14.9658 11.5458C14.868 10.9151 14.572 10.3319 14.1208 9.88059C13.6695 9.4293 13.0863 9.13336 12.4556 9.0356C12.4361 9.03258 12.4161 9.03582 12.3985 9.04484C12.3809 9.05386 12.3666 9.06821 12.3577 9.08583C12.3488 9.10346 12.3456 9.12345 12.3487 9.14297C12.3518 9.16249 12.361 9.18052 12.375 9.1945Z"
                        fill="#313131"
                      />
                      <path
                        d="M23.0156 12.8137C23.1708 12.5702 23.2529 12.2872 23.252 11.9984C23.2512 11.7096 23.1675 11.4271 23.0109 11.1844C21.7706 9.26625 20.1614 7.63688 18.3577 6.47203C16.3594 5.18203 14.1562 4.5 11.985 4.5C10.8404 4.50157 9.7035 4.6882 8.61843 5.05266C8.58807 5.06276 8.56079 5.08046 8.5392 5.10409C8.51761 5.12772 8.50242 5.15647 8.49509 5.18763C8.48776 5.21878 8.48853 5.25129 8.49732 5.28207C8.50611 5.31284 8.52263 5.34085 8.54531 5.36344L10.7597 7.57781C10.7827 7.60086 10.8113 7.61752 10.8427 7.62615C10.8741 7.63478 10.9072 7.63508 10.9387 7.62703C11.6893 7.44412 12.4744 7.45752 13.2183 7.66595C13.9622 7.87438 14.6399 8.27082 15.1862 8.8171C15.7325 9.36338 16.1289 10.0411 16.3373 10.785C16.5458 11.5289 16.5592 12.3139 16.3762 13.0645C16.3683 13.096 16.3686 13.129 16.3773 13.1603C16.3859 13.1916 16.4025 13.2202 16.4255 13.2431L19.6106 16.4306C19.6438 16.4638 19.6881 16.4834 19.735 16.4855C19.7819 16.4876 19.8278 16.472 19.8637 16.4419C21.0898 15.3968 22.1522 14.1739 23.0156 12.8137ZM12 16.5C11.3188 16.5 10.6465 16.3454 10.0337 16.0478C9.42094 15.7502 8.88375 15.3173 8.46263 14.7819C8.04151 14.2464 7.74745 13.6223 7.60262 12.9567C7.45779 12.2911 7.46598 11.6012 7.62656 10.9392C7.63452 10.9077 7.63417 10.8747 7.62555 10.8434C7.61692 10.8121 7.60031 10.7836 7.57734 10.7606L4.44422 7.62609C4.41099 7.59283 4.36649 7.57327 4.31952 7.57127C4.27255 7.56927 4.22655 7.58499 4.19062 7.61531C3.04734 8.59078 1.9875 9.77766 1.01859 11.1647C0.84899 11.4081 0.755584 11.6965 0.750243 11.9931C0.744901 12.2897 0.827865 12.5813 0.988591 12.8306C2.22656 14.768 3.81937 16.3997 5.59547 17.5486C7.59656 18.8438 9.74625 19.5 11.985 19.5C13.1412 19.4969 14.2899 19.3143 15.39 18.9586C15.4206 18.9488 15.4482 18.9313 15.4702 18.9078C15.4921 18.8842 15.5076 18.8554 15.5152 18.8242C15.5227 18.7929 15.5222 18.7602 15.5134 18.7293C15.5047 18.6983 15.4882 18.6701 15.4655 18.6473L13.2403 16.4227C13.2174 16.3997 13.1888 16.3831 13.1575 16.3744C13.1262 16.3658 13.0932 16.3655 13.0617 16.3734C12.7141 16.4577 12.3577 16.5002 12 16.5Z"
                        fill="#313131"
                      />
                    </svg>
                  </div>
                </div>

                {/* Remember me + Forgot password */}
                <div className={`${styles.formGroup} ${styles.rememberForgot}`}>
                  <label htmlFor="remember" className={styles.checkboxLabel}>
                    <input
                      type="checkbox"
                      id="remember"
                      name="remember"
                      checked={rememberMe}
                      onChange={(e) => setRememberMe(e.target.checked)}
                      disabled={loading}
                    />
                    Remember me
                  </label>
                  <Link to="/forgot-password">Forgot password?</Link>
                </div>

                {/* Login button */}
                <div className={styles.btnRow}>
                  <button
                    type="button"
                    className={styles.btnPrimary}
                    onClick={handleLogin}
                    disabled={loading || !email || !password}
                  >
                    {loading ? (
                      <span
                        className={styles.spinner}
                        aria-label="Logging in…"
                      />
                    ) : (
                      "Login"
                    )}
                  </button>
                </div>

                {/* Sign up link */}
                <p className={styles.loginRow}>
                  Don't have an account? <Link to="/signup">Sign up</Link>
                </p>

                {/* Divider */}
                <div className={styles.divider}>
                  <span>or login with</span>
                </div>

                {/* Social login (UI only) */}
                <div className={styles.socialOptions}>
                  <button
                    type="button"
                    className={styles.btnSocial}
                    title="Facebook"
                  >
                    <svg width="24" height="24" viewBox="0 0 24 24" fill="none">
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
                    <svg width="24" height="24" viewBox="0 0 24 24" fill="none">
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
                    <svg width="24" height="24" viewBox="0 0 24 24" fill="none">
                      <path
                        d="M17.5172 12.5555C17.5078 10.957 18.232 9.75234 19.6945 8.86406C18.8766 7.69219 17.6391 7.04766 16.0078 6.92344C14.4633 6.80156 12.7734 7.82344 12.1547 7.82344C11.5008 7.82344 10.0055 6.96563 8.82891 6.96563C6.40078 7.00313 3.82031 8.90156 3.82031 12.7641C3.82031 13.9055 4.02891 15.0844 4.44609 16.2984C5.00391 17.8969 7.01484 21.8133 9.1125 21.75C10.2094 21.7242 10.9852 20.9719 12.4125 20.9719C13.7977 20.9719 14.5148 21.75 15.7383 21.75C17.8547 21.7195 19.6734 18.1594 20.2031 16.5563C17.3648 15.218 17.5172 12.6375 17.5172 12.5555ZM15.0539 5.40703C16.2422 3.99609 16.1344 2.71172 16.0992 2.25C15.0492 2.31094 13.8352 2.96484 13.1437 3.76875C12.382 4.63125 11.9344 5.69766 12.0305 6.9C13.1648 6.98672 14.2008 6.40313 15.0539 5.40703Z"
                        fill="#313131"
                      />
                    </svg>
                  </button>
                </div>
              </div>
            </div>
          </div>

          {/* Image panel */}
          <div className={styles.imagePanel}>
            <div className={styles.heroImageWrap}>
              <img src={lockImage} alt="lock image" />
            </div>
          </div>
        </div>
      </main>
    </>
  );
}

export default LoginPage;
