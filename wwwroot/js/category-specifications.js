/**
 * category-specifications.js
 * Handles category-specific specification assignment UI.
 */

(function () {
    'use strict';

    const token = () =>
        document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';

    bindAvailableSpecFilter();
    bindAssignSpecificationValidation();
    bindInlineUpdates();
    bindToastDismiss();

    function bindAssignSpecificationValidation() {
        const assignForm = Array.from(document.querySelectorAll('form'))
            .find(form => form.querySelector('[name="SpecificationId"]'));

        if (!assignForm) {
            return;
        }

        const alertBox = assignForm.closest('.surface-form-card')?.querySelector('[data-surface-form-alert]')
            ?? assignForm.querySelector('[data-surface-form-alert]');
        let hasSubmitted = !alertBox?.classList.contains('hidden');
        const setAlertVisible = isVisible => {
            alertBox?.classList.toggle('hidden', !isVisible);
        };

        const specificationSelect = assignForm.querySelector('[name="SpecificationId"]');
        const groupNameInput = assignForm.querySelector('[name="GroupName"]');
        const sortOrderInput = assignForm.querySelector('[name="SortOrder"]');

        const validators = [
            showErrors => validateRequiredSelect(specificationSelect, 'Vui lòng chọn thông số cần gán.', showErrors),
            showErrors => validateMaxLength(groupNameInput, 120, 'Tên nhóm tối đa 120 ký tự.', showErrors),
            showErrors => validateNumberRange(sortOrderInput, 0, 9999, 'Thứ tự phải từ 0 đến 9999.', showErrors),
        ];

        [specificationSelect, groupNameInput, sortOrderInput].filter(Boolean).forEach(field => {
            ['input', 'change', 'blur'].forEach(eventName => {
                field.addEventListener(eventName, () => {
                    validateAssignForm(validators, true);
                    if (hasSubmitted) {
                        setAlertVisible(!validateAssignForm(validators, false));
                    }
                });
            });
        });

        assignForm.addEventListener('submit', event => {
            hasSubmitted = true;
            const isValid = validateAssignForm(validators, true);
            setAlertVisible(!isValid);

            if (!isValid) {
                event.preventDefault();
                assignForm.querySelector('[aria-invalid="true"]')?.focus();
            }
        });
    }

    function validateAssignForm(validators, showErrors) {
        return validators
            .map(validate => validate(showErrors))
            .every(Boolean);
    }

    function validateRequiredSelect(field, message, showError) {
        if (!field) {
            return true;
        }

        const error = field.value ? '' : message;
        if (showError) {
            setInlineError(field, error);
        }

        return error === '';
    }

    function validateMaxLength(field, maxLength, message, showError) {
        if (!field) {
            return true;
        }

        const error = field.value.trim().length > maxLength ? message : '';
        if (showError) {
            setInlineError(field, error);
        }

        return error === '';
    }

    function validateNumberRange(field, min, max, message, showError) {
        if (!field) {
            return true;
        }

        const rawValue = field.value.trim();
        const value = Number(rawValue);
        const error = rawValue !== '' && (!Number.isFinite(value) || value < min || value > max)
            ? message
            : '';

        if (showError) {
            setInlineError(field, error);
        }

        return error === '';
    }

    function setInlineError(field, message) {
        let messageElement = field.parentElement?.querySelector('[data-client-error-for="' + field.name + '"]');
        if (!messageElement) {
            messageElement = document.createElement('span');
            messageElement.dataset.clientErrorFor = field.name;
            messageElement.className = 'text-xs text-red-500 mt-1 block';
            field.insertAdjacentElement('afterend', messageElement);
        }

        messageElement.textContent = message;
        field.setAttribute('aria-invalid', message ? 'true' : 'false');
        field.classList.toggle('input-validation-error', Boolean(message));
    }

    function bindAvailableSpecFilter() {
        const availableSearch = document.getElementById('availableSpecSearch');
        const availableList = document.getElementById('availableSpecList');

        if (!availableSearch || !availableList) {
            return;
        }

        availableSearch.addEventListener('input', () => {
            const term = availableSearch.value.toLowerCase().trim();

            availableList.querySelectorAll('[data-spec-item]').forEach(item => {
                const text = `${item.dataset.name ?? ''} ${item.dataset.key ?? ''}`.toLowerCase();
                item.style.display = !term || text.includes(term) ? '' : 'none';
            });
        });
    }

    function bindInlineUpdates() {
        document.querySelectorAll('[data-spec-row]').forEach(row => {
            let debounceTimer;

            row.querySelectorAll('[data-field]').forEach(field => {
                field.addEventListener('change', () => {
                    clearTimeout(debounceTimer);
                    debounceTimer = setTimeout(() => updateCategorySpec(row), 500);
                });
            });

            row.querySelector('[data-field="isRequired"]')?.addEventListener('click', event => {
                event.currentTarget.classList.toggle('active');
                clearTimeout(debounceTimer);
                debounceTimer = setTimeout(() => updateCategorySpec(row), 200);
            });
        });
    }

    async function updateCategorySpec(row) {
        const categoryId = row.dataset.categoryId;
        const specificationId = row.dataset.specId;
        const groupName = row.querySelector('[data-field="groupName"]')?.value ?? '';
        const sortOrder = Number.parseInt(row.querySelector('[data-field="sortOrder"]')?.value ?? '0', 10);
        const isRequired = row.querySelector('[data-field="isRequired"]')?.classList.contains('active') ?? false;

        if (!categoryId || !specificationId) {
            showToast('error', 'Không xác định được liên kết thông số - danh mục.');
            return;
        }

        try {
            const response = await fetch('/CategorySpecifications/Update', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    RequestVerificationToken: token(),
                    'X-Requested-With': 'XMLHttpRequest',
                },
                body: new URLSearchParams({
                    CategoryId: categoryId,
                    SpecificationId: specificationId,
                    GroupName: groupName,
                    SortOrder: Number.isNaN(sortOrder) ? 0 : sortOrder,
                    IsRequired: isRequired,
                }),
            });

            const data = await response.json();
            showToast(data.succeeded ? 'success' : 'error', data.message);
        } catch {
            showToast('error', 'Lỗi kết nối, vui lòng thử lại.');
        }
    }

    function showToast(type, message) {
        const existing = document.getElementById('inlineToast');
        existing?.remove();

        const colors = type === 'success'
            ? 'bg-emerald-50 border-emerald-200 text-emerald-700'
            : 'bg-red-50 border-red-200 text-red-700';

        const icon = type === 'success' ? 'check' : 'alert-circle';

        const element = document.createElement('div');
        element.id = 'inlineToast';
        element.className = `fixed bottom-5 right-5 z-50 flex items-center gap-3 ${colors} border
            rounded-xl px-4 py-3 text-sm shadow-lg spec-anim`;
        element.innerHTML = `<i data-lucide="${icon}" class="w-4 h-4 flex-shrink-0"></i><span>${message}</span>`;
        document.body.appendChild(element);

        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }

        setTimeout(() => element.remove(), 4000);
    }

    function bindToastDismiss() {
        document.querySelectorAll('[data-dismiss-target]').forEach(button => {
            button.addEventListener('click', () =>
                document.getElementById(button.dataset.dismissTarget)?.remove());
        });

        setTimeout(() => {
            ['toastSuccess', 'toastError'].forEach(id => document.getElementById(id)?.remove());
        }, 5000);
    }
})();
