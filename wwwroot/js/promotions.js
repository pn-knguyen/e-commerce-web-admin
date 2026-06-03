'use strict';

document.addEventListener('DOMContentLoaded', () => {
    bindPromotionFormValidation();
    bindPromotionStatusToggles();
    bindPromotionDeleteConfirmation();
    bindToastDismiss();
});

function bindPromotionFormValidation() {
    const form = document.querySelector('[data-promotion-form]');
    if (!form) {
        return;
    }

    form.setAttribute('novalidate', 'novalidate');

    const touchedFields = new Set();
    const alertBox = form.querySelector('[data-promotion-form-alert]');
    const actionTypeField = getPromotionField(form, 'ActionType');
    const giftField = getPromotionField(form, 'GiftProductVariantId');
    const giftGroup = form.querySelector('[data-promotion-gift-field]');
    const giftRequiredMark = form.querySelector('[data-promotion-gift-required]');
    const targetTypeField = getPromotionField(form, 'TargetType');
    const targetPicker = form.querySelector('[data-promotion-target-picker]');
    const targetSearch = form.querySelector('[data-promotion-target-search]');
    const targetCount = form.querySelector('[data-promotion-target-count]');
    const targetEmpty = form.querySelector('[data-promotion-target-empty]');
    const targetHint = form.querySelector('[data-promotion-target-hint]');
    let hasSubmitted = !alertBox?.classList.contains('hidden');

    const watchedFields = [
        'Name',
        'Description',
        'TargetType',
        'TargetIds',
        'ActionType',
        'DiscountValue',
        'MaxDiscountValue',
        'BuyQuantity',
        'GetQuantity',
        'GiftProductVariantId',
        'StartDate',
        'EndDate',
        'Priority',
        'MinOrderValue',
        'UsageLimit',
    ];

    const setAlertVisible = isVisible => {
        alertBox?.classList.toggle('hidden', !isVisible);
    };

    const syncActionFields = () => {
        const isGiftPromotion = actionTypeField?.value === 'GiftProduct';
        if (giftField) {
            giftField.disabled = !isGiftPromotion;
            if (!isGiftPromotion) {
                giftField.value = '';
            }
        }

        giftGroup?.classList.toggle('is-muted', !isGiftPromotion);
        giftRequiredMark?.classList.toggle('hidden', !isGiftPromotion);
    };

    const syncTargetOptions = () => {
        if (!targetTypeField || !targetPicker) {
            return;
        }

        const selectedType = targetTypeField.value;
        const searchTerm = normalizeSearchTerm(targetSearch?.value || '');
        let visibleCount = 0;

        targetPicker.querySelectorAll('[data-promotion-target-option]').forEach(option => {
            const checkbox = option.querySelector('input[name="TargetIds"]');
            const matchesType = option.dataset.targetType === selectedType;
            const matchesSearch = !searchTerm || normalizeSearchTerm(option.textContent || '').includes(searchTerm);
            const isVisible = matchesType && matchesSearch;

            option.hidden = !isVisible;

            if (checkbox) {
                checkbox.disabled = !matchesType;
                if (!matchesType) {
                    checkbox.checked = false;
                }
            }

            if (isVisible) {
                visibleCount += 1;
            }
        });

        const selectedTypeOption = targetTypeField.selectedOptions[0];
        if (targetHint) {
            targetHint.textContent = selectedTypeOption?.dataset.targetHint || 'Chọn phạm vi phù hợp với quy tắc khuyến mãi.';
        }

        const checkedCount = getPromotionFields(form, 'TargetIds')
            .filter(item => item.checked && !item.disabled)
            .length;

        if (targetCount) {
            targetCount.textContent = `${checkedCount} đã chọn`;
        }

        targetEmpty?.classList.toggle('hidden', visibleCount > 0);
    };

    watchedFields.forEach(fieldName => {
        const fields = fieldName === 'TargetIds'
            ? getPromotionFields(form, fieldName)
            : [getPromotionField(form, fieldName)].filter(Boolean);

        if (fields.length === 0) {
            return;
        }

        fields.forEach(field => ['input', 'change', 'blur'].forEach(eventName => {
            field.addEventListener(eventName, () => {
                touchedFields.add(fieldName);
                touchPromotionDependentFields(touchedFields, fieldName);

                if (fieldName === 'ActionType') {
                    syncActionFields();
                }

                if (fieldName === 'TargetType') {
                    if (targetSearch) {
                        targetSearch.value = '';
                    }
                    syncTargetOptions();
                    touchedFields.add('TargetIds');
                }

                if (fieldName === 'TargetIds') {
                    syncTargetOptions();
                }

                const isValid = validatePromotionForm(form, touchedFields, false);
                if (hasSubmitted) {
                    setAlertVisible(!isValid);
                }
            });
        }));
    });

    targetSearch?.addEventListener('input', () => {
        syncTargetOptions();
    });

    syncActionFields();
    syncTargetOptions();

    form.addEventListener('submit', event => {
        hasSubmitted = true;
        watchedFields.forEach(fieldName => touchedFields.add(fieldName));
        syncActionFields();
        syncTargetOptions();

        const isValid = validatePromotionForm(form, touchedFields, true);
        setAlertVisible(!isValid);

        if (!isValid) {
            event.preventDefault();
            form.querySelector('.promotion-field-invalid')?.focus();
        }
    });
}

