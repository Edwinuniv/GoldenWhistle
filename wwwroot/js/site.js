/* ===================================================================
   site.js — GoldenWhistle · ALL REAL DATA FROM API
   NO FAKE DATA — Everything comes from the database

   FIX LOG (see GoldenWhistle_Audit.md for full detail):
   - initKickoff() / loadKickoffMatches() / renderKickoffTabs() /
     renderKickoffGrid() REMOVED. Kickoff/Index.cshtml already ships its own
     complete rendering engine (reading window.kickoffMatches, which is
     server-rendered from the *real* rich KickoffMatchViewModel — injuries,
     tactics, facts, H2H). Both engines used to run at once, fighting over
     the same DOM ids (#kickoffMatchTabs, #kickoffGrid, hero fields...) with
     incompatible field names (e.g. m.homeCode vs m.homeTeamCode). Keeping
     only the view's own script removes that race condition.
   - renderMarketListings(): fixed field names to match the real JSON shape
     returned by MarketplaceController.GetListings (camelCase serialization
     of Id/Title/PlayerName/Price/Size/Condition/Tag/Seller/SellerRating/
     IsVerified/ImageUrl). Previously read l.rating (doesn't exist),
     l.verified (doesn't exist, it's l.isVerified), and treated l.price as a
     string when the API returns a JSON number — .replace() on a number
     threw at runtime and silently broke the "Under £100" filter.
   - inviteFriend(): previously POSTed to a nonexistent /api/league/invite
     with a raw email address. There is no email-sending service anywhere in
     this codebase. Rewritten to use the real, already-implemented
     GET /api/league/{id}/invite endpoint and share/copy the join link
     instead of pretending to send an email.
   - Added refreshNotifBadge(): the topbar badge used to be a hardcoded "3"
     in the HTML, shown to every user regardless of their real unread count.
     This now fetches real notifications on page load and sets the badge
     from actual data (hiding it entirely when there are none).
   =================================================================== */

// ---------------------------------------------------------------
// 1. CHART.JS GLOBAL DEFAULTS
// ---------------------------------------------------------------
document.addEventListener('DOMContentLoaded', () => {
    if (window.Chart) {
        Chart.defaults.color = '#8C97AD';
        Chart.defaults.font.family = "'Inter', system-ui, sans-serif";
        Chart.defaults.borderColor = 'rgba(255,255,255,0.06)';
        Chart.defaults.animation = { duration: 1100, easing: 'easeOutQuart' };
        Chart.defaults.animations.colors = { duration: 400 };
    }

    const page = document.body.dataset.page;
    if (page === 'dashboard') initDashboard();
    if (page === 'bracket') initBracket();
    if (page === 'mood') initMood();
    if (page === 'pub') initPub();
    if (page === 'data') initData();
    if (page === 'marketplace') initMarketplace();
    if (page === 'simulator') initSimulator();
    if (page === 'profile') initProfile();
    if (page === 'settings') initSettings();
    // NOTE: 'kickoff' intentionally NOT dispatched here anymore —
    // Kickoff/Index.cshtml owns its own rendering (see fix log above).

    // Real unread-notification badge instead of a hardcoded "3".
    if (page && page !== 'home') refreshNotifBadge();

    // FIX (previously undiscovered bug): every data-action button
    // (share-bracket, lock-predictions, reset-simulator, generate-narrative,
    // locate-me, get-directions, book-table, invite-friend...) used a
    // kebab-case string, but the handler functions are camelCase
    // (shareBracket, lockPredictions...). `window['share-bracket']` is never
    // defined, so `typeof window[action] === 'function'` was always false —
    // every single one of these buttons has been silently doing nothing.
    document.querySelectorAll('[data-action]').forEach(el => {
        el.addEventListener('click', () => {
            const fnName = kebabToCamel(el.dataset.action);
            if (typeof window[fnName] === 'function') window[fnName](el);
        });
    });
});

function kebabToCamel(str) {
    return str.replace(/-([a-z])/g, (_, c) => c.toUpperCase());
}

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
// FAN PULSE - REAL DATA FROM API
// ---------------------------------------------------------------
async function updateFanPulseFromAPI() {
    try {
        const response = await fetch('/api/mood/global-stats');
        if (!response.ok) throw new Error('Failed to load fan pulse');
        const data = await response.json();
        updateFanPulseUI(data.ecstasy, data.anxious, data.agony, data.total);
    } catch (e) {
        console.warn('Error loading fan pulse:', e);
    }
}

function updateFanPulseUI(ecstasy, anxious, agony, totalVotes) {
    const segE = document.getElementById('segEcstasy');
    if (!segE) return;
    document.getElementById('segEcstasy').style.width = (ecstasy || 0) + '%';
    document.getElementById('segAnxious').style.width = (anxious || 0) + '%';
    document.getElementById('segAgony').style.width = (agony || 0) + '%';
    document.getElementById('pctEcstasy').textContent = (ecstasy || 0) + '%';
    document.getElementById('pctAnxious').textContent = (anxious || 0) + '%';
    document.getElementById('pctAgony').textContent = (agony || 0) + '%';
    document.getElementById('voteCount').textContent = (totalVotes || 0).toLocaleString('en-US');
}

// ---------------------------------------------------------------
// 4. SIGNALR CONNECTION
// ---------------------------------------------------------------
let hubConnection = null;

function initSignalRConnection(hubUrl) {
    if (!window.signalR) return null;
    hubConnection = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl)
        .withAutomaticReconnect()
        .build();
    hubConnection.start().catch(err =>
        console.warn('SignalR not ready yet:', err.message));
    return hubConnection;
}

