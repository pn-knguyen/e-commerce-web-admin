/**
 * specifications.js
 * Handles:
 *  - Auto-format Key field (lowercase + underscore)
 *  - Filter available specs list
 *  - AJAX inline update (GroupName, SortOrder, IsRequired) for CategorySpecifications
 *  - Confirm delete on Specification rows
 *  - Toast auto-dismiss
 */

(function () {
    'use strict';

    // ── Key field formatter ────────────────────────────────────────────────

    const keyInput = document.getElementById('specKey');
    if (keyInput) {
        keyInput.addEventListener('input', () => {
            const caret = keyInput.selectionStart;
            keyInput.value = keyInput.value.toLowerCase().replace(/[^a-z0-9_]/g, '_');
            keyInput.setSelectionRange(caret, caret);
        });
        keyInput.addEventListener('blur', () => {
            keyInput.value = keyInput.value.replace(/_+/g, '_').replace(/^_|_$/g, '');
        });
    }

    // ── Filter available specs ─────────────────────────────────────────────

    const availableSearch = document.getElementById('availableSpecSearch');
    const availableList = document.getElementById('availableSpecList');

    if (availableSearch && availableList) {
        availableSearch.addEventListener('input', () => {
            const term = availableSearch.value.toLowerCase().trim();
            availableList.querySelectorAll('[data-spec-item]').forEach(item => {
                const text = (item.dataset.name + ' ' + item.dataset.key).toLowerCase();
                item.style.display = !term || text.includes(term) ? '' : 'none';
            });
        });
    }

    // ── Inline update (CategorySpec row) ──────────────────────────────────

    const token = () =>
        document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';

    async function patchCategorySpec(row) {
        const categoryId = row.dataset.categoryId;
        const specId = row.dataset.specId;
        const groupName = row.querySelector('[data-field="groupName"]')?.value ?? '';
        const sortOrder = parseInt(row.querySelector('[data-field="sortOrder"]')?.value ?? '0', 10);
        const isRequired = row.querySelector('[data-field="isRequired"]')?.classList.contains('active') ?? false;

        try {
            const res = await fetch('/CategorySpecifications/Update', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'RequestVerificationToken': token(),
                },
                body: new URLSearchParams({ categoryId, specId, groupName, sortOrder, isRequired }),
            });

            const data = await res.json();
            showToast(data.succeeded ? 'success' : 'error', data.message);
        } catch {
            showToast('error', 'Lỗi kết nối, vui lòng thử lại.');
        }
    }

    // Debounce inline input changes
    let debounceTimer;
    document.querySelectorAll('[data-spec-row]').forEach(row => {
        row.querySelectorAll('[data-field]').forEach(field => {
            field.addEventListener('change', () => {
                clearTimeout(debounceTimer);
                debounceTimer = setTimeout(() => patchCategorySpec(row), 500);
            });
        });
    });

    // Required toggle buttons
    document.querySelectorAll('.req-toggle').forEach(btn => {
        btn.addEventListener('click', () => {
            btn.classList.toggle('active');
            const row = btn.closest('[data-spec-row]');
            if (row) {
                clearTimeout(debounceTimer);
                debounceTimer = setTimeout(() => patchCategorySpec(row), 200);
            }
        });
    });

    // ── Confirm delete (Specification) ────────────────────────────────────

    document.querySelectorAll('[data-spec-delete]').forEach(form => {
        form.addEventListener('submit', e => {
            const name = form.dataset.specName || 'thông số này';
            const cats = parseInt(form.dataset.categoryCount || '0', 10);
            const prods = parseInt(form.dataset.productCount || '0', 10);

            if (cats > 0 || prods > 0) {
                e.preventDefault();
                alert(`Không thể xoá "${name}" vì đang được dùng (${cats} danh mục, ${prods} sản phẩm).`);
                return;
            }
            if (!confirm(`Bạn có chắc muốn xoá thông số "${name}"?`)) e.preventDefault();
        });
    });

    // ── Toast helpers ──────────────────────────────────────────────────────

    function showToast(type, message) {
        const existing = document.getElementById('inlineToast');
        existing?.remove();

        const colors = type === 'success'
            ? 'bg-emerald-50 border-emerald-200 text-emerald-700'
            : 'bg-red-50 border-red-200 text-red-700';

        const icon = type === 'success' ? 'check' : 'alert-circle';

        const el = document.createElement('div');
        el.id = 'inlineToast';
        el.className = `fixed bottom-5 right-5 z-50 flex items-center gap-3 ${colors} border
                        rounded-xl px-4 py-3 text-sm shadow-lg spec-anim`;
        el.innerHTML = `<i data-lucide="${icon}" class="w-4 h-4 flex-shrink-0"></i><span>${message}</span>`;
        document.body.appendChild(el);

        if (typeof lucide !== 'undefined') lucide.createIcons();
        setTimeout(() => el.remove(), 4000);
    }

    // Static toast auto-dismiss
    document.querySelectorAll('[data-dismiss-target]').forEach(btn => {
        btn.addEventListener('click', () =>
            document.getElementById(btn.dataset.dismissTarget)?.remove());
    });

    setTimeout(() => {
        ['toastSuccess', 'toastError'].forEach(id => document.getElementById(id)?.remove());
    }, 5000);

})();
