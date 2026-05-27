(() => {
  const sidebar = document.querySelector("[data-admin-sidebar]");
  const toggle = document.querySelector("[data-admin-sidebar-toggle]");

  if (sidebar && toggle) {
    toggle.addEventListener("click", () => {
      sidebar.classList.toggle("is-open");
    });
  }

  const path = window.location.pathname.toLowerCase();
  document.querySelectorAll(".admin-nav a[href]").forEach((link) => {
    const href = link.getAttribute("href")?.toLowerCase();

    if (href && href !== "/" && path.startsWith(href)) {
      link.classList.add("is-active");
    }
  });
})();
