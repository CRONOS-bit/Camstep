(function () {
    "use strict";

    if (!window.fetch || !window.DOMParser || !history.pushState) return;

    const player = document.querySelector(".global-music");
    if (!player) return;
    const userId = player.dataset.user;
    const reduceMotion = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;

    window.CAMSTEP_SOFTNAV = true;

    const SKIP = /\/(Export|Stream|Background)(\/|\?|$)|^\/Account\//i;

    let controller = null;

    function sameApp(url) {
        return url.origin === location.origin && !SKIP.test(url.pathname);
    }

    function fullNavigation(url) {
        location.href = url;
    }

    function saveScroll() {
        try {
            history.replaceState(Object.assign({}, history.state, { camstep: true, scroll: window.scrollY }), "");
        } catch (e) { }
    }

    function runScripts(container) {
        container.querySelectorAll("script").forEach(function (old) {
            const s = document.createElement("script");
            Array.from(old.attributes).forEach(function (a) { s.setAttribute(a.name, a.value); });
            s.textContent = old.textContent;
            old.replaceWith(s);
        });
    }

    function restartTrail() {
        const trail = document.querySelector(".nav-trail");
        if (trail) trail.replaceWith(trail.cloneNode(true));
    }

    function syncNav(doc) {
        const oldItems = document.querySelectorAll("#mainNav a");
        const newItems = doc.querySelectorAll("#mainNav a");
        if (oldItems.length !== newItems.length) return;
        oldItems.forEach(function (a, i) {
            const b = newItems[i];
            a.className = b.className;
            if (!a.classList.contains("bell")) a.innerHTML = b.innerHTML;
        });
        const oldMore = document.querySelector("#mainNav .nav-more");
        const newMore = doc.querySelector("#mainNav .nav-more");
        if (oldMore && newMore) { oldMore.className = newMore.className; oldMore.open = false; }
        const nav = document.getElementById("mainNav");
        const toggle = document.querySelector(".menu-toggle");
        if (nav) nav.classList.remove("open");
        if (toggle) toggle.setAttribute("aria-expanded", "false");
    }

    function syncPlayerSettings(doc) {
        const next = doc.querySelector(".global-music");
        if (!next) return;
        let changed = false;
        ["ambient", "ambientVolume"].forEach(function (k) {
            if (player.dataset[k] !== next.dataset[k]) {
                player.dataset[k] = next.dataset[k];
                changed = true;
            }
        });
        if (changed && window.camstepPlayer) window.camstepPlayer.applySettings();
    }

    function swap(doc, url, push, scrollTo) {
        const newMain = doc.getElementById("contenido");
        const oldMain = document.getElementById("contenido");
        if (!newMain || !oldMain) return false;

        document.title = doc.title;
        document.body.className = doc.body.className;
        document.body.dataset.quotes = doc.body.dataset.quotes || "0";
        const style = doc.body.getAttribute("style");
        if (style) document.body.setAttribute("style", style); else document.body.removeAttribute("style");

        syncNav(doc);
        syncPlayerSettings(doc);

        const imported = document.importNode(newMain, true);
        oldMain.replaceWith(imported);
        runScripts(imported);

        const oldScripts = document.getElementById("page-scripts");
        const newScripts = doc.getElementById("page-scripts");
        if (oldScripts) {
            oldScripts.innerHTML = newScripts ? newScripts.innerHTML : "";
            runScripts(oldScripts);
        }

        if (push) history.pushState({ camstep: true, scroll: 0 }, "", url);
        restartTrail();

        const hash = new URL(url, location.href).hash;
        const target = hash && document.getElementById(decodeURIComponent(hash.slice(1)));
        if (typeof scrollTo === "number") window.scrollTo(0, scrollTo);
        else if (target) target.scrollIntoView();
        else window.scrollTo(0, 0);

        const auto = imported.querySelector("[autofocus]");
        if (auto) auto.focus();
        else {
            imported.setAttribute("tabindex", "-1");
            imported.focus({ preventScroll: true });
        }

        document.dispatchEvent(new CustomEvent("camstep:pageload", { detail: { url: url } }));
        return true;
    }

    function navigate(url, options) {
        options = options || {};
        if (controller) controller.abort();
        controller = new AbortController();
        if (options.push !== false) saveScroll();
        if (!reduceMotion) document.body.classList.add("leaving");

        const started = Date.now();
        fetch(url, {
            method: options.method || "GET",
            body: options.body,
            credentials: "same-origin",
            headers: { "Accept": "text/html", "X-Requested-With": "camstep-softnav" },
            signal: controller.signal
        }).then(function (response) {
            const type = response.headers.get("content-type") || "";
            const finalUrl = response.url || url;
            if (!type.includes("text/html")) {
                document.body.classList.remove("leaving");
                if (options.method === "POST") fullNavigation(finalUrl); else fullNavigation(url);
                return null;
            }
            return response.text().then(function (html) { return { html: html, url: finalUrl, status: response.status }; });
        }).then(function (result) {
            if (!result) return;
            const doc = new DOMParser().parseFromString(result.html, "text/html");
            const next = doc.querySelector(".global-music");
            if (!next || next.dataset.user !== userId) {
                fullNavigation(result.url);
                return;
            }
            const keepHash = new URL(url, location.href).hash;
            const shownUrl = result.url + (keepHash && !result.url.includes("#") ? keepHash : "");
            const wait = reduceMotion ? 0 : Math.max(0, 90 - (Date.now() - started));
            setTimeout(function () {
                if (!swap(doc, shownUrl, options.push !== false, options.scroll)) fullNavigation(result.url);
                document.body.classList.remove("leaving");
            }, wait);
        }).catch(function (error) {
            document.body.classList.remove("leaving");
            if (error && error.name === "AbortError") return;
            if (options.fallback) options.fallback(); else fullNavigation(url);
        });
    }

    document.addEventListener("click", function (event) {
        if (event.defaultPrevented || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return;
        const link = event.target instanceof Element ? event.target.closest("a[href]") : null;
        if (!link || link.target || link.hasAttribute("download") || link.dataset.reload !== undefined) return;
        const url = new URL(link.href, location.href);
        if (!sameApp(url)) return;
        if (url.pathname === location.pathname && url.search === location.search && url.hash) return;
        event.preventDefault();
        navigate(url.href);
    });

    document.addEventListener("submit", function (event) {
        if (event.defaultPrevented) return;
        const form = event.target;
        if (!(form instanceof HTMLFormElement) || form.target || form.dataset.reload !== undefined) return;
        const submitter = event.submitter || null;
        const action = new URL((submitter && submitter.getAttribute("formaction")) || form.getAttribute("action") || location.href, location.href);
        if (!sameApp(action)) return;
        const method = ((submitter && submitter.getAttribute("formmethod")) || form.getAttribute("method") || "GET").toUpperCase();

        let data;
        try { data = new FormData(form, submitter); } catch (e) {
            data = new FormData(form);
            if (submitter && submitter.name) data.append(submitter.name, submitter.value);
        }

        event.preventDefault();
        if (method === "GET") {
            action.search = new URLSearchParams(data).toString();
            navigate(action.href);
            return;
        }
        const multipart = (form.getAttribute("enctype") || "").toLowerCase() === "multipart/form-data";
        navigate(action.href, {
            method: "POST",
            body: multipart ? data : new URLSearchParams(data),
            fallback: function () { HTMLFormElement.prototype.submit.call(form); }
        });
    });

    try { history.replaceState(Object.assign({}, history.state, { camstep: true, scroll: window.scrollY }), ""); } catch (e) { }
    window.addEventListener("popstate", function (event) {
        if (!event.state || !event.state.camstep) return;
        navigate(location.href, { push: false, scroll: event.state.scroll || 0 });
    });
})();
