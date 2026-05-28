'use strict';

document.addEventListener('DOMContentLoaded', () => {
    bindSlugGenerator();
    bindImagePreview();
    bindLogoFallbacks();
    bindStatusToggles();
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
    const nameInput = document.getElementById('brandName');
    const slugInput = document.getElementById('brandSlug');

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

function bindImagePreview() {
    const fileInput = document.getElementById('imageFileInput');
    const previewWrap = document.getElementById('imagePreview');
    const previewImg = document.getElementById('previewImg');

    if (!fileInput || !previewWrap || !previewImg) {
        return;
    }

    fileInput.addEventListener('change', () => {
        const file = fileInput.files && fileInput.files[0];
        if (!file) {
            return;
        }

        previewImg.src = URL.createObjectURL(file);
        previewWrap.classList.remove('hidden');
    });

    previewImg.addEventListener('error', () => {
        previewWrap.classList.add('hidden');
    });
}

function bindLogoFallbacks() {
    document.querySelectorAll('.brand-logo img').forEach(image => {
        image.addEventListener('error', () => image.remove());
    });
}

function bindStatusToggles() {
    document.querySelectorAll('[data-brand-toggle]').forEach(button => {
        button.addEventListener('click', () => toggleBrandStatus(button));
    });
}

async function toggleBrandStatus(button) {
    const id = button.dataset.brandId;
    if (!id) {
        return;
    }

    button.disabled = true;

    try {
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
        const response = await fetch(`/Brands/ToggleActive/${id}`, {
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
        alert('Không thể cập nhật trạng thái. Vui lòng thử lại.');
        button.disabled = false;
    }
}

function bindDeleteConfirmation() {
    document.querySelectorAll('[data-brand-delete]').forEach(form => {
        form.addEventListener('submit', event => {
            const name = form.dataset.brandName || 'thương hiệu này';
            const productCount = Number(form.dataset.productCount || 0);

            if (productCount > 0) {
                event.preventDefault();
                alert(`Không thể xoá "${name}" vì có ${productCount} sản phẩm đang dùng thương hiệu này.`);
                return;
            }

            if (!confirm(`Bạn có chắc muốn xoá thương hiệu "${name}"?`)) {
                event.preventDefault();
            }
        });
    });
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
