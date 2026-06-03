'use strict';

document.addEventListener('DOMContentLoaded', () => {
    bindToastDismiss();
    bindOrderStatusForm();
});

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

function bindOrderStatusForm() {
    const form = document.querySelector('[data-order-status-form]');
    if (!form) {
        return;
    }

    form.setAttribute('novalidate', 'novalidate');

    const alertBox = form.querySelector('[data-order-form-alert]');
    const orderSelect = form.querySelector('[data-order-status-select]');
    const paymentSelect = form.querySelector('[data-payment-status-select]');
    const currentOrderStatus = form.dataset.currentOrderStatus || '';
    const currentPaymentStatus = form.dataset.currentPaymentStatus || '';
    let hasSubmitted = !alertBox?.classList.contains('hidden');

    const setAlertVisible = isVisible => {
        alertBox?.classList.toggle('hidden', !isVisible);
    };

    const labels = {
        order: {
            Pending: 'Chờ xác nhận',
            Confirmed: 'Đã xác nhận',
            Processing: 'Đang xử lý',
            Shipping: 'Đang giao',
            Completed: 'Hoàn tất',
            Cancelled: 'Đã hủy',
            Returned: 'Đã trả hàng',
        },
        payment: {
            Unpaid: 'Chưa thanh toán',
            Paid: 'Đã thanh toán',
            Failed: 'Thanh toán lỗi',
            Refunded: 'Đã hoàn tiền',
        },
    };

    const allowedOrderTransitions = {
        Pending: ['Pending', 'Confirmed', 'Cancelled'],
        Confirmed: ['Confirmed', 'Processing', 'Cancelled'],
        Processing: ['Processing', 'Shipping', 'Cancelled'],
        Shipping: ['Shipping', 'Completed', 'Returned'],
        Completed: ['Completed', 'Returned'],
        Cancelled: ['Cancelled'],
        Returned: ['Returned'],
    };

    const allowedPaymentTransitions = {
        Unpaid: ['Unpaid', 'Paid', 'Failed'],
        Failed: ['Failed', 'Unpaid', 'Paid'],
        Paid: ['Paid', 'Refunded'],
        Refunded: ['Refunded'],
    };

    const getLabel = (group, value) => labels[group]?.[value] || 'Không xác định';

    const getMessageElement = fieldName => Array.from(form.querySelectorAll('[data-valmsg-for]'))
        .find(element => element.dataset.valmsgFor === fieldName) ?? null;

    const setFieldError = (field, fieldName, message) => {
        const hasError = Boolean(message);
        const messageElement = getMessageElement(fieldName);

        field?.setAttribute('aria-invalid', hasError ? 'true' : 'false');
        field?.classList.toggle('input-validation-error', hasError);

        if (messageElement) {
            messageElement.textContent = message;
            messageElement.classList.toggle('field-validation-error', hasError);
            messageElement.classList.toggle('field-validation-valid', !hasError);
        }
    };

    const canChangeOrderStatus = (current, next) =>
        (allowedOrderTransitions[current] || []).includes(next);

    const canChangePaymentStatus = (current, next) =>
        (allowedPaymentTransitions[current] || []).includes(next);

    const getValidationMessages = () => {
        const nextOrderStatus = orderSelect?.value || '';
        const nextPaymentStatus = paymentSelect?.value || '';
        let orderMessage = '';
        let paymentMessage = '';

        if (!canChangeOrderStatus(currentOrderStatus, nextOrderStatus)) {
            orderMessage = `Không thể chuyển đơn từ "${getLabel('order', currentOrderStatus)}" sang "${getLabel('order', nextOrderStatus)}".`;
        }

        if (!canChangePaymentStatus(currentPaymentStatus, nextPaymentStatus)) {
            paymentMessage = `Không thể chuyển thanh toán từ "${getLabel('payment', currentPaymentStatus)}" sang "${getLabel('payment', nextPaymentStatus)}".`;
        }

        if (nextOrderStatus === 'Completed' && nextPaymentStatus !== 'Paid') {
            paymentMessage = 'Đơn hoàn tất phải có trạng thái thanh toán là đã thanh toán.';
        }

        if (nextPaymentStatus === 'Refunded' && !['Cancelled', 'Returned'].includes(nextOrderStatus)) {
            paymentMessage = 'Chỉ hoàn tiền cho đơn đã hủy hoặc đã trả hàng.';
        }

        if (['Cancelled', 'Returned'].includes(nextOrderStatus) && nextPaymentStatus === 'Paid') {
            paymentMessage = 'Đơn đã hủy hoặc trả hàng không thể giữ trạng thái đã thanh toán.';
        }

        if (['Cancelled', 'Returned'].includes(nextOrderStatus) &&
            currentPaymentStatus === 'Paid' &&
            nextPaymentStatus !== 'Refunded') {
            paymentMessage = 'Đơn đã thanh toán khi hủy hoặc trả hàng phải chuyển sang đã hoàn tiền.';
        }

        return { orderMessage, paymentMessage };
    };

    const validateForm = showErrors => {
        const { orderMessage, paymentMessage } = getValidationMessages();
        const isValid = !orderMessage && !paymentMessage;

        if (showErrors) {
            setFieldError(orderSelect, 'OrderStatus', orderMessage);
            setFieldError(paymentSelect, 'PaymentStatus', paymentMessage);
            setAlertVisible(!isValid);
        }

        return {
            isValid,
            firstInvalid: orderMessage ? orderSelect : paymentMessage ? paymentSelect : null,
        };
    };

    [orderSelect, paymentSelect].forEach(field => {
        field?.addEventListener('change', () => {
            if (hasSubmitted) {
                validateForm(true);
            }
        });
    });

    form.addEventListener('submit', event => {
        hasSubmitted = true;

        const result = validateForm(true);
        if (!result.isValid) {
            event.preventDefault();
            result.firstInvalid?.focus();
        }
    });
}
