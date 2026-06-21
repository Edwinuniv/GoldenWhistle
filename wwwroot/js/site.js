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

    const xgCanvas = document.getElementById('xgByMatchChart');
    const xgLabels = xgCanvas ? xgCanvas.dataset.labels.split(',') : [];
    const xgValues = xgCanvas ? xgCanvas.dataset.values.split(',').map(Number) : [];
    new Chart(xgCanvas, {
        type: 'bar',
        data: {
            labels: xgLabels,
            datasets: [{
                data: xgValues,
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
    // Read real values from Razor-rendered data attributes
    const moodCanvas = document.getElementById('moodDoughnut');
    if (moodCanvas) {
        moodState.ecstasy = parseInt(moodCanvas.dataset.ecstasy) || 48;
        moodState.anxious = parseInt(moodCanvas.dataset.anxiety) || 31;
        moodState.agony = parseInt(moodCanvas.dataset.agony) || 21;
    }

    moodDoughnutChart = new Chart(moodCanvas, {
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
    const tlCanvas = document.getElementById('moodTimelineChart');
    const tlLabels = tlCanvas?.dataset.labels?.split(',') || ["0'", "15'", "30'", "45'", "60'", "75'", "90'"];
    const tlData = tlCanvas?.dataset.ecstasy?.split(',').map(Number) || [0, 0, 0, 0, 0, 0, 0];
    new Chart(tlCanvas, {
        type: 'line',
        data: {
            labels: tlLabels,
            datasets: [{
                label: 'Ecstasy',
                data: tlData,
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

    // SignalR — MoodMapHub (Dev A confirmed method: ReceiveMoodUpdate)
    const moodConn = initSignalRConnection('/hubs/moodmap');
    if (moodConn) {
        moodConn.on('ReceiveMoodUpdate', (matchId, mood, ecstasy, agony, anxiety) => {
            applyMoodUpdate(ecstasy, anxiety, agony, moodState.total + 1);
        });
    }
}

async function castVote(emotion, matchId) {
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

    // POST to Dev A's real endpoint
    // MoodType enum: Ecstasy=0, Agony=1, Anxiety=2
    const moodMap = { ecstasy: 'Ecstasy', anxious: 'Anxiety', agony: 'Agony' };
    try {
        await fetch('/api/mood/vote', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ matchId: matchId || parseInt(document.getElementById('currentMatchId')?.value), mood: moodMap[emotion] })
        });
    } catch (e) { console.warn('Vote API not ready yet:', e.message); }
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

// ---------------------------------------------------------------
// 9. DATA VISUALIZER PAGE (M5) — full implementation
// ---------------------------------------------------------------
function initData() {
    updateFanPulse(48, 31, 21, 247831);

    // xG Timeline — draws itself line by line
    new Chart(document.getElementById('xgTimelineChart'), {
        type: 'line',
        data: {
            labels: ["5'", "15'", "23'", "35'", "45'", "54'", "65'", "73'"],
            datasets: [
                {
                    label: 'Brazil xG',
                    data: [0.1, 0.3, 0.55, 0.75, 0.95, 1.2, 1.6, 1.95],
                    borderColor: '#00FF87',
                    backgroundColor: 'rgba(0,255,135,0.06)',
                    fill: false, tension: 0.4,
                    pointBackgroundColor: '#00FF87', pointRadius: 4
                },
                {
                    label: 'Argentina xG',
                    data: [0.05, 0.15, 0.2, 0.3, 0.45, 0.45, 0.45, 0.45],
                    borderColor: '#FFB700',
                    backgroundColor: 'rgba(255,183,0,0.06)',
                    fill: false, tension: 0.4,
                    pointBackgroundColor: '#FFB700', pointRadius: 4
                }
            ]
        },
        options: {
            animation: drawInLineAnimation(),
            plugins: {
                legend: { display: true, labels: { color: '#8C97AD' } },
                tooltip: {
                    callbacks: {
                        label: ctx => `${ctx.dataset.label}: ${ctx.parsed.y.toFixed(2)}`
                    }
                }
            },
            scales: {
                y: { min: 0, max: 2.5, grid: { color: 'rgba(255,255,255,0.06)' } },
                x: { grid: { display: false } }
            }
        }
    });

    // Team Radar
    new Chart(document.getElementById('teamRadarChart'), {
        type: 'radar',
        data: {
            labels: ['Shots', 'Passes', 'Possession', 'Duels Won', 'Dribbles', 'Saves'],
            datasets: [{
                label: 'Brazil',
                data: [75, 80, 56, 65, 70, 60],
                borderColor: '#00FF87',
                backgroundColor: 'rgba(0,255,135,0.15)',
                pointBackgroundColor: '#00FF87'
            }]
        },
        options: {
            animation: { duration: 1000, easing: 'easeOutQuart' },
            plugins: { legend: { display: false } },
            scales: {
                r: {
                    grid: { color: 'rgba(255,255,255,0.08)' },
                    pointLabels: { color: '#8C97AD', font: { size: 11 } },
                    ticks: { display: false }
                }
            }
        }
    });

    // Possession Flow bars
    new Chart(document.getElementById('possessionChart'), {
        type: 'bar',
        data: {
            labels: ["15'", "30'", "45'", "60'", "75'"],
            datasets: [{
                label: 'Brazil %',
                data: [54, 58, 55, 57, 56],
                backgroundColor: '#00FF87',
                borderRadius: 6,
                maxBarThickness: 40
            }]
        },
        options: {
            animation: drawInBarAnimation(),
            plugins: { legend: { display: false } },
            scales: {
                y: { min: 0, max: 100, grid: { color: 'rgba(255,255,255,0.06)' } },
                x: { grid: { display: false } }
            }
        }
    });

    // Heat map canvas drawing
    drawHeatmap('home');
}

function drawHeatmap(team) {
    const canvas = document.getElementById('heatmapCanvas');
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    canvas.width = canvas.offsetWidth;
    canvas.height = canvas.offsetHeight;
    ctx.clearRect(0, 0, canvas.width, canvas.height);

    const homePoints = [
        { x: 0.65, y: 0.45, r: 90, intensity: 0.9 },
        { x: 0.72, y: 0.35, r: 70, intensity: 0.7 },
        { x: 0.58, y: 0.55, r: 60, intensity: 0.6 },
        { x: 0.80, y: 0.50, r: 50, intensity: 0.5 },
    ];
    const awayPoints = [
        { x: 0.30, y: 0.45, r: 60, intensity: 0.6 },
        { x: 0.22, y: 0.55, r: 45, intensity: 0.45 },
    ];

    const points = team === 'home' ? homePoints : awayPoints;
    const color = team === 'home' ? '0,255,135' : '255,77,109';

    points.forEach(p => {
        const grd = ctx.createRadialGradient(
            p.x * canvas.width, p.y * canvas.height, 0,
            p.x * canvas.width, p.y * canvas.height, p.r
        );
        grd.addColorStop(0, `rgba(${color},${p.intensity})`);
        grd.addColorStop(1, `rgba(${color},0)`);
        ctx.fillStyle = grd;
        ctx.beginPath();
        ctx.arc(p.x * canvas.width, p.y * canvas.height, p.r, 0, Math.PI * 2);
        ctx.fill();
    });
}

function setHeatmapTeam(btn, team) {
    document.querySelectorAll('.heatmap-team-btn').forEach(b => b.classList.remove('active'));
    btn.classList.add('active');
    drawHeatmap(team);
}

// ---------------------------------------------------------------
// 10. MARKETPLACE PAGE (M6) — full implementation
// ---------------------------------------------------------------
const LISTINGS_DATA = [
    { id: 1, title: 'Brazil 2026 Home', player: 'Vinicius Jr. #7', price: '£89', size: 'M', condition: 'BNWT', tag: 'hot', seller: 'FootballBay_UK', rating: 5.0, verified: true },
    { id: 2, title: 'Argentina 2026 Away', player: 'Messi #10', price: '£145', size: 'L', condition: 'Worn Once', tag: 'rare', seller: 'PampaKits', rating: 4.8, verified: true },
    { id: 3, title: 'France 2026 Home', player: 'Mbappé #10', price: '£78', size: 'S', condition: 'BNWT', tag: null, seller: 'LesBleus_Store', rating: 4.3, verified: true },
    { id: 4, title: 'England 2026 Home', player: 'Bellingham #10', price: '£65', size: 'M', condition: 'BNWT', tag: null, seller: 'ThreeLions_FC', rating: 4.6, verified: false },
    { id: 5, title: 'Portugal 2026 Away', player: 'Ronaldo #7', price: '£110', size: 'XL', condition: 'Match Worn', tag: 'rare', seller: 'FCP_Kits', rating: 4.9, verified: true },
    { id: 6, title: 'Germany 2026 Home', player: 'Müller #25', price: '£72', size: 'L', condition: 'BNWT', tag: null, seller: 'DFBShop', rating: 4.1, verified: false },
];

let activeMarketFilter = 'all';
let selectedListingId = null;

function initMarketplace() {
    updateFanPulse(48, 31, 21, 247831);
    renderListings(LISTINGS_DATA);
}

function renderListings(listings) {
    const grid = document.getElementById('listingsGrid');
    if (!grid) return;
    grid.innerHTML = '';

    listings.forEach(l => {
        const col = document.createElement('div');
        col.className = 'col-md-4';
        const tagHtml = l.tag === 'hot'
            ? `<span class="listing-tag listing-tag-hot">🔥 Hot</span>`
            : l.tag === 'rare'
                ? `<span class="listing-tag listing-tag-rare">💎 Rare</span>`
                : '';
        const stars = '★'.repeat(Math.floor(l.rating)) + '☆'.repeat(5 - Math.floor(l.rating));
        const condClass = l.condition !== 'BNWT' ? 'listing-badge-condition' : '';

        col.innerHTML = `
            <div class="listing-card">
                <div class="listing-img">
                    <svg viewBox="0 0 24 24" width="48" height="48" fill="none" stroke="var(--text-primary)" stroke-width="1"><path d="M20.84 4.61a5.5 5.5 0 00-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 00-7.78 7.78L12 21.23l8.84-8.84a5.5 5.5 0 000-7.78z"/></svg>
                    ${tagHtml}
                    <span class="listing-price">${l.price}</span>
                </div>
                <div class="listing-body">
                    <div class="listing-title">${l.title}</div>
                    <div class="listing-player">${l.player}</div>
                    <div class="listing-badges">
                        <span class="listing-badge">Size ${l.size}</span>
                        <span class="listing-badge ${condClass}">${l.condition}</span>
                    </div>
                    <div class="listing-seller">
                        <div>
                            <span class="listing-seller-stars">${stars}</span>
                            <span class="listing-seller-name ms-1">${l.seller}</span>
                        </div>
                        ${l.verified ? '<span class="badge-verified">Verified</span>' : ''}
                    </div>
                    <div class="listing-actions">
                        <button class="btn btn-primary" onclick="buyNow(${l.id})">Buy Now</button>
                        <button class="btn-msg" onclick="openMessageModal(${l.id})" title="Message seller">
                            <svg viewBox="0 0 24 24" fill="none" stroke="var(--text-secondary)" stroke-width="2"><path d="M21 15a2 2 0 01-2 2H7l-4 4V5a2 2 0 012-2h14a2 2 0 012 2z"/></svg>
                        </button>
                    </div>
                </div>
            </div>`;
        grid.appendChild(col);
    });
}

function filterListings(query) {
    const q = query.toLowerCase();
    const filtered = LISTINGS_DATA.filter(l =>
        (l.title.toLowerCase().includes(q) || l.player.toLowerCase().includes(q)) &&
        matchesMarketFilter(l)
    );
    renderListings(filtered);
}

function matchesMarketFilter(l) {
    if (activeMarketFilter === 'all') return true;
    if (activeMarketFilter === 'bnwt') return l.condition === 'BNWT';
    if (activeMarketFilter === 'match') return l.condition === 'Match Worn';
    if (activeMarketFilter === 'player') return l.player.includes('#');
    if (activeMarketFilter === 'budget') return parseInt(l.price.replace(/\D/g, '')) < 100;
    if (activeMarketFilter === 'auth') return l.verified;
    return true;
}

function setMarketFilter(el, filter) {
    activeMarketFilter = filter;
    document.querySelectorAll('.chip').forEach(c => c.classList.remove('active'));
    el.classList.add('active');
    filterListings(document.getElementById('marketSearchInput')?.value || '');
}

function openMessageModal(id) {
    selectedListingId = id;
    const l = LISTINGS_DATA.find(x => x.id === id);
    if (!l) return;
    document.getElementById('modalListingTitle').textContent = l.title;
    document.getElementById('modalListingMeta').textContent = `${l.player} · ${l.price}`;
    document.getElementById('messageModal').classList.add('visible');
}

function closeMessageModal() {
    document.getElementById('messageModal').classList.remove('visible');
    document.getElementById('messageText').value = '';
}

function buyNow(id) {
    // TODO (Dev A): POST /api/listings/{id}/buy
    alert('Buy flow coming once Dev A wires the backend!');
}

function sendMessage() {
    const msg = document.getElementById('messageText').value.trim();
    if (!msg) return;
    // TODO (Dev A): POST /api/messages { listingId: selectedListingId, message: msg }
    closeMessageModal();
    alert('Message sent! (Dev A backend pending)');
}

// ---------------------------------------------------------------
// 11. SIMULATOR PAGE (M7) — full implementation
// ---------------------------------------------------------------
const SIM_MATCHES = [
    { id: 1, home: 'Brazil', homeCode: 'BR', away: 'Argentina', awayCode: 'AR', homeScore: 2, awayScore: 1 },
    { id: 2, home: 'France', homeCode: 'FR', away: 'Spain', awayCode: 'ES', homeScore: 1, awayScore: 1 },
    { id: 3, home: 'Germany', homeCode: 'DE', away: 'England', awayCode: 'EN', homeScore: 0, awayScore: 2 },
    { id: 4, home: 'Portugal', homeCode: 'PT', away: 'Netherlands', awayCode: 'NL', homeScore: 1, awayScore: 0 },
];

function initSimulator() {
    updateFanPulse(48, 31, 21, 247831);
    renderSliders();
    updateBracket();
}

function renderSliders() {
    const list = document.getElementById('slidersList');
    if (!list) return;
    list.innerHTML = '';

    SIM_MATCHES.forEach(m => {
        const div = document.createElement('div');
        div.className = 'sim-match-row';
        div.innerHTML = `
            <div>
                <div class="sim-team-name">${m.homeCode} ${m.home}</div>
                <div class="sim-slider-wrap mt-2">
                    <input type="range" class="sim-slider home" min="0" max="5" value="${m.homeScore}"
                        oninput="updateScore(${m.id},'home',this.value)" />
                </div>
            </div>
            <div class="sim-score-display">
                <div class="sim-score-num" id="sim-home-${m.id}">${m.homeScore}</div>
                <span class="sim-score-sep">–</span>
                <div class="sim-score-num away" id="sim-away-${m.id}">${m.awayScore}</div>
            </div>
            <div style="text-align:right;">
                <div class="sim-team-name right">${m.awayCode} ${m.away}</div>
                <div class="sim-slider-wrap mt-2">
                    <input type="range" class="sim-slider away" min="0" max="5" value="${m.awayScore}"
                        oninput="updateScore(${m.id},'away',this.value)" />
                </div>
            </div>`;
        list.appendChild(div);
    });
}

function updateScore(matchId, side, value) {
    const match = SIM_MATCHES.find(m => m.id === matchId);
    if (!match) return;
    match[side === 'home' ? 'homeScore' : 'awayScore'] = parseInt(value);
    const el = document.getElementById(`sim-${side}-${matchId}`);
    if (el) el.textContent = value;
    updateBracket();
}

function updateBracket() {
    const winners = SIM_MATCHES.map(m => {
        if (m.homeScore > m.awayScore) return { name: m.home, code: m.homeCode, upset: false, draw: false };
        if (m.awayScore > m.homeScore) return { name: m.away, code: m.awayCode, upset: m.id === 3, draw: false };
        return { name: 'Draw', code: '—', upset: false, draw: true };
    });

    // QF winners
    const qfEl = document.getElementById('qfWinners');
    if (qfEl) {
        qfEl.innerHTML = winners.map(w => `
            <div class="sim-qf-winner ${w.draw ? 'draw' : w.upset ? 'upset' : 'expected'}">
                <span>${w.code} ${w.name}</span>
                ${w.upset ? '<span style="font-size:11px;">⚡ Upset!</span>' : ''}
            </div>`).join('');
    }

    // SF teams
    const sfEl = document.getElementById('sfTeams');
    if (sfEl) {
        sfEl.innerHTML = `
            <div class="sim-qf-winner">${winners[0].name}<br>vs<br>${winners[1].name}</div>
            <div class="sim-qf-winner mt-2">${winners[2].name}<br>vs<br>${winners[3].name}</div>`;
    }

    // Final
    const f1 = document.getElementById('finalTeam1');
    const f2 = document.getElementById('finalTeam2');
    if (f1) f1.textContent = winners[0].name;
    if (f2) f2.textContent = winners[2].name;

    // Win probabilities
    const probEl = document.getElementById('winProbList');
    if (probEl) {
        const total = SIM_MATCHES.reduce((s, m) => s + m.homeScore + m.awayScore, 0) || 1;
        const probs = [
            { team: winners[0].name, pct: 58, color: 'var(--green)' },
            { team: winners[2].name, pct: 42, color: 'var(--amber)' },
        ];
        probEl.innerHTML = probs.map(p => `
            <div class="sim-prob-row">
                <div class="d-flex justify-content-between mb-1">
                    <span class="fw-bold">${p.team}</span>
                    <span style="color:${p.color};font-weight:700;">${p.pct}%</span>
                </div>
                <div class="sim-prob-bar-track">
                    <div class="sim-prob-bar" style="width:${p.pct}%;background:${p.color};"></div>
                </div>
            </div>`).join('');
    }

    // Surprise factor
    const surpriseEl = document.getElementById('surpriseList');
    if (surpriseEl) {
        surpriseEl.innerHTML = SIM_MATCHES.map(m => {
            const isUpset = m.awayScore > m.homeScore && m.id === 3;
            return `<div class="sim-surprise-item">
                <div class="${isUpset ? 'sim-surprise-dot-upset' : 'sim-surprise-dot-expected'}"></div>
                <span>${isUpset ? '⚡ Upset! — ' : 'Expected — '}${m.home}/${m.away}</span>
            </div>`;
        }).join('');
    }
}

function resetSimulator() {
    SIM_MATCHES.forEach(m => { m.homeScore = 0; m.awayScore = 0; });
    renderSliders();
    updateBracket();
    document.getElementById('narrativeOutput').style.display = 'none';
}

function generateNarrative() {
    const output = document.getElementById('narrativeOutput');
    const text = document.getElementById('narrativeText');
    if (!output || !text) return;

    const winners = SIM_MATCHES.map(m =>
        m.homeScore > m.awayScore ? m.home : m.awayScore > m.homeScore ? m.away : 'Draw'
    );

    // TODO (Dev A): POST /api/simulator/run and get real AI narrative
    text.textContent = `In this alternate timeline, ${winners[0]} overcame their quarterfinal clash to advance, while the shock result between Germany and England sent ripples through the draw. ${winners[2]} capitalized on the chaos to reach the final, setting up a mouth-watering clash against ${winners[0]}. History will remember this bracket as the tournament where nothing went to script.`;
    output.style.display = 'block';
    output.scrollIntoView({ behavior: 'smooth' });
}

// ---------------------------------------------------------------
// 14. KICKOFF COMPANION PAGE (M3) — full implementation
// ---------------------------------------------------------------

// TODO (Dev A): replace with real data from MatchPreviews table
const KICKOFF_MATCHES = [
    {
        id: 1,
        homeFlag: '🇧🇷', homeName: 'Brazil', homeCode: 'BR', homeRecord: 'W4 D1 L0',
        awayFlag: '🇦🇷', awayName: 'Argentina', awayCode: 'AR', awayRecord: 'W3 D2 L0',
        info: 'QF · MetLife Stadium · 19:00 CET',
        kickoff: new Date(Date.now() + 2 * 60 * 60 * 1000), // 2h from now
        injuries: {
            home: [
                { name: 'Neymar Jr.', role: 'Forward', status: 'out' },
                { name: 'Militão', role: 'Defender', status: 'doubt' },
                { name: 'Danilo', role: 'Defender', status: 'return' },
            ],
            away: [
                { name: 'Di María', role: 'Winger', status: 'out' },
                { name: 'Mac Allister', role: 'Midfield', status: 'doubt' },
            ]
        },
        tactics: {
            home: {
                formation: '4-3-3',
                style: 'High press with width exploitation. Vinicius Jr. and Rodrygo will look to stretch the Argentine backline. Expect Paquetá as the creative hub.',
                keyPlayer: 'Vinicius Jr.', keyPlayerInitial: 'VJ'
            },
            away: {
                formation: '4-4-2',
                style: 'Compact defensive block with quick transitions. Messi drops deep to receive and turn. De Paul and Mac Allister control the midfield battle.',
                keyPlayer: 'Lionel Messi', keyPlayerInitial: 'LM'
            }
        },
        facts: [
            { emoji: '🏆', text: '<strong>Brazil vs Argentina</strong> is the most-played fixture in international football history with 109 meetings.', color: 'green' },
            { emoji: '😮', text: 'Argentina have <strong>never beaten Brazil</strong> in a knockout stage World Cup match.', color: 'red' },
            { emoji: '⚡', text: 'Vinicius Jr. has scored in <strong>5 consecutive</strong> matches this tournament — a Brazilian record.', color: 'gold' },
            { emoji: '🎲', text: 'The referee for this match has shown <strong>0 red cards</strong> in 12 World Cup games. Could end tonight.', color: 'blue' },
            { emoji: '📅', text: 'This is the <strong>8th time</strong> these teams meet at a World Cup. Brazil lead 4-2-1.', color: 'green' },
            { emoji: '🌡️', text: 'MetLife Stadium will be <strong>31°C</strong> at kickoff — the hottest conditions either team has played in this tournament.', color: 'gold' },
        ],
        h2h: {
            homeWins: 47, draws: 25, awayWins: 37,
            homeGoals: 171, awayGoals: 157
        }
    },
    {
        id: 2,
        homeFlag: '🇫🇷', homeName: 'France', homeCode: 'FR', homeRecord: 'W4 D0 L1',
        awayFlag: '🇪🇸', awayName: 'Spain', awayCode: 'ES', awayRecord: 'W5 D0 L0',
        info: 'QF · SoFi Stadium · 22:00 CET',
        kickoff: new Date(Date.now() + 5 * 60 * 60 * 1000),
        injuries: {
            home: [{ name: 'Upamecano', role: 'Defender', status: 'doubt' }],
            away: [{ name: 'Pedri', role: 'Midfield', status: 'out' }]
        },
        tactics: {
            home: { formation: '4-2-3-1', style: 'Counter-attacking with pace. Mbappé leads the line, threatening on the break.', keyPlayer: 'Kylian Mbappé', keyPlayerInitial: 'KM' },
            away: { formation: '4-3-3', style: 'Possession-based tiki-taka. Yamal and Williams terrorize full-backs.', keyPlayer: 'Lamine Yamal', keyPlayerInitial: 'LY' }
        },
        facts: [
            { emoji: '🏅', text: 'France are the <strong>reigning World Cup champions</strong> going for back-to-back titles.', color: 'gold' },
            { emoji: '🌟', text: 'Lamine Yamal at 17 is the <strong>youngest player</strong> to reach a World Cup quarterfinal.', color: 'green' },
        ],
        h2h: { homeWins: 28, draws: 11, awayWins: 16, homeGoals: 90, awayGoals: 65 }
    },
    {
        id: 3,
        homeFlag: '🇩🇪', homeName: 'Germany', homeCode: 'DE', homeRecord: 'W3 D1 L1',
        awayFlag: '🏴󠁧󠁢󠁥󠁮󠁧󠁿', awayName: 'England', awayCode: 'EN', awayRecord: 'W4 D1 L0',
        info: 'QF · AT&T Stadium · 22:00 CET',
        kickoff: new Date(Date.now() + 5 * 60 * 60 * 1000),
        injuries: {
            home: [{ name: 'Rüdiger', role: 'Defender', status: 'doubt' }],
            away: [{ name: 'Saka', role: 'Winger', status: 'return' }]
        },
        tactics: {
            home: { formation: '3-4-3', style: 'High defensive line with wing-backs pushing forward. Wirtz as the creative force.', keyPlayer: 'Florian Wirtz', keyPlayerInitial: 'FW' },
            away: { formation: '4-3-3', style: 'Direct play through Bellingham. High press and set-piece threat.', keyPlayer: 'Jude Bellingham', keyPlayerInitial: 'JB' }
        },
        facts: [
            { emoji: '⚔️', text: 'Germany vs England at a major tournament — <strong>it\'s never boring</strong>. 3 of their last 4 knockout meetings went to extra time.', color: 'red' },
            { emoji: '🎯', text: 'England have <strong>never beaten Germany</strong> in a World Cup knockout stage on German/neutral soil.', color: 'gold' },
        ],
        h2h: { homeWins: 15, draws: 4, awayWins: 13, homeGoals: 57, awayGoals: 52 }
    }
];

let activeKickoffId = 1;
let countdownInterval = null;

function initKickoff() {
    updateFanPulse(48, 31, 21, 247831);
    renderKickoffTabs();
    renderKickoffGrid();
    loadKickoffMatch(1);
}

function renderKickoffTabs() {
    const tabs = document.getElementById('kickoffMatchTabs');
    if (!tabs) return;
    tabs.innerHTML = KICKOFF_MATCHES.map(m => `
        <span class="chip ${m.id === activeKickoffId ? 'active' : ''}"
              onclick="loadKickoffMatch(${m.id})">
            ${m.homeFlag} ${m.homeCode} vs ${m.awayCode} ${m.awayFlag}
        </span>`).join('');
}

function renderKickoffGrid() {
    const grid = document.getElementById('kickoffGrid');
    if (!grid) return;
    grid.innerHTML = KICKOFF_MATCHES.map(m => `
        <div class="col-md-4">
            <div class="kickoff-preview-card ${m.id === activeKickoffId ? 'active' : ''}"
                 onclick="loadKickoffMatch(${m.id})">
                <div class="kickoff-preview-time">${formatTime(m.kickoff)}</div>
                <div class="kickoff-preview-teams">
                    <span>${m.homeFlag} ${m.homeName}</span>
                    <span class="kickoff-preview-vs">vs</span>
                    <span>${m.awayName} ${m.awayFlag}</span>
                </div>
                <div class="text-secondary mt-2" style="font-size:12px;">${m.info}</div>
                <div class="d-flex gap-2 mt-3">
                    <span class="badge badge-ft" style="font-size:10px;">Injuries</span>
                    <span class="badge badge-ft" style="font-size:10px;">Tactics</span>
                    <span class="badge badge-ft" style="font-size:10px;">Facts</span>
                </div>
            </div>
        </div>`).join('');
}

function loadKickoffMatch(id) {
    activeKickoffId = id;
    const m = KICKOFF_MATCHES.find(x => x.id === id);
    if (!m) return;

    // Update tabs + grid highlight
    renderKickoffTabs();
    renderKickoffGrid();

    // Update hero card teams
    document.getElementById('heroHomeFlag').textContent = m.homeFlag;
    document.getElementById('heroHomeName').textContent = m.homeName;
    document.getElementById('heroHomeRecord').textContent = m.homeRecord;
    document.getElementById('heroAwayFlag').textContent = m.awayFlag;
    document.getElementById('heroAwayName').textContent = m.awayName;
    document.getElementById('heroAwayRecord').textContent = m.awayRecord;
    document.getElementById('heroMatchInfo').textContent = m.info;

    // Start countdown
    if (countdownInterval) clearInterval(countdownInterval);
    updateCountdown(m.kickoff);
    countdownInterval = setInterval(() => updateCountdown(m.kickoff), 1000);

    // Load default tab content
    showKickoffTab(null, 'injuries');
    renderInjuries(m);
    renderTactics(m);
    renderFacts(m);
    renderH2H(m);
}

function updateCountdown(kickoffDate) {
    const diff = kickoffDate - new Date();
    if (diff <= 0) {
        document.getElementById('kickoffCountdown').textContent = 'LIVE NOW';
        return;
    }
    const h = Math.floor(diff / 3600000);
    const m = Math.floor((diff % 3600000) / 60000);
    const s = Math.floor((diff % 60000) / 1000);
    document.getElementById('kickoffCountdown').textContent =
        `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`;
}

function renderInjuries(m) {
    const el = document.getElementById('injuriesList');
    if (!el) return;
    const makeRows = (players, teamName) => `
        <div class="col-md-6">
            <div class="injury-card">
                <div class="injury-team-label">${teamName} — Injury Report</div>
                ${players.map(p => `
                    <div class="injury-player-row">
                        <div>
                            <div class="injury-player-name">${p.name}</div>
                            <div class="injury-player-role">${p.role}</div>
                        </div>
                        <span class="injury-status injury-status-${p.status}">
                            ${p.status === 'out' ? '❌ Out' : p.status === 'doubt' ? '⚠️ Doubt' : '✅ Return'}
                        </span>
                    </div>`).join('')}
            </div>
        </div>`;
    el.innerHTML = makeRows(m.injuries.home, m.homeName) + makeRows(m.injuries.away, m.awayName);
}

function renderTactics(m) {
    const el = document.getElementById('tacticsList');
    if (!el) return;
    const makeCard = (t, teamName) => `
        <div class="col-md-6">
            <div class="tactic-card">
                <div class="injury-team-label">${teamName}</div>
                <div class="tactic-formation">${t.formation}</div>
                <div class="tactic-style">${t.style}</div>
                <div class="tactic-key-player">
                    <div class="tactic-key-player-avatar">${t.keyPlayerInitial}</div>
                    <div>
                        <div class="fw-bold" style="font-size:13px;">Key Player</div>
                        <div class="text-secondary" style="font-size:13px;">${t.keyPlayer}</div>
                    </div>
                </div>
            </div>
        </div>`;
    el.innerHTML = makeCard(m.tactics.home, m.homeName) + makeCard(m.tactics.away, m.awayName);
}

function renderFacts(m) {
    const el = document.getElementById('factsList');
    if (!el) return;
    el.innerHTML = m.facts.map(f => `
        <div class="fact-card ${f.color}">
            <div class="fact-emoji">${f.emoji}</div>
            <div class="fact-text">${f.text}</div>
        </div>`).join('');
}

function renderH2H(m) {
    const el = document.getElementById('h2hContent');
    if (!el) return;
    const h = m.h2h;
    const totalMatches = h.homeWins + h.draws + h.awayWins;
    const homePct = Math.round((h.homeWins / totalMatches) * 100);
    const drawPct = Math.round((h.draws / totalMatches) * 100);
    const awayPct = 100 - homePct - drawPct;

    el.innerHTML = `
        <div class="d-flex justify-content-between mb-4">
            <div class="text-center">
                <div style="font-size:32px;font-weight:800;color:var(--green);">${h.homeWins}</div>
                <div class="text-secondary" style="font-size:13px;">${m.homeName} Wins</div>
            </div>
            <div class="text-center">
                <div style="font-size:32px;font-weight:800;color:var(--text-secondary);">${h.draws}</div>
                <div class="text-secondary" style="font-size:13px;">Draws</div>
            </div>
            <div class="text-center">
                <div style="font-size:32px;font-weight:800;color:var(--amber);">${h.awayWins}</div>
                <div class="text-secondary" style="font-size:13px;">${m.awayName} Wins</div>
            </div>
        </div>
        <div class="h2h-stat-row">
            <span class="h2h-val" style="color:var(--green);">${homePct}%</span>
            <div class="h2h-bar-track">
                <div class="h2h-bar-home" style="width:${homePct}%;"></div>
                <div class="h2h-bar-away" style="width:${awayPct}%;"></div>
            </div>
            <span class="h2h-val" style="color:var(--amber);">${awayPct}%</span>
        </div>
        <div class="d-flex justify-content-between mt-4">
            <div class="text-center">
                <div style="font-size:24px;font-weight:800;color:var(--green);">${h.homeGoals}</div>
                <div class="text-secondary" style="font-size:12px;">Goals Scored</div>
            </div>
            <div class="text-center">
                <div style="font-size:24px;font-weight:800;color:var(--text-primary);">${totalMatches}</div>
                <div class="text-secondary" style="font-size:12px;">Total Matches</div>
            </div>
            <div class="text-center">
                <div style="font-size:24px;font-weight:800;color:var(--amber);">${h.awayGoals}</div>
                <div class="text-secondary" style="font-size:12px;">Goals Scored</div>
            </div>
        </div>`;
}

function showKickoffTab(evt, tabName) {
    document.querySelectorAll('.kickoff-tab-panel').forEach(p => p.classList.remove('active'));
    document.querySelectorAll('.kickoff-tab').forEach(t => t.classList.remove('active'));
    const panel = document.getElementById('kpanel-' + tabName);
    if (panel) panel.classList.add('active');
    if (evt) evt.currentTarget.classList.add('active');
}

function filterKickoff(query) {
    const q = query.toLowerCase();
    const filtered = KICKOFF_MATCHES.filter(m =>
        m.homeName.toLowerCase().includes(q) || m.awayName.toLowerCase().includes(q)
    );
    if (filtered.length > 0) loadKickoffMatch(filtered[0].id);
}

function formatTime(date) {
    return date.toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit' });
}
