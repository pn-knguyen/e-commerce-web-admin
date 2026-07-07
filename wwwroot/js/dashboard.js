/**
 * dashboard.js — Frontend logic cho trang Bảng điều khiển
 *
 * Kiến trúc:
 *   ┌─────────────────────────────┐
 *   │  API Service (fetch layer)  │  ← Gọi /api/dashboard/*
 *   └────────────┬────────────────┘
 *                │
 *   ┌────────────▼────────────────┐
 *   │  Renderers (DOM updaters)   │  ← Cập nhật giao diện
 *   └────────────┬────────────────┘
 *                │
 *   ┌────────────▼────────────────┐
 *   │  Chart builders (Chart.js)  │  ← Vẽ biểu đồ
 *   └─────────────────────────────┘
 *
 * Mọi thay đổi dữ liệu → chỉ cần sửa backend API.
 * Mọi thay đổi giao diện → chỉ cần sửa file này.
 */

'use strict';

// ╔══════════════════════════════════════════════════════════════════╗
// ║  CONFIG                                                          ║
// ╚══════════════════════════════════════════════════════════════════╝
const CONFIG = {
    api: {
        kpis:           '/api/dashboard/kpis',
        revenueChart:   '/api/dashboard/revenue-chart',
        orderStatus:    '/api/dashboard/order-status',
        topProducts:    '/api/dashboard/top-products',
        recentOrders:   '/api/dashboard/recent-orders',
        categoryRevenue:'/api/dashboard/category-revenue',
        traffic:        '/api/dashboard/traffic',
    },
    colors: {
        brand:    '#14b8a6',
        brandDim: 'rgba(20,184,166,0.12)',
        violet:   '#8b5cf6',
        amber:    '#f59e0b',
        slate:    '#94a3b8',
        slateDim: 'rgba(148,163,184,0.12)',
    },
    chart: {
        fontFamily: "'Inter', system-ui, sans-serif",
        fontSize:   12,
    },
};

// ╔══════════════════════════════════════════════════════════════════╗
// ║  CHART.JS GLOBAL DEFAULTS                                        ║
// ╚══════════════════════════════════════════════════════════════════╝
Chart.defaults.font.family  = CONFIG.chart.fontFamily;
Chart.defaults.font.size    = CONFIG.chart.fontSize;
Chart.defaults.color        = '#94a3b8';
Chart.defaults.plugins.legend.display = false;
Chart.defaults.plugins.tooltip.backgroundColor = '#1e293b';
Chart.defaults.plugins.tooltip.titleColor       = '#f1f5f9';
Chart.defaults.plugins.tooltip.bodyColor        = '#cbd5e1';
Chart.defaults.plugins.tooltip.padding          = 10;
Chart.defaults.plugins.tooltip.cornerRadius     = 10;
Chart.defaults.plugins.tooltip.displayColors    = true;
Chart.defaults.plugins.tooltip.boxPadding       = 4;

// Registry để destroy chart cũ trước khi vẽ lại
const chartRegistry = new Map();

// ╔══════════════════════════════════════════════════════════════════╗
// ║  API SERVICE — Tất cả giao tiếp với backend ở đây               ║
// ╚══════════════════════════════════════════════════════════════════╝
const ApiService = {
    /**
     * Gọi GET tới endpoint, tự xử lý lỗi.
     * @param {string} url
     * @returns {Promise<any>}
     */
    async get(url) {
        const response = await fetch(url, {
            method: 'GET',
            headers: { 'Accept': 'application/json' },
            cache: 'no-store',
        });
        if (!response.ok) {
            throw new Error(`API ${url} trả về ${response.status}`);
        }
        return response.json();
    },

    withPeriod(url) {
        const period = document.getElementById('periodSelect')?.value || 'month';
        return `${url}?period=${encodeURIComponent(period)}`;
    },

    fetchKpis()           { return this.get(this.withPeriod(CONFIG.api.kpis)); },
    fetchRevenueChart()   { return this.get(CONFIG.api.revenueChart); },
    fetchOrderStatus()    { return this.get(this.withPeriod(CONFIG.api.orderStatus)); },
    fetchTopProducts()    { return this.get(this.withPeriod(CONFIG.api.topProducts)); },
    fetchRecentOrders()   { return this.get(CONFIG.api.recentOrders); },
    fetchCategoryRevenue(){ return this.get(this.withPeriod(CONFIG.api.categoryRevenue)); },
    fetchTraffic()        { return this.get(CONFIG.api.traffic); },
};

