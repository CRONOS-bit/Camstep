(function () {
    "use strict";

    const player = document.querySelector(".global-music");
    if (!player) return;
    const key = "camstep:" + player.dataset.user + ":focus";
    const BREAK_MIN = 5;
    const pill = document.getElementById("focusPill");
    const pillTime = document.getElementById("focusPillTime");

    function read() {
        try { return JSON.parse(localStorage.getItem(key) || "null") || { phase: "idle" }; } catch (e) { return { phase: "idle" }; }
    }
    function write(s) {
        try { localStorage.setItem(key, JSON.stringify(s)); } catch (e) { }
        render();
    }
    function left(s) {
        if (s.pausedLeft != null) return s.pausedLeft;
        return Math.max(0, (s.endsAt || 0) - Date.now());
    }
    function mmss(ms) {
        const total = Math.ceil(ms / 1000);
        const m = Math.floor(total / 60), sec = total % 60;
        return String(m).padStart(2, "0") + ":" + String(sec).padStart(2, "0");
    }
    function token() {
        const input = document.querySelector('input[name="__RequestVerificationToken"]');
        return input ? input.value : "";
    }

    function chime() {
        try {
            const Ctx = window.AudioContext || window.webkitAudioContext;
            if (!Ctx) return;
            const ctx = new Ctx();
            [659.25, 783.99, 987.77].forEach(function (f, i) {
                const o = ctx.createOscillator(), g = ctx.createGain();
                o.type = "sine";
                o.frequency.value = f;
                const t = ctx.currentTime + i * 0.22;
                g.gain.setValueAtTime(0, t);
                g.gain.linearRampToValueAtTime(0.18, t + 0.02);
                g.gain.exponentialRampToValueAtTime(0.0001, t + 1.6);
                o.connect(g).connect(ctx.destination);
                o.start(t);
                o.stop(t + 1.7);
            });
            setTimeout(function () { ctx.close(); }, 2600);
        } catch (e) { }
    }

    function notify(title, body) {
        try {
            if ("Notification" in window && Notification.permission === "granted" && document.hidden)
                new Notification(title, { body: body, tag: "camstep-focus" });
        } catch (e) { }
    }

    function save(s, minutes) {
        const body = new URLSearchParams();
        body.append("__RequestVerificationToken", token());
        body.append("minutes", String(minutes));
        if (s.taskId) body.append("taskId", String(s.taskId));
        return fetch("/Focus/Complete", { method: "POST", credentials: "same-origin", body: body, headers: { "Accept": "application/json" } })
            .then(function (r) { return r.json().catch(function () { return { ok: false, message: "No se pudo guardar la sesión." }; }); })
            .catch(function () { return { ok: false, message: "Sin conexión: no se pudo guardar la sesión." }; });
    }

    function finishFocus(s, minutes) {
        const done = { phase: "done", taskId: s.taskId, taskTitle: s.taskTitle, minutes: minutes, message: "Guardando tu sesión…", saved: false };
        write(done);
        chime();
        notify("¡Sesión de enfoque completa! 🌿", minutes + " minutos" + (s.taskTitle ? " en «" + s.taskTitle + "»" : "") + ".");
        save(s, minutes).then(function (res) {
            const now = read();
            if (now.phase !== "done") return;
            now.message = res.message || (res.ok ? "¡Sesión guardada!" : "No se pudo guardar la sesión.");
            now.saved = !!res.ok;
            write(now);
            document.dispatchEvent(new CustomEvent("camstep:focussaved"));
        });
    }

    function tick() {
        const s = read();
        if ((s.phase === "focus" || s.phase === "break") && s.pausedLeft == null && left(s) <= 0) {
            if (s.phase === "focus") {
                finishFocus(s, s.minutes);
            } else {
                write({ phase: "idle", message: "Descanso terminado. Cuando quieras, otra sesión." });
                chime();
                notify("Descanso terminado", "Cuando quieras, empieza otra sesión de enfoque.");
            }
            return;
        }
        render();
    }

    function render() {
        const s = read();
        const running = s.phase === "focus" || s.phase === "break";
        window.CAMSTEP_FOCUS_RUNNING = s.phase === "focus" && s.pausedLeft == null;

        const onPage = !!document.getElementById("focusPage");
        if (pill) {
            pill.hidden = onPage || !(running || s.phase === "done");
            if (running) pillTime.textContent = (s.phase === "break" ? "Descanso " : "") + mmss(left(s)) + (s.pausedLeft != null ? " ⏸" : "");
            else if (s.phase === "done") pillTime.textContent = "¡Sesión lista!";
        }
        document.body.classList.toggle("focus-calm", onPage && s.phase === "focus" && !!s.calm);
        if (onPage) renderPage(s);
    }

    function $(id) { return document.getElementById(id); }

    function renderPage(s) {
        const running = s.phase === "focus" || s.phase === "break";
        const total = (s.phase === "break" ? BREAK_MIN : (s.minutes || selectedMinutes())) * 60000;
        const ms = running ? left(s) : (s.phase === "done" ? 0 : selectedMinutes() * 60000);
        $("focusTime").textContent = mmss(ms);
        const ring = $("focusRing");
        const len = 2 * Math.PI * 96;
        ring.style.strokeDasharray = String(len);
        ring.style.strokeDashoffset = String(running ? len * (1 - ms / total) : (s.phase === "done" ? 0 : len));
        $("focusTimer").classList.toggle("break", s.phase === "break");
        $("focusTimer").classList.toggle("paused", s.pausedLeft != null);

        $("focusPhase").textContent =
            s.phase === "focus" ? (s.pausedLeft != null ? "En pausa" : "Enfocado") :
            s.phase === "break" ? (s.pausedLeft != null ? "Descanso en pausa" : "Descansando") :
            s.phase === "done" ? "¡Lo lograste!" : (s.message || "Listo para empezar");
        $("focusTaskName").textContent = running || s.phase === "done" ? (s.taskTitle || (s.phase === "break" ? "" : "Enfoque libre")) : selectedTaskTitle();

        $("focusSetup").hidden = running || s.phase === "done";
        $("focusStart").hidden = running || s.phase === "done";
        $("focusPause").hidden = !running;
        $("focusPause").textContent = s.pausedLeft != null ? "Seguir" : "Pausar";
        $("focusFinish").hidden = s.phase !== "focus";
        $("focusCancel").hidden = !running;
        $("focusCancel").textContent = s.phase === "break" ? "Saltar descanso" : "Cancelar";

        const done = $("focusDone");
        done.hidden = s.phase !== "done";
        if (s.phase === "done") {
            $("focusDoneText").textContent = s.message || "";
            $("focusMarkWrap").hidden = !s.taskId || !s.saved || s.marked;
        }
        document.title = running ? mmss(ms) + " · " + (s.phase === "break" ? "Descanso" : "Enfoque") + " - CAMSTEP" : "Modo enfoque - CAMSTEP";
    }

    function selectedMinutes() {
        const r = document.querySelector('input[name="focusMinutes"]:checked');
        return r ? Number(r.value) : 25;
    }
    function selectedTaskTitle() {
        const sel = $("focusTask");
        if (!sel || !sel.value) return "";
        return sel.options[sel.selectedIndex].text.replace(/ · \d{2}\/\d{2}\/\d{4}$/, "");
    }

    function bindPage() {
        const page = $("focusPage");
        if (!page || page.dataset.bound) return;
        page.dataset.bound = "1";

        $("focusStart").addEventListener("click", function () {
            const minutes = selectedMinutes();
            const sel = $("focusTask");
            const s = {
                phase: "focus",
                taskId: sel && sel.value ? Number(sel.value) : null,
                taskTitle: selectedTaskTitle(),
                minutes: minutes,
                endsAt: Date.now() + minutes * 60000,
                pausedLeft: null,
                calm: $("focusCalm").checked
            };
            write(s);
            if ($("focusAmbient").checked && window.camstepPlayer && window.camstepPlayer.playAmbient) window.camstepPlayer.playAmbient();
            try { if ("Notification" in window && Notification.permission === "default") Notification.requestPermission(); } catch (e) { }
        });

        $("focusPause").addEventListener("click", function () {
            const s = read();
            if (s.pausedLeft != null) { s.endsAt = Date.now() + s.pausedLeft; s.pausedLeft = null; }
            else { s.pausedLeft = left(s); }
            write(s);
        });

        $("focusFinish").addEventListener("click", function () {
            const s = read();
            const elapsed = Math.floor((s.minutes * 60000 - left(s)) / 60000);
            if (elapsed < 1) { write({ phase: "idle", message: "Muy cortica para guardarla. ¡Inténtalo de nuevo!" }); return; }
            finishFocus(s, elapsed);
        });

        $("focusCancel").addEventListener("click", function () {
            const s = read();
            if (s.phase === "focus" && !window.confirm("¿Cancelar la sesión? No se guardará.")) return;
            write({ phase: "idle" });
        });

        $("focusSave").addEventListener("click", function () { afterDone(true); });
        $("focusSaveOnly").addEventListener("click", function () { afterDone(false); });

        document.querySelectorAll('input[name="focusMinutes"], #focusTask').forEach(function (el) {
            el.addEventListener("change", render);
        });
    }

    function afterDone(takeBreak) {
        const s = read();
        const mark = $("focusMarkDone") && $("focusMarkDone").checked && !$("focusMarkWrap").hidden && s.taskId;
        const next = function () {
            write(takeBreak
                ? { phase: "break", endsAt: Date.now() + BREAK_MIN * 60000, pausedLeft: null, minutes: BREAK_MIN }
                : { phase: "idle", message: "¡Bien hecho! Una huella más en tu sendero 🐾" });
        };
        if (!mark) { next(); return; }
        const body = new URLSearchParams();
        body.append("__RequestVerificationToken", token());
        body.append("taskId", String(s.taskId));
        fetch("/Focus/MarkTaskDone", { method: "POST", credentials: "same-origin", body: body })
            .then(function () { next(); })
            .catch(function () { next(); });
    }

    bindPage();
    document.addEventListener("camstep:pageload", function () { bindPage(); render(); });
    window.addEventListener("storage", function (e) { if (e.key === key) render(); });
    render();
    setInterval(tick, 500);
})();
