'use strict';

document.addEventListener('DOMContentLoaded', () => {
    bindInventoryForm();
    bindInventoryConfirmations();
    bindInventoryToastDismiss();
});

function bindInventoryForm() {
    const form = document.querySelector('[data-inv-form]');
    if (!form) {
        return;
    }

    const itemRoot = form.querySelector('[data-inv-items]');
    const template = form.querySelector('[data-inv-item-template]');
    const addButton = form.querySelector('[data-inv-add-item]');
    const alertBox = form.querySelector('[data-inv-form-alert]');
    let hasSubmitted = !alertBox?.classList.contains('hidden');

    form.addEventListener('input', event => {
        const field = event.target.closest('input, select, textarea');
        if (!field) {
            return;
        }

        if (field.matches('[data-inv-code]')) {
            field.value = field.value.toUpperCase().replace(/[^A-Z0-9_-]/g, '').slice(0, 50);
        }

        validateInventoryField(field, true);
        syncInventoryTotals(form);

        if (hasSubmitted) {
            setInventoryAlertVisible(alertBox, !validateInventoryForm(form, false));
        }
    });

    form.addEventListener('change', event => {
        const field = event.target.closest('input, select, textarea');
        if (!field) {
            return;
        }

        const row = field.closest('[data-inv-item-row]');
        if (row) {
            syncInventoryStockNote(row);
        }

        validateInventoryField(field, true);
        syncInventoryTotals(form);

        if (hasSubmitted) {
            setInventoryAlertVisible(alertBox, !validateInventoryForm(form, false));
        }
    });

    addButton?.addEventListener('click', () => {
        const row = createInventoryItemRow(itemRoot, template);
        if (!row) {
            return;
        }

        itemRoot.appendChild(row);
        reindexInventoryRows(form);
        syncInventoryStockNote(row);
        syncInventoryTotals(form);
        row.querySelector('select')?.focus();

        if (window.lucide?.createIcons) {
            window.lucide.createIcons();
        }
    });

    itemRoot?.addEventListener('click', event => {
        const removeButton = event.target.closest('[data-inv-remove-item]');
        if (!removeButton) {
            return;
        }

        const row = removeButton.closest('[data-inv-item-row]');
        if (!row) {
            return;
        }

        if (row.dataset.existing === 'true') {
            const removeValue = row.querySelector('[data-inv-item-remove-value]');
            if (removeValue) {
                removeValue.value = 'true';
            }
            row.classList.add('is-removed');
        } else {
            row.remove();
        }

        ensureAtLeastOneInventoryRow(form, itemRoot, template);
        reindexInventoryRows(form);
        syncInventoryTotals(form);
    });

    form.addEventListener('submit', event => {
        hasSubmitted = true;
        reindexInventoryRows(form);

        if (!validateInventoryForm(form, true)) {
            event.preventDefault();
            setInventoryAlertVisible(alertBox, true);
            form.querySelector('[aria-invalid="true"]')?.focus();
        }
    });

    form.querySelectorAll('[data-inv-item-row]').forEach(syncInventoryStockNote);
    syncInventoryTotals(form);
}

function createInventoryItemRow(itemRoot, template) {
    if (!itemRoot || !template) {
        return null;
    }

    const index = itemRoot.querySelectorAll('[data-inv-item-row]').length;
    const wrapper = document.createElement('div');
    wrapper.innerHTML = template.innerHTML.replaceAll('__index__', String(index)).trim();
    return wrapper.firstElementChild;
}

function ensureAtLeastOneInventoryRow(form, itemRoot, template) {
    if (!form || !itemRoot || itemRoot.querySelector('[data-inv-item-row]:not(.is-removed)')) {
        return;
    }

    const row = createInventoryItemRow(itemRoot, template);
    if (row) {
        itemRoot.appendChild(row);
    }
}

function reindexInventoryRows(scope) {
    const itemRoot = scope.querySelector('[data-inv-items]');
    if (!itemRoot) {
        return;
    }

    itemRoot.querySelectorAll('[data-inv-item-row]').forEach((row, index) => {
        row.querySelectorAll('[data-inv-item-field]').forEach(field => {
            const property = field.dataset.invItemField;
            field.name = `Items[${index}].${property}`;

            if (field.type !== 'hidden') {
                field.id = `Items_${index}__${property}`;
                const label = field.closest('[data-inv-field]')?.querySelector('label.inv-label');
                label?.setAttribute('for', field.id);
            }

            const suffixMap = {
                ProductVariantId: 'variant',
                Quantity: 'quantity',
                ImportPrice: 'price',
            };
            const suffix = suffixMap[property];
            if (suffix) {
                const errorId = `inv-item-${index}-${suffix}-error`;
                field.dataset.invErrorTarget = errorId;
                const errorElement = field.closest('[data-inv-field]')?.querySelector('.inv-field-error');
                if (errorElement) {
                    errorElement.id = errorId;
                }
            }
        });
    });
}

