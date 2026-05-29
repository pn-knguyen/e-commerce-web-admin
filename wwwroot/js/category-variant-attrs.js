'use strict';

document.addEventListener('submit', e => {
    const form = e.target.closest('[data-cva-remove]');
    if (!form) return;

    const name = form.dataset.attrName || 'thuộc tính này';
    const categoryName = form.dataset.categoryName || 'danh mục này';
    const usage = Number.parseInt(form.dataset.usage || '0', 10);

    if (usage > 0) {
        e.preventDefault();
        alert(`Không thể bỏ gán "${name}" khỏi "${categoryName}" vì đang được dùng bởi ${usage} biến thể sản phẩm.`);
        return;
    }

    if (!confirm(`Bỏ gán thuộc tính "${name}" khỏi danh mục "${categoryName}"?\n\nHành động này không thể hoàn tác.`)) {
        e.preventDefault();
    }
});
