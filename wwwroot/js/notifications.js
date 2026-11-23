(function () {
    "use strict";

    const badge = document.getElementById("bellCount");
    if (!badge) return;

    const userId = document.querySelector(".global-music")?.dataset.user || "0";
    const seenKey = "camstep:" + userId + ":notifSeen";
    const enabledKey = "camstep:" + userId + ":browserNotif";

    function seen() {
        try { return new Set(JSON.parse(localStorage.getItem(seenKey) || "[]")); } catch (e) { return new Set(); }
    }
    function saveSeen(set) {
        try { localStorage.setItem(seenKey, JSON.stringify(Array.from(set).slice(-200))); } catch (e) { }
    }
    function browserEnabled() {
        try { return localStorage.getItem(enabledKey) === "1" && "Notification" in window && Notification.permission === "granted"; } catch (e) { return false; }
    }

    function render(count) {
        badge.textContent = count > 99 ? "99+" : String(count);
        badge.hidden = count === 0;
        const bell = badge.closest("a");
        if (bell) bell.setAttribute("aria-label", "Avisos: " + count + " sin leer");
    }

    function check() {
        fetch("/Notifications/Summary", { credentials: "same-origin", headers: { "Accept": "application/json" } })
            .then(function (r) { return r.ok && (r.headers.get("content-type") || "").includes("json") ? r.json() : null; })
            .then(function (data) {
                if (!data) return;
                render(data.unread);
                const already = seen();
                (data.latest || []).forEach(function (n) {
                    if (already.has(n.id)) return;
                    already.add(n.id);
                    if (browserEnabled()) {
                        const notice = new Notification(n.title, { body: n.message, tag: "camstep-" + n.id });
                        notice.onclick = function () { window.focus(); window.location.href = n.url; };
                    }
                });
                saveSeen(already);
            })
            .catch(function () { });
    }

    check();
    setInterval(check, 60000);

    function showStatus() {
        const button = document.getElementById("enableBrowserNotifications");
        const status = document.getElementById("browserNotifStatus");
        if (!status) return;
        if (!("Notification" in window)) { status.textContent = "Tu navegador no admite avisos."; return; }
        if (Notification.permission === "denied") { status.textContent = "Los avisos están bloqueados en la configuración del navegador para este sitio."; return; }
        status.textContent = browserEnabled() ? "Avisos del navegador activados." : "Avisos del navegador desactivados.";
        if (button) button.textContent = browserEnabled() ? "Desactivar avisos del navegador" : "Activar avisos del navegador";
    }
    document.addEventListener("click", function (event) {
        if (!(event.target instanceof Element) || !event.target.closest("#enableBrowserNotifications")) return;
        (function () {
            if (!("Notification" in window)) { showStatus(); return; }
            if (browserEnabled()) {
                localStorage.setItem(enabledKey, "0");
                showStatus();
                return;
            }
            Notification.requestPermission().then(function (permission) {
                if (permission === "granted") {
                    localStorage.setItem(enabledKey, "1");
                    new Notification("CAMSTEP", { body: "Listo: así verás tus avisos." });
                }
                showStatus();
            });
        })();
    });
    showStatus();

    document.addEventListener("camstep:pageload", function () {
        check();
        showStatus();
    });
})();