function touchPromotionDependentFields(touchedFields, fieldName) {
    if (['ActionType', 'DiscountValue', 'MaxDiscountValue'].includes(fieldName)) {
        touchedFields.add('DiscountValue');
        touchedFields.add('MaxDiscountValue');
    }

    if (['ActionType', 'GetQuantity'].includes(fieldName)) {
        touchedFields.add('GetQuantity');
        touchedFields.add('GiftProductVariantId');
    }

    if (fieldName === 'UsageLimit') {
        touchedFields.add('UsageLimit');
    }

    if (fieldName === 'StartDate') {
        touchedFields.add('EndDate');
    }

    if (fieldName === 'TargetType') {
        touchedFields.add('TargetIds');
    }
}

function validatePromotionForm(form, touchedFields, showAll) {
    let isValid = true;
    const actionType = getPromotionField(form, 'ActionType')?.value || 'DiscountOrder';
    const name = validateTextField(form, 'Name', { required: true });
    const description = validateTextField(form, 'Description');
    const targetType = validateSelectField(form, 'TargetType', { required: true });
    const targetIds = validateTargetIdsField(form);
    const minOrderValue = validateNumberField(form, 'MinOrderValue', { required: true });
    const maxDiscountValue = validateNumberField(form, 'MaxDiscountValue');
    const usageLimit = validateNumberField(form, 'UsageLimit', { integer: true });
    const discountValue = validateNumberField(form, 'DiscountValue', { required: true });
    const buyQuantity = validateNumberField(form, 'BuyQuantity', { required: true, integer: true });
    const getQuantity = validateNumberField(form, 'GetQuantity', { required: true, integer: true });
    const priority = validateNumberField(form, 'Priority', { required: true, integer: true });
    const giftVariant = validateGiftVariantField(form, actionType);
    const dateErrors = validateDateFields(form);

    if (!discountValue.error) {
        if (['DiscountOrder', 'DiscountProduct'].includes(actionType) && discountValue.value <= 0) {
            discountValue.error = form.dataset.discountPositiveMessage || 'Giá trị giảm phải lớn hơn 0.';
        }

        if (actionType === 'BuyXGetY' &&
            discountValue.value <= 0 &&
            !getQuantity.error &&
            getQuantity.value <= 0) {
            discountValue.error = form.dataset.buyxgetyBenefitMessage || 'Mua X nhận Y cần có giá trị giảm hoặc số lượng nhận lớn hơn 0.';
        }
    }

    if (!getQuantity.error && actionType === 'GiftProduct' && getQuantity.value <= 0) {
        getQuantity.error = form.dataset.giftQuantityPositiveMessage || 'Số lượng quà tặng phải lớn hơn 0.';
    }

    if (!maxDiscountValue.error &&
        ['DiscountOrder', 'DiscountProduct', 'BuyXGetY'].includes(actionType) &&
        discountValue.value !== null &&
        discountValue.value > 0 &&
        maxDiscountValue.value !== null &&
        maxDiscountValue.value < discountValue.value) {
        maxDiscountValue.error = form.dataset.maxDiscountLessMessage || 'Mức giảm tối đa không được nhỏ hơn giá trị giảm.';
    }

    if (!usageLimit.error && usageLimit.value !== null) {
        const usedCount = readFormNumber(form, 'usedCount') ?? 0;
        if (usageLimit.value < usedCount) {
            usageLimit.error = form.dataset.usageLimitLessThanUsedMessage || `Giới hạn sử dụng không được nhỏ hơn số lượt đã dùng (${usedCount}).`;
        }
    }

    isValid = applyPromotionFieldError(form, 'Name', name.error, touchedFields, showAll) && isValid;
    isValid = applyPromotionFieldError(form, 'Description', description.error, touchedFields, showAll) && isValid;
    isValid = applyPromotionFieldError(form, 'TargetType', targetType.error, touchedFields, showAll) && isValid;
    isValid = applyPromotionFieldError(form, 'TargetIds', targetIds.error, touchedFields, showAll) && isValid;
    isValid = applyPromotionFieldError(form, 'ActionType', '', touchedFields, showAll) && isValid;
    isValid = applyPromotionFieldError(form, 'DiscountValue', discountValue.error, touchedFields, showAll) && isValid;
    isValid = applyPromotionFieldError(form, 'MaxDiscountValue', maxDiscountValue.error, touchedFields, showAll) && isValid;
    isValid = applyPromotionFieldError(form, 'BuyQuantity', buyQuantity.error, touchedFields, showAll) && isValid;
    isValid = applyPromotionFieldError(form, 'GetQuantity', getQuantity.error, touchedFields, showAll) && isValid;
    isValid = applyPromotionFieldError(form, 'GiftProductVariantId', giftVariant.error, touchedFields, showAll) && isValid;
    isValid = applyPromotionFieldError(form, 'StartDate', dateErrors.startDate, touchedFields, showAll) && isValid;
    isValid = applyPromotionFieldError(form, 'EndDate', dateErrors.endDate, touchedFields, showAll) && isValid;
    isValid = applyPromotionFieldError(form, 'Priority', priority.error, touchedFields, showAll) && isValid;
    isValid = applyPromotionFieldError(form, 'MinOrderValue', minOrderValue.error, touchedFields, showAll) && isValid;
    isValid = applyPromotionFieldError(form, 'UsageLimit', usageLimit.error, touchedFields, showAll) && isValid;

    return isValid;
}

