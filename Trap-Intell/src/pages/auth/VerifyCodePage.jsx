import { useState } from "react";
import { Link, useNavigate, useLocation } from "react-router-dom";
import Logo from "../../components/Logo";
import verifyImage from "../../assets/Images/passwordf.png";
import styles from "./VerifyCodePage.module.css";

function VerifyCodePage() {
  const navigate = useNavigate();
  const location = useLocation();
  console.log("Verify state:", location.state);

  const [code, setCode] = useState("");
  const [error, setError] = useState("");

  const handleVerify = (e) => {
    e.preventDefault();
    setError("");

    // DEV ONLY
    if (code.length === 6) {
      navigate("/set-password", {
        state: {
          email: location.state?.email,
        },
        replace: true,
      });
      return;
    }

    setError("Please enter a valid 6-digit code.");
  };

  return (
    <>
      <header className={styles.header}>
        <Logo />
      </header>

      <main>
        <div className={styles.page}>
          {/* left — form */}
          <div className={styles.leftPanel}>
            <Link to="/forgot-password" className={styles.backLink}>
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
              <div className={styles.formHeader}>
                <h1 className={styles.formTitle}>Verify code</h1>
                <p className={styles.formSubtitle}>
                  An authentication code has been sent to your email.
                </p>
              </div>

              <form onSubmit={handleVerify}>
                {/* code input */}
                <div className={styles.formGroup}>
                  <div className={styles.fieldWrap}>
                    <label htmlFor="code">Enter code</label>
                    <input
                      type="text"
                      id="code"
                      name="code"
                      placeholder="Enter the code"
                      value={code}
                      onChange={(e) => setCode(e.target.value)}
                      maxLength={6}
                    />
                  </div>
                </div>

                {error && (
                  <div className={styles.formGroup}>
                    <p
                      style={{
                        color: "#dc2626",
                        fontSize: "0.875rem",
                        margin: 0,
                      }}
                    >
                      {error}
                    </p>
                  </div>
                )}

                {/* resend */}
                <div className={styles.formGroup}>
                  <p className={styles.resendLink}>
                    Didn't receive a code? <a href="#">Resend code</a>
                  </p>
                </div>

                {/* submit */}
                <div className={styles.formGroup}>
                  <button type="submit" className={styles.btnSubmit}>
                    Verify
                  </button>
                </div>
              </form>
            </div>
          </div>

          {/* right — image */}
          <div className={styles.rightPanel}>
            <div className={styles.heroImageWrap}>
              <img src={verifyImage} alt="verify code illustration" />
            </div>
          </div>
        </div>
      </main>
    </>
  );
}

export default VerifyCodePage;
