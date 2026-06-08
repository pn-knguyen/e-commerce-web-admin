(function () {
    'use strict';

    const token = () =>
        document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';

    bindAssignSpecificationForm();
    bindInlineUpdates();
    bindToastDismiss();

    function bindAssignSpecificationForm() {
        const form = document.querySelector('[data-category-spec-assign-form]');
        if (!form) {
            return;
        }

        const alertBox = form.closest('.surface-form-card')?.querySelector('[data-surface-form-alert]');
        const alertText = alertBox?.querySelector('[data-category-spec-alert-text]');
        const searchInput = document.getElementById('categorySpecAssignSearch');
        const selectedCount = form.querySelector('[data-category-spec-selected-count]');
        const rows = Array.from(form.querySelectorAll('[data-category-spec-assign-row]'));
        let hasSubmitted = false;

        const setAlert = (message) => {
            const hasMessage = Boolean(message);
            if (alertText && message) {
                alertText.textContent = message;
            }

            alertBox?.classList.toggle('hidden', !hasMessage);
        };

        const checkedRows = () => rows.filter(row => row.querySelector('[data-category-spec-select]')?.checked);

        const updateRowState = (row) => {
            const checkbox = row.querySelector('[data-category-spec-select]');
            row.classList.toggle('is-selected', Boolean(checkbox?.checked));
        };

        const updateSelectedCount = () => {
            const count = checkedRows().length;
            if (selectedCount) {
                selectedCount.textContent = `${count} đã chọn`;
            }
        };

        const selectRow = (row) => {
            const checkbox = row.querySelector('[data-category-spec-select]');
            if (!checkbox?.checked) {
                checkbox.checked = true;
                updateRowState(row);
                updateSelectedCount();
            }
        };

        const clearFieldError = (field) => setFieldError(field, '');

        rows.forEach(row => {
            const select = row.querySelector('[data-category-spec-select]');
            const groupInput = row.querySelector('[data-category-spec-group]');
            const orderInput = row.querySelector('[data-category-spec-order]');
            const requiredInput = row.querySelector('[data-category-spec-required]');

            select?.addEventListener('change', () => {
                updateRowState(row);
                updateSelectedCount();
                if (hasSubmitted) {
                    validateAssignForm(form, rows, false, setAlert);
                }
            });

            [groupInput, orderInput, requiredInput].filter(Boolean).forEach(field => {
                field.addEventListener('input', () => {
                    selectRow(row);
                    clearFieldError(field);
                    if (hasSubmitted) {
                        validateAssignForm(form, rows, false, setAlert);
                    }
                });

                field.addEventListener('change', () => {
                    selectRow(row);
                    clearFieldError(field);
                    if (hasSubmitted) {
                        validateAssignForm(form, rows, false, setAlert);
                    }
                });
            });

            updateRowState(row);
        });

        searchInput?.addEventListener('input', () => {
            const term = searchInput.value.toLowerCase().trim();
            rows.forEach(row => {
                const text = `${row.dataset.name ?? ''} ${row.dataset.key ?? ''}`.toLowerCase();
                row.hidden = Boolean(term) && !text.includes(term);
            });
        });

        document.querySelectorAll('[data-category-spec-group-chip]').forEach(button => {
            button.addEventListener('click', () => {
                const groupName = button.dataset.categorySpecGroupChip ?? '';
                const targets = checkedRows().length > 0 ? checkedRows() : rows.filter(row => !row.hidden);
                targets.forEach(row => {
                    const groupInput = row.querySelector('[data-category-spec-group]');
                    if (groupInput) {
                        groupInput.value = groupName;
                        selectRow(row);
                        clearFieldError(groupInput);
                    }
                });
            });
        });

        form.addEventListener('submit', event => {
            hasSubmitted = true;
            const result = validateAssignForm(form, rows, true, setAlert);
            if (!result.isValid) {
                event.preventDefault();
                result.firstInvalid?.focus();
            }
        });

        updateSelectedCount();
    }

    function validateAssignForm(form, rows, showErrors, setAlert) {
        let firstInvalid = null;
        let isValid = true;
        const selectedRows = rows.filter(row => row.querySelector('[data-category-spec-select]')?.checked);

        rows.forEach(row => {
            clearRowErrors(row);
        });

        if (selectedRows.length === 0) {
            setAlert('Vui lòng chọn ít nhất một thông số cần gán.');
            return { isValid: false, firstInvalid: form.querySelector('[data-category-spec-select]') };
        }

        selectedRows.forEach(row => {
            const groupInput = row.querySelector('[data-category-spec-group]');
            const orderInput = row.querySelector('[data-category-spec-order]');

            if (groupInput && groupInput.value.trim().length > 120) {
                isValid = false;
                firstInvalid ??= groupInput;
                if (showErrors) {
                    setFieldError(groupInput, 'Tên nhóm tối đa 120 ký tự.');
                }
            }

            const rawOrder = orderInput?.value.trim() ?? '';
            const orderValue = Number(rawOrder);
            if (!orderInput || rawOrder === '' || !Number.isFinite(orderValue) || orderValue < 0 || orderValue > 9999) {
                isValid = false;
                firstInvalid ??= orderInput;
                if (showErrors && orderInput) {
                    setFieldError(orderInput, 'Thứ tự phải từ 0 đến 9999.');
                }
            }
        });

        setAlert(isValid ? '' : 'Vui lòng kiểm tra lại thông tin.');
        return { isValid, firstInvalid };
    }

    function clearRowErrors(row) {
        row.querySelectorAll('[data-category-spec-group], [data-category-spec-order]').forEach(field => {
            setFieldError(field, '');
        });
    }

    function setFieldError(field, message) {
        if (!field) {
            return;
        }

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
                    IsRequired: String(isRequired),
                }),
            });

            const data = await response.json();
            showToast(data.succeeded ? 'success' : 'error', data.message || 'Không thể cập nhật thông số.');
        } catch {
            showToast('error', 'Lỗi kết nối, vui lòng thử lại.');
        }
    }

    function showToast(type, message) {
        document.getElementById('inlineToast')?.remove();

        const colors = type === 'success'
            ? 'bg-emerald-50 border-emerald-200 text-emerald-700'
            : 'bg-red-50 border-red-200 text-red-700';
        const icon = type === 'success' ? 'check' : 'alert-circle';

        const element = document.createElement('div');
        element.id = 'inlineToast';
        element.className = `fixed bottom-5 right-5 z-50 flex items-center gap-3 ${colors} border rounded-xl px-4 py-3 text-sm shadow-lg spec-anim`;

        const iconElement = document.createElement('i');
        iconElement.setAttribute('data-lucide', icon);
        iconElement.className = 'w-4 h-4 flex-shrink-0';

        const text = document.createElement('span');
        text.textContent = message;

        element.append(iconElement, text);
        document.body.appendChild(element);

        if (typeof lucide !== 'undefined') {
            lucide.createIcons({ nodes: [element] });
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