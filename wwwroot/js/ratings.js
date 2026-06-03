'use strict';

document.addEventListener('DOMContentLoaded', () => {
    bindRatingToggles();
    bindRatingDeleteConfirmation();
    bindToastDismiss();
});

function getAntiForgeryToken(scope) {
    return scope?.querySelector('input[name="__RequestVerificationToken"]')?.value
        ?? document.querySelector('input[name="__RequestVerificationToken"]')?.value
        ?? '';
}

function bindRatingToggles() {
    document.querySelectorAll('[data-rating-toggle]').forEach(button => {
        button.addEventListener('click', () => toggleRatingApproval(button));
    });
}

async function toggleRatingApproval(button) {
    const url = button.dataset.ratingToggleUrl;
    if (!url) {
        return;
    }

    button.disabled = true;

    try {
        const response = await fetch(url, {
            method: 'POST',
            headers: {
                RequestVerificationToken: getAntiForgeryToken(document),
                'X-Requested-With': 'XMLHttpRequest',
            },
        });

        if (!response.ok) {
            throw new Error('Không thể cập nhật trạng thái đánh giá.');
        }

        const result = await response.json();
        button.dataset.approved = String(result.isApproved);
        button.classList.toggle('is-approved', Boolean(result.isApproved));
        button.classList.toggle('is-pending', !result.isApproved);

        const label = button.querySelector('.status-label');
        if (label) {
            label.textContent = result.isApproved ? 'Đã duyệt' : 'Chờ duyệt';
        }

        showRatingNotice(result.message || 'Đã cập nhật trạng thái đánh giá.', 'success');
    } catch {
        showRatingNotice('Không thể cập nhật trạng thái đánh giá. Vui lòng thử lại.', 'error');
    } finally {
        button.disabled = false;
    }
}

function bindRatingDeleteConfirmation() {
    document.querySelectorAll('[data-rating-delete]').forEach(form => {
        form.addEventListener('submit', event => {
            const name = form.dataset.ratingName || 'đánh giá này';
            if (!window.confirm(`Bạn có chắc muốn xóa đánh giá của "${name}"?\nHành động này không thể hoàn tác.`)) {
                event.preventDefault();
            }
        });
    });
}

function showRatingNotice(message, type = 'success') {
    const root = document.querySelector('[data-rating-toast-root]');
    if (!root) {
        return;
    }

    const toast = document.createElement('div');
    toast.className = `rating-toast is-${type}`;

    const marker = document.createElement('span');
    marker.className = 'rating-toast-marker';

    const text = document.createElement('span');
    text.textContent = message;

    toast.append(marker, text);
    root.appendChild(toast);

    window.setTimeout(() => {
        toast.remove();
    }, 4200);
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
