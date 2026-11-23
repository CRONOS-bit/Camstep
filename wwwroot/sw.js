const VERSION = "camstep-v1";
const OFFLINE = "/offline.html";
const PRECACHE = [OFFLINE, "/favicon.svg", "/img/icon-192.png", "/img/camstep-logo.svg",
    "/fonts/quicksand-latin-700-normal.woff2", "/fonts/inter-latin-400-normal.woff2"];

self.addEventListener("install", function (event) {
    event.waitUntil(caches.open(VERSION).then(function (c) { return c.addAll(PRECACHE); }).then(function () { return self.skipWaiting(); }));
});

self.addEventListener("activate", function (event) {
    event.waitUntil(caches.keys().then(function (keys) {
        return Promise.all(keys.filter(function (k) { return k !== VERSION; }).map(function (k) { return caches.delete(k); }));
    }).then(function () { return self.clients.claim(); }));
});

function isStatic(url) {
    return url.origin === self.location.origin && /^\/(css|js|fonts|img)\//.test(url.pathname);
}

self.addEventListener("fetch", function (event) {
    const req = event.request;
    if (req.method !== "GET") return;
    const url = new URL(req.url);
    if (url.origin !== self.location.origin) return;

    if (req.mode === "navigate") {
        event.respondWith(fetch(req).catch(function () { return caches.match(OFFLINE); }));
        return;
    }

    if (isStatic(url)) {
        event.respondWith(caches.open(VERSION).then(function (cache) {
            return cache.match(req).then(function (hit) {
                const network = fetch(req).then(function (res) {
                    if (res.ok) cache.put(req, res.clone());
                    return res;
                }).catch(function () { return hit; });
                return hit || network;
            });
        }));
    }
});
