'use strict';

document.addEventListener('DOMContentLoaded', () => {
    bindCodeFormatter();
    bindDiscountType();
    bindStatusToggles();
    bindDeleteConfirmation();
    bindToastDismiss();
});

function toVoucherCode(value) {
    return value
        .normalize('NFD')
        .replace(/[\u0300-\u036f]/g, '')
        .replace(/[đĐ]/g, 'd')
        .replace(/\s+/g, '-')
        .replace(/[^A-Za-z0-9_-]/g, '')
        .replace(/-+/g, '-')
        .replace(/_+/g, '_')
        .replace(/^[-_]+|[-_]+$/g, '')
        .toUpperCase();
}

function bindCodeFormatter() {
    const codeInput = document.getElementById('voucherCode');
    if (!codeInput) {
        return;
    }

    codeInput.addEventListener('input', () => {
        const cursor = codeInput.selectionStart;
        codeInput.value = toVoucherCode(codeInput.value);
        if (cursor !== null) {
            codeInput.setSelectionRange(cursor, cursor);
        }
    });

    codeInput.addEventListener('blur', () => {
        codeInput.value = toVoucherCode(codeInput.value);
    });
}

function bindDiscountType() {
    const typeSelect = document.getElementById('discountType');
    const valueInput = document.getElementById('discountValue');
    const unitLabel = document.getElementById('discountUnit');

    if (!typeSelect || !valueInput || !unitLabel) {
        return;
    }

    const syncDiscountInput = () => {
        const isPercentage = typeSelect.value === 'Percentage';
        unitLabel.textContent = isPercentage ? '%' : 'đ';
        valueInput.step = isPercentage ? '0.1' : '1000';

        if (isPercentage) {
            valueInput.max = '100';
        } else {
            valueInput.removeAttribute('max');
        }
    };

    typeSelect.addEventListener('change', syncDiscountInput);
    syncDiscountInput();
}

function bindStatusToggles() {
    document.querySelectorAll('[data-voucher-toggle]').forEach(button => {
        button.addEventListener('click', () => toggleVoucherStatus(button));
    });
}

async function toggleVoucherStatus(button) {
    const id = button.dataset.voucherId;
    if (!id) {
        return;
    }

    button.disabled = true;

    try {
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
        const response = await fetch(`/Vouchers/ToggleActive/${id}`, {
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
        alert('Không thể cập nhật trạng thái voucher. Vui lòng thử lại.');
        button.disabled = false;
    }
}

function bindDeleteConfirmation() {
    document.querySelectorAll('[data-voucher-delete]').forEach(form => {
        form.addEventListener('submit', event => {
            const code = form.dataset.voucherCode || 'voucher này';
            const usedCount = Number(form.dataset.usedCount || 0);
            const usageCount = Number(form.dataset.usageCount || 0);
            const orderCount = Number(form.dataset.orderCount || 0);

            if (usedCount > 0 || usageCount > 0 || orderCount > 0) {
                event.preventDefault();
                alert(`Không thể xoá "${code}" vì voucher đã phát sinh đơn hàng hoặc lượt sử dụng.`);
                return;
            }

            if (!confirm(`Bạn có chắc muốn xoá voucher "${code}"?\nHành động này không thể hoàn tác.`)) {
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

    ['toastSuccess', 'toastError'].forEach(id => {
        const element = document.getElementById(id);
        if (element) {
            setTimeout(() => element.remove(), 5000);
        }
    });
}
