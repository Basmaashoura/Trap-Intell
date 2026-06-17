import { useEffect, useState } from "react";
// import "../assets/styles/homepage.css";
import styles from "./Header.module.css";
import Button from "./Button";
import Logo from "./Logo";

function Header() {
  const [isScrolled, setIsScrolled] = useState(false);

  useEffect(() => {
    const handleScroll = () => {
      setIsScrolled(window.scrollY > 20);
    };

    window.addEventListener("scroll", handleScroll, { passive: true });
    return () => window.removeEventListener("scroll", handleScroll);
  }, []);

  return (
    <nav
      className={`${styles.navbar} ${isScrolled ? styles.scrolled : ""}`}
      id="navbar"
    >
      <div className={styles.navContainer}>
        <Logo />
        {/* <!-- Links --> */}
        <ul className={styles.navLinks}>
          <li>
            <a href="#features">Features</a>
          </li>
          <li>
            <a href="#about">About Us</a>
          </li>
          <li>
            <a href="#services">Services</a>
          </li>
          <li>
            <a href="#pricing">Pricing</a>
          </li>
        </ul>
        {/* <!-- Right --> */}
        <div className={styles.navRight}>
          <button className={styles.navGlobe} aria-label="Language">
            <svg
              width="18"
              height="18"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
              strokeLinecap="round"
              strokeLinejoin="round"
            >
              <circle cx="12" cy="12" r="10" />
              <line x1="2" y1="12" x2="22" y2="12" />
              <path d="M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z" />
            </svg>
          </button>
          <a href="login.html" className={styles.btnTerminal}>
            Access Terminal
          </a>
        </div>
        {/* <!-- Hamburger --> */}
        {/* <Button className="nav-hamburger" id="hamburger" aria-label="Menu"> </Button>*/}

        <button
          className={styles.navHamburger}
          id="hamburger"
          aria-label="Menu"
        >
          <span></span>
          <span></span>
          <span></span>
        </button>
      </div>
    </nav>
  );
}

export default Header;
