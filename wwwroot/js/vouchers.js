'use strict';

document.addEventListener('DOMContentLoaded', () => {
    bindCodeFormatter();
    bindDiscountType();
    bindVoucherFormValidation();
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
        .toUpperCase();
}

function bindCodeFormatter() {
    const codeInput = document.getElementById('voucherCode');
    if (!codeInput) {
        return;
    }

    codeInput.addEventListener('input', () => {
        const cursor = codeInput.selectionStart ?? codeInput.value.length;
        const nextValue = toVoucherCode(codeInput.value);
        const nextCursor = toVoucherCode(codeInput.value.slice(0, cursor)).length;

        codeInput.value = nextValue;
        codeInput.setSelectionRange(nextCursor, nextCursor);
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

function bindVoucherFormValidation() {
    const form = document.querySelector('[data-voucher-form]');
    if (!form) {
        return;
    }

    const touchedFields = new Set();
    const alertBox = form.querySelector('[data-surface-form-alert]');
    let hasSubmitted = !alertBox?.classList.contains('hidden');
    const setAlertVisible = isVisible => {
        alertBox?.classList.toggle('hidden', !isVisible);
    };
    const watchedFields = [
        'Code',
        'DiscountType',
        'DiscountValue',
        'MaxDiscountValue',
        'Description',
        'MinOrderValue',
        'MaxUses',
        'MaxUsesPerUser',
        'StartDate',
        'EndDate',
        'Priority',
    ];

    watchedFields.forEach(fieldName => {
        const field = getVoucherField(form, fieldName);
        if (!field) {
            return;
        }

        ['input', 'change', 'blur'].forEach(eventName => {
            field.addEventListener(eventName, () => {
                touchedFields.add(fieldName);
                touchDependentFields(touchedFields, fieldName);
                const isValid = validateVoucherForm(form, touchedFields, false);
                if (hasSubmitted) {
                    setAlertVisible(!isValid);
                }
            });
        });
    });

    form.addEventListener('submit', event => {
        hasSubmitted = true;
        watchedFields.forEach(fieldName => touchedFields.add(fieldName));
        const isValid = validateVoucherForm(form, touchedFields, true);
        setAlertVisible(!isValid);

        if (!isValid) {
            event.preventDefault();
            form.querySelector('.voucher-field-invalid')?.focus();
        }
    });
}

function touchDependentFields(touchedFields, fieldName) {
    if (fieldName === 'DiscountType' || fieldName === 'DiscountValue') {
        touchedFields.add('DiscountValue');
        touchedFields.add('MaxDiscountValue');
    }

    if (fieldName === 'MaxUses') {
        touchedFields.add('MaxUsesPerUser');
    }

    if (fieldName === 'StartDate') {
        touchedFields.add('EndDate');
    }
}

function validateVoucherForm(form, touchedFields, showAll) {
    let isValid = true;
    const discountType = getVoucherField(form, 'DiscountType')?.value ?? 'FixedAmount';
    const discountValue = validateNumberField(form, 'DiscountValue', { required: true });
    const maxDiscountValue = validateNumberField(form, 'MaxDiscountValue');
    const minOrderValue = validateNumberField(form, 'MinOrderValue', { required: true });
    const maxUses = validateNumberField(form, 'MaxUses', { integer: true });
    const maxUsesPerUser = validateNumberField(form, 'MaxUsesPerUser', { integer: true });
    const priority = validateNumberField(form, 'Priority', { required: true, integer: true });
    const description = validateTextField(form, 'Description');

    isValid = applyVoucherFieldError(
        form,
        'Code',
        validateCodeField(form),
        touchedFields,
        showAll) && isValid;

    const percentageMax = readFormNumber(form, 'percentageDiscountMax');
    if (!discountValue.error &&
        discountType === 'Percentage' &&
        percentageMax !== null &&
        discountValue.value !== null &&
        discountValue.value > percentageMax) {
        discountValue.error = form.dataset.percentageDiscountMaxMessage || getRangeMessage(getVoucherField(form, 'DiscountValue'));
    }

    if (!maxDiscountValue.error &&
        discountType === 'FixedAmount' &&
        discountValue.value !== null &&
        maxDiscountValue.value !== null &&
        maxDiscountValue.value < discountValue.value) {
        maxDiscountValue.error = form.dataset.fixedMaxDiscountMessage || getRangeMessage(getVoucherField(form, 'MaxDiscountValue'));
    }

    if (!maxUsesPerUser.error &&
        maxUses.value !== null &&
        maxUsesPerUser.value !== null &&
        maxUsesPerUser.value > maxUses.value) {
        maxUsesPerUser.error = form.dataset.maxUsesPerUserMessage || getRangeMessage(getVoucherField(form, 'MaxUsesPerUser'));
    }

    const dateErrors = validateDateFields(form);

    isValid = applyVoucherFieldError(form, 'DiscountValue', discountValue.error, touchedFields, showAll) && isValid;
    isValid = applyVoucherFieldError(form, 'MaxDiscountValue', maxDiscountValue.error, touchedFields, showAll) && isValid;
    isValid = applyVoucherFieldError(form, 'Description', description.error, touchedFields, showAll) && isValid;
    isValid = applyVoucherFieldError(form, 'MinOrderValue', minOrderValue.error, touchedFields, showAll) && isValid;
    isValid = applyVoucherFieldError(form, 'MaxUses', maxUses.error, touchedFields, showAll) && isValid;
    isValid = applyVoucherFieldError(form, 'MaxUsesPerUser', maxUsesPerUser.error, touchedFields, showAll) && isValid;
    isValid = applyVoucherFieldError(form, 'StartDate', dateErrors.startDate, touchedFields, showAll) && isValid;
    isValid = applyVoucherFieldError(form, 'EndDate', dateErrors.endDate, touchedFields, showAll) && isValid;
    isValid = applyVoucherFieldError(form, 'Priority', priority.error, touchedFields, showAll) && isValid;

    return isValid;
}

function validateCodeField(form) {
    const field = getVoucherField(form, 'Code');
    const code = field?.value.trim() ?? '';

    if (!field) {
        return '';
    }

    if (code.length === 0) {
        return field.dataset.valRequired || 'Mã voucher là bắt buộc.';
    }

    const maxLength = Number(field.dataset.valLengthMax || field.getAttribute('maxlength') || 0);
    if (maxLength > 0 && code.length > maxLength) {
        return field.dataset.valLength || `Mã voucher tối đa ${maxLength} ký tự.`;
    }

    const pattern = field.dataset.valRegexPattern;
    if (pattern && !matchesPattern(code, pattern)) {
        return field.dataset.valRegex || 'Mã voucher không hợp lệ.';
    }

    return '';
}

function validateTextField(form, fieldName) {
    const field = getVoucherField(form, fieldName);
    const value = field?.value ?? '';
    const maxLength = Number(field?.dataset.valLengthMax || field?.getAttribute('maxlength') || 0);

    if (!field) {
        return { error: '' };
    }

    if (maxLength > 0 && value.length > maxLength) {
        return {
            error: field.dataset.valLength || `${getFieldLabel(form, field)} tối đa ${maxLength} ký tự.`,
        };
    }

    return { error: '' };
}

function validateNumberField(form, fieldName, options = {}) {
    const field = getVoucherField(form, fieldName);
    const parsed = readNumber(field);
    let error = '';

    if (!field) {
        return { error, value: null };
    }

    if (!parsed.hasValue) {
        error = options.required ? getRequiredMessage(field) : '';
    } else if (!parsed.isValid) {
        error = `${getFieldLabel(form, field)} phải là một số hợp lệ.`;
    } else if (options.integer && !Number.isInteger(parsed.value)) {
        error = `${getFieldLabel(form, field)} phải là số nguyên.`;
    } else {
        error = validateRange(field, parsed.value);
    }

    return {
        error,
        value: error ? null : parsed.value,
    };
}

function validateDateFields(form) {
    const startField = getVoucherField(form, 'StartDate');
    const endField = getVoucherField(form, 'EndDate');
    const startDate = readDate(startField);
    const endDate = readDate(endField);
    let startError = '';
    let endError = '';

    if (!startDate.hasValue) {
        startError = getRequiredMessage(startField);
    } else if (!startDate.isValid) {
        startError = `${getFieldLabel(form, startField)} không hợp lệ.`;
    }

    if (!endDate.hasValue) {
        endError = getRequiredMessage(endField);
    } else if (!endDate.isValid) {
        endError = `${getFieldLabel(form, endField)} không hợp lệ.`;
    } else if (startDate.value && endDate.value <= startDate.value) {
        endError = form.dataset.endAfterStartMessage || getRangeMessage(endField);
    }

    return {
        startDate: startError,
        endDate: endError,
    };
}

function readNumber(field) {
    const rawValue = field?.value.trim() ?? '';

    if (rawValue === '') {
        return { hasValue: false, isValid: true, value: null };
    }

    const value = Number(rawValue.replace(',', '.'));
    return {
        hasValue: true,
        isValid: Number.isFinite(value),
        value,
    };
}

function readDate(field) {
    const rawValue = field?.value ?? '';
    if (!rawValue) {
        return { hasValue: false, isValid: true, value: null };
    }

    const value = new Date(rawValue);
    return {
        hasValue: true,
        isValid: !Number.isNaN(value.getTime()),
        value,
    };
}

function readFormNumber(form, dataKey) {
    const rawValue = form.dataset[dataKey];
    if (!rawValue) {
        return null;
    }

    const value = Number(rawValue);
    return Number.isFinite(value) ? value : null;
}

function validateRange(field, value) {
    const min = readConstraintNumber(field, 'valRangeMin', 'min');
    const max = readConstraintNumber(field, 'valRangeMax', 'max');

    if (min !== null && value < min) {
        return getRangeMessage(field);
    }

    if (max !== null && value > max) {
        return getRangeMessage(field);
    }

    return '';
}

function readConstraintNumber(field, dataKey, attributeName) {
    const rawValue = field?.dataset[dataKey] || field?.getAttribute(attributeName);
    if (!rawValue) {
        return null;
    }

    const value = Number(rawValue);
    return Number.isFinite(value) ? value : null;
}

function getRequiredMessage(field) {
    return field?.dataset.valRequired || `${getFieldLabel(field?.form, field)} là bắt buộc.`;
}

function getRangeMessage(field) {
    return field?.dataset.valRange || `${getFieldLabel(field?.form, field)} không hợp lệ.`;
}

function getFieldLabel(form, field) {
    if (!form || !field) {
        return 'Giá trị';
    }

    const label = field.id ? form.querySelector(`label[for="${field.id}"]`) : null;
    return (label?.textContent || field.name || 'Giá trị').replace('*', '').replace(/\s+/g, ' ').trim();
}

function matchesPattern(value, pattern) {
    try {
        return new RegExp(pattern).test(value);
    } catch {
        return false;
    }
}

function applyVoucherFieldError(form, fieldName, message, touchedFields, showAll) {
    const shouldShow = showAll || touchedFields.has(fieldName);
    const visibleMessage = shouldShow ? message : '';
    const field = getVoucherField(form, fieldName);
    const messageElement = getVoucherValidationMessage(form, fieldName);

    if (field) {
        field.setCustomValidity(message || '');
        field.setAttribute('aria-invalid', message ? 'true' : 'false');
        field.classList.toggle('voucher-field-invalid', Boolean(visibleMessage));
    }

    if (messageElement) {
        messageElement.textContent = visibleMessage;
        messageElement.classList.toggle('field-validation-error', Boolean(visibleMessage));
        messageElement.classList.toggle('field-validation-valid', !visibleMessage);
    }

    return message === '';
}

function getVoucherField(form, fieldName) {
    return form.querySelector(`[name="${fieldName}"]`);
}

function getVoucherValidationMessage(form, fieldName) {
    return form.querySelector(`[data-valmsg-for="${fieldName}"]`);
}

function bindStatusToggles() {
    document.querySelectorAll('[data-voucher-toggle]').forEach(button => {
        button.addEventListener('click', () => toggleVoucherStatus(button));
    });
}

async function toggleVoucherStatus(button) {
    const url = button.dataset.voucherToggleUrl;
    if (!url) {
        return;
    }

    button.disabled = true;

    try {
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
        const response = await fetch(url, {
            method: 'POST',
            headers: {
                RequestVerificationToken: token,
                'X-Requested-With': 'XMLHttpRequest',
            },
        });

        if (!response.ok) {
            throw new Error('Máy chủ trả về lỗi.');
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
