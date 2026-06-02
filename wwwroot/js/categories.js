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
    bindCategoryFormValidation();
    bindSlugGenerator();
    bindImagePreview();
    bindCategoryImageFallbacks();
    bindToastDismiss();
    bindStatusToggles();
    bindDeleteConfirmation();
});

function bindCategoryFormValidation() {
    const form = document.querySelector('.surface-form-grid')?.closest('form');
    if (!form) {
        return;
    }

    bindSurfaceFormClientValidation(form);
}

function bindSurfaceFormClientValidation(form) {
    if (form.dataset.clientValidationBound === 'true') {
        return;
    }

    form.dataset.clientValidationBound = 'true';
    form.setAttribute('novalidate', 'novalidate');

    const alertBox = form.querySelector('[data-surface-form-alert]')
        ?? form.closest('.surface-form-grid')?.querySelector('[data-surface-form-alert]')
        ?? form.closest('.surface-form-card')?.querySelector('[data-surface-form-alert]');
    let hasSubmitted = !alertBox?.classList.contains('hidden');
    const setAlertVisible = isVisible => {
        alertBox?.classList.toggle('hidden', !isVisible);
    };

    const ignoredTypes = new Set(['hidden', 'button', 'submit', 'reset', 'image', 'checkbox', 'radio', 'file']);
    const fallbackRequired = 'Trường này là bắt buộc.';
    const fallbackInvalid = 'Giá trị không hợp lệ.';
    const fallbackNumber = 'Giá trị phải là một số hợp lệ.';

    const getFields = () => Array.from(form.querySelectorAll('input[name], select[name], textarea[name]'))
        .filter(field => !field.disabled && !ignoredTypes.has((field.type || '').toLowerCase()))
        .filter(field => field.dataset.val === 'true' || hasNativeRules(field));

    const hasNativeRules = field =>
        field.hasAttribute('required') ||
        field.hasAttribute('min') ||
        field.hasAttribute('max') ||
        field.hasAttribute('minlength') ||
        field.hasAttribute('maxlength') ||
        field.hasAttribute('pattern');

    const getValidationMessage = fieldName => Array.from(form.querySelectorAll('[data-valmsg-for]'))
        .find(element => element.dataset.valmsgFor === fieldName) ?? null;

    const getFieldValue = field => (typeof field.value === 'string' ? field.value.trim() : '');

    const getNumber = rawValue => {
        const value = Number(String(rawValue).replace(',', '.'));
        return Number.isFinite(value) ? value : null;
    };

    const getConstraint = (field, dataKey, attrName) => {
        const rawValue = field.dataset[dataKey] || field.getAttribute(attrName);
        return rawValue ? getNumber(rawValue) : null;
    };

    const getFieldError = field => {
        const value = getFieldValue(field);

        if ((field.dataset.valRequired || field.required) && value === '') {
            return field.dataset.valRequired || fallbackRequired;
        }

        if (value === '') {
            return '';
        }

        const shouldBeNumber = field.type === 'number' || field.dataset.valNumber || field.dataset.valRange;
        if (shouldBeNumber && getNumber(value) === null) {
            return field.dataset.valNumber || fallbackNumber;
        }

        const min = getConstraint(field, 'valRangeMin', 'min');
        const max = getConstraint(field, 'valRangeMax', 'max');
        const numericValue = getNumber(value);
        if (numericValue !== null && ((min !== null && numericValue < min) || (max !== null && numericValue > max))) {
            return field.dataset.valRange || fallbackInvalid;
        }

        const minLength = getConstraint(field, 'valLengthMin', 'minlength');
        const maxLength = getConstraint(field, 'valLengthMax', 'maxlength') ?? getConstraint(field, 'valMaxlengthMax', 'maxlength');
        if (minLength !== null && value.length < minLength) {
            return field.dataset.valLength || fallbackInvalid;
        }

        if (maxLength !== null && value.length > maxLength) {
            return field.dataset.valLength || field.dataset.valMaxlength || fallbackInvalid;
        }

        const pattern = field.dataset.valRegexPattern || field.pattern;
        if (pattern) {
            try {
                if (!new RegExp(pattern).test(value)) {
                    return field.dataset.valRegex || fallbackInvalid;
                }
            } catch {
                return '';
            }
        }

        if (!field.validity.valid) {
            return fallbackInvalid;
        }

        return '';
    };

    const setFieldError = (field, message) => {
        const messageElement = getValidationMessage(field.name);
        const hasError = Boolean(message);

        field.setAttribute('aria-invalid', hasError ? 'true' : 'false');
        field.classList.toggle('input-validation-error', hasError);

        if (messageElement) {
            messageElement.textContent = message;
            messageElement.classList.toggle('field-validation-error', hasError);
            messageElement.classList.toggle('field-validation-valid', !hasError);
        }
    };

    const validateField = (field, showError) => {
        const message = getFieldError(field);
        if (showError) {
            setFieldError(field, message);
        }

        return message === '';
    };

    const validateForm = showError => {
        let firstInvalid = null;
        const isValid = getFields()
            .map(field => {
                const fieldValid = validateField(field, showError);
                if (!fieldValid) {
                    firstInvalid ??= field;
                }
                return fieldValid;
            })
            .every(Boolean);

        if (showError) {
            setAlertVisible(!isValid);
        }

        return { isValid, firstInvalid };
    };

    const refreshAlertAfterFieldChange = () => {
        if (!hasSubmitted) {
            return;
        }

        const result = validateForm(false);
        setAlertVisible(!result.isValid);
    };

    ['input', 'change'].forEach(eventName => {
        form.addEventListener(eventName, event => {
            const field = event.target.closest('input[name], select[name], textarea[name]');
            if (field && form.contains(field)) {
                validateField(field, true);
                refreshAlertAfterFieldChange();
            }
        });
    });

    form.addEventListener('blur', event => {
        const field = event.target.closest('input[name], select[name], textarea[name]');
        if (field && form.contains(field)) {
            validateField(field, true);
        }
    }, true);

    form.addEventListener('submit', event => {
        hasSubmitted = true;
        const result = validateForm(true);
        if (!result.isValid) {
            event.preventDefault();
            result.firstInvalid?.focus();
        }
    });
}

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
            throw new Error('Máy chủ trả về lỗi.');
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