function validateGiftVariantField(form, actionType) {
    const field = getPromotionField(form, 'GiftProductVariantId');
    if (!field || actionType !== 'GiftProduct') {
        return { error: '' };
    }

    return {
        error: field.value ? '' : form.dataset.giftVariantRequiredMessage || 'Sản phẩm quà tặng là bắt buộc.',
    };
}

function validateSelectField(form, fieldName, options = {}) {
    const field = getPromotionField(form, fieldName);
    if (!field) {
        return { error: '' };
    }

    if (options.required && !field.value) {
        return { error: getRequiredMessage(field) };
    }

    return { error: '' };
}

function validateTargetIdsField(form) {
    const fields = getPromotionFields(form, 'TargetIds');
    if (fields.length === 0) {
        return { error: '' };
    }

    const selectedOptions = fields.filter(item => item.checked && !item.disabled);

    return {
        error: selectedOptions.length > 0
            ? ''
            : form.dataset.targetRequiredMessage || 'Vui lòng chọn ít nhất một phạm vi áp dụng.',
    };
}

function normalizeSearchTerm(value) {
    return value
        .toLocaleLowerCase('vi-VN')
        .normalize('NFD')
        .replace(/[\u0300-\u036f]/g, '')
        .trim();
}

function validateTextField(form, fieldName, options = {}) {
    const field = getPromotionField(form, fieldName);
    const value = field?.value.trim() ?? '';
    const maxLength = Number(field?.dataset.valLengthMax || field?.getAttribute('maxlength') || 0);

    if (!field) {
        return { error: '' };
    }

    if (!value && options.required) {
        return { error: getRequiredMessage(field) };
    }

    if (maxLength > 0 && value.length > maxLength) {
        return {
            error: field.dataset.valLength || `${getFieldLabel(form, field)} tối đa ${maxLength} ký tự.`,
        };
    }

    return { error: '' };
}

