// Sticky navbar shadow
window.addEventListener("scroll", () => {
  document
    .getElementById("navbar")
    .classList.toggle("scrolled", window.scrollY > 20);
});
// Mobile menu
document.getElementById("hamburger").addEventListener("click", function () {
  this.classList.toggle("open");
  document.querySelector(".nav-links").classList.toggle("open");
});
// Pricing toggle
function setPricing(type) {
  document
    .getElementById("btn-monthly")
    .classList.toggle("active", type === "monthly");
  document
    .getElementById("btn-yearly")
    .classList.toggle("active", type === "yearly");
  document.querySelectorAll(".price-num").forEach((el) => {
    el.textContent = el.dataset[type];
  });
}
// Scroll reveal
const observer = new IntersectionObserver(
  (entries) => {
    entries.forEach((e) => {
      if (e.isIntersecting) e.target.classList.add("visible");
    });
  },
  { threshold: 0.1 },
);
document
  .querySelectorAll(
    ".stat-card, .mv-card, .value-card, .service-card, .feature-card, .price-card",
  )
  .forEach((el) => observer.observe(el));
