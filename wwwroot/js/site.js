(function () {
    try {
        const legacy = "vidaenorden";
        const keys = [];
        for (let i = 0; i < localStorage.length; i++) keys.push(localStorage.key(i));
        keys.forEach(function (key) {
            if (!key || key.indexOf(legacy) !== 0) return;
            if (key.indexOf(legacy + ":") === 0) {
                const next = "camstep:" + key.slice(legacy.length + 1);
                if (localStorage.getItem(next) === null) localStorage.setItem(next, localStorage.getItem(key));
            }
            localStorage.removeItem(key);
        });
    } catch (e) { }
})();

(function () {
    "use strict";

    const toggle = document.querySelector(".menu-toggle");
    const nav = document.getElementById("mainNav");

    if (toggle && nav) {
        toggle.addEventListener("click", function () {
            const open = nav.classList.toggle("open");
            toggle.setAttribute("aria-expanded", open ? "true" : "false");
        });
    }

    document.addEventListener("submit", function (event) {
        const form = event.target;
        if (form instanceof HTMLFormElement && form.dataset.confirm) {
            if (!window.confirm(form.dataset.confirm)) {
                event.preventDefault();
            }
        }
    });

    document.addEventListener("submit", function (event) {
        const form = event.target;
        if (!(form instanceof HTMLFormElement) || event.defaultPrevented) return;
        const button = form.querySelector('button[type="submit"]');
        if (button) {
            setTimeout(function () { button.disabled = true; }, 0);
        }
    });
    const intro = document.getElementById("intro");
    const root = document.documentElement;
    if (intro && root.classList.contains("intro-on")) {
        const finish = function () {
            root.classList.remove("intro-on", "intro-reduced");
        };
        intro.addEventListener("animationend", function (event) {
            if (event.target === intro) finish();
        });
        intro.addEventListener("click", finish);
        document.addEventListener("keydown", function onKey() {
            document.removeEventListener("keydown", onKey);
            finish();
        });
        setTimeout(finish, 2500);
    }

    const reduceMotion = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    document.addEventListener("click", function (event) {
        if (window.CAMSTEP_SOFTNAV) return;
        if (reduceMotion || event.defaultPrevented || event.button !== 0 ||
            event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return;
        const link = event.target instanceof Element ? event.target.closest(".topbar a, footer a") : null;
        if (!link || link.target || link.hasAttribute("download")) return;
        const url = new URL(link.href, location.href);
        if (url.origin !== location.origin || (url.pathname === location.pathname && url.hash)) return;
        event.preventDefault();
        document.body.classList.add("leaving");
        setTimeout(function () { location.href = url.href; }, 100);
    });
    window.addEventListener("pageshow", function () { document.body.classList.remove("leaving"); });

    document.addEventListener("click", function (event) {
        document.querySelectorAll("details.nav-more[open]").forEach(function (d) {
            if (!(event.target instanceof Node) || !d.contains(event.target) || (event.target instanceof Element && event.target.closest(".nav-more-menu a"))) d.open = false;
        });
    });
    document.addEventListener("keydown", function (event) {
        if (event.key !== "Escape") return;
        document.querySelectorAll("details.nav-more[open]").forEach(function (d) { d.open = false; d.querySelector("summary")?.focus(); });
    });

    if ("serviceWorker" in navigator && (location.protocol === "https:" || location.hostname === "localhost" || location.hostname === "127.0.0.1")) {
        window.addEventListener("load", function () {
            navigator.serviceWorker.register("/sw.js").catch(function () { });
        });
    }
    let installPrompt = null;
    function renderInstall() {
        const button = document.getElementById("installApp");
        const status = document.getElementById("installStatus");
        if (!button) return;
        const installed = window.matchMedia("(display-mode: standalone)").matches || window.navigator.standalone === true;
        button.hidden = installed || !installPrompt;
        if (status) status.textContent = installed
            ? "✓ Ya estás usando CAMSTEP como app."
            : installPrompt ? "" : "Si no ves el botón, usa las instrucciones de abajo según tu equipo.";
    }
    window.addEventListener("beforeinstallprompt", function (event) {
        event.preventDefault();
        installPrompt = event;
        renderInstall();
    });
    window.addEventListener("appinstalled", function () { installPrompt = null; renderInstall(); });
    document.addEventListener("click", function (event) {
        if (!(event.target instanceof Element) || !event.target.closest("#installApp") || !installPrompt) return;
        installPrompt.prompt();
        installPrompt.userChoice.finally(function () { installPrompt = null; renderInstall(); });
    });
    document.addEventListener("camstep:pageload", renderInstall);
    renderInstall();

    document.addEventListener("click", function (event) {
        if (event.target instanceof Element && event.target.closest("[data-print]")) window.print();
    });

    window.addEventListener("pageshow", function () {
        document.querySelectorAll('button[type="submit"][disabled]').forEach(function (b) { b.disabled = false; });
    });
})();
