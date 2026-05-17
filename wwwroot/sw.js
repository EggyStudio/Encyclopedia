// Encyclopedia service worker.
// Strategy: network-first for HTML, cache-first for static assets.
// On install we precache the shell + manifest so the app can boot offline.

const CACHE = 'encyclopedia-v1';
const PRECACHE = [
    '/',
    '/manifest.webmanifest',
    '/_content/BlueprintShell/css/app.css',
    '/_content/BlueprintShell/styles/themes/default.css',
    '/css/wiki.css',
];

self.addEventListener('install', e => {
    e.waitUntil(caches.open(CACHE).then(c => c.addAll(PRECACHE)).then(() => self.skipWaiting()));
});

self.addEventListener('activate', e => {
    e.waitUntil(
        caches.keys()
            .then(keys => Promise.all(keys.filter(k => k !== CACHE).map(k => caches.delete(k))))
            .then(() => self.clients.claim())
    );
});

self.addEventListener('fetch', e => {
    const req = e.request;
    if (req.method !== 'GET') return;

    const url = new URL(req.url);
    if (url.origin !== self.location.origin) return;

    // Never cache the Blazor SignalR / hub paths.
    if (url.pathname.startsWith('/_blazor') || url.pathname.startsWith('/shell-hub')) return;

    const accept = req.headers.get('accept') || '';
    if (accept.includes('text/html')) {
        e.respondWith(
            fetch(req)
                .then(r => { const cp = r.clone(); caches.open(CACHE).then(c => c.put(req, cp)); return r; })
                .catch(() => caches.match(req).then(r => r ?? caches.match('/')))
        );
        return;
    }

    e.respondWith(
        caches.match(req).then(cached => cached ?? fetch(req).then(r => {
            const cp = r.clone();
            caches.open(CACHE).then(c => c.put(req, cp));
            return r;
        }))
    );
});