// ╔══════════════════════════════════════════════════════════════════╗
// ║  HELPERS                                                         ║
// ╚══════════════════════════════════════════════════════════════════╝
const Helpers = {
    /**
     * Định dạng số tiền VNĐ
     * @param {number} value
     */
    formatVND(value) {
        if (value >= 1_000_000_000) {
            return (value / 1_000_000_000).toFixed(1).replace('.', ',') + ' tỷ ₫';
        }
        if (value >= 1_000_000) {
            return (value / 1_000_000).toFixed(0) + ' tr ₫';
        }
        return new Intl.NumberFormat('vi-VN', {
            style: 'currency', currency: 'VND', maximumFractionDigits: 0,
        }).format(value);
    },

    /**
     * Định dạng số nguyên
     * @param {number} value
     */
    formatNumber(value) {
        return new Intl.NumberFormat('vi-VN').format(value);
    },

    /**
     * Trả về HTML badge tăng/giảm
     * @param {number} change
     * @param {'up'|'down'} trend
     */
    changeBadge(change, trend) {
        if (change === 0) {
            return '<span class="bg-slate-100 text-slate-600 text-xs font-semibold px-2 py-0.5 rounded-full kpi-badge">→ 0,0%</span>';
        }

        const isUp   = trend === 'up';
        const color  = isUp ? 'bg-emerald-50 text-emerald-700' : 'bg-red-50 text-red-600';
        const arrow  = isUp ? '↑' : '↓';
        const abs    = Math.abs(change).toFixed(1);
        return `<span class="${color} text-xs font-semibold px-2 py-0.5 rounded-full kpi-badge">${arrow} ${abs}%</span>`;
    },

    escapeHtml(value) {
        const element = document.createElement('div');
        element.textContent = value ?? '';
        return element.innerHTML;
    },

    /**
     * Xóa class skeleton khỏi element
     * @param {Element} el
     */
    unSkeleton(el) {
        el.classList.remove('skeleton', 'w-32', 'w-24', 'w-28', 'w-16', 'h-7', 'h-5');
    },

    /**
     * Màu gradient cho sparkline dựa theo trend
     * @param {CanvasRenderingContext2D} ctx
     * @param {string} trend
     */
    sparklineGradient(ctx, trend) {
        const grad = ctx.createLinearGradient(0, 0, 0, 40);
        if (trend === 'up') {
            grad.addColorStop(0, 'rgba(20,184,166,0.4)');
            grad.addColorStop(1, 'rgba(20,184,166,0)');
        } else {
            grad.addColorStop(0, 'rgba(239,68,68,0.4)');
            grad.addColorStop(1, 'rgba(239,68,68,0)');
        }
        return grad;
    },
};