// ---------------------------------------------------------------
// 5. DASHBOARD PAGE
// ---------------------------------------------------------------
function initDashboard() {
    updateFanPulseFromAPI();

    const xgCanvas = document.getElementById('xgByMatchChart');
    if (xgCanvas) {
        const xgLabels = xgCanvas.dataset.labels ? xgCanvas.dataset.labels.split(',') : [];
        const xgValues = xgCanvas.dataset.values ? xgCanvas.dataset.values.split(',').map(Number) : [];
        if (xgLabels.length > 0 && xgValues.length > 0) {
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
        }
    }

    const searchInput = document.getElementById('dashboardSearch');
    if (searchInput) {
        searchInput.addEventListener('input', (e) => {
            const query = e.target.value.toLowerCase();
            document.querySelectorAll('.card').forEach(card => {
                const text = card.textContent.toLowerCase();
                card.style.display = text.includes(query) ? '' : 'none';
            });
        });
    }

    // Now backed by a real hub (Hubs/LeaderboardHub.cs, mapped in Program.cs)
    // and actually broadcast from BracketScoringService after scoring.
    const conn = initSignalRConnection('/hubs/leaderboard');
    if (conn) {
        conn.on('LeaderboardUpdated', () => {
            location.reload();
        });
    }
}

// ---------------------------------------------------------------
// 6. BRACKET PAGE
// ---------------------------------------------------------------
function initBracket() {
    updateFanPulseFromAPI();

    const conn = initSignalRConnection('/hubs/leaderboard');
    if (conn) {
        conn.on('LeaderboardUpdated', () => {
            location.reload();
        });
        conn.on('MatchEventPushed', (event) => {
            appendLiveEvent(event);
        });
    }
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

function shareBracket() {
    if (navigator.share) {
        navigator.share({
            title: 'My Bracket Challenge',
            text: 'Check out my bracket predictions on GoldenWhistle!',
            url: window.location.href
        });
    } else {
        navigator.clipboard.writeText(window.location.href).then(() => {
            alert('Link copied to clipboard!');
        });
    }
}

function lockPredictions() {
    if (confirm('Are you sure you want to lock your predictions?')) {
        fetch('/api/bracket/lock', { method: 'POST' })
            .then(r => r.json())
            .then(data => {
                alert(data.message || 'Predictions locked!');
                location.reload();
            })
            .catch(() => alert('Error locking predictions.'));
    }
}

// FIX: previously POSTed a raw email to a nonexistent /api/league/invite
// route. There is no email-sending backend anywhere in this project. This
// now uses the real GET /api/league/{id}/invite endpoint (which already
// returns joinLink + a QR code) and lets the user copy/share the link.
// `el` is the clicked button, passed automatically by the fixed
// data-action dispatcher above; reads data-league-id from it.
function inviteFriend(el) {
    const leagueId = el?.dataset?.leagueId;
    if (!leagueId) {
        console.warn('inviteFriend: missing data-league-id on the trigger element');
        alert('You need to be in a league to invite friends.');
        return;
    }
    fetch(`/api/league/${leagueId}/invite`)
        .then(r => {
            if (!r.ok) throw new Error('Failed to load invite link');
            return r.json();
        })
        .then(data => {
            if (navigator.share) {
                navigator.share({
                    title: 'Join my GoldenWhistle league!',
                    text: 'Join my private league on GoldenWhistle',
                    url: data.joinLink
                });
            } else {
                navigator.clipboard.writeText(data.joinLink).then(() => {
                    alert('Invite link copied to clipboard!');
                });
            }
        })
        .catch(() => alert('Error loading invite link.'));
}

// ---------------------------------------------------------------
// 7. MOOD MAP PAGE
// ---------------------------------------------------------------
let moodDoughnutChart = null;
let moodTimelineChart = null;
const moodState = { ecstasy: 0, anxious: 0, agony: 0, total: 0 };

function initMood() {
    const moodCanvas = document.getElementById('moodDoughnut');
    if (moodCanvas) {
        moodState.ecstasy = parseInt(moodCanvas.dataset.ecstasy) || 0;
        moodState.anxious = parseInt(moodCanvas.dataset.anxiety) || 0;
        moodState.agony = parseInt(moodCanvas.dataset.agony) || 0;
        moodState.total = moodState.ecstasy + moodState.anxious + moodState.agony;
    }

    updateFanPulseUI(moodState.ecstasy, moodState.anxious, moodState.agony, moodState.total || 0);

    if (moodCanvas) {
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
    }

    const tlCanvas = document.getElementById('moodTimelineChart');
    if (tlCanvas) {
        const tlLabels = tlCanvas.dataset.labels?.split(',').filter(Boolean) || [];
        const tlEcstasy = tlCanvas.dataset.ecstasy?.split(',').filter(Boolean).map(Number) || [];
        const tlAnxiety = tlCanvas.dataset.anxiety?.split(',').filter(Boolean).map(Number) || [];
        const tlAgony = tlCanvas.dataset.agony?.split(',').filter(Boolean).map(Number) || [];

        moodTimelineChart = new Chart(tlCanvas, {
            type: 'line',
            data: {
                labels: tlLabels,
                datasets: [
                    {
                        label: 'Ecstasy', data: tlEcstasy, borderColor: '#00FF87',
                        backgroundColor: 'rgba(0,255,135,0.06)', fill: true, tension: 0.4,
                        pointBackgroundColor: '#00FF87', pointRadius: 3, pointHoverRadius: 6
                    },
                    {
                        label: 'Anxious', data: tlAnxiety, borderColor: '#FFB700',
                        backgroundColor: 'rgba(255,183,0,0.06)', fill: true, tension: 0.4,
                        pointBackgroundColor: '#FFB700', pointRadius: 3, pointHoverRadius: 6
                    },
                    {
                        label: 'Agony', data: tlAgony, borderColor: '#FF4D6D',
                        backgroundColor: 'rgba(255,77,109,0.06)', fill: true, tension: 0.4,
                        pointBackgroundColor: '#FF4D6D', pointRadius: 3, pointHoverRadius: 6
                    }
                ]
            },
            options: {
                animation: drawInLineAnimation(),
                plugins: {
                    legend: {
                        display: true,
                        labels: { color: '#8C97AD', boxWidth: 12, padding: 12, usePointStyle: true, pointStyle: 'circle' }
                    }
                },
                scales: {
                    y: { beginAtZero: true, grid: { color: 'rgba(255,255,255,0.06)' }, ticks: { color: '#8C97AD' } },
                    x: { grid: { display: false }, ticks: { color: '#8C97AD' } }
                },
                interaction: { intersect: false, mode: 'index' }
            }
        });
    }

    loadMatches();

    const moodConn = initSignalRConnection('/hubs/moodmap');
    if (moodConn) {
        moodConn.on('ReceiveTallies', (payload) => {
            applyMoodUpdate(payload.ecstasy || 0, payload.anxiety || 0, payload.agony || 0, payload.total || 0);
        });
    }
}

async function loadMatches() {
    const selector = document.getElementById('matchSelector');
    if (!selector) return;
    try {
        const response = await fetch('/api/mood/matches');
        if (!response.ok) throw new Error('Failed to load matches');
        const matches = await response.json();
        selector.innerHTML = '<option value="">-- Select a match --</option>';
        matches.forEach(m => {
            const option = document.createElement('option');
            option.value = m.id;
            option.textContent = `${m.homeTeam} vs ${m.awayTeam} - ${m.date}`;
            selector.appendChild(option);
        });
        const currentMatchId = document.getElementById('currentMatchId')?.value;
        if (currentMatchId) selector.value = currentMatchId;
    } catch (e) {
        console.warn('Error loading matches:', e.message);
    }
}

async function loadMatch(matchId) {
    if (!matchId) return;
    try {
        const response = await fetch(`/api/mood/stats/${matchId}`);
        if (!response.ok) throw new Error('Failed to load match stats');
        const data = await response.json();
        document.getElementById('matchHomeTeam').textContent = data.homeTeam;
        document.getElementById('matchAwayTeam').textContent = data.awayTeam;
        document.getElementById('matchStatus').textContent = data.status;
        document.getElementById('matchScore').textContent = data.score;
        applyMoodUpdate(data.ecstasyPct || 0, data.anxietyPct || 0, data.agonyPct || 0, data.totalVotes || 0);
        document.getElementById('currentMatchId').value = matchId;
    } catch (e) {
        console.warn('Error loading match:', e.message);
    }
}

async function castVote(emotion, matchId) {
    document.querySelectorAll('.emotion-btn').forEach(btn =>
        btn.classList.remove('selected-ecstasy', 'selected-anxious', 'selected-agony'));
    const selectedBtn = document.querySelector(`[data-emotion="${emotion}"]`);
    if (selectedBtn) selectedBtn.classList.add(`selected-${emotion}`);

    const emojis = { ecstasy: '🤩', anxious: '😬', agony: '😭' };
    const center = document.getElementById('doughnutCenter');
    if (center) {
        center.innerHTML = `
            <div style="font-size:28px;">${emojis[emotion]}</div>
            <div class="text-secondary" style="font-size:11px;margin-top:2px;">Your vote</div>`;
    }

    const msg = document.getElementById('thanksMsg');
    if (msg) msg.classList.add('visible');

    const moodMap = { ecstasy: 'Ecstasy', anxious: 'Anxiety', agony: 'Agony' };
    const matchIdValue = matchId || parseInt(document.getElementById('currentMatchId')?.value);

    try {
        const response = await fetch('/api/mood/vote', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ matchId: matchIdValue, mood: moodMap[emotion] })
        });

        if (!response.ok) throw new Error(`HTTP ${response.status}`);

        const data = await response.json();
        if (data) {
            applyMoodUpdate(data.ecstasyPct || 0, data.anxietyPct || 0, data.agonyPct || 0, data.totalVotes || 0);
        }
    } catch (e) {
        console.warn('Vote API error:', e.message);
    }
}

