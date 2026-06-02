'use strict';

document.addEventListener('DOMContentLoaded', () => {
    bindSupplierFormValidation();
    bindStatusToggles();
    bindDeleteConfirmation();
    bindToastDismiss();
});

function bindSupplierFormValidation() {
    const requiredFields = Array.from(document.querySelectorAll('[data-supplier-required]'));
    const phoneInput = document.querySelector('[data-supplier-phone]');
    const form = phoneInput?.closest('form') ?? requiredFields[0]?.closest('form');
    if (!form) {
        return;
    }

    const alertBox = document.querySelector('[data-supplier-form-alert]');
    const requiredPhoneMessage = 'Số điện thoại là bắt buộc.';
    const formatPhoneMessage = 'Số điện thoại phải gồm đúng 10 chữ số.';
    let hasSubmitted = !alertBox?.classList.contains('hidden');

    const setFieldError = (field, message, showError) => {
        const targetId = field.dataset.supplierErrorTarget;
        const errorElement = targetId ? document.getElementById(targetId) : null;
        if (errorElement && showError) {
            errorElement.textContent = message;
        }

        const fieldGroup = field.closest('[data-supplier-field]');
        if (fieldGroup && showError) {
            fieldGroup.classList.toggle('has-error', Boolean(message));
        }

        if (showError) {
            field.setAttribute('aria-invalid', message ? 'true' : 'false');
        }
    };

    const setAlertVisible = isVisible => {
        alertBox?.classList.toggle('hidden', !isVisible);
    };

    const validateRequiredField = (field, showError) => {
        const message = field.value.trim() ? '' : field.dataset.supplierRequired;
        setFieldError(field, message, showError);
        return message === '';
    };

    const validatePhone = showError => {
        if (!phoneInput) {
            return true;
        }

        const value = phoneInput.value.trim();
        let message = '';

        if (!value) {
            message = requiredPhoneMessage;
        } else if (!/^\d{10}$/.test(value)) {
            message = formatPhoneMessage;
        }

        setFieldError(phoneInput, message, showError);
        return message === '';
    };

    const validateForm = showError => {
        const requiredFieldsValid = requiredFields
            .map(field => validateRequiredField(field, showError))
            .every(Boolean);
        const phoneValid = validatePhone(showError);
        const isValid = requiredFieldsValid && phoneValid;

        if (showError) {
            setAlertVisible(!isValid);
        }

        return isValid;
    };

    requiredFields.forEach(field => {
        field.addEventListener('input', () => {
            validateRequiredField(field, true);
            if (hasSubmitted) {
                setAlertVisible(!validateForm(false));
            }
        });
    });

    phoneInput?.addEventListener('input', () => {
        phoneInput.value = phoneInput.value.replace(/\D/g, '').slice(0, 10);
        validatePhone(true);
        if (hasSubmitted) {
            setAlertVisible(!validateForm(false));
        }
    });

    form.addEventListener('submit', event => {
        hasSubmitted = true;
        if (!validateForm(true)) {
            event.preventDefault();
            const firstInvalid = [...requiredFields, phoneInput]
                .filter(Boolean)
                .find(field => !field.value.trim() || (field === phoneInput && !/^\d{10}$/.test(field.value.trim())));
            firstInvalid?.focus();
        }
    });
}

function getAntiForgeryToken(scope) {
    return scope?.querySelector('input[name="__RequestVerificationToken"]')?.value
        ?? document.querySelector('input[name="__RequestVerificationToken"]')?.value
        ?? '';
}

function bindStatusToggles() {
    document.querySelectorAll('[data-supplier-toggle]').forEach(button => {
        button.addEventListener('click', () => toggleSupplier(button));
    });
}

async function toggleSupplier(button) {
    const id = button.dataset.supplierId;
    if (!id) {
        return;
    }

    button.disabled = true;

    try {
        const response = await fetch(`/Suppliers/ToggleActive/${encodeURIComponent(id)}`, {
            method: 'POST',
            headers: {
                RequestVerificationToken: getAntiForgeryToken(document),
                'X-Requested-With': 'XMLHttpRequest',
            },
        });

        if (!response.ok) {
        throw new Error('Cập nhật trạng thái thất bại.');
        }

        await response.json();
        window.location.reload();
    } catch {
        showSupplierNotice('Không thể cập nhật trạng thái nhà cung cấp. Vui lòng thử lại.', 'error');
        button.disabled = false;
    }
}

function bindDeleteConfirmation() {
    document.querySelectorAll('[data-supplier-delete]').forEach(form => {
        form.addEventListener('submit', async event => {
            if (form.dataset.deleteChecked === 'true') {
                return;
            }

            event.preventDefault();

            const name = form.dataset.supplierName || 'nhà cung cấp này';
            const submitButton = form.querySelector('button[type="submit"]');
            submitButton?.setAttribute('disabled', 'disabled');

            try {
                const result = await checkSupplierDelete(form);

                if (!result.canDelete) {
                    showSupplierNotice(result.message || `Không thể xóa "${name}" vì còn dữ liệu liên quan.`, 'error');
                    return;
                }

                if (!window.confirm(`Bạn có chắc muốn xóa nhà cung cấp "${name}"?\nHành động này không thể hoàn tác.`)) {
                    return;
                }

                form.dataset.deleteChecked = 'true';
                if (typeof form.requestSubmit === 'function') {
                    form.requestSubmit();
                } else {
                    form.submit();
                }
            } catch {
                showSupplierNotice('Không thể kiểm tra điều kiện xóa. Vui lòng thử lại.', 'error');
            } finally {
                if (form.dataset.deleteChecked !== 'true') {
                    submitButton?.removeAttribute('disabled');
                }
            }
        });
    });
}

async function checkSupplierDelete(form) {
    const id = form.dataset.supplierId;
    if (!id) {
        throw new Error('Thiếu mã nhà cung cấp.');
    }

    const response = await fetch(`/Suppliers/CheckDelete/${encodeURIComponent(id)}`, {
        method: 'POST',
        headers: {
            RequestVerificationToken: getAntiForgeryToken(form),
            'X-Requested-With': 'XMLHttpRequest',
        },
    });

    if (!response.ok) {
        throw new Error('Kiểm tra điều kiện xóa thất bại.');
    }

    return response.json();
}

function showSupplierNotice(message, type = 'success') {
    const root = document.querySelector('[data-supplier-toast-root]');
    if (!root) {
        return;
    }

    const toast = document.createElement('div');
    toast.className = `supplier-toast is-${type}`;

    const marker = document.createElement('span');
    marker.className = 'supplier-toast-marker';

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
