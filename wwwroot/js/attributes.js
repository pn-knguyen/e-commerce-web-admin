'use strict';

/* ── Helpers ─────────────────────────────────────────────────────────────── */

function getAntiForgeryToken() {
    const el = document.querySelector('input[name="__RequestVerificationToken"]');
    return el ? el.value : '';
}

async function apiPost(url, body) {
    const res = await fetch(url, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': getAntiForgeryToken(),
        },
        body: JSON.stringify(body),
    });
    if (!res.ok) {
        const text = await res.text().catch(() => '');
        throw new Error(text || `HTTP ${res.status}`);
    }
    return res.json();
}

function showBanner(message, type = 'success') {
    const banner = document.getElementById(type === 'success' ? 'toastSuccess' : 'toastError');
    if (!banner) return;
    const textEl = banner.querySelector('[data-toast-text]');
    if (textEl) textEl.textContent = message;
    banner.classList.remove('hidden');
    setTimeout(() => banner.classList.add('hidden'), 4000);
}

/* ── Toast dismiss ───────────────────────────────────────────────────────── */

document.addEventListener('click', e => {
    const btn = e.target.closest('[data-dismiss-target]');
    if (!btn) return;
    const target = document.getElementById(btn.dataset.dismissTarget);
    target?.classList.add('hidden');
});

/* ── Delete confirm (standard form) ─────────────────────────────────────── */

document.addEventListener('submit', e => {
    const form = e.target.closest('[data-attr-delete]');
    if (!form) return;
    const name = form.dataset.attrName ?? 'thuộc tính này';
    const optionCount = parseInt(form.dataset.optionCount ?? '0', 10);
    const categoryCount = parseInt(form.dataset.categoryCount ?? '0', 10);

    if (categoryCount > 0 || optionCount > 0) {
        e.preventDefault();

        const blockers = [];
        if (categoryCount > 0) blockers.push(`${categoryCount} danh mục`);
        if (optionCount > 0) blockers.push(`${optionCount} giá trị`);

        alert(`Không thể xoá "${name}" vì đang được dùng (${blockers.join(', ')}).`);
        return;
    }

    if (!confirm(`Bạn có chắc muốn xoá thuộc tính "${name}"?`)) {
        e.preventDefault();
    }
});

/* ── Create page option drafts ──────────────────────────────────────────── */

const createOptionList = document.querySelector('[data-create-option-list]');
if (createOptionList) {
    const createOptionTemplate = document.getElementById('createOptionRowTemplate');
    const createOptionAddBtn = document.querySelector('[data-add-create-option]');
    const createAttributeForm = createOptionList.closest('form');

    function getCreateOptionRows() {
        return [...createOptionList.querySelectorAll('[data-create-option-row]')];
    }

    function reindexCreateOptionRows() {
        getCreateOptionRows().forEach((row, index) => {
            const valueInput = row.querySelector('[data-create-option-value]');
            const labelInput = row.querySelector('[data-create-option-label]');
            const valueError = row.querySelector('[data-valmsg-for$=".Value"]');
            const labelError = row.querySelector('[data-valmsg-for$=".Label"]');

            if (valueInput) valueInput.name = `Options[${index}].Value`;
            if (labelInput) labelInput.name = `Options[${index}].Label`;
            if (valueError) valueError.dataset.valmsgFor = `Options[${index}].Value`;
            if (labelError) labelError.dataset.valmsgFor = `Options[${index}].Label`;
        });
    }

    function appendCreateOptionRow() {
        if (!createOptionTemplate) return;

        const index = getCreateOptionRows().length;
        const wrapper = document.createElement('div');
        wrapper.innerHTML = createOptionTemplate.innerHTML.replaceAll('__index__', String(index)).trim();

        const row = wrapper.firstElementChild;
        if (!row) return;

        createOptionList.appendChild(row);
        row.querySelector('[data-create-option-value]')?.focus();
        lucide.createIcons({ nodes: [row] });
    }

    createOptionAddBtn?.addEventListener('click', appendCreateOptionRow);

    createOptionList.addEventListener('click', e => {
        const removeBtn = e.target.closest('[data-remove-create-option]');
        if (!removeBtn) return;

        const rows = getCreateOptionRows();
        const row = removeBtn.closest('[data-create-option-row]');
        if (!row) return;

        if (rows.length === 1) {
            row.querySelectorAll('input').forEach(input => {
                input.value = '';
            });
            return;
        }

        row.remove();
        reindexCreateOptionRows();
    });

    createAttributeForm?.addEventListener('submit', reindexCreateOptionRows);
}

