'use strict';

document.addEventListener('DOMContentLoaded', () => {
    bindStatusToggles();
    bindDeleteConfirmation();
    bindToastDismiss();
});

function getAntiForgeryToken(scope) {
    return scope?.querySelector('input[name="__RequestVerificationToken"]')?.value
        ?? document.querySelector('input[name="__RequestVerificationToken"]')?.value
        ?? '';
}

function bindStatusToggles() {
    document.querySelectorAll('[data-payment-toggle]').forEach(button => {
        button.addEventListener('click', () => togglePaymentMethod(button));
    });
}

async function togglePaymentMethod(button) {
    const id = button.dataset.paymentId;
    if (!id) {
        return;
    }

    button.disabled = true;

    try {
        const response = await fetch(`/PaymentMethods/ToggleActive/${encodeURIComponent(id)}`, {
            method: 'POST',
            headers: {
                RequestVerificationToken: getAntiForgeryToken(document),
                'X-Requested-With': 'XMLHttpRequest',
            },
        });

        if (!response.ok) {
            throw new Error('Toggle failed');
        }

        await response.json();
        window.location.reload();
    } catch {
        alert('Không thể cập nhật trạng thái phương thức thanh toán. Vui lòng thử lại.');
        button.disabled = false;
    }
}

function bindDeleteConfirmation() {
    document.querySelectorAll('[data-payment-delete]').forEach(form => {
        form.addEventListener('submit', async event => {
            if (form.dataset.deleteChecked === 'true') {
                return;
            }

            event.preventDefault();

            const name = form.dataset.paymentName || 'phương thức thanh toán này';
            const submitButton = form.querySelector('button[type="submit"]');
            submitButton?.setAttribute('disabled', 'disabled');

            try {
                const result = await checkPaymentMethodDelete(form);

                if (!result.canDelete) {
                    alert(result.message || `Không thể xóa "${name}" vì còn dữ liệu liên quan.`);
                    return;
                }

                if (!confirm(`Bạn có chắc muốn xóa phương thức thanh toán "${name}"?\nHành động này không thể hoàn tác.`)) {
                    return;
                }

                form.dataset.deleteChecked = 'true';
                if (typeof form.requestSubmit === 'function') {
                    form.requestSubmit();
                } else {
                    form.submit();
                }
            } catch {
                alert('Không thể kiểm tra điều kiện xóa. Vui lòng thử lại.');
            } finally {
                if (form.dataset.deleteChecked !== 'true') {
                    submitButton?.removeAttribute('disabled');
                }
            }
        });
    });
}

async function checkPaymentMethodDelete(form) {
    const id = form.dataset.paymentId;
    if (!id) {
        throw new Error('Missing payment method id');
    }

    const response = await fetch(`/PaymentMethods/CheckDelete/${encodeURIComponent(id)}`, {
        method: 'POST',
        headers: {
            RequestVerificationToken: getAntiForgeryToken(form),
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