function validateInventoryForm(form, showErrors) {
    const headerFieldsValid = Array.from(form.querySelectorAll('[data-inv-required], [data-inv-pattern], [data-inv-number-min]'))
        .filter(field => !field.closest('[data-inv-item-row]'))
        .filter(isInventoryFieldActive)
        .map(field => validateInventoryField(field, showErrors))
        .every(Boolean);

    const itemRows = Array.from(form.querySelectorAll('[data-inv-item-row]'))
        .filter(row => !row.classList.contains('is-removed'));
    const rowsWithValue = itemRows.filter(hasInventoryRowValue);
    const itemRowsValid = rowsWithValue
        .map(row => validateInventoryRow(row, showErrors))
        .every(Boolean);

    if (showErrors && rowsWithValue.length === 0) {
        const firstRow = itemRows[0];
        firstRow?.querySelectorAll('[data-inv-required]').forEach(field => validateInventoryField(field, true));
    }

    return headerFieldsValid && itemRowsValid && rowsWithValue.length > 0;
}

function validateInventoryRow(row, showErrors) {
    return Array.from(row.querySelectorAll('[data-inv-required], [data-inv-number-min]'))
        .filter(isInventoryFieldActive)
        .map(field => validateInventoryField(field, showErrors))
        .every(Boolean);
}

function validateInventoryField(field, showError) {
    if (!field || !isInventoryFieldActive(field)) {
        return true;
    }

    const row = field.closest('[data-inv-item-row]');
    if (row && !hasInventoryRowValue(row)) {
        clearInventoryFieldError(field);
        return true;
    }

    let message = '';
    const value = typeof field.value === 'string' ? field.value.trim() : '';

    if (field.dataset.invRequired && value === '') {
        message = field.dataset.invRequired;
    } else if (field.dataset.invPattern && value !== '') {
        const pattern = new RegExp(field.dataset.invPattern);
        if (!pattern.test(value)) {
            message = field.dataset.invPatternMessage || 'Giá trị không hợp lệ.';
        }
    } else if (field.dataset.invNumberMin !== undefined && value !== '') {
        const numberValue = Number(value);
        const minValue = Number(field.dataset.invNumberMin);
        if (!Number.isFinite(numberValue) || numberValue < minValue) {
            message = field.dataset.invNumberMessage || 'Giá trị số không hợp lệ.';
        }
    }

    setInventoryFieldError(field, message, showError);
    return message === '';
}

function setInventoryFieldError(field, message, showError) {
    if (!field || !showError) {
        return;
    }

    const targetId = field.dataset.invErrorTarget;
    const errorElement = targetId ? document.getElementById(targetId) : null;
    if (errorElement) {
        errorElement.textContent = message;
    }

    field.closest('[data-inv-field]')?.classList.toggle('has-error', Boolean(message));
    field.setAttribute('aria-invalid', message ? 'true' : 'false');
}

function clearInventoryFieldError(field) {
    if (!field) {
        return;
    }

    const targetId = field.dataset.invErrorTarget;
    const errorElement = targetId ? document.getElementById(targetId) : null;
    if (errorElement) {
        errorElement.textContent = '';
    }

    field.closest('[data-inv-field]')?.classList.remove('has-error');
    field.setAttribute('aria-invalid', 'false');
}

function isInventoryFieldActive(field) {
    return !field.disabled && !field.closest('.is-removed');
}

function hasInventoryRowValue(row) {
    const variant = row.querySelector('[data-inv-item-field="ProductVariantId"]')?.value.trim() || '';
    const quantity = row.querySelector('[data-inv-item-field="Quantity"]')?.value.trim() || '';
    const price = row.querySelector('[data-inv-item-field="ImportPrice"]')?.value.trim() || '';
    const id = row.querySelector('[data-inv-item-field="Id"]')?.value.trim() || '';
    return variant !== '' || quantity !== '' || price !== '' || id !== '';
}

function syncInventoryTotals(form) {
    const rows = Array.from(form.querySelectorAll('[data-inv-item-row]'))
        .filter(row => !row.classList.contains('is-removed'));
    let total = 0;

    rows.forEach(row => {
        const quantity = Number(row.querySelector('[data-inv-item-field="Quantity"]')?.value || 0);
        const price = Number(row.querySelector('[data-inv-item-field="ImportPrice"]')?.value || 0);
        const lineTotal = Number.isFinite(quantity) && Number.isFinite(price) && quantity > 0 && price >= 0
            ? quantity * price
            : 0;

        total += lineTotal;
        const lineTotalElement = row.querySelector('[data-inv-line-total]');
        if (lineTotalElement) {
            lineTotalElement.textContent = formatInventoryMoney(lineTotal);
        }
    });

    const totalElement = form.querySelector('[data-inv-total]');
    if (totalElement) {
        totalElement.textContent = formatInventoryMoney(total);
    }
}

function syncInventoryStockNote(row) {
    const select = row.querySelector('[data-inv-item-field="ProductVariantId"]');
    const note = row.querySelector('[data-inv-stock-note]');
    if (!select || !note) {
        return;
    }

    const option = select.selectedOptions[0];
    const stock = option?.dataset.stock;
    note.textContent = stock === undefined || select.value === ''
        ? 'Chưa chọn SKU'
        : `Tồn hiện tại: ${stock}`;
}

function formatInventoryMoney(value) {
    return `${new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 0 }).format(value || 0)} đ`;
}

function setInventoryAlertVisible(alertBox, isVisible) {
    alertBox?.classList.toggle('hidden', !isVisible);
}

function bindInventoryConfirmations() {
    document.querySelectorAll('[data-inv-confirm]').forEach(form => {
        form.addEventListener('submit', event => {
            const message = form.dataset.invConfirm || 'Xác nhận thao tác này?';
            if (!window.confirm(message)) {
                event.preventDefault();
            }
        });
    });
}

function bindInventoryToastDismiss() {
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
