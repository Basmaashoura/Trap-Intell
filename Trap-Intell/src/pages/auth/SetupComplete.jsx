import styles from "./SetupComplete.module.css";

export default function SetupComplete() {
  return (
    <div className={styles.container}>
      {/* Background */}
      <div className={styles.binaryBg} aria-hidden="true" />

      {/* Main */}
      <main className={styles.page}>
        {/* Check Circle */}
        <div className={styles.checkCircle}>
          <svg
            width="80"
            height="80"
            viewBox="0 0 80 80"
            fill="none"
            xmlns="http://www.w3.org/2000/svg"
          >
            <circle cx="40" cy="40" r="40" fill="#4044e4" fillOpacity="0.15" />

            <circle
              cx="40"
              cy="40"
              r="32"
              stroke="#4044e4"
              strokeWidth="2.5"
              fill="none"
            />

            <path
              className={styles.checkPath}
              d="M24 40L34 51L56 29"
              stroke="#4044e4"
              strokeWidth="4"
              strokeLinecap="round"
              strokeLinejoin="round"
            />
          </svg>
        </div>

        {/* Text */}
        <h1 className={styles.title}>Setup Complete</h1>

        <p className={styles.subtitle}>
          Your adaptive AI system is now active.
        </p>
      </main>
    </div>
  );
}
