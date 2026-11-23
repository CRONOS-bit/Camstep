(function () {
    "use strict";

    const box = document.querySelector(".global-music");
    const audio = document.getElementById("globalMusicAudio");
    const label = document.getElementById("globalMusicName");
    if (!box || !(audio instanceof HTMLAudioElement)) return;

    const userId = box.dataset.user;
    const libraryUrl = box.dataset.libraryUrl;
    const storageKey = "camstep:" + userId + ":player";
    const repeatButton = document.getElementById("playerRepeat");
    const shuffleButton = document.getElementById("playerShuffle");
    const ambientButton = document.getElementById("playerAmbient");
    let ambientEnabled = box.dataset.ambient === "1";
    const ambientUrl = box.dataset.ambientUrl;
    let ambientPref = Math.min(100, Math.max(5, Number(box.dataset.ambientVolume) || 35)) / 100;
    const justEntered = box.dataset.justEntered === "1";
    const repeatModes = ["off", "all", "one"];
    const repeatTitles = { off: "Repetir: desactivado", all: "Repetir: toda la lista", one: "Repetir: esta canción" };

    let queue = [];
    let listName = null;
    let state = readState();

    function readState() {
        try {
            const p = JSON.parse(localStorage.getItem(storageKey) || "{}");
            return {
                id: p.id || null,
                time: Number(p.time) || 0,
                playing: !!p.playing,
                playlist: p.playlist || null,
                repeat: repeatModes.includes(p.repeat) ? p.repeat : "off",
                shuffle: !!p.shuffle,
                order: Array.isArray(p.order) ? p.order : null,
                volume: typeof p.volume === "number" ? p.volume : 1,
                ambient: p.ambient !== false,
                ambientTime: Number(p.ambientTime) || 0,
                ambientPlaying: p.ambientPlaying !== false,
                ambientVolume: typeof p.ambientVolume === "number" && p.ambientPref === ambientPref ? p.ambientVolume : ambientPref,
                ambientPref: ambientPref,
                sc: p.sc && typeof p.sc.url === "string" ? p.sc : null
            };
        } catch (e) {
            return {
                id: null, time: 0, playing: false, playlist: null, repeat: "off", shuffle: false, order: null, volume: 1,
                ambient: true, ambientTime: 0, ambientPlaying: true, ambientVolume: ambientPref, ambientPref: ambientPref, sc: null
            };
        }
    }

    let fading = false;
    let ambientActive = false;

    function saveState() {
        try {
            if (ambientActive) {
                state.ambientTime = audio.currentTime || 0;
                state.ambientPlaying = !audio.paused;
                if (!fading) state.ambientVolume = audio.volume;
            } else if (state.id) {
                state.time = audio.currentTime || 0;
                state.playing = !audio.paused && !audio.ended;
                if (!fading) state.volume = audio.volume;
            }
            state.order = queue.map(function (s) { return s.id; });
            localStorage.setItem(storageKey, JSON.stringify(state));
        } catch (e) { }
    }

    function shuffled(list) {
        const copy = list.slice();
        for (let i = copy.length - 1; i > 0; i--) {
            const j = Math.floor(Math.random() * (i + 1));
            [copy[i], copy[j]] = [copy[j], copy[i]];
        }
        return copy;
    }

    function fetchQueue(playlistId) {
        const url = libraryUrl + (playlistId ? "?playlist=" + encodeURIComponent(playlistId) : "");
        return fetch(url, { credentials: "same-origin", headers: { "Accept": "application/json" } })
            .then(function (r) { return r.ok && (r.headers.get("content-type") || "").includes("json") ? r.json() : { songs: [] }; })
            .then(function (data) {
                state.playlist = data.playlist || null;
                listName = data.name || null;
                const songs = Array.isArray(data.songs) ? data.songs : [];
                songs.forEach(function (song, i) { song._i = i; });
                return songs;
            });
    }

    function setQueue(songs, keepOrder) {
        if (keepOrder && state.order) {
            const byId = new Map(songs.map(function (s) { return [s.id, s]; }));
            const ordered = state.order.map(function (id) { return byId.get(id); }).filter(Boolean);
            songs.forEach(function (s) { if (!ordered.includes(s)) ordered.push(s); });
            queue = ordered;
        } else {
            queue = state.shuffle ? shuffled(songs) : songs;
        }
    }

    function indexOfCurrent() {
        return queue.findIndex(function (s) { return s.id === state.id; });
    }

    function showLabel(song, extra) {
        label.textContent = song.title + (listName ? " · " + listName : "") + (extra || "");
    }

    let fadeTimer = null;
    function fadeTo(target) {
        clearInterval(fadeTimer);
        fading = true;
        audio.volume = 0;
        const start = Date.now();
        fadeTimer = setInterval(function () {
            const k = Math.min(1, (Date.now() - start) / 800);
            audio.volume = target * k;
            if (k >= 1) {
                clearInterval(fadeTimer);
                fading = false;
            }
        }, 40);
    }

    function playWhenAllowed(onBlocked) {
        const p = audio.play();
        if (p && typeof p.catch === "function") {
            p.catch(function () {
                if (onBlocked) onBlocked();
                const retry = function (event) {
                    if (event.target instanceof Element && event.target.closest(".global-music")) return cleanup();
                    audio.play().then(function () { if (ambientActive) fadeTo(state.ambientVolume); }).catch(function () { });
                    cleanup();
                };
                const cleanup = function () {
                    document.removeEventListener("pointerdown", retry, true);
                    document.removeEventListener("keydown", retry, true);
                };
                document.addEventListener("pointerdown", retry, true);
                document.addEventListener("keydown", retry, true);
            });
        }
    }

    function renderAmbient() {
        if (!ambientButton) return;
        ambientButton.hidden = !ambientEnabled;
        ambientButton.classList.toggle("on", ambientActive);
        ambientButton.setAttribute("aria-pressed", ambientActive ? "true" : "false");
    }

    function loadAmbient(startAt, autoplay) {
        if (!ambientEnabled || !ambientUrl) return;
        ambientActive = true;
        state.ambient = true;
        state.id = null;
        label.textContent = "🌿 Ambiente de CAMSTEP";
        audio.src = ambientUrl;
        audio.loop = true;
        audio.volume = state.ambientVolume;

        audio.addEventListener("loadedmetadata", function onMeta() {
            audio.removeEventListener("loadedmetadata", onMeta);
            if (startAt > 0 && startAt < audio.duration - 1) audio.currentTime = startAt;
            if (autoplay) {
                fadeTo(state.ambientVolume);
                playWhenAllowed(function () {
                    label.textContent = "🌿 Ambiente de CAMSTEP · toca la página o ▶ para escuchar";
                });
            }
        });

        audio.load();
        renderAmbient();
        saveState();
    }

    function load(song, startAt, autoplay) {
        if (!song) return;
        if (ambientActive) {
            ambientActive = false;
            state.ambient = false;
            audio.volume = Math.min(1, Math.max(0, state.volume));
            renderAmbient();
        }
        state.id = song.id;
        showLabel(song);
        audio.src = song.url;

        audio.addEventListener("loadedmetadata", function onMeta() {
            audio.removeEventListener("loadedmetadata", onMeta);
            if (startAt > 0 && startAt < audio.duration) audio.currentTime = startAt;
            if (autoplay) {
                playWhenAllowed(function () {
                    showLabel(song, " · pulsa ▶ para continuar");
                });
            }
        });

        audio.load();
        saveState();
    }

    function step(delta) {
        if (queue.length === 0) return;
        let index = indexOfCurrent();
        index = index < 0 ? 0 : (index + delta + queue.length) % queue.length;
        load(queue[index], 0, true);
    }

    function play(songId, playlistId) {
        fetchQueue(playlistId).then(function (songs) {
            setQueue(songs, false);
            if (songId) {
                const chosen = queue.find(function (s) { return s.id === songId; });
                if (chosen && state.shuffle) queue = [chosen].concat(queue.filter(function (s) { return s !== chosen; }));
                load(chosen || queue[0], 0, true);
            } else {
                load(queue[0], 0, true);
            }
        });
    }

    function renderButtons() {
        if (repeatButton) {
            repeatButton.title = repeatTitles[state.repeat];
            repeatButton.setAttribute("aria-pressed", state.repeat === "off" ? "false" : "true");
            repeatButton.classList.toggle("on", state.repeat !== "off");
            repeatButton.dataset.mode = state.repeat;
        }
        if (shuffleButton) {
            shuffleButton.title = state.shuffle ? "Aleatorio: activado" : "Aleatorio: desactivado";
            shuffleButton.setAttribute("aria-pressed", state.shuffle ? "true" : "false");
            shuffleButton.classList.toggle("on", state.shuffle);
        }
        audio.loop = ambientActive || state.repeat === "one";
        renderAmbient();
    }

    audio.addEventListener("ended", function () {
        if (ambientActive || state.repeat === "one") return;
        const isLast = indexOfCurrent() === queue.length - 1;
        if (state.repeat === "all" || !isLast) step(1);
        else saveState();
    });

    let lastSave = 0;
    audio.addEventListener("timeupdate", function () {
        const now = Date.now();
        if (now - lastSave > 1000) { lastSave = now; saveState(); }
    });
    audio.addEventListener("play", saveState);
    audio.addEventListener("pause", saveState);
    audio.addEventListener("volumechange", saveState);
    window.addEventListener("pagehide", saveState);

    ambientButton?.addEventListener("click", function () {
        if (ambientActive) {
            if (audio.paused) audio.play().catch(function () { }); else audio.pause();
        } else {
            loadAmbient(state.ambientTime, true);
        }
    });
    document.getElementById("playerPrev")?.addEventListener("click", function () { step(-1); });
    document.getElementById("playerNext")?.addEventListener("click", function () { step(1); });
    repeatButton?.addEventListener("click", function () {
        state.repeat = repeatModes[(repeatModes.indexOf(state.repeat) + 1) % repeatModes.length];
        renderButtons();
        saveState();
    });
    shuffleButton?.addEventListener("click", function () {
        state.shuffle = !state.shuffle;
        const current = queue[indexOfCurrent()];
        const base = queue.slice().sort(function (a, b) { return a._i - b._i; });
        queue = state.shuffle ? shuffled(base) : base;
        if (current && state.shuffle) queue = [current].concat(queue.filter(function (s) { return s !== current; }));
        renderButtons();
        saveState();
    });

    const scSlot = document.getElementById("scSlot");
    const scFrame = document.getElementById("scFrame");
    const scToggle = document.getElementById("scToggle");
    const SC_URL = /^https:\/\/soundcloud\.com\/[A-Za-z0-9_\-]+(\/[A-Za-z0-9_\-]+){0,3}$/;

    function scSrc(url, autoplay) {
        return "https://w.soundcloud.com/player/?url=" + encodeURIComponent(url) +
            "&auto_play=" + (autoplay ? "true" : "false") +
            "&color=%232f6f62&visual=false&show_comments=false&show_reposts=false&show_teaser=false&hide_related=true&buying=false&sharing=false&download=false";
    }

    function openSoundCloud(item, autoplay) {
        if (!scSlot || !scFrame || !item || !SC_URL.test(item.url)) return;
        if (!audio.paused) audio.pause();
        if (ambientActive) {
            ambientActive = false;
            audio.loop = state.repeat === "one";
        }
        state.ambient = false;
        state.sc = { url: item.url, title: String(item.title || "SoundCloud").slice(0, 120), kind: item.kind || "track" };

        const frame = document.createElement("iframe");
        frame.title = "Reproductor de SoundCloud: " + state.sc.title;
        frame.allow = "autoplay; encrypted-media";
        frame.height = state.sc.kind === "track" ? "120" : "300";
        frame.src = scSrc(state.sc.url, autoplay);
        scFrame.replaceChildren(frame);
        scSlot.hidden = false;
        scSlot.classList.remove("collapsed");
        if (scToggle) { scToggle.textContent = "▾"; scToggle.setAttribute("aria-expanded", "true"); }
        label.textContent = "☁ " + state.sc.title;
        renderAmbient();
        saveState();
    }

    function closeSoundCloud() {
        if (!scSlot || scSlot.hidden) return;
        scFrame.replaceChildren();
        scSlot.hidden = true;
        state.sc = null;
        saveState();
    }

    audio.addEventListener("play", closeSoundCloud);

    document.getElementById("scClose")?.addEventListener("click", function () {
        closeSoundCloud();
        label.textContent = queue.length > 0 ? "Elige una canción en Mi música o pulsa ⏭" : "Sube tu música en Mi música";
    });
    scToggle?.addEventListener("click", function () {
        const collapsed = scSlot.classList.toggle("collapsed");
        scToggle.textContent = collapsed ? "▸" : "▾";
        scToggle.setAttribute("aria-expanded", collapsed ? "false" : "true");
        scToggle.title = collapsed ? "Mostrar el reproductor de SoundCloud" : "Ocultar el reproductor de SoundCloud";
    });

    document.addEventListener("click", function (event) {
        const scButton = event.target instanceof Element ? event.target.closest("[data-sc-play]") : null;
        if (scButton) {
            event.preventDefault();
            openSoundCloud({
                url: scButton.getAttribute("data-sc-url"),
                title: scButton.getAttribute("data-sc-title"),
                kind: scButton.getAttribute("data-sc-kind")
            }, true);
        }
    });

    document.addEventListener("click", function (event) {
        const target = event.target instanceof Element ? event.target : null;
        const songButton = target?.closest("[data-play-song]");
        const listButton = target?.closest("[data-play-playlist]");
        if (songButton) {
            event.preventDefault();
            play(Number(songButton.getAttribute("data-play-song")), songButton.getAttribute("data-playlist"));
        } else if (listButton) {
            event.preventDefault();
            play(null, listButton.getAttribute("data-play-playlist"));
        }
    });

    audio.volume = Math.min(1, Math.max(0, state.volume));
    renderButtons();

    window.camstepPlayer = {
        playAmbient: function () {
            if (!ambientEnabled) return;
            if (ambientActive) { if (audio.paused) audio.play().catch(function () { }); return; }
            loadAmbient(state.ambientTime, true);
        },
        applySettings: function () {
            const wasEnabled = ambientEnabled;
            ambientEnabled = box.dataset.ambient === "1";
            ambientPref = Math.min(100, Math.max(5, Number(box.dataset.ambientVolume) || 35)) / 100;
            state.ambientPref = ambientPref;
            state.ambientVolume = ambientPref;

            if (!ambientEnabled && ambientActive) {
                audio.pause();
                ambientActive = false;
                audio.loop = state.repeat === "one";
                audio.removeAttribute("src");
                audio.load();
                label.textContent = queue.length > 0 ? "Elige una canción en Mi música o pulsa ⏭" : "Sube tu música en Mi música";
            } else if (ambientEnabled && ambientActive) {
                clearInterval(fadeTimer);
                fading = false;
                audio.volume = ambientPref;
            } else if (ambientEnabled && !wasEnabled && audio.paused) {
                loadAmbient(0, true);
            }
            renderAmbient();
            saveState();
        }
    };

    const ambientFirst = ambientEnabled && justEntered;
    let scRestored = false;
    if (state.sc && !ambientFirst) {
        openSoundCloud(state.sc, false);
        scRestored = true;
    } else if (state.sc) {
        state.sc = null;
    }

    fetchQueue(state.playlist).then(function (songs) {
        setQueue(songs, true);
        const current = queue.find(function (s) { return s.id === state.id; });

        if (scRestored) {
            return;
        } else if (current && !state.ambient && !ambientFirst) {
            load(current, state.time, state.playing);
        } else if (ambientEnabled && (state.ambient || !current || ambientFirst)) {
            if (justEntered) { state.ambientTime = 0; state.ambientPlaying = true; }
            loadAmbient(state.ambientTime, state.ambientPlaying);
        } else if (current) {
            load(current, state.time, state.playing);
        } else if (queue.length > 0) {
            state.id = null;
            label.textContent = "Elige una canción en Mi música o pulsa ⏭";
        } else {
            label.textContent = "Sube tu música en Mi música";
        }
    }).catch(function () {
        if (ambientEnabled && !scRestored) loadAmbient(justEntered ? 0 : state.ambientTime, justEntered || state.ambientPlaying);
    });
})();