// ╔══════════════════════════════════════════════════════════════════╗
// ║  CHART BUILDERS                                                  ║
// ╚══════════════════════════════════════════════════════════════════╝
const ChartBuilder = {

    /**
     * Vẽ sparkline nhỏ bên dưới mỗi KPI card
     * @param {HTMLCanvasElement} canvas
     * @param {number[]} data
     * @param {'up'|'down'} trend
     */
    buildSparkline(canvas, data, trend) {
        data = Array.isArray(data) && data.length ? data : [0, 0];
        const ctx = canvas.getContext('2d');
        const lineColor = trend === 'up' ? CONFIG.colors.brand : '#ef4444';
        const gradient  = Helpers.sparklineGradient(ctx, trend);

        const id = canvas.dataset.chartId || Math.random().toString(36).slice(2);
        canvas.dataset.chartId = id;
        if (chartRegistry.has(id)) chartRegistry.get(id).destroy();

        const chart = new Chart(ctx, {
            type: 'line',
            data: {
                labels: data.map((_, i) => i),
                datasets: [{
                    data,
                    borderColor:     lineColor,
                    borderWidth:     2,
                    backgroundColor: gradient,
                    tension:         0.4,
                    pointRadius:     0,
                    fill:            true,
                }],
            },
            options: {
                responsive:          true,
                maintainAspectRatio: false,
                animation:           { duration: 800, easing: 'easeOutQuart' },
                plugins:             { legend: { display: false }, tooltip: { enabled: false } },
                scales:              { x: { display: false }, y: { display: false } },
                elements:            { line: { borderCapStyle: 'round' } },
            },
        });

        chartRegistry.set(id, chart);
        return chart;
    },

    /**
     * Biểu đồ đường doanh thu theo tháng
     * @param {object} data
     */
    buildRevenueChart(data) {
        const canvas = document.getElementById('revenueChart');
        if (!canvas) return;

        const ctx = canvas.getContext('2d');

        const gradCurrent  = ctx.createLinearGradient(0, 0, 0, 300);
        gradCurrent.addColorStop(0, 'rgba(20,184,166,0.2)');
        gradCurrent.addColorStop(1, 'rgba(20,184,166,0)');

        const gradPrevious = ctx.createLinearGradient(0, 0, 0, 300);
        gradPrevious.addColorStop(0, 'rgba(148,163,184,0.15)');
        gradPrevious.addColorStop(1, 'rgba(148,163,184,0)');

        if (chartRegistry.has('revenue')) chartRegistry.get('revenue').destroy();

        const chart = new Chart(ctx, {
            type: 'line',
            data: {
                labels: data.labels,
                datasets: [
                    {
                        label:           'Năm nay',
                        data:            data.currentYear,
                        borderColor:     CONFIG.colors.brand,
                        backgroundColor: gradCurrent,
                        borderWidth:     2.5,
                        tension:         0.4,
                        fill:            true,
                        pointRadius:     3,
                        pointHoverRadius:6,
                        pointBackgroundColor: CONFIG.colors.brand,
                    },
                    {
                        label:           'Năm ngoái',
                        data:            data.previousYear,
                        borderColor:     CONFIG.colors.slate,
                        backgroundColor: gradPrevious,
                        borderWidth:     1.5,
                        tension:         0.4,
                        fill:            true,
                        pointRadius:     2,
                        pointHoverRadius:5,
                        borderDash:      [5, 4],
                        pointBackgroundColor: CONFIG.colors.slate,
                    },
                ],
            },
            options: {
                responsive:          true,
                maintainAspectRatio: false,
                interaction:         { mode: 'index', intersect: false },
                animation:           { duration: 900, easing: 'easeOutQuart' },
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        callbacks: {
                            label: ctx => ` ${ctx.dataset.label}: ${ctx.parsed.y.toLocaleString('vi-VN')} tr ₫`,
                        },
                    },
                },
                scales: {
                    x: {
                        grid: { display: false },
                        border: { dash: [4, 4] },
                        ticks: { font: { size: 11 } },
                    },
                    y: {
                        grid: { color: '#f1f5f9' },
                        border: { display: false },
                        ticks: {
                            font: { size: 11 },
                            callback: v => v + ' tr',
                        },
                    },
                },
            },
        });

        chartRegistry.set('revenue', chart);
    },

    /**
     * Biểu đồ tròn trạng thái đơn hàng
     * @param {object} data
     */
    buildOrderStatusChart(data) {
        const canvas = document.getElementById('orderStatusChart');
        if (!canvas) return;

        if (chartRegistry.has('orderStatus')) chartRegistry.get('orderStatus').destroy();

        const total = data.values.reduce((sum, value) => sum + value, 0);
        const hasData = total > 0;

        const chart = new Chart(canvas.getContext('2d'), {
            type: 'doughnut',
            data: {
                labels: hasData ? data.labels : ['Chưa có dữ liệu'],
                datasets: [{
                    data: hasData ? data.values : [1],
                    backgroundColor: hasData ? data.colors : ['#e2e8f0'],
                    borderWidth:     0,
                    hoverOffset:     8,
                }],
            },
            options: {
                responsive:          true,
                maintainAspectRatio: false,
                cutout:              '70%',
                animation:           { duration: 900, animateRotate: true },
                plugins: {
                    legend:  { display: false },
                    tooltip: {
                        callbacks: {
                            label: ctx => hasData
                                ? ` ${ctx.label}: ${Helpers.formatNumber(ctx.raw)} đơn`
                                : ' Chưa có đơn hàng trong kỳ',
                        },
                    },
                },
            },
        });

        chartRegistry.set('orderStatus', chart);

        // Vẽ legend tùy chỉnh
        const legendEl = document.getElementById('orderStatusLegend');
        if (legendEl) {
            if (!hasData) {
                legendEl.innerHTML = '<p class="text-xs text-slate-400 text-center py-3">Chưa có đơn hàng trong kỳ</p>';
                return;
            }

            legendEl.innerHTML = data.labels.map((label, i) => {
                const pct = ((data.values[i] / total) * 100).toFixed(1);
                return `
                    <div class="flex items-center justify-between gap-2">
                        <div class="flex items-center gap-2 min-w-0">
                            <span class="w-2.5 h-2.5 rounded-full shrink-0" style="background:${data.colors[i]}"></span>
                            <span class="text-xs text-slate-600 truncate">${label}</span>
                        </div>
                        <div class="flex items-center gap-2 shrink-0">
                            <span class="text-xs font-semibold text-slate-800">${Helpers.formatNumber(data.values[i])}</span>
                            <span class="text-[10px] text-slate-400">${pct}%</span>
                        </div>
                    </div>`;
            }).join('');
        }
    },

    /** Biểu đồ hoạt động hệ thống trong 7 ngày gần nhất. */
    buildTrafficChart(data) {
        const canvas = document.getElementById('trafficChart');
        if (!canvas) return;

        if (chartRegistry.has('traffic')) chartRegistry.get('traffic').destroy();

        const ctx = canvas.getContext('2d');

        const chart = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: data.labels,
                datasets: [
                    {
                        type:            'bar',
                        label:           'Đơn hàng',
                        data:            data.orders,
                        backgroundColor: CONFIG.colors.brandDim,
                        borderColor:     CONFIG.colors.brand,
                        borderWidth:     1.5,
                        borderRadius:    6,
                        order:           2,
                        yAxisID:         'y',
                    },
                    {
                        type:            'line',
                        label:           'Doanh thu (triệu ₫)',
                        data:            data.revenue,
                        borderColor:     CONFIG.colors.violet,
                        backgroundColor: 'transparent',
                        borderWidth:     2,
                        tension:         0.4,
                        pointRadius:     3,
                        pointHoverRadius:6,
                        pointBackgroundColor: CONFIG.colors.violet,
                        order:           1,
                        yAxisID:         'yRevenue',
                    },
                    {
                        type:            'line',
                        label:           'Khách mới',
                        data:            data.newCustomers,
                        borderColor:     CONFIG.colors.amber,
                        backgroundColor: 'transparent',
                        borderWidth:     2,
                        tension:         0.4,
                        pointRadius:     3,
                        pointHoverRadius:6,
                        borderDash:      [4, 3],
                        pointBackgroundColor: CONFIG.colors.amber,
                        order:           0,
                        yAxisID:         'y',
                    },
                ],
            },
            options: {
                responsive:          true,
                maintainAspectRatio: false,
                interaction:         { mode: 'index', intersect: false },
                animation:           { duration: 900, easing: 'easeOutQuart' },
                plugins: {
                    legend:  { display: false },
                    tooltip: {
                        callbacks: {
                            label: ctx => ctx.dataset.yAxisID === 'yRevenue'
                                ? ` ${ctx.dataset.label}: ${ctx.parsed.y.toLocaleString('vi-VN')} tr ₫`
                                : ` ${ctx.dataset.label}: ${Helpers.formatNumber(ctx.parsed.y)}`,
                        },
                    },
                },
                scales: {
                    x: {
                        grid:   { display: false },
                        border: { dash: [4, 4] },
                        ticks:  { font: { size: 11 } },
                    },
                    y: {
                        grid:   { color: '#f1f5f9' },
                        border: { display: false },
                        beginAtZero: true,
                        ticks: { font: { size: 11 }, precision: 0 },
                    },
                    yRevenue: {
                        position: 'right',
                        beginAtZero: true,
                        grid: { drawOnChartArea: false },
                        border: { display: false },
                        ticks: {
                            font: { size: 11 },
                            callback: value => value + ' tr',
                        },
                    },
                },
            },
        });

        chartRegistry.set('traffic', chart);
    },
};

