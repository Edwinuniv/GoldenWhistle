/* ===================================================================
   site.js — WorldCupHub · Dev B · ALL JS IN ONE FILE
   Contains: Chart defaults, shared helpers, SignalR stub,
             Dashboard logic, Bracket logic, Mood logic
   =================================================================== */

// ---------------------------------------------------------------
// 1. CHART.JS GLOBAL DEFAULTS — dark theme + draw-in animations
// ---------------------------------------------------------------
document.addEventListener('DOMContentLoaded', () => {
    if (!window.Chart) return;

    Chart.defaults.color = '#8C97AD';
    Chart.defaults.font.family = "'Inter', system-ui, sans-serif";
    Chart.defaults.borderColor = 'rgba(255,255,255,0.06)';
    Chart.defaults.animation = { duration: 1100, easing: 'easeOutQuart' };
    Chart.defaults.animations.colors = { duration: 400 };

    // Init the right page
    const page = document.body.dataset.page;
    if (page === 'dashboard') initDashboard();
    if (page === 'bracket') initBracket();
    if (page === 'mood') initMood();
    if (page === 'pub') initPub();
    if (page === 'data') initData();
    if (page === 'marketplace') initMarketplace();
    if (page === 'simulator') initSimulator();
    if (page === 'profile') initProfile();
});

// ---------------------------------------------------------------
// 2. SHARED ANIMATION HELPERS
// ---------------------------------------------------------------
function drawInLineAnimation() {
    return {
        x: {
            type: 'number', easing: 'linear', duration: 1200, from: NaN,
            delay(ctx) {
                if (ctx.type !== 'data' || ctx.xStarted) return 0;
                ctx.xStarted = true;
                return ctx.index * 60;
            }
        },
        y: {
            type: 'number', easing: 'easeOutQuart', duration: 800,
            from(ctx) {
                if (ctx.index === 0) return ctx.chart.scales.y.getPixelForValue(0);
                return ctx.chart.getDatasetMeta(ctx.datasetIndex).data[ctx.index - 1].getProps(['y'], true).y;
            }
        }
    };
}

function drawInBarAnimation() {
    return {
        y: {
            duration: 900, easing: 'easeOutQuart',
            from: (ctx) => ctx.chart.scales.y.getPixelForValue(0)
        }
    };
}

// ---------------------------------------------------------------
// 3. FAN PULSE BAR — shared top bar updater
// ---------------------------------------------------------------
function updateFanPulse(ecstasy, anxious, agony, totalVotes) {
    const segE = document.getElementById('segEcstasy');
    if (!segE) return;
    document.getElementById('segEcstasy').style.width = ecstasy + '%';
    document.getElementById('segAnxious').style.width = anxious + '%';
    document.getElementById('segAgony').style.width = agony + '%';
    document.getElementById('pctEcstasy').textContent = ecstasy + '%';
    document.getElementById('pctAnxious').textContent = anxious + '%';
    document.getElementById('pctAgony').textContent = agony + '%';
    document.getElementById('voteCount').textContent = totalVotes.toLocaleString('en-US');
}

// ---------------------------------------------------------------
// 4. SIGNALR STUB — connects once hubs exist (Dev A M1/M2)
// ---------------------------------------------------------------
let hubConnection = null;
function initSignalRConnection(hubUrl) {
    if (!window.signalR) return null;
    hubConnection = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl)
        .withAutomaticReconnect()
        .build();
    hubConnection.start().catch(err =>
        console.warn('SignalR not ready yet (expected until backend ships):', err.message));
    return hubConnection;
}

// ---------------------------------------------------------------
// 5. DASHBOARD PAGE
// ---------------------------------------------------------------
function initDashboard() {
    updateFanPulse(48, 31, 21, 247831);

    new Chart(document.getElementById('xgByMatchChart'), {
        type: 'bar',
        data: {
            labels: ['BRA v ARG', 'FRA v ESP', 'GER v ENG', 'POR v NED', 'ITA v URU'],
            datasets: [{
                data: [1.95, 1.8, 1.6, 1.3, 2.1],
                backgroundColor: '#FFB700',
                borderRadius: 6,
                maxBarThickness: 56
            }]
        },
        options: {
            animation: drawInBarAnimation(),
            plugins: { legend: { display: false } },
            scales: {
                y: { beginAtZero: true, max: 3, grid: { color: 'rgba(255,255,255,0.06)' } },
                x: { grid: { display: false } }
            }
        }
    });

    // TODO: const conn = initSignalRConnection('/hubs/leaderboard');
    // conn.on('LeaderboardUpdated', payload => { /* re-render */ });
}

// ---------------------------------------------------------------
// 6. BRACKET PAGE
// ---------------------------------------------------------------
function initBracket() {
    updateFanPulse(48, 31, 21, 247831);

    // TODO: const conn = initSignalRConnection('/hubs/leaderboard');
    // conn.on('LeagueRankUpdated', payload => { /* update standings */ });
    // conn.on('MatchEventPushed', event => appendLiveEvent(event));
}

function appendLiveEvent(event) {
    const feed = document.getElementById('liveUpdatesFeed');
    if (!feed) return;
    const el = document.createElement('div');
    el.className = 'd-flex gap-3';
    el.innerHTML = `
        <span class="text-mono text-secondary" style="font-size:13px;width:32px;">${event.minute}'</span>
        <div>
            <div>${event.icon} <strong>${event.title}</strong></div>
            <div class="text-secondary" style="font-size:13px;">${event.score}</div>
        </div>`;
    feed.prepend(el);
}

// ---------------------------------------------------------------
// 7. MOOD MAP PAGE
// ---------------------------------------------------------------
let moodDoughnutChart = null;
const moodState = { ecstasy: 48, anxious: 31, agony: 21, total: 247731 };

function initMood() {
    updateFanPulse(moodState.ecstasy, moodState.anxious, moodState.agony, moodState.total);

    // Doughnut
    moodDoughnutChart = new Chart(document.getElementById('moodDoughnut'), {
        type: 'doughnut',
        data: {
            datasets: [{
                data: [moodState.ecstasy, moodState.anxious, moodState.agony],
                backgroundColor: ['#00FF87', '#FFB700', '#FF4D6D'],
                borderWidth: 0,
                hoverOffset: 6
            }]
        },
        options: {
            cutout: '70%',
            animation: { animateRotate: true, duration: 1000 },
            plugins: { legend: { display: false }, tooltip: { enabled: false } }
        }
    });

    // Timeline line chart
    new Chart(document.getElementById('moodTimelineChart'), {
        type: 'line',
        data: {
            labels: ["0'", "15'", "23'", "35'", "45'", "54'", "65'", "68'", "73'"],
            datasets: [{
                label: 'Ecstasy',
                data: [28, 32, 48, 58, 65, 52, 44, 38, 44],
                borderColor: '#00FF87',
                backgroundColor: 'rgba(0,255,135,0.08)',
                fill: true,
                tension: 0.4,
                pointBackgroundColor: '#00FF87',
                pointRadius: 4
            }]
        },
        options: {
            animation: drawInLineAnimation(),
            plugins: { legend: { display: false } },
            scales: {
                y: { min: 0, max: 80, grid: { color: 'rgba(255,255,255,0.06)' } },
                x: { grid: { display: false } }
            }
        }
    });

    // TODO: const conn = initSignalRConnection('/hubs/mood');
    // conn.on('MoodUpdated', (e,a,g,t) => applyMoodUpdate(e,a,g,t));
}

function castVote(emotion) {
    document.querySelectorAll('.emotion-btn').forEach(btn =>
        btn.classList.remove('selected-ecstasy', 'selected-anxious', 'selected-agony'));
    document.querySelector(`[data-emotion="${emotion}"]`).classList.add(`selected-${emotion}`);

    const emojis = { ecstasy: '🤩', anxious: '😬', agony: '😭' };
    const center = document.getElementById('doughnutCenter');
    if (center) center.innerHTML = `
        <div style="font-size:28px;">${emojis[emotion]}</div>
        <div class="text-secondary" style="font-size:13px;margin-top:4px;">Your vote</div>`;

    const msg = document.getElementById('thanksMsg');
    if (msg) msg.classList.add('visible');

    // TODO: POST to /api/mood/vote once Dev A ships M2
}

function applyMoodUpdate(ecstasy, anxious, agony, total) {
    Object.assign(moodState, { ecstasy, anxious, agony, total });

    if (moodDoughnutChart) {
        moodDoughnutChart.data.datasets[0].data = [ecstasy, anxious, agony];
        moodDoughnutChart.update();
    }

    ['E', 'A', 'G'].forEach((k, i) => {
        const vals = [ecstasy, anxious, agony];
        const ids = ['E', 'A', 'G'];
        const el = document.getElementById('legendPct' + k);
        const bar = document.getElementById('bar' + k);
        const pct = document.getElementById('pct' + k);
        if (el) el.textContent = vals[i] + '%';
        if (bar) bar.style.width = vals[i] + '%';
        if (pct) pct.textContent = vals[i] + '%';
    });

    const panel = document.getElementById('voteCountPanel');
    if (panel) panel.textContent = total.toLocaleString('en-US') + ' fans voted';

    updateFanPulse(ecstasy, anxious, agony, total);
}

