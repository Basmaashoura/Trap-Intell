import { useEffect, useRef } from "react";

export function useScrollReveal() {
  const ref = useRef(null);

  useEffect(() => {
    if (!ref.current) return;

    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((el) => {
          if (el.isIntersecting) {
            el.target.setAttribute("data-visible", "true");
          }
        });
      },
      { threshold: 0.1 },
    );

    const elements = ref.current.querySelectorAll("[data-reveal]");

    elements.forEach((el) => observer.observe(el));

    // Cleanup - remove listeners when component unmounts
    return () => observer.disconnect();
  }, []);

  return ref;
}