/* ══════════════════════════════════════════════════════════════════════════
   OPTIONS PANEL  (chỉ hoạt động trên trang Edit)
   ══════════════════════════════════════════════════════════════════════════ */

const optionsPanel = document.getElementById('optionsPanel');
if (optionsPanel) {
    const attributeId = optionsPanel.dataset.attributeId;
    const optionsList  = document.getElementById('optionsList');
    const optionCount  = document.getElementById('optionCount');
    const addForm      = document.getElementById('addOptionForm');
    const filterInput  = document.getElementById('optionFilter');

    /* ── Render options list ─────────────────────────────────────────────── */

    function renderOptions(options) {
        if (!optionsList) return;
        optionsList.innerHTML = '';

        if (options.length === 0) {
            optionsList.innerHTML = `
                <div class="py-10 text-center">
                    <i data-lucide="inbox" class="w-8 h-8 text-slate-300 mx-auto mb-2"></i>
                    <p class="text-sm text-slate-400">Chưa có giá trị nào. Thêm bên dưới.</p>
                </div>`;
            lucide.createIcons({ nodes: [optionsList] });
            return;
        }

        options.forEach(opt => {
            const row = document.createElement('div');
            row.className = 'option-row';
            row.dataset.optionId = opt.id;
            row.innerHTML = `
                <span class="attr-code-badge text-xs">${escHtml(opt.value)}</span>
                <input type="text"
                       class="option-label-input"
                       value="${escHtml(opt.label)}"
                       data-original="${escHtml(opt.label)}"
                       data-label-input />
                <span class="inline-flex items-center gap-1 text-xs font-semibold rounded-full px-2 py-0.5
                             ${opt.variantUsageCount > 0 ? 'bg-blue-100 text-blue-700' : 'bg-slate-100 text-slate-400'}">
                    <i data-lucide="layers" class="w-3 h-3"></i>
                    ${opt.variantUsageCount}
                </span>
                <div class="flex gap-1">
                    <button type="button" class="saving-spinner" data-spinner></button>
                    <button type="button"
                            class="attr-action-icon attr-action-edit hidden"
                            title="Lưu thay đổi"
                            data-save-btn>
                        <i data-lucide="check" class="w-3.5 h-3.5"></i>
                    </button>
                    <button type="button"
                            class="attr-action-icon attr-action-delete"
                            title="Xoá option"
                            data-delete-btn
                            data-option-label="${escHtml(opt.label)}"
                            data-usage="${opt.variantUsageCount}">
                        <i data-lucide="trash-2" class="w-3.5 h-3.5"></i>
                    </button>
                </div>`;
            optionsList.appendChild(row);
        });

        lucide.createIcons({ nodes: [optionsList] });
        if (optionCount) optionCount.textContent = options.length;
    }

    /* ── Load options from API ───────────────────────────────────────────── */

    async function loadOptions() {
        if (!optionsList) return;
        optionsList.innerHTML = `
            <div class="p-4 space-y-2">
                ${[1,2,3].map(() => `
                    <div class="option-skeleton w-full h-9 rounded-lg"></div>`).join('')}
            </div>`;

        try {
            const res = await fetch(`/Attributes/${attributeId}/Options`);
            if (!res.ok) throw new Error(`HTTP ${res.status}`);
            const data = await res.json();
            renderOptions(data.options ?? []);
        } catch (err) {
            optionsList.innerHTML = `
                <div class="p-4 text-center text-sm text-red-500">
                    Không tải được danh sách. <button type="button" class="underline" data-retry-options>Thử lại</button>
                </div>`;
        }
    }

    /* ── Inline label edit ───────────────────────────────────────────────── */

    optionsList?.addEventListener('input', e => {
        const input = e.target.closest('[data-label-input]');
        if (!input) return;
        const isDirty = input.value !== input.dataset.original;
        input.classList.toggle('dirty', isDirty);
        const row = input.closest('.option-row');
        row?.querySelector('[data-save-btn]')?.classList.toggle('hidden', !isDirty);
    });

    optionsList?.addEventListener('click', async e => {
        const retryBtn = e.target.closest('[data-retry-options]');
        if (retryBtn) {
            await loadOptions();
            return;
        }

        /* Save button */
        const saveBtn = e.target.closest('[data-save-btn]');
        if (saveBtn) {
            const row = saveBtn.closest('.option-row');
            const optionId = row.dataset.optionId;
            const input = row.querySelector('[data-label-input]');
            const spinner = row.querySelector('[data-spinner]');

            spinner.style.display = 'inline-block';
            saveBtn.classList.add('hidden');

            try {
                const data = await apiPost(`/Attributes/Options/${optionId}/Update`, { label: input.value });
                if (data.succeeded) {
                    input.dataset.original = input.value;
                    input.classList.remove('dirty');
                    showBanner(data.message, 'success');
                } else {
                    showBanner(data.message, 'error');
                    saveBtn.classList.remove('hidden');
                }
            } catch {
                showBanner('Lỗi kết nối, vui lòng thử lại.', 'error');
                saveBtn.classList.remove('hidden');
            } finally {
                spinner.style.display = 'none';
            }
            return;
        }

        /* Delete button */
        const deleteBtn = e.target.closest('[data-delete-btn]');
        if (deleteBtn) {
            const label   = deleteBtn.dataset.optionLabel;
            const usage   = parseInt(deleteBtn.dataset.usage ?? '0', 10);
            const row     = deleteBtn.closest('.option-row');
            const optionId = row.dataset.optionId;

            if (usage > 0) {
                alert(`Không thể xoá option "${label}" vì đang được dùng bởi ${usage} biến thể sản phẩm.`);
                return;
            }

            if (!confirm(`Bạn có chắc muốn xoá option "${label}"?`)) return;

            try {
                const data = await apiPost(`/Attributes/Options/${optionId}/Delete`, {});
                if (data.succeeded) {
                    row.remove();
                    const remaining = optionsList.querySelectorAll('.option-row').length;
                    if (optionCount) optionCount.textContent = remaining;
                    if (remaining === 0) renderOptions([]);
                    showBanner(data.message, 'success');
                } else {
                    showBanner(data.message, 'error');
                }
            } catch {
                showBanner('Lỗi kết nối, vui lòng thử lại.', 'error');
            }
        }
    });

    /* ── Add option form ─────────────────────────────────────────────────── */

    addForm?.addEventListener('submit', async e => {
        e.preventDefault();
        const valueInput = addForm.querySelector('[name="value"]');
        const labelInput = addForm.querySelector('[name="label"]');
        const submitBtn  = addForm.querySelector('[type="submit"]');

        const payload = {
            value: valueInput.value.trim(),
            label: labelInput.value.trim(),
        };

        if (!payload.value || !payload.label) {
            showBanner('Vui lòng nhập đầy đủ mã giá trị và tên hiển thị.', 'error');
            return;
        }

        submitBtn.disabled = true;
        submitBtn.textContent = 'Đang thêm...';

        try {
            const data = await apiPost(`/Attributes/${attributeId}/Options/Add`, payload);
            if (data.succeeded) {
                valueInput.value = '';
                labelInput.value = '';
                await loadOptions();
                showBanner(data.message, 'success');
            } else {
                showBanner(data.message, 'error');
            }
        } catch {
            showBanner('Lỗi kết nối, vui lòng thử lại.', 'error');
        } finally {
            submitBtn.disabled = false;
            submitBtn.textContent = 'Thêm';
        }
    });

    /* ── Filter options ──────────────────────────────────────────────────── */

    filterInput?.addEventListener('input', () => {
        const term = filterInput.value.toLowerCase();
        optionsList?.querySelectorAll('.option-row').forEach(row => {
            const text = row.textContent.toLowerCase();
            row.style.display = text.includes(term) ? '' : 'none';
        });
    });

    /* ── Init ────────────────────────────────────────────────────────────── */
    loadOptions();
}

/* ── Utility ─────────────────────────────────────────────────────────────── */

function escHtml(str) {
    return String(str)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;');
}