// ---------------------------------------------------------------
// 8. PUB FINDER PAGE — placeholder (Leaflet init goes here)
// ---------------------------------------------------------------
function initPub() {
    updateFanPulse(48, 31, 21, 247831);
    // Leaflet map init will go here once M4 view is built
}

// ---------------------------------------------------------------
// 9. DATA VISUALIZER PAGE — placeholder
// ---------------------------------------------------------------
function initData() {
    updateFanPulse(48, 31, 21, 247831);
    // xG Timeline, Possession Flow, Heat Map, Radar charts go here
}

// ---------------------------------------------------------------
// 10. MARKETPLACE PAGE — placeholder
// ---------------------------------------------------------------
function initMarketplace() {
    updateFanPulse(48, 31, 21, 247831);
}

// ---------------------------------------------------------------
// 11. SIMULATOR PAGE — placeholder
// ---------------------------------------------------------------
function initSimulator() {
    updateFanPulse(48, 31, 21, 247831);
}

// ---------------------------------------------------------------
// 12. PROFILE / SETTINGS PAGE
// ---------------------------------------------------------------
function initProfile() {
    updateFanPulse(48, 31, 21, 247831);
    // Show account tab by default
    const accountTab = document.getElementById('tab-account');
    if (accountTab) accountTab.classList.add('active');
}
function showTab(evt, tabName) {
    // Hide all tab panels
    document.querySelectorAll('.settings-tab-panel').forEach(el => el.classList.remove('active'));
    // Show selected panel
    const target = document.getElementById('tab-' + tabName);
    if (target) target.classList.add('active');
    // Update tab button styles
    document.querySelectorAll('.settings-tab').forEach(btn => btn.classList.remove('active'));
    evt.currentTarget.classList.add('active');
}

// ---------------------------------------------------------------
// 13. PUB FINDER PAGE
// ---------------------------------------------------------------

// Mock pub data — TODO (Dev A): replace with /api/pubs?lat=&lng= once
// PubLocations table + geolocation endpoint is wired in M4 backend
const PUB_DATA = [
    {
        id: 1, name: 'The Offside Trap', area: 'Shoreditch', city: 'London',
        distance: '0.2 km', rating: 4.9, reviews: 342, screens: 8,
        status: 'Packed', statusClass: 'badge-live',
        lat: 51.524, lng: -0.079, open: true, free: true, hd: true,
        img: ''
    },
    {
        id: 2, name: 'Corner Flag & Craft', area: 'Islington', city: 'London',
        distance: '0.6 km', rating: 4.6, reviews: 198, screens: 5,
        status: 'Available', statusClass: 'badge-success',
        lat: 51.536, lng: -0.071, open: true, free: true, hd: true,
        img: ''
    },
    {
        id: 3, name: 'Golazo Sports Bar', area: 'Hackney', city: 'London',
        distance: '1.1 km', rating: 4.3, reviews: 87, screens: 3,
        status: 'Open', statusClass: 'badge-ft',
        lat: 51.529, lng: -0.063, open: true, free: false, hd: true,
        img: ''
    },
    {
        id: 4, name: 'The Football Factory', area: 'Bethnal Green', city: 'London',
        distance: '1.4 km', rating: 4.1, reviews: 54, screens: 6,
        status: 'Open', statusClass: 'badge-ft',
        lat: 51.521, lng: -0.056, open: true, free: false, hd: false,
        img: ''
    }
];

let leafletMap = null;
let leafletMarkers = {};
let activeFilter = 'all';
let activePubId = null;

function initPub() {
    updateFanPulse(48, 31, 21, 247831);
    renderPubList(PUB_DATA);
    initLeafletMap();
}

