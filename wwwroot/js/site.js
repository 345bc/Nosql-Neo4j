// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
"use strict";
const menu = document.querySelector(".menu-toggle");
menu?.addEventListener("click", () => {
  const open = menu.getAttribute("aria-expanded") !== "true";
  menu.setAttribute("aria-expanded", String(open));
  document.querySelector("#main-nav")?.classList.toggle("open", open);
});
for (const form of document.querySelectorAll("[data-loading-form]"))
  form.addEventListener("submit", () => {
    if (!form.checkValidity()) return;
    form.setAttribute("aria-busy", "true");
    const button = form.querySelector('button[type="submit"]');
    if (button) {
      button.textContent = "Đang tải…";
      button.disabled = true;
    }
  });
window.addEventListener("pageshow", () => {
  for (const form of document.querySelectorAll("[data-loading-form]")) {
    form.removeAttribute("aria-busy");
    const button = form.querySelector('button[type="submit"]');
    if (button) {
      button.disabled = false;
      button.textContent = form.classList.contains("graph-form")
        ? "Tìm đường đi"
        : "Tìm kiếm";
    }
  }
});
