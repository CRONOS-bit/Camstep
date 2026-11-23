(function () {
    "use strict";

    const toast = document.getElementById("quoteToast");
    const player = document.querySelector(".global-music");
    if (!toast || !player) return;

    const EVERY = 15 * 60 * 1000;
    const SHOW = 10 * 1000;
    const FIRST = 60 * 1000;
    const key = "camstep:" + player.dataset.user + ":quoteNext";

    const text = document.getElementById("quoteText");
    const author = document.getElementById("quoteAuthor");
    let hideTimer = null;
    let remaining = SHOW;
    let shownAt = 0;
    let busy = false;

    function enabled() { return document.body.dataset.quotes === "1"; }

    function nextTime() {
        try { return Number(localStorage.getItem(key)) || 0; } catch (e) { return 0; }
    }
    function setNext(ms) {
        try { localStorage.setItem(key, String(ms)); } catch (e) { }
    }

    function token() {
        const input = document.querySelector('input[name="__RequestVerificationToken"]');
        return input ? input.value : "";
    }

    function hide() {
        clearTimeout(hideTimer);
        toast.classList.remove("show");
        setTimeout(function () { if (!toast.classList.contains("show")) toast.hidden = true; }, 400);
    }

    function startHideTimer(ms) {
        clearTimeout(hideTimer);
        shownAt = Date.now();
        remaining = ms;
        toast.style.setProperty("--quote-ms", ms + "ms");
        toast.classList.remove("paused");
        hideTimer = setTimeout(hide, ms);
    }

    function pause() {
        if (!toast.classList.contains("show")) return;
        clearTimeout(hideTimer);
        remaining = Math.max(1500, remaining - (Date.now() - shownAt));
        toast.classList.add("paused");
    }

    function resume() {
        if (!toast.classList.contains("show") || !toast.classList.contains("paused")) return;
        startHideTimer(remaining);
    }

    function show(q) {
        text.textContent = q.text;
        author.textContent = q.author === "CAMSTEP" ? "— CAMSTEP 🦝" : "— " + q.author;
        toast.hidden = false;
        toast.classList.remove("show");
        void toast.offsetWidth;
        toast.classList.add("show");
        startHideTimer(SHOW);
    }

    function fetchAndShow() {
        if (busy) return;
        busy = true;
        const body = new URLSearchParams();
        body.append("__RequestVerificationToken", token());
        fetch("/Quotes/Next", {
            method: "POST",
            credentials: "same-origin",
            headers: { "Accept": "application/json" },
            body: body
        }).then(function (r) {
            return r.status === 200 && (r.headers.get("content-type") || "").includes("json") ? r.json() : null;
        }).then(function (q) {
            if (q && q.text) show(q);
        }).catch(function () { })
          .finally(function () { busy = false; });
    }

    function tick() {
        if (!enabled() || document.hidden || window.CAMSTEP_FOCUS_RUNNING) return;
        const now = Date.now();
        const next = nextTime();
        if (!next) { setNext(now + FIRST); return; }
        if (now < next) return;
        setNext(now + EVERY);
        fetchAndShow();
    }

    toast.addEventListener("mouseenter", pause);
    toast.addEventListener("mouseleave", resume);
    toast.addEventListener("focusin", pause);
    toast.addEventListener("focusout", resume);
    document.getElementById("quoteClose")?.addEventListener("click", hide);
    document.addEventListener("keydown", function (e) {
        if (e.key === "Escape" && toast.classList.contains("show")) hide();
    });

    window.CAMSTEP_quoteNow = function () { setNext(Date.now() + EVERY); fetchAndShow(); };

    tick();
    setInterval(tick, 20 * 1000);
    document.addEventListener("visibilitychange", tick);
})();
