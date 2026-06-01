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
    const requiredMessage = 'Số điện thoại là bắt buộc.';
    const formatMessage = 'Số điện thoại phải gồm đúng 10 chữ số.';
    let hasSubmitted = !alertBox?.classList.contains('hidden');

    const setFieldError = (field, message, showError) => {
        const targetId = field.dataset.supplierErrorTarget;
        const errorElement = targetId ? document.getElementById(targetId) : null;
        if (errorElement && showError) {
            errorElement.textContent = message;
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
            message = requiredMessage;
        } else if (!/^\d{10}$/.test(value)) {
            message = formatMessage;
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
            throw new Error('Toggle failed');
        }

        await response.json();
        window.location.reload();
    } catch {
        alert('Không thể cập nhật trạng thái nhà cung cấp. Vui lòng thử lại.');
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
                    alert(result.message || `Không thể xóa "${name}" vì còn dữ liệu liên quan.`);
                    return;
                }

                if (!confirm(`Bạn có chắc muốn xóa nhà cung cấp "${name}"?\nHành động này không thể hoàn tác.`)) {
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

async function checkSupplierDelete(form) {
    const id = form.dataset.supplierId;
    if (!id) {
        throw new Error('Missing supplier id');
    }

    const response = await fetch(`/Suppliers/CheckDelete/${encodeURIComponent(id)}`, {
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