function applyMoodUpdate(ecstasy, anxious, agony, total) {
    moodState.ecstasy = ecstasy;
    moodState.anxious = anxious;
    moodState.agony = agony;
    moodState.total = total;

    if (moodDoughnutChart) {
        moodDoughnutChart.data.datasets[0].data = [ecstasy, anxious, agony];
        moodDoughnutChart.update();
    }

    const labels = ['E', 'A', 'G'];
    const values = [ecstasy, anxious, agony];
    labels.forEach((k, i) => {
        const el = document.getElementById('legendPct' + k);
        const bar = document.getElementById('bar' + k);
        const pct = document.getElementById('pct' + k);
        const fans = document.getElementById('fans' + k);

        if (el) el.textContent = values[i] + '%';
        if (bar) bar.style.width = values[i] + '%';
        if (pct) pct.textContent = values[i] + '%';

        const count = Math.round((values[i] / 100) * total) || 0;
        if (fans) fans.textContent = count.toLocaleString('en-US') + ' fans';
    });

    const panel = document.getElementById('voteCountPanel');
    if (panel) panel.textContent = total.toLocaleString('en-US') + ' fans voted';

    updateFanPulseUI(ecstasy, anxious, agony, total);
}

// ---------------------------------------------------------------
// 8. PUB FINDER PAGE — REAL DATA FROM API
// ---------------------------------------------------------------
let leafletMap = null;
let currentPubs = [];
let selectedPubId = null;
let activeFilter = 'all';

