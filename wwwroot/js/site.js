/* ===================================================================
   site.js — WorldCupHub shared frontend utilities
   Dev B: chart animation helpers + fan pulse live updates
   =================================================================== */

// ---------------------------------------------------------------
// Chart.js global defaults — dark theme + "draws itself" animation
// ---------------------------------------------------------------
if (window.Chart) {
    Chart.defaults.color = '#8C97AD';
    Chart.defaults.font.family = "'Inter', system-ui, sans-serif";
    Chart.defaults.borderColor = 'rgba(255,255,255,0.06)';

    // Default animation: charts draw progressively left-to-right / bottom-up
    Chart.defaults.animation = {
        duration: 1100,
        easing: 'easeOutQuart'
    };
    Chart.defaults.animations.colors = { duration: 400 };
}

/**
 * Returns a Chart.js animation config that makes lines/areas
 * "draw themselves" — point-by-point reveal, used for xG Timeline,
 * Mood Timeline, etc.
 */
function drawInLineAnimation() {
    return {
        x: { type: 'number', easing: 'linear', duration: 1200, from: NaN, delay(ctx) {
            if (ctx.type !== 'data' || ctx.xStarted) return 0;
            ctx.xStarted = true;
            return ctx.index * 60;
        }},
        y: { type: 'number', easing: 'easeOutQuart', duration: 800, from(ctx) {
            if (ctx.index === 0) return ctx.chart.scales.y.getPixelForValue(0);
            return ctx.chart.getDatasetMeta(ctx.datasetIndex).data[ctx.index - 1].getProps(['y'], true).y;
        }}
    };
}

/**
 * Returns a Chart.js animation config for bars growing from baseline.
 */
function drawInBarAnimation() {
    return {
        y: { duration: 900, easing: 'easeOutQuart', from: (ctx) => ctx.chart.scales.y.getPixelForValue(0) }
    };
}

// ---------------------------------------------------------------
// Fan Pulse bar — live update helper
// Called by SignalR handlers on every page once MoodHub is wired up.
// ---------------------------------------------------------------
function updateFanPulse(ecstasy, anxious, agony, totalVotes) {
    const segE = document.getElementById('segEcstasy');
    const segA = document.getElementById('segAnxious');
    const segG = document.getElementById('segAgony');
    if (!segE) return;

    segE.style.width = ecstasy + '%';
    segA.style.width = anxious + '%';
    segG.style.width = agony + '%';

    document.getElementById('pctEcstasy').textContent = ecstasy + '%';
    document.getElementById('pctAnxious').textContent = anxious + '%';
    document.getElementById('pctAgony').textContent = agony + '%';
    document.getElementById('voteCount').textContent = totalVotes.toLocaleString('en-US');
}

// ---------------------------------------------------------------
// SignalR connection stub
// NOTE: hub URL + method names to be finalized with Dev A (M1 contract).
// This connects once the MoodHub / LeaderboardHub exist server-side.
// ---------------------------------------------------------------
let hubConnection = null;

function initSignalRConnection(hubUrl) {
    if (!window.signalR) return null;

    hubConnection = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl)
        .withAutomaticReconnect()
        .build();

    hubConnection.start().catch(err => console.warn('SignalR connection failed (expected until backend is wired):', err.message));

    return hubConnection;
}

// Placeholder: will receive real mood updates from MoodHub once Dev A's
// backend pushes them. For now pages call updateFanPulse() with mock data.