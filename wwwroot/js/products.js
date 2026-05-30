'use strict';

document.addEventListener('DOMContentLoaded', () => {
    bindSlugGenerator();
    bindStatusToggles();
    bindFeaturedToggles();
    bindDeleteConfirmation();
    bindToastDismiss();
});

function toSlug(text) {
    return text
        .toLowerCase()
        .normalize('NFD')
        .replace(/[\u0300-\u036f]/g, '')
        .replace(/đ/g, 'd')
        .replace(/[^a-z0-9\s-]/g, '')
        .replace(/\s+/g, '-')
        .replace(/-+/g, '-')
        .replace(/^-+|-+$/g, '');
}

function bindSlugGenerator() {
    const nameInput = document.getElementById('productName');
    const slugInput = document.getElementById('productSlug');

    if (!nameInput || !slugInput) {
        return;
    }

    let slugEdited = slugInput.value.trim() !== '';

    nameInput.addEventListener('input', () => {
        if (!slugEdited) {
            slugInput.value = toSlug(nameInput.value);
        }
    });

    slugInput.addEventListener('input', () => {
        slugEdited = slugInput.value.trim() !== '';
    });

    slugInput.addEventListener('blur', () => {
        slugInput.value = toSlug(slugInput.value);
        slugEdited = slugInput.value.trim() !== '';
    });
}

function bindStatusToggles() {
    document.querySelectorAll('[data-product-toggle]').forEach(button => {
        button.addEventListener('click', () => toggleProductState(button, 'ToggleActive'));
    });
}

function bindFeaturedToggles() {
    document.querySelectorAll('[data-featured-toggle]').forEach(button => {
        button.addEventListener('click', () => toggleProductState(button, 'ToggleFeatured'));
    });
}

async function toggleProductState(button, action) {
    const id = button.dataset.productId;
    if (!id) {
        return;
    }

    button.disabled = true;

    try {
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
        const response = await fetch(`/Products/${action}/${id}`, {
            method: 'POST',
            headers: {
                RequestVerificationToken: token,
                'X-Requested-With': 'XMLHttpRequest',
            },
        });

        if (!response.ok) {
            throw new Error('Server error');
        }

        await response.json();
        window.location.reload();
    } catch {
        alert('Không thể cập nhật sản phẩm. Vui lòng thử lại.');
        button.disabled = false;
    }
}

function bindDeleteConfirmation() {
    document.querySelectorAll('[data-product-delete]').forEach(form => {
        form.addEventListener('submit', async event => {
            if (form.dataset.deleteChecked === 'true') {
                return;
            }

            event.preventDefault();

            const name = form.dataset.productName || 'sản phẩm này';
            const submitButton = form.querySelector('button[type="submit"]');
            submitButton?.setAttribute('disabled', 'disabled');

            try {
                const result = await checkProductDelete(form);

                if (!result.canDelete) {
                    alert(result.message || `Không thể xoá "${name}" vì còn dữ liệu liên quan.`);
                    return;
                }

                if (!confirm(`Bạn có chắc muốn xoá sản phẩm "${name}"?\nHành động này không thể hoàn tác.`)) {
                    return;
                }

                form.dataset.deleteChecked = 'true';
                if (typeof form.requestSubmit === 'function') {
                    form.requestSubmit();
                } else {
                    form.submit();
                }
            } catch {
                alert('Không thể kiểm tra điều kiện xoá sản phẩm. Vui lòng thử lại.');
            } finally {
                if (form.dataset.deleteChecked !== 'true') {
                    submitButton?.removeAttribute('disabled');
                }
            }
        });
    });
}

async function checkProductDelete(form) {
    const id = form.dataset.productId;
    if (!id) {
        throw new Error('Missing product id');
    }

    const token = form.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
    const response = await fetch(`/Products/CheckDelete/${encodeURIComponent(id)}`, {
        method: 'POST',
        headers: {
            RequestVerificationToken: token,
            'X-Requested-With': 'XMLHttpRequest',
        },
    });

    if (!response.ok) {
        throw new Error('Delete check failed');
    }

    return response.json();
}

function bindToastDismiss() {
    document.querySelectorAll('[data-dismiss-target]').forEach(button => {
        button.addEventListener('click', () => {
            document.getElementById(button.dataset.dismissTarget)?.remove();
        });
    });

    setTimeout(() => {
        document.getElementById('toastSuccess')?.remove();
        document.getElementById('toastError')?.remove();
    }, 5000);
}