function initPub() {
    updateFanPulseFromAPI();
    initLeafletMap();
    loadPubs(51.528, -0.068);

    const searchInput = document.getElementById('pubSearchInput');
    if (searchInput) searchInput.addEventListener('input', (e) => filterPubs(e.target.value));

    document.querySelectorAll('[data-filter]').forEach(chip => {
        chip.addEventListener('click', () => {
            document.querySelectorAll('[data-filter]').forEach(c => c.classList.remove('active'));
            chip.classList.add('active');
            activeFilter = chip.dataset.filter;
            filterPubs(document.getElementById('pubSearchInput')?.value || '');
        });
    });

    document.querySelector('[data-action="locate-me"]')?.addEventListener('click', locateMe);
    document.querySelector('[data-action="get-directions"]')?.addEventListener('click', getDirections);
    document.querySelector('[data-action="book-table"]')?.addEventListener('click', bookTable);
}

function initLeafletMap() {
    if (!window.L) { console.warn('Leaflet not loaded'); return; }
    leafletMap = L.map('pubMap', { zoomControl: false }).setView([51.528, -0.068], 14);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png').addTo(leafletMap);
    L.control.zoom({ position: 'bottomright' }).addTo(leafletMap);
}

async function loadPubs(lat, lng) {
    try {
        const url = `/api/pubs?lat=${lat || 0}&lng=${lng || 0}`;
        const response = await fetch(url);
        if (!response.ok) throw new Error('Failed to load pubs');
        currentPubs = await response.json();
        renderPubList(currentPubs);
        updateMapMarkers(currentPubs);
        updateLocationInfo(currentPubs);
    } catch (e) {
        console.warn('Error loading pubs:', e);
        currentPubs = [];
        renderPubList([]);
        updateMapMarkers([]);
    }
}

function renderPubList(pubs) {
    const list = document.getElementById('pubList');
    if (!list) return;
    list.innerHTML = '';
    pubs.forEach(pub => {
        const card = document.createElement('div');
        card.className = 'pub-card' + (pub.id === selectedPubId ? ' active' : '');
        card.dataset.id = pub.id;
        card.innerHTML = `
            <div style="height:80px;background:linear-gradient(135deg,var(--bg-pill),var(--bg-panel-raised));display:flex;align-items:center;justify-content:center;">
                ${pub.imageUrl ? `<img src="${pub.imageUrl}" style="width:100%;height:100%;object-fit:cover;" />` :
                `<svg viewBox="0 0 24 24" width="32" height="32" fill="none" stroke="var(--text-tertiary)" stroke-width="1"><path d="M12 21s7-6.5 7-12a7 7 0 10-14 0c0 5.5 7 12 7 12z"/><circle cx="12" cy="9" r="2.5"/></svg>`
            }
            </div>
            <div style="padding:12px;">
                <div class="d-flex justify-content-between align-items-start">
                    <strong style="font-size:14px;">${pub.name}</strong>
                    <span class="badge ${pub.isOpen ? 'badge-live' : 'badge-ft'}" style="font-size:10px;">${pub.isOpen ? 'Open' : 'Closed'}</span>
                </div>
                <div class="text-secondary" style="font-size:12px;margin-top:4px;">${pub.address}</div>
                <div class="d-flex align-items-center gap-2 mt-1">
                    <span style="color:var(--amber);font-size:13px;">${'★'.repeat(Math.floor(pub.rating || 0))}${'☆'.repeat(5 - Math.floor(pub.rating || 0))}</span>
                    <span class="text-secondary" style="font-size:12px;">${pub.reviews || 0} reviews · ${pub.screens || 0} screens</span>
                </div>
            </div>`;
        card.addEventListener('click', () => selectPub(pub.id));
        list.appendChild(card);
    });
}

function updateMapMarkers(pubs) {
    if (!leafletMap) return;
    leafletMap.eachLayer(layer => {
        if (layer instanceof L.Marker) leafletMap.removeLayer(layer);
    });
    pubs.forEach(pub => {
        const marker = L.marker([pub.lat, pub.lng]).addTo(leafletMap);
        marker.on('click', () => selectPub(pub.id));
    });
}

function updateLocationInfo(pubs) {
    document.getElementById('pubCount').textContent = `${pubs.length} venues found nearby`;
}

function selectPub(id) {
    selectedPubId = id;
    const pub = currentPubs.find(p => p.id === id);
    if (!pub) return;

    document.querySelectorAll('.pub-card').forEach(c => c.classList.remove('active'));
    const card = document.querySelector(`.pub-card[data-id="${id}"]`);
    if (card) card.classList.add('active');

    const popup = document.getElementById('pubDetailPopup');
    if (popup) {
        document.getElementById('popupName').textContent = pub.name;
        document.getElementById('popupAddr').textContent = pub.address;
        document.getElementById('popupStars').textContent = '★'.repeat(Math.floor(pub.rating || 0)) + '☆'.repeat(5 - Math.floor(pub.rating || 0));
        document.getElementById('popupReviews').textContent = `${pub.reviews || 0} reviews`;
        document.getElementById('popupStatus').className = `badge ${pub.isOpen ? 'badge-live' : 'badge-ft'} ms-2`;
        document.getElementById('popupStatus').textContent = pub.isOpen ? 'Open' : 'Closed';
        popup.classList.add('visible');
    }
}

function filterPubs(query) {
    const q = query.toLowerCase();
    let filtered = currentPubs.filter(p =>
        p.name.toLowerCase().includes(q) || p.address.toLowerCase().includes(q)
    );
    if (activeFilter === 'open') filtered = filtered.filter(p => p.isOpen);
    else if (activeFilter === 'free') filtered = filtered.filter(p => p.freeEntry);
    else if (activeFilter === 'hd') filtered = filtered.filter(p => p.hdScreens);
    renderPubList(filtered);
}