function validateNumberField(form, fieldName, options = {}) {
    const field = getPromotionField(form, fieldName);
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
    const startField = getPromotionField(form, 'StartDate');
    const endField = getPromotionField(form, 'EndDate');
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
        endError = form.dataset.endAfterStartMessage || 'Ngày kết thúc phải sau ngày bắt đầu.';
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

function applyPromotionFieldError(form, fieldName, message, touchedFields, showAll) {
    const shouldShow = showAll || touchedFields.has(fieldName);
    const visibleMessage = shouldShow ? message : '';
    const field = getPromotionErrorField(form, fieldName);
    const messageElement = getPromotionValidationMessage(form, fieldName);
    const fieldGroup = field?.closest('[data-promotion-field]');

    if (field) {
        field.setCustomValidity(message || '');
        field.setAttribute('aria-invalid', message ? 'true' : 'false');
        field.classList.toggle('promotion-field-invalid', Boolean(visibleMessage));
    }

    fieldGroup?.classList.toggle('has-error', Boolean(visibleMessage));

    if (messageElement) {
        messageElement.textContent = visibleMessage;
        messageElement.classList.toggle('field-validation-error', Boolean(visibleMessage));
        messageElement.classList.toggle('field-validation-valid', !visibleMessage);
    }

    return message === '';
}

function getPromotionField(form, fieldName) {
    return form.querySelector(`[name="${fieldName}"]`);
}

function getPromotionErrorField(form, fieldName) {
    if (fieldName === 'TargetIds') {
        return form.querySelector('[data-promotion-target-search]')
            || form.querySelector('[name="TargetIds"]:not(:disabled)')
            || getPromotionField(form, fieldName);
    }

    return getPromotionField(form, fieldName);
}

function getPromotionFields(form, fieldName) {
    return Array.from(form.querySelectorAll(`[name="${fieldName}"]`));
}

function getPromotionValidationMessage(form, fieldName) {
    return form.querySelector(`[data-valmsg-for="${fieldName}"]`);
}

function getAntiForgeryToken(scope) {
    return scope?.querySelector('input[name="__RequestVerificationToken"]')?.value
        ?? document.querySelector('input[name="__RequestVerificationToken"]')?.value
        ?? '';
}

function bindPromotionStatusToggles() {
    document.querySelectorAll('[data-promotion-toggle]').forEach(button => {
        button.addEventListener('click', () => togglePromotionStatus(button));
    });
}

async function togglePromotionStatus(button) {
    const url = button.dataset.promotionToggleUrl;
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
            throw new Error('Không thể cập nhật trạng thái khuyến mãi.');
        }

        await response.json();
        window.location.reload();
    } catch {
        showPromotionNotice('Không thể cập nhật trạng thái khuyến mãi. Vui lòng thử lại.', 'error');
        button.disabled = false;
    }
}

function bindPromotionDeleteConfirmation() {
    document.querySelectorAll('[data-promotion-delete]').forEach(form => {
        form.addEventListener('submit', event => {
            const name = form.dataset.promotionName || 'khuyến mãi này';
            const usedCount = Number(form.dataset.usedCount || 0);

            if (usedCount > 0) {
                event.preventDefault();
                showPromotionNotice(`Không thể xoá "${name}" vì khuyến mãi đã phát sinh lượt sử dụng.`, 'error');
                return;
            }

            if (!window.confirm(`Bạn có chắc muốn xoá khuyến mãi "${name}"?\nHành động này không thể hoàn tác.`)) {
                event.preventDefault();
            }
        });
    });
}

function showPromotionNotice(message, type = 'success') {
    const root = document.querySelector('[data-promotion-toast-root]');
    if (!root) {
        return;
    }

    const toast = document.createElement('div');
    toast.className = `promotion-toast is-${type}`;

    const marker = document.createElement('span');
    marker.className = 'promotion-toast-marker';

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