// ╔══════════════════════════════════════════════════════════════════╗
// ║  RENDERERS — Cập nhật DOM                                        ║
// ╚══════════════════════════════════════════════════════════════════╝
const Renderer = {

    /**
     * Render KPI cards từ data API
     */
    renderKpis(data) {
        const cards = [
            { id: 'kpi-revenue',   key: 'revenue',   format: Helpers.formatVND },
            { id: 'kpi-orders',    key: 'orders',    format: Helpers.formatNumber },
            { id: 'kpi-customers', key: 'customers', format: Helpers.formatNumber },
            { id: 'kpi-products',  key: 'products',  format: Helpers.formatNumber },
        ];

        cards.forEach(({ id, key, format }) => {
            const card   = document.getElementById(id);
            if (!card) return;
            const info   = data[key] || { value: 0, change: 0, trend: 'up', sparkline: [0, 0] };
            const valueEl = card.querySelector('.kpi-value');
            const badgeEl = card.querySelector('.kpi-badge');
            const sparkEl = card.querySelector('.kpi-spark');

            // Animate value
            Helpers.unSkeleton(valueEl);
            valueEl.textContent = format(info.value);

            // Badge
            Helpers.unSkeleton(badgeEl);
            badgeEl.outerHTML = Helpers.changeBadge(info.change, info.trend);

            // Sparkline
            if (sparkEl) {
                ChartBuilder.buildSparkline(sparkEl, info.sparkline, info.trend);
            }
        });
    },

    /**
     * Render top products
     */
    renderTopProducts(data) {
        const container = document.getElementById('topProductsList');
        if (!container) return;

        const rankColors = ['text-amber-500', 'text-slate-400', 'text-orange-400', 'text-slate-300', 'text-slate-200'];

        if (!data.length) {
            container.innerHTML = '<p class="text-sm text-slate-400 text-center py-10">Chưa có sản phẩm bán thành công trong kỳ</p>';
            return;
        }

        container.innerHTML = data.map((p, i) => {
            const growthClass = p.growth >= 0 ? 'text-emerald-600' : 'text-red-500';
            const growthSign  = p.growth >= 0 ? '+' : '';
            return `
            <div class="flex items-center gap-3 py-2.5 border-b border-slate-50 last:border-0 hover:bg-slate-50 rounded-lg px-2 -mx-2 transition-colors cursor-default">
                <span class="text-lg font-black ${rankColors[i]} w-5 text-center shrink-0">${p.rank}</span>
                <div class="flex-1 min-w-0">
                    <p class="text-sm font-medium text-slate-800 truncate" title="${Helpers.escapeHtml(p.name)}">${Helpers.escapeHtml(p.name)}</p>
                    <p class="text-xs text-slate-400">${Helpers.escapeHtml(p.category)} · ${Helpers.formatNumber(p.sold)} sản phẩm</p>
                </div>
                <div class="text-right shrink-0">
                    <p class="text-sm font-bold text-slate-800">${Helpers.formatVND(p.revenue)}</p>
                    <p class="text-xs font-medium ${growthClass}">${growthSign}${p.growth}%</p>
                </div>
            </div>`;
        }).join('');
    },

    /**
     * Render bảng đơn hàng gần đây
     */
    renderRecentOrders(data) {
        const tbody = document.getElementById('recentOrdersBody');
        if (!tbody) return;

        if (!data.length) {
            tbody.innerHTML = '<tr><td colspan="5" class="py-10 text-sm text-slate-400 text-center">Chưa có đơn hàng</td></tr>';
            return;
        }

        tbody.innerHTML = data.map(order => {
            return `
            <tr class="border-b border-slate-50 hover:bg-slate-50/60 transition-colors">
                <td class="py-3 pr-4">
                    <span class="font-mono text-xs font-bold text-brand-700 bg-brand-50 px-2 py-0.5 rounded">${Helpers.escapeHtml(order.id)}</span>
                </td>
                <td class="py-3 pr-4">
                    <span class="text-sm text-slate-700 font-medium">${Helpers.escapeHtml(order.customer)}</span>
                </td>
                <td class="py-3 pr-4">
                    <span class="text-sm font-bold text-slate-800">${Helpers.formatVND(order.total)}</span>
                </td>
                <td class="py-3 pr-4">
                    <span class="badge-${order.statusKey} text-xs font-semibold px-2.5 py-0.5 rounded-full whitespace-nowrap">
                        ${Helpers.escapeHtml(order.status)}
                    </span>
                </td>
                <td class="py-3">
                    <span class="text-xs text-slate-400">${order.date}</span>
                </td>
            </tr>`;
        }).join('');
    },

    /**
     * Render thanh bar ngang cho doanh thu theo danh mục
     */
    renderCategoryRevenue(data) {
        const container = document.getElementById('categoryRevenueBars');
        if (!container) return;

        const colors = [
            'bg-brand-500', 'bg-blue-500', 'bg-violet-500',
            'bg-amber-500', 'bg-rose-500', 'bg-slate-400',
        ];

        if (!data.labels.length) {
            container.innerHTML = '<p class="text-sm text-slate-400 text-center py-10">Chưa có doanh thu trong kỳ</p>';
            return;
        }

        container.innerHTML = data.labels.map((label, i) => {
            const pct = data.values[i];
            return `
            <div>
                <div class="flex items-center justify-between mb-1">
                    <span class="text-xs font-medium text-slate-700">${Helpers.escapeHtml(label)}</span>
                    <span class="text-xs font-bold text-slate-800">${pct}%</span>
                </div>
                <div class="h-2 bg-slate-100 rounded-full overflow-hidden">
                    <div class="${colors[i]} h-full rounded-full transition-all duration-700"
                         style="width: 0%"
                         data-width="${pct}%"></div>
                </div>
            </div>`;
        }).join('');

        // Animate bars sau khi render
        requestAnimationFrame(() => {
            container.querySelectorAll('[data-width]').forEach(bar => {
                setTimeout(() => { bar.style.width = bar.dataset.width; }, 100);
            });
        });
    },

    /**
     * Cập nhật timestamp "cập nhật lần cuối"
     */
    renderLastUpdated() {
        const el = document.getElementById('lastUpdated');
        if (el) {
            el.textContent = new Date().toLocaleString('vi-VN', {
                day: '2-digit', month: '2-digit', year: 'numeric',
                hour: '2-digit', minute: '2-digit', second: '2-digit',
            });
        }
    },
};