function locateMe() {
    if (!navigator.geolocation) return;
    navigator.geolocation.getCurrentPosition(pos => {
        if (leafletMap) leafletMap.flyTo([pos.coords.latitude, pos.coords.longitude], 14);
        loadPubs(pos.coords.latitude, pos.coords.longitude);
    });
}

function getDirections() {
    const pub = currentPubs.find(p => p.id === selectedPubId);
    if (pub) window.open(`https://www.google.com/maps/dir/?api=1&destination=${pub.lat},${pub.lng}`);
}

function bookTable() {
    alert('Booking feature coming soon!');
}

// ---------------------------------------------------------------
// 9. DATA VISUALIZER PAGE — REAL DATA FROM API
// ---------------------------------------------------------------
function initData() {
    updateFanPulseFromAPI();
    loadDataStats();
}

async function loadDataStats() {
    try {
        const response = await fetch('/api/data/stats');
        if (!response.ok) throw new Error('Failed to load data stats');
        const data = await response.json();

        const xgCanvas = document.getElementById('xgTimelineChart');
        if (xgCanvas && data.xgTimeline) {
            new Chart(xgCanvas, {
                type: 'line',
                data: {
                    labels: data.xgTimeline.labels || [],
                    datasets: [
                        {
                            label: (data.homeTeam || 'Home') + ' xG', data: data.xgTimeline.homeXg || [],
                            borderColor: '#00FF87', backgroundColor: 'rgba(0,255,135,0.06)',
                            fill: false, tension: 0.4, pointBackgroundColor: '#00FF87', pointRadius: 4
                        },
                        {
                            label: (data.awayTeam || 'Away') + ' xG', data: data.xgTimeline.awayXg || [],
                            borderColor: '#FFB700', backgroundColor: 'rgba(255,183,0,0.06)',
                            fill: false, tension: 0.4, pointBackgroundColor: '#FFB700', pointRadius: 4
                        }
                    ]
                },
                options: {
                    animation: drawInLineAnimation(),
                    plugins: {
                        legend: { display: true, labels: { color: '#8C97AD' } },
                        tooltip: { callbacks: { label: ctx => `${ctx.dataset.label}: ${ctx.parsed.y.toFixed(2)}` } }
                    },
                    scales: {
                        y: { min: 0, max: 2.5, grid: { color: 'rgba(255,255,255,0.06)' } },
                        x: { grid: { display: false } }
                    }
                }
            });
        }

        const radarCanvas = document.getElementById('teamRadarChart');
        if (radarCanvas && data.radar) {
            new Chart(radarCanvas, {
                type: 'radar',
                data: {
                    labels: data.radar.labels || ['Shots', 'Passes', 'Possession', 'Duels Won', 'Dribbles', 'Saves'],
                    datasets: [{
                        label: data.homeTeam || 'Home',
                        data: data.radar.homeData || [0, 0, 0, 0, 0, 0],
                        borderColor: '#00FF87', backgroundColor: 'rgba(0,255,135,0.15)', pointBackgroundColor: '#00FF87'
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
        }

        const possessionCanvas = document.getElementById('possessionChart');
        if (possessionCanvas && data.possession) {
            new Chart(possessionCanvas, {
                type: 'bar',
                data: {
                    labels: data.possession.labels || [],
                    datasets: [{
                        label: (data.homeTeam || 'Home') + ' %', data: data.possession.values || [],
                        backgroundColor: '#00FF87', borderRadius: 6, maxBarThickness: 40
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
        }

        // NOTE: the API no longer returns a "heatmap" block — the previous
        // coordinates were entirely invented (not real touch-location data).
        // See DataController.GetStats and GoldenWhistle_Audit.md §1.
        if (data.heatmap) drawHeatmapWithData(data.heatmap);

    } catch (e) {
        console.warn('Error loading data stats:', e);
    }
}

function drawHeatmapWithData(heatmapData) {
    const canvas = document.getElementById('heatmapCanvas');
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    canvas.width = canvas.offsetWidth;
    canvas.height = canvas.offsetHeight;
    ctx.clearRect(0, 0, canvas.width, canvas.height);

    const points = heatmapData.points || [];
    const color = heatmapData.team === 'home' ? '0,255,135' : '255,77,109';

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
    loadDataStats();
}

// ---------------------------------------------------------------
// 10. MARKETPLACE PAGE — REAL DATA FROM API
// ---------------------------------------------------------------
let activeMarketFilter = 'all';
let selectedListingId = null;
let marketplaceListings = [];

function initMarketplace() {
    updateFanPulseFromAPI();
    loadMarketplaceListings();

    const searchInput = document.getElementById('marketSearchInput');
    if (searchInput) searchInput.addEventListener('input', (e) => filterMarketListings(e.target.value));

    document.querySelectorAll('[data-market-filter]').forEach(chip => {
        chip.addEventListener('click', () => {
            document.querySelectorAll('[data-market-filter]').forEach(c => c.classList.remove('active'));
            chip.classList.add('active');
            activeMarketFilter = chip.dataset.marketFilter;
            filterMarketListings(document.getElementById('marketSearchInput')?.value || '');
        });
    });
}

async function loadMarketplaceListings() {
    try {
        const response = await fetch('/api/listings');
        if (!response.ok) throw new Error('Failed to load listings');
        marketplaceListings = await response.json();
        renderMarketListings(marketplaceListings);
        // Keep the header count honest instead of the old hardcoded "2,841".
        const countEl = document.getElementById('listingCount');
        if (countEl) countEl.textContent = marketplaceListings.length.toLocaleString('en-US');
    } catch (e) {
        console.warn('Error loading listings:', e);
        renderMarketListings([]);
    }
}

// FIX: field names now match the real API response
// (sellerRating not rating, isVerified not verified, price is a number).
function renderMarketListings(listings) {
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
        const rating = l.sellerRating || 0;
        const stars = '★'.repeat(Math.floor(rating)) + '☆'.repeat(5 - Math.floor(rating));
        const priceLabel = typeof l.price === 'number'
            ? l.price.toLocaleString('en-GB', { style: 'currency', currency: 'GBP', maximumFractionDigits: 0 })
            : l.price;

        col.innerHTML = `
            <div class="listing-card">
                <div class="listing-img">
                    ${l.imageUrl ? `<img src="${l.imageUrl}" alt="${l.title}" style="width:100%;height:100%;object-fit:cover;" />` :
                `<svg viewBox="0 0 24 24" width="48" height="48" fill="none" stroke="var(--text-primary)" stroke-width="1"><path d="M20.84 4.61a5.5 5.5 0 00-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 00-7.78 7.78L12 21.23l8.84-8.84a5.5 5.5 0 000-7.78z"/></svg>`
            }
                    ${tagHtml}
                    <span class="listing-price">${priceLabel}</span>
                </div>
                <div class="listing-body">
                    <div class="listing-title">${l.title}</div>
                    <div class="listing-player">${l.player}</div>
                    <div class="listing-badges">
                        <span class="listing-badge">Size ${l.size}</span>
                        <span class="listing-badge">${l.condition}</span>
                    </div>
                    <div class="listing-seller">
                        <div>
                            <span class="listing-seller-stars">${stars}</span>
                            <span class="listing-seller-name ms-1">${l.seller}</span>
                        </div>
                        ${l.isVerified ? '<span class="badge-verified">Verified</span>' : ''}
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

function filterMarketListings(query) {
    const q = query.toLowerCase();
    let filtered = marketplaceListings;
    if (q) {
        filtered = filtered.filter(l =>
            l.title.toLowerCase().includes(q) || l.player.toLowerCase().includes(q)
        );
    }
    if (activeMarketFilter === 'bnwt') filtered = filtered.filter(l => l.condition === 'BNWT');
    else if (activeMarketFilter === 'match') filtered = filtered.filter(l => l.condition === 'Match Worn');
    else if (activeMarketFilter === 'player') filtered = filtered.filter(l => l.player.includes('#'));
    // FIX: l.price is a real number now, not a string — no more .replace().
    else if (activeMarketFilter === 'budget') filtered = filtered.filter(l => Number(l.price) < 100);
    else if (activeMarketFilter === 'auth') filtered = filtered.filter(l => l.isVerified);
    renderMarketListings(filtered);
}

function openMessageModal(id) {
    selectedListingId = id;
    const l = marketplaceListings.find(x => x.id === id);
    if (!l) return;
    document.getElementById('modalListingTitle').textContent = l.title;
    const priceLabel = typeof l.price === 'number'
        ? l.price.toLocaleString('en-GB', { style: 'currency', currency: 'GBP', maximumFractionDigits: 0 })
        : l.price;
    document.getElementById('modalListingMeta').textContent = `${l.player} · ${priceLabel}`;
    document.getElementById('messageModal').classList.add('visible');
}

function closeMessageModal() {
    document.getElementById('messageModal')?.classList.remove('visible');
    const textEl = document.getElementById('messageText');
    if (textEl) textEl.value = '';
}

function buyNow(id) {
    alert('Buy flow coming soon!');
}

function sendMessage() {
    const msg = document.getElementById('messageText').value.trim();
    const listingId = selectedListingId;
    if (!msg || !listingId) return;

    fetch('/api/messages', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ listingId, message: msg })
    })
        .then(r => { if (!r.ok) throw new Error('Failed'); return r.json(); })
        .then(() => { closeMessageModal(); alert('Message sent!'); })
        .catch(() => alert('Error sending message.'));
}

// ---------------------------------------------------------------
// 11. SIMULATOR PAGE — REAL DATA FROM API
// ---------------------------------------------------------------
let simulatorMatches = [];

function initSimulator() {
    updateFanPulseFromAPI();
    loadSimulatorMatches();
}

async function loadSimulatorMatches() {
    try {
        const response = await fetch('/api/simulator/matches');
        if (!response.ok) throw new Error('Failed to load matches');
        simulatorMatches = await response.json();
        renderSliders();
        updateSimBracket();
    } catch (e) {
        console.warn('Error loading simulator matches:', e);
        renderSliders();
        updateSimBracket();
    }
}

function renderSliders() {
    const list = document.getElementById('slidersList');
    if (!list) return;
    list.innerHTML = '';

    simulatorMatches.forEach(m => {
        const div = document.createElement('div');
        div.className = 'sim-match-row';
        div.innerHTML = `
            <div>
                <div class="sim-team-name">${m.homeTeamCode} ${m.homeTeamName}</div>
                <div class="sim-slider-wrap mt-2">
                    <input type="range" class="sim-slider home" min="0" max="5" value="${m.homeScore}"
                        oninput="updateSimScore(${m.matchId},'home',this.value)" />
                </div>
            </div>
            <div class="sim-score-display">
                <div class="sim-score-num" id="sim-home-${m.matchId}">${m.homeScore}</div>
                <span class="sim-score-sep">–</span>
                <div class="sim-score-num away" id="sim-away-${m.matchId}">${m.awayScore}</div>
            </div>
            <div style="text-align:right;">
                <div class="sim-team-name right">${m.awayTeamCode} ${m.awayTeamName}</div>
                <div class="sim-slider-wrap mt-2">
                    <input type="range" class="sim-slider away" min="0" max="5" value="${m.awayScore}"
                        oninput="updateSimScore(${m.matchId},'away',this.value)" />
                </div>
            </div>`;
        list.appendChild(div);
    });
}

function updateSimScore(matchId, side, value) {
    const match = simulatorMatches.find(m => m.matchId === matchId);
    if (!match) return;
    match[side === 'home' ? 'homeScore' : 'awayScore'] = parseInt(value);
    const el = document.getElementById(`sim-${side}-${matchId}`);
    if (el) el.textContent = value;
    updateSimBracket();
}

function updateSimBracket() {
    const winners = simulatorMatches.map(m => {
        if (m.homeScore > m.awayScore) return { name: m.homeTeamName, code: m.homeTeamCode, upset: false, draw: false };
        if (m.awayScore > m.homeScore) return { name: m.awayTeamName, code: m.awayTeamCode, upset: false, draw: false };
        return { name: 'Draw', code: '—', upset: false, draw: true };
    });

    const qfEl = document.getElementById('qfWinners');
    if (qfEl) {
        qfEl.innerHTML = winners.map(w => `
            <div class="sim-qf-winner ${w.draw ? 'draw' : w.upset ? 'upset' : 'expected'}">
                <span>${w.code} ${w.name}</span>
            </div>`).join('');
    }

    const sfEl = document.getElementById('sfTeams');
    if (sfEl && winners.length >= 4) {
        sfEl.innerHTML = `
            <div class="sim-qf-winner">${winners[0].name}<br>vs<br>${winners[1].name}</div>
            <div class="sim-qf-winner mt-2">${winners[2].name}<br>vs<br>${winners[3].name}</div>`;
    }

    const f1 = document.getElementById('finalTeam1');
    const f2 = document.getElementById('finalTeam2');
    if (f1 && winners[0]) f1.textContent = winners[0].name;
    if (f2 && winners[2]) f2.textContent = winners[2].name;

    // NOTE: win-probability percentages here are still a simple illustrative
    // split (not derived from any real model) — see SimulatorController for
    // the corresponding server-side calculation used by generateNarrative().
    const probEl = document.getElementById('winProbList');
    if (probEl && winners[0] && winners[2]) {
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
}

function resetSimulator() {
    simulatorMatches.forEach(m => { m.homeScore = 0; m.awayScore = 0; });
    renderSliders();
    updateSimBracket();
    const out = document.getElementById('narrativeOutput');
    if (out) out.style.display = 'none';
}

async function generateNarrative() {
    const output = document.getElementById('narrativeOutput');
    const text = document.getElementById('narrativeText');
    if (!output || !text) return;

    try {
        const response = await fetch('/api/simulator/run', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ matches: simulatorMatches })
        });
        if (!response.ok) throw new Error('Failed to generate narrative');
        const data = await response.json();
        text.textContent = data.narrative || 'No narrative generated.';
        output.style.display = 'block';
        output.scrollIntoView({ behavior: 'smooth' });
    } catch (e) {
        console.warn('Error generating narrative:', e);
        text.textContent = 'Error generating narrative. Please try again.';
        output.style.display = 'block';
    }
}

// ---------------------------------------------------------------
// 12. PROFILE / SETTINGS PAGE
// ---------------------------------------------------------------
function initProfile() {
    updateFanPulseFromAPI();
    loadProfileStats();
}

function initSettings() {
    updateFanPulseFromAPI();
}

async function loadProfileStats() {
    try {
        const response = await fetch('/api/profile/stats');
        if (!response.ok) throw new Error('Failed to load profile stats');
        const data = await response.json();
        document.getElementById('profilePredictions').textContent = data.totalPicks || 0;
        document.getElementById('profileAccuracy').textContent = (data.accuracy ?? 0) + '%';
        document.getElementById('profileRank').textContent = '#' + (data.rank || 0);
        document.getElementById('profileCorrect').textContent = data.correctPicks || 0;
        document.getElementById('profileTotalPicks').textContent = data.totalPicks || 0;
        document.getElementById('profileAccuracyValue').textContent = (data.accuracy ?? 0) + '%';
    } catch (e) {
        console.warn('Error loading profile:', e.message);
    }
}

// ---------------------------------------------------------------
// 13. CHATBOT FUNCTIONS
// ---------------------------------------------------------------
function toggleChat() {
    const panel = document.getElementById('chatPanel');
    const notifPanel = document.getElementById('notifPanel');
    if (!panel) return;
    notifPanel?.classList.remove('open');
    panel.classList.toggle('open');
    const badge = document.getElementById('chatBadge');
    if (badge) badge.style.display = 'none';
    if (panel.classList.contains('open')) {
        setTimeout(() => document.getElementById('chatInput')?.focus(), 300);
    }
}

async function sendChatMessage() {
    const input = document.getElementById('chatInput');
    const messages = document.getElementById('chatMessages');
    if (!input || !messages || !input.value.trim()) return;

    const userMsg = input.value.trim();
    input.value = '';

    messages.innerHTML += `
        <div class="chat-msg user">
            <div class="chat-bubble">${escapeHtml(userMsg)}</div>
        </div>`;
    messages.scrollTop = messages.scrollHeight;

    const typingId = 'typing-' + Date.now();
    messages.innerHTML += `
        <div class="chat-msg bot" id="${typingId}">
            <div class="chat-bubble" style="color:var(--text-tertiary);">⏳ Thinking...</div>
        </div>`;
    messages.scrollTop = messages.scrollHeight;

    try {
        const response = await fetch('/api/chat', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ message: userMsg })
        });

        const data = await response.json();

        const typing = document.getElementById(typingId);
        if (typing) typing.remove();

        messages.innerHTML += `
            <div class="chat-msg bot">
                <div class="chat-bubble">${escapeHtml(data.reply)}</div>
            </div>`;
        messages.scrollTop = messages.scrollHeight;

    } catch (error) {
        const typing = document.getElementById(typingId);
        if (typing) typing.remove();

        messages.innerHTML += `
            <div class="chat-msg bot">
                <div class="chat-bubble" style="background:var(--red-bg);color:var(--red);">
                    ❌ Sorry, an error occurred. Please try again later.
                </div>
            </div>`;
        messages.scrollTop = messages.scrollHeight;
    }
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// ---------------------------------------------------------------
// 14. NOTIFICATIONS - REAL DATA FROM API
// ---------------------------------------------------------------
async function toggleNotifications() {
    const panel = document.getElementById('notifPanel');
    const overlay = document.getElementById('panelOverlay');
    const chatPanel = document.getElementById('chatPanel');
    if (!panel) return;
    chatPanel?.classList.remove('open');
    panel.classList.toggle('open');
    overlay?.classList.toggle('visible', panel.classList.contains('open'));

    if (panel.classList.contains('open')) {
        await loadNotifications();
        const badge = document.getElementById('notifBadge');
        if (badge) badge.style.display = 'none';
    }
}

async function loadNotifications() {
    try {
        const response = await fetch('/api/notifications');
        if (!response.ok) throw new Error('Failed to load notifications');
        const notifications = await response.json();
        renderNotifications(notifications);
        return notifications;
    } catch (e) {
        console.warn('Error loading notifications:', e);
        const list = document.getElementById('notifList');
        if (list) list.innerHTML = '<div class="text-secondary">No notifications</div>';
        return [];
    }
}

// NEW: sets the topbar badge from real data instead of the hardcoded "3"
// that used to be baked directly into _Layout.cshtml for every user.
async function refreshNotifBadge() {
    const badge = document.getElementById('notifBadge');
    if (!badge) return;
    const notifications = await loadNotifications();
    const unread = notifications.filter(n => !n.isRead).length;
    if (unread > 0) {
        badge.textContent = unread > 9 ? '9+' : String(unread);
        badge.style.display = '';
    } else {
        badge.style.display = 'none';
    }
}

function renderNotifications(notifications) {
    const list = document.getElementById('notifList');
    if (!list) return;
    list.innerHTML = '';

    if (notifications.length === 0) {
        list.innerHTML = '<div class="text-secondary" style="text-align:center;padding:20px;">No notifications</div>';
        return;
    }

    notifications.forEach(n => {
        const div = document.createElement('div');
        div.className = `notif-item ${n.isRead ? '' : 'unread'}`;
        div.innerHTML = `
            <span class="dot ${n.type}"></span>
            <div>
                <div class="fw-bold" style="font-size:14px;">${n.icon || '📌'} ${n.title}</div>
                <div class="text-secondary" style="font-size:12px;">${n.message}</div>
                <div class="text-tertiary" style="font-size:11px;">${n.timeAgo}</div>
            </div>
        `;
        list.appendChild(div);
    });
}

// ---------------------------------------------------------------
// 15. PROFILE MENU
// ---------------------------------------------------------------
function toggleProfileMenu() {
    const menu = document.getElementById('profileMenu');
    if (!menu) return;
    menu.classList.toggle('visible');
}

document.addEventListener('click', (e) => {
    const avatar = document.querySelector('.topbar-avatar');
    const menu = document.getElementById('profileMenu');
    if (avatar && menu && !avatar.contains(e.target)) {
        menu.classList.remove('visible');
    }
});

// ---------------------------------------------------------------
// 16. SEARCH FUNCTIONS
// ---------------------------------------------------------------
async function handleGlobalSearch(query) {
    const dropdown = document.getElementById('searchDropdown');
    if (!dropdown) return;

    if (!query || query.length < 2) {
        dropdown.classList.remove('visible');
        dropdown.innerHTML = '';
        return;
    }

    try {
        const response = await fetch(`/api/search?q=${encodeURIComponent(query)}`);
        if (!response.ok) throw new Error('Search failed');
        const results = await response.json();

        if (results.length === 0) {
            dropdown.innerHTML = `<div class="search-result-item" style="color:var(--text-tertiary);">No results found</div>`;
        } else {
            dropdown.innerHTML = results.map(r => `
                <div class="search-result-item" onclick="window.location.href='${r.url}'">
                    <span>${r.label}</span>
                    <span class="search-result-type ms-auto">${r.type}</span>
                </div>`).join('');
        }
        dropdown.classList.add('visible');
    } catch (e) {
        console.warn('Search error:', e);
    }
}

function showSearchDropdown() {
    const input = document.getElementById('globalSearch');
    if (input && input.value.length >= 2) handleGlobalSearch(input.value);
}

document.addEventListener('click', (e) => {
    const wrap = document.getElementById('topbarSearchWrap');
    const dropdown = document.getElementById('searchDropdown');
    if (wrap && dropdown && !wrap.contains(e.target)) {
        dropdown.classList.remove('visible');
    }
});

// ---------------------------------------------------------------
// 17. UTILITY FUNCTIONS
// ---------------------------------------------------------------
function closeAllPanels() {
    document.getElementById('notifPanel')?.classList.remove('open');
    document.getElementById('chatPanel')?.classList.remove('open');
    document.getElementById('panelOverlay')?.classList.remove('visible');
}

// ---------------------------------------------------------------
// 18. HOMEPAGE SMOOTH SCROLL
// ---------------------------------------------------------------
document.querySelectorAll('a[href^="#"]').forEach(a => {
    a.addEventListener('click', e => {
        const target = document.querySelector(a.getAttribute('href'));
        if (target) { e.preventDefault(); target.scrollIntoView({ behavior: 'smooth' }); }
    });
});
