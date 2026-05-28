'use strict';

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

document.addEventListener('DOMContentLoaded', () => {
    bindSlugGenerator();
    bindImagePreview();
    bindCategoryImageFallbacks();
    bindToastDismiss();
    bindStatusToggles();
    bindDeleteConfirmation();
});

function bindSlugGenerator() {
    const nameInput = document.getElementById('categoryName');
    const slugInput = document.getElementById('categorySlug');
    let slugEdited = slugInput && slugInput.value.trim() !== '';

    if (!nameInput || !slugInput) {
        return;
    }

    nameInput.addEventListener('input', () => {
        if (!slugEdited) {
            slugInput.value = toSlug(nameInput.value);
        }
    });

    slugInput.addEventListener('input', () => {
        slugEdited = slugInput.value.trim() !== '';
    });

    slugInput.addEventListener('blur', () => {
        if (slugInput.value.trim() === '') {
            slugEdited = false;
            slugInput.value = toSlug(nameInput.value);
        }
    });
}

function bindImagePreview() {
    const imgInput = document.getElementById('imageFileInput');
    const previewBox = document.getElementById('imagePreview');
    const previewImg = document.getElementById('previewImg');

    if (!imgInput || !previewBox || !previewImg) {
        return;
    }

    imgInput.addEventListener('change', () => {
        const file = imgInput.files && imgInput.files[0];
        if (!file) {
            return;
        }

        previewImg.src = URL.createObjectURL(file);
        previewBox.classList.remove('hidden');
    });

    previewImg.addEventListener('error', () => {
        previewBox.classList.add('hidden');
    });
}

function bindCategoryImageFallbacks() {
    document.querySelectorAll('[data-category-image]').forEach(image => {
        image.addEventListener('error', () => image.remove());
    });
}

function bindToastDismiss() {
    document.querySelectorAll('[data-dismiss-target]').forEach(button => {
        button.addEventListener('click', () => {
            document.getElementById(button.dataset.dismissTarget)?.remove();
        });
    });

    ['toastSuccess', 'toastError'].forEach(id => {
        const element = document.getElementById(id);
        if (element) {
            setTimeout(() => element.remove(), 4000);
        }
    });
}

function bindStatusToggles() {
    document.querySelectorAll('[data-category-toggle]').forEach(button => {
        button.addEventListener('click', () => toggleActive(button));
    });
}

async function toggleActive(button) {
    const id = button.dataset.categoryId;
    if (!id) {
        return;
    }

    button.disabled = true;

    try {
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
        const response = await fetch(`/Categories/ToggleActive/${id}`, {
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
    document.querySelectorAll('[data-category-delete]').forEach(form => {
        form.addEventListener('submit', event => {
            const name = form.dataset.categoryName ?? '';
            const productCount = Number(form.dataset.productCount ?? 0);
            const childCount = Number(form.dataset.childCount ?? 0);

            if (!canDeleteCategory(name, productCount, childCount)) {
                event.preventDefault();
            }
        });
    });
}

function canDeleteCategory(name, productCount, childCount) {
    if (childCount > 0) {
        alert(`Không thể xoá "${name}" vì có ${childCount} danh mục con.\nHãy xoá hoặc chuyển các danh mục con trước.`);
        return false;
    }

    if (productCount > 0) {
        alert(`Không thể xoá "${name}" vì có ${productCount} sản phẩm đang thuộc danh mục này.`);
        return false;
    }

    return confirm(`Bạn có chắc muốn xoá danh mục "${name}"?\nHành động này không thể hoàn tác.`);
}