// ╔══════════════════════════════════════════════════════════════════╗
// ║  DASHBOARD CONTROLLER — Điều phối tất cả                         ║
// ╚══════════════════════════════════════════════════════════════════╝
const Dashboard = {

    /**
     * Tải và render tất cả widget song song
     */
    async loadAll() {
        // Indicator loading trên nút refresh
        const refreshBtn = document.getElementById('refreshBtn');
        if (refreshBtn) {
            refreshBtn.querySelector('svg, i')?.classList.add('animate-spin');
        }

        try {
            // Chạy tất cả API calls song song
            const [
                kpis,
                revenueData,
                orderStatusData,
                topProducts,
                recentOrders,
                categoryRevenue,
                trafficData,
            ] = await Promise.allSettled([
                ApiService.fetchKpis(),
                ApiService.fetchRevenueChart(),
                ApiService.fetchOrderStatus(),
                ApiService.fetchTopProducts(),
                ApiService.fetchRecentOrders(),
                ApiService.fetchCategoryRevenue(),
                ApiService.fetchTraffic(),
            ]);

            // Render từng phần, bắt lỗi độc lập
            if (kpis.status === 'fulfilled')
                Renderer.renderKpis(kpis.value);

            if (revenueData.status === 'fulfilled')
                ChartBuilder.buildRevenueChart(revenueData.value);

            if (orderStatusData.status === 'fulfilled')
                ChartBuilder.buildOrderStatusChart(orderStatusData.value);

            if (topProducts.status === 'fulfilled')
                Renderer.renderTopProducts(topProducts.value);

            if (recentOrders.status === 'fulfilled')
                Renderer.renderRecentOrders(recentOrders.value);

            if (categoryRevenue.status === 'fulfilled')
                Renderer.renderCategoryRevenue(categoryRevenue.value);

            if (trafficData.status === 'fulfilled')
                ChartBuilder.buildTrafficChart(trafficData.value);

            Renderer.renderLastUpdated();

            // Re-init Lucide icons sau khi render HTML mới
            if (window.lucide) lucide.createIcons();

        } catch (err) {
            console.error('[Dashboard] Lỗi không mong đợi:', err);
        } finally {
            if (refreshBtn) {
                refreshBtn.querySelector('svg, i')?.classList.remove('animate-spin');
            }
        }
    },
};

// ╔══════════════════════════════════════════════════════════════════╗
// ║  INIT                                                            ║
// ╚══════════════════════════════════════════════════════════════════╝

// Expose refresh function cho nút bấm trong layout
window.dashboardRefresh = () => Dashboard.loadAll();

// Tải dữ liệu lần đầu khi DOM sẵn sàng
document.addEventListener('DOMContentLoaded', () => {
    document.getElementById('periodSelect')?.addEventListener('change', () => Dashboard.loadAll());
    Dashboard.loadAll();
});
