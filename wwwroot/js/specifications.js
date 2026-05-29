/**
 * specifications.js
 * Handles UI behavior for the global Specification CRUD pages.
 */

(function () {
    'use strict';

    bindKeyFormatter();
    bindDeleteConfirmation();
    bindToastDismiss();

    function bindKeyFormatter() {
        const keyInput = document.getElementById('specKey');
        if (!keyInput) {
            return;
        }

        keyInput.addEventListener('input', () => {
            const caret = keyInput.selectionStart;
            keyInput.value = keyInput.value.toLowerCase().replace(/[^a-z0-9_]/g, '_');
            keyInput.setSelectionRange(caret, caret);
        });

        keyInput.addEventListener('blur', () => {
            keyInput.value = keyInput.value.replace(/_+/g, '_').replace(/^_|_$/g, '');
        });
    }

    function bindDeleteConfirmation() {
        document.querySelectorAll('[data-spec-delete]').forEach(form => {
            form.addEventListener('submit', event => {
                const name = form.dataset.specName || 'thông số này';
                const categoryCount = Number.parseInt(form.dataset.categoryCount || '0', 10);
                const productCount = Number.parseInt(form.dataset.productCount || '0', 10);

                if (categoryCount > 0 || productCount > 0) {
                    event.preventDefault();
                    alert(`Không thể xoá "${name}" vì đang được dùng (${categoryCount} danh mục, ${productCount} sản phẩm).`);
                    return;
                }

                if (!confirm(`Bạn có chắc muốn xoá thông số "${name}"?`)) {
                    event.preventDefault();
                }
            });
        });
    }

    function bindToastDismiss() {
        document.querySelectorAll('[data-dismiss-target]').forEach(button => {
            button.addEventListener('click', () =>
                document.getElementById(button.dataset.dismissTarget)?.remove());
        });

        setTimeout(() => {
            ['toastSuccess', 'toastError'].forEach(id => document.getElementById(id)?.remove());
        }, 5000);
    }
})();
