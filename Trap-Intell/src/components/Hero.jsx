import { Link } from "react-router-dom";
import styles from "./Hero.module.css";

function Hero() {
  return (
    <section className={styles.hero} id="home">
      <div className={styles.heroBgGrid} aria-hidden="true"></div>
      <div className={styles.heroContainer}>
        <div className={styles.heroText}>
          <h1 className={styles.heroTitle}>
            Advanced Threat
            <br />
            <span className={styles.heroAccent}>_Deception Platform</span>
          </h1>
          <p className={styles.heroDesc}>
            Deploy intelligent honeypots in seconds. Monitor global attack
            vectors in real-time. Neutralize threats before they breach your
            perimeter.
          </p>
          <div className={styles.heroBtns}>
            <Link to="/signup" className={styles.btnHeroPrimary}>
              Initialize System
            </Link>
            <a href="#features" className={styles.btnHeroOutline}>
              View Documentation
            </a>
          </div>
          <div className={styles.heroCompat}>
            <span>Compatible with</span>
            {/* Apple */}
            <svg width="18" height="18" viewBox="0 0 24 24" fill="white">
              <path d="M12.152 6.896c-.948 0-2.415-1.078-3.96-1.04-2.04.027-3.91 1.183-4.961 3.014-2.117 3.675-.54 9.103 1.519 12.09 1.013 1.454 2.208 3.09 3.792 3.039 1.52-.065 2.09-.987 3.935-.987 1.831 0 2.35.987 3.96.948 1.637-.026 2.676-1.48 3.676-2.948 1.156-1.688 1.636-3.325 1.662-3.415-.039-.013-3.182-1.221-3.22-4.857-.026-3.04 2.48-4.494 2.597-4.559-1.429-2.09-3.623-2.324-4.39-2.376-2-.156-3.675 1.09-4.61 1.09zM15.53 3.83c.843-1.012 1.4-2.427 1.245-3.83-1.207.052-2.662.805-3.532 1.818-.78.896-1.454 2.338-1.273 3.714 1.338.104 2.715-.688 3.559-1.701" />
            </svg>
            {/* Windows */}
            <svg width="18" height="18" viewBox="0 0 24 24" fill="white">
              <path d="M0 3.449L9.75 2.1v9.451H0m10.949-9.602L24 0v11.4H10.949M0 12.6h9.75v9.451L0 20.699M10.949 12.6H24V24l-12.9-1.801" />
            </svg>
          </div>
        </div>
      </div>
    </section>
  );
}

export default Hero;