function renderPubList(pubs) {
    const list = document.getElementById('pubList');
    if (!list) return;
    list.innerHTML = '';
    pubs.forEach(pub => {
        const card = document.createElement('div');
        card.className = 'pub-card' + (pub.id === activePubId ? ' active' : '');
        card.dataset.id = pub.id;
        card.innerHTML = `
            <div style="height:80px;background:linear-gradient(135deg,var(--bg-pill),var(--bg-panel-raised));display:flex;align-items:center;justify-content:center;">
                <svg viewBox="0 0 24 24" width="32" height="32" fill="none" stroke="var(--text-tertiary)" stroke-width="1"><path d="M12 21s7-6.5 7-12a7 7 0 10-14 0c0 5.5 7 12 7 12z"/><circle cx="12" cy="9" r="2.5"/></svg>
            </div>
            <div style="padding:12px;">
                <div class="d-flex justify-content-between align-items-start">
                    <strong style="font-size:14px;">${pub.name}</strong>
                    <span class="badge ${pub.statusClass}" style="font-size:10px;">${pub.status}</span>
                </div>
                <div class="text-secondary" style="font-size:12px;margin-top:4px;">${pub.area} · ${pub.distance}</div>
                <div class="d-flex align-items-center gap-2 mt-1">
                    <span style="color:var(--amber);font-size:13px;">${'★'.repeat(Math.floor(pub.rating))}${'☆'.repeat(5 - Math.floor(pub.rating))}</span>
                    <span class="text-secondary" style="font-size:12px;">${pub.reviews} reviews · ${pub.screens} screens</span>
                </div>
            </div>`;
        card.addEventListener('click', () => selectPub(pub.id));
        list.appendChild(card);
    });
}

function initLeafletMap() {
    if (!window.L) { console.warn('Leaflet not loaded'); return; }

    leafletMap = L.map('pubMap', { zoomControl: false }).setView([51.528, -0.068], 14);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png').addTo(leafletMap);

    L.control.zoom({ position: 'bottomright' }).addTo(leafletMap);

    PUB_DATA.forEach(pub => {
        const icon = L.divIcon({
            className: '',
            html: `<div class="pub-marker-label" id="marker-${pub.id}">${pub.name}</div>`,
            iconAnchor: [0, 12]
        });
        const marker = L.marker([pub.lat, pub.lng], { icon }).addTo(leafletMap);
        marker.on('click', () => selectPub(pub.id));
        leafletMarkers[pub.id] = marker;
    });
}

function selectPub(id) {
    activePubId = id;
    const pub = PUB_DATA.find(p => p.id === id);
    if (!pub) return;

    // Highlight card
    document.querySelectorAll('.pub-card').forEach(c => c.classList.remove('active'));
    const card = document.querySelector(`.pub-card[data-id="${id}"]`);
    if (card) { card.classList.add('active'); card.scrollIntoView({ behavior: 'smooth', block: 'nearest' }); }

    // Highlight marker
    document.querySelectorAll('.pub-marker-label').forEach(m => m.classList.remove('selected'));
    const markerEl = document.getElementById(`marker-${id}`);
    if (markerEl) markerEl.classList.add('selected');

    // Pan map
    if (leafletMap) leafletMap.flyTo([pub.lat, pub.lng], 15, { duration: 0.8 });

    // Show detail popup
    const popup = document.getElementById('pubDetailPopup');
    if (popup) {
        document.getElementById('popupName').textContent = pub.name;
        document.getElementById('popupAddr').textContent = `${pub.area}, ${pub.city} · ${pub.distance}`;
        document.getElementById('popupStars').textContent = '★'.repeat(Math.floor(pub.rating)) + '☆'.repeat(5 - Math.floor(pub.rating));
        document.getElementById('popupReviews').textContent = `${pub.reviews} reviews · ${pub.screens} screens`;
        document.getElementById('popupStatus').className = `badge ${pub.statusClass} ms-2`;
        document.getElementById('popupStatus').textContent = pub.status;
        popup.classList.add('visible');
    }
}

function filterPubs(query) {
    const q = query.toLowerCase();
    const filtered = PUB_DATA.filter(p =>
        (p.name.toLowerCase().includes(q) || p.area.toLowerCase().includes(q)) &&
        matchesFilter(p)
    );
    renderPubList(filtered);
}

function matchesFilter(pub) {
    if (activeFilter === 'all') return true;
    if (activeFilter === 'open') return pub.open;
    if (activeFilter === 'free') return pub.free;
    if (activeFilter === 'hd') return pub.hd;
    return true;
}

function setFilter(el, filter) {
    activeFilter = filter;
    document.querySelectorAll('.chip').forEach(c => c.classList.remove('active'));
    el.classList.add('active');
    filterPubs(document.getElementById('pubSearchInput')?.value || '');
}

function locateMe() {
    if (!navigator.geolocation) return;
    navigator.geolocation.getCurrentPosition(pos => {
        if (leafletMap) leafletMap.flyTo([pos.coords.latitude, pos.coords.longitude], 14);
    });
}