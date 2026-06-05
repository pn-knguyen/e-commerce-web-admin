'use strict';

document.addEventListener('DOMContentLoaded', () => {
    bindProductVariantFormValidation();
    bindProductAttributeVisibility();
    bindVariantImageRows();
    bindVariantStatusToggles();
    bindVariantDefaultButtons();
    bindVariantDeleteConfirmation();
    bindToastDismiss();
});

function bindProductVariantFormValidation() {
    const form = document.querySelector('[data-pv-form]');
    if (!form) {
        return;
    }

    const alertBox = form.querySelector('[data-pv-form-alert]');
    let hasSubmitted = !alertBox?.classList.contains('hidden');

    const validateForm = showErrors => {
        const requiredFieldsValid = getValidatableFields(form)
            .map(field => validateField(field, showErrors))
            .every(Boolean);
        const imagesValid = validateImageRows(form, showErrors);
        const isValid = requiredFieldsValid && imagesValid;

        if (showErrors) {
            setFormAlertVisible(alertBox, !isValid);
        }

        return isValid;
    };

    form.addEventListener('input', event => {
        const field = event.target.closest('input, select, textarea');
        if (!field) {
            return;
        }

        if (field.matches('[data-pv-code]')) {
            field.value = field.value.toUpperCase().replace(/[^A-Z0-9_-]/g, '').slice(0, 80);
        }

        validateField(field, true);
        if (field.closest('[data-pv-image-row]')) {
            validateImageRows(form, true);
        }

        if (hasSubmitted) {
            setFormAlertVisible(alertBox, !validateForm(false));
        }
    });

    form.addEventListener('change', event => {
        const field = event.target.closest('input, select, textarea');
        if (!field) {
            return;
        }

        validateField(field, true);
        if (hasSubmitted) {
            setFormAlertVisible(alertBox, !validateForm(false));
        }
    });

    form.addEventListener('submit', event => {
        hasSubmitted = true;
        reindexImageRows(form);

        if (!validateForm(true)) {
            event.preventDefault();
            const firstInvalid = form.querySelector('[aria-invalid="true"]');
            firstInvalid?.focus();
        }
    });
}

function getValidatableFields(scope) {
    return Array.from(scope.querySelectorAll('[data-pv-required], [data-pv-pattern], [data-pv-number-min]'))
        .filter(field => !field.disabled)
        .filter(field => !field.closest('[hidden], .is-removed'));
}

function validateField(field, showError) {
    let message = '';
    const value = typeof field.value === 'string' ? field.value.trim() : '';

    if (field.dataset.pvRequired && value === '') {
        message = field.dataset.pvRequired;
    } else if (field.dataset.pvPattern && value !== '') {
        const pattern = new RegExp(field.dataset.pvPattern);
        if (!pattern.test(value)) {
            message = field.dataset.pvPatternMessage || 'Giá trị không hợp lệ.';
        }
    } else if (field.dataset.pvNumberMin !== undefined && value !== '') {
        const numberValue = Number(value);
        const minValue = Number(field.dataset.pvNumberMin);
        if (!Number.isFinite(numberValue) || numberValue < minValue) {
            message = field.dataset.pvNumberMessage || 'Giá trị số không hợp lệ.';
        }
    }

    setFieldError(field, message, showError);
    return message === '';
}

function validateImageRows(scope, showError) {
    return Array.from(scope.querySelectorAll('[data-pv-image-row]'))
        .map(row => validateImageRow(row, showError))
        .every(Boolean);
}

function validateImageRow(row, showError) {
    if (row.classList.contains('is-removed')) {
        return true;
    }

    const colorField = row.querySelector('[data-pv-image-field="Color"]');
    const pathField = row.querySelector('[data-pv-image-field="ImagePath"]');
    const fileField = row.querySelector('[data-pv-image-field="ImageFile"]');
    const altField = row.querySelector('[data-pv-image-field="AltText"]');
    const positionField = row.querySelector('[data-pv-image-field="Position"]');

    const color = colorField?.value.trim() || '';
    const path = pathField?.value.trim() || '';
    const hasFile = Boolean(fileField?.files?.length);
    const alt = altField?.value.trim() || '';
    const hasAnyValue = color !== '' || path !== '' || hasFile || alt !== '';

    let isValid = true;
    if (hasAnyValue && color === '') {
        setFieldError(colorField, 'Màu ảnh là bắt buộc.', showError);
        isValid = false;
    } else {
        setFieldError(colorField, '', showError);
    }

    if (hasAnyValue && path === '' && !hasFile) {
        setFieldError(fileField, 'Vui lòng chọn ảnh để tải lên.', showError);
        isValid = false;
    } else {
        setFieldError(fileField, '', showError);
    }

    if (positionField && positionField.value.trim() !== '' && Number(positionField.value) < 0) {
        setFieldError(positionField, 'Thứ tự ảnh không được âm.', showError);
        isValid = false;
    } else {
        setFieldError(positionField, '', showError);
    }

    return isValid;
}

function setFieldError(field, message, showError) {
    if (!field || !showError) {
        return;
    }

    const targetId = field.dataset.pvErrorTarget;
    const errorElement = targetId ? document.getElementById(targetId) : null;
    if (errorElement) {
        errorElement.textContent = message;
    }

    const fieldGroup = field.closest('[data-pv-field]');
    fieldGroup?.classList.toggle('has-error', Boolean(message));
    field.setAttribute('aria-invalid', message ? 'true' : 'false');
}

function clearFieldError(field) {
    if (!field) {
        return;
    }

    const targetId = field.dataset.pvErrorTarget;
    const errorElement = targetId ? document.getElementById(targetId) : null;
    if (errorElement) {
        errorElement.textContent = '';
    }

    field.closest('[data-pv-field]')?.classList.remove('has-error');
    field.setAttribute('aria-invalid', 'false');
}

function setFormAlertVisible(alertBox, isVisible) {
    alertBox?.classList.toggle('hidden', !isVisible);
}

function bindProductAttributeVisibility() {
    const form = document.querySelector('[data-pv-form]');
    if (!form) {
        return;
    }

    const productSelect = form.querySelector('[data-pv-product-select]');
    const lockedCategoryInput = form.querySelector('[data-pv-locked-category-id]');
    const attributeFields = Array.from(form.querySelectorAll('[data-pv-attribute-field]'));
    const emptyNote = form.querySelector('[data-pv-attribute-empty]');

    const getSelectedCategoryId = () => {
        if (productSelect) {
            return productSelect.selectedOptions[0]?.dataset.categoryId || '';
        }

        return lockedCategoryInput?.value || '';
    };

    const sync = () => {
        const selectedCategoryId = getSelectedCategoryId();
        let visibleCount = 0;

        attributeFields.forEach(field => {
            const isVisible = Boolean(selectedCategoryId) && field.dataset.categoryId === selectedCategoryId;
            field.hidden = !isVisible;

            const select = field.querySelector('[data-pv-attribute-select]');
            if (select) {
                select.disabled = !isVisible;
                if (!isVisible) {
                    clearFieldError(select);
                }
            }

            if (isVisible) {
                visibleCount += 1;
            }
        });

        if (emptyNote) {
            emptyNote.hidden = Boolean(selectedCategoryId) && visibleCount > 0;
            emptyNote.textContent = selectedCategoryId
                ? 'Danh mục của sản phẩm này chưa được cấu hình thuộc tính biến thể.'
                : 'Chọn sản phẩm để hiển thị thuộc tính biến thể.';
        }
    };

    productSelect?.addEventListener('change', sync);
    sync();
}

function bindVariantImageRows() {
    const imageRoot = document.querySelector('[data-pv-images]');
    const addButton = document.querySelector('[data-pv-add-image]');
    const template = document.querySelector('[data-pv-image-template]');

    if (!imageRoot || !addButton || !template) {
        return;
    }

    imageRoot.addEventListener('click', event => {
        const removeButton = event.target.closest('[data-pv-remove-image]');
        if (!removeButton) {
            return;
        }

        const row = removeButton.closest('[data-pv-image-row]');
        if (!row) {
            return;
        }

        if (row.dataset.existing === 'true') {
            const removeValue = row.querySelector('[data-pv-image-remove-value]');
            if (removeValue) {
                removeValue.value = 'true';
            }
            row.classList.add('is-removed');
        } else {
            row.remove();
        }

        reindexImageRows(document);
    });

    imageRoot.addEventListener('input', event => {
        const colorPicker = event.target.closest('[data-pv-color-picker]');
        if (colorPicker) {
            syncImageColorControl(colorPicker.closest('[data-pv-image-row]'), colorPicker.value);
            return;
        }

        const colorText = event.target.closest('[data-pv-color-text]');
        if (colorText) {
            syncImageColorControl(colorText.closest('[data-pv-image-row]'), colorText.value, { fromText: true });
        }
    });

    imageRoot.addEventListener('change', event => {
        const fileInput = event.target.closest('[data-pv-file-input]');
        if (!fileInput) {
            return;
        }

        expandSelectedImageFiles(fileInput, imageRoot, template);
        syncImageFileLabel(fileInput);
        syncImagePreview(fileInput.closest('[data-pv-image-row]'), fileInput);
        validateImageRow(fileInput.closest('[data-pv-image-row]'), true);
    });

    addButton.addEventListener('click', () => {
        const row = createImageRowFromTemplate(imageRoot, template);
        if (!row) {
            return;
        }

        imageRoot.appendChild(row);
        reindexImageRows(document);

        if (window.lucide?.createIcons) {
            window.lucide.createIcons();
        }

        row.querySelector('[data-pv-color-text]')?.focus();
    });

    reindexImageRows(document);
    imageRoot.querySelectorAll('[data-pv-file-input]').forEach(syncImageFileLabel);
    imageRoot.querySelectorAll('[data-pv-image-row]').forEach(row => syncImagePreview(row));
}

function createImageRowFromTemplate(imageRoot, template) {
    const index = imageRoot.querySelectorAll('[data-pv-image-row]').length;
    const wrapper = document.createElement('div');
    wrapper.innerHTML = template.innerHTML.replaceAll('__index__', String(index)).trim();
    return wrapper.firstElementChild;
}

function expandSelectedImageFiles(fileInput, imageRoot, template) {
    const files = Array.from(fileInput.files || []);
    if (files.length <= 1) {
        return;
    }

    const currentRow = fileInput.closest('[data-pv-image-row]');
    const colorPicker = currentRow?.querySelector('[data-pv-color-picker]');
    const selectedColor = colorPicker?.value || '#111827';

    setFileInputFiles(fileInput, [files[0]]);
    syncImageColorControl(currentRow, selectedColor);

    files.slice(1).forEach(file => {
        const row = createImageRowFromTemplate(imageRoot, template);
        if (!row) {
            return;
        }

        imageRoot.appendChild(row);
        syncImageColorControl(row, selectedColor);

        const newFileInput = row.querySelector('[data-pv-file-input]');
        if (newFileInput && setFileInputFiles(newFileInput, [file])) {
            syncImageFileLabel(newFileInput);
            syncImagePreview(row, newFileInput);
        }
    });

    reindexImageRows(document);

    if (window.lucide?.createIcons) {
        window.lucide.createIcons();
    }
}

function setFileInputFiles(fileInput, files) {
    if (!fileInput || !files.length || typeof DataTransfer === 'undefined') {
        return false;
    }

    const transfer = new DataTransfer();
    files.forEach(file => transfer.items.add(file));
    fileInput.files = transfer.files;
    return true;
}

function syncImageColorControl(row, color, options = {}) {
    if (!row) {
        return;
    }

    const normalizedColor = (color || '').trim();
    const colorValue = row.querySelector('[data-pv-color-value]');
    const colorPicker = row.querySelector('[data-pv-color-picker]');
    const colorText = row.querySelector('[data-pv-color-text]');

    if (colorValue) {
        colorValue.value = normalizedColor;
    }

    if (colorText && !options.fromText) {
        colorText.value = normalizedColor ? normalizedColor.toUpperCase() : '';
    }

    if (colorPicker && isHexColor(normalizedColor)) {
        colorPicker.value = normalizedColor;
    }
}

function isHexColor(value) {
    return /^#[0-9a-fA-F]{6}$/.test((value || '').trim());
}

function syncImageFileLabel(fileInput) {
    const row = fileInput?.closest('[data-pv-image-row]');
    const label = row?.querySelector('[data-pv-file-label]');
    const fileName = fileInput?.files?.[0]?.name;

    if (fileName) {
        const colorValue = row?.querySelector('[data-pv-color-value]');
        const colorPicker = row?.querySelector('[data-pv-color-picker]');
        if (colorValue && colorPicker && colorValue.value.trim() === '') {
            syncImageColorControl(row, colorPicker.value);
        }
    }

    if (label) {
        label.textContent = fileName || 'Chọn ảnh từ máy';
    }
}

function syncImagePreview(row, fileInput = null) {
    if (!row) {
        return;
    }

    const preview = row.querySelector('[data-pv-image-preview]');
    if (!preview) {
        return;
    }

    const selectedFile = fileInput?.files?.[0] || row.querySelector('[data-pv-file-input]')?.files?.[0] || null;
    const existingPath = row.querySelector('[data-pv-image-field="ImagePath"]')?.value.trim() || '';

    if (selectedFile) {
        const previousUrl = preview.dataset.objectUrl;
        if (previousUrl) {
            URL.revokeObjectURL(previousUrl);
        }

        const objectUrl = URL.createObjectURL(selectedFile);
        preview.dataset.objectUrl = objectUrl;
        renderImagePreview(preview, objectUrl, selectedFile.name);
        return;
    }

    if (existingPath) {
        renderImagePreview(preview, existingPath, 'Ảnh biến thể hiện tại');
        return;
    }

    preview.removeAttribute('data-object-url');
    preview.classList.add('is-empty');
    preview.innerHTML = '<span class="pv-preview-icon"><i data-lucide="image-plus" class="w-5 h-5"></i></span><p>Chưa chọn ảnh</p>';

    if (window.lucide?.createIcons) {
        window.lucide.createIcons();
    }
}

function renderImagePreview(preview, src, alt) {
    preview.classList.remove('is-empty');
    preview.innerHTML = '';

    const image = document.createElement('img');
    image.src = src;
    image.alt = alt || 'Ảnh biến thể';
    preview.appendChild(image);
}

function reindexImageRows(scope) {
    const imageRoot = scope.querySelector?.('[data-pv-images]') ?? document.querySelector('[data-pv-images]');
    if (!imageRoot) {
        return;
    }

    const rows = Array.from(imageRoot.querySelectorAll('[data-pv-image-row]'));
    rows.forEach((row, index) => {
        const colorText = row.querySelector('[data-pv-color-text]');
        const colorLabel = row.querySelector('[data-pv-color-label]');

        if (colorText) {
            colorText.id = `Images_${index}__ColorText`;
        }

        if (colorLabel) {
            colorLabel.setAttribute('for', `Images_${index}__ColorText`);
        }

        row.querySelectorAll('[data-pv-image-field]').forEach(field => {
            const property = field.dataset.pvImageField;
            field.name = `Images[${index}].${property}`;

            if (field.type !== 'hidden') {
                field.id = `Images_${index}__${property}`;
                const label = field.closest('[data-pv-field]')?.querySelector('label');
                if (label) {
                    label.setAttribute('for', field.id);
                }
            }

            const suffixMap = {
                Color: 'color',
                ImagePath: 'path',
                ImageFile: 'file',
                Position: 'position',
                AltText: 'alt',
            };
            const suffix = suffixMap[property];
            if (suffix) {
                const errorId = `pv-image-${index}-${suffix}-error`;
                field.dataset.pvErrorTarget = errorId;
                const errorElement = field.closest('[data-pv-field]')?.querySelector('.pv-field-error');
                if (errorElement) {
                    errorElement.id = errorId;
                }
            }
        });
    });
}

function getAntiForgeryToken(scope) {
    return scope?.querySelector('input[name="__RequestVerificationToken"]')?.value
        ?? document.querySelector('input[name="__RequestVerificationToken"]')?.value
        ?? '';
}

function bindVariantStatusToggles() {
    document.querySelectorAll('[data-pv-toggle]').forEach(button => {
        button.addEventListener('click', () => toggleVariant(button));
    });
}

async function toggleVariant(button) {
    const id = button.dataset.pvId;
    if (!id) {
        return;
    }

    button.disabled = true;

    try {
        const response = await fetch(`/ProductVariants/ToggleActive/${encodeURIComponent(id)}`, {
            method: 'POST',
            headers: {
                RequestVerificationToken: getAntiForgeryToken(document),
                'X-Requested-With': 'XMLHttpRequest',
            },
        });

        if (!response.ok) {
            throw new Error('Toggle failed.');
        }

        await response.json();
        window.location.reload();
    } catch {
        showVariantNotice('Không thể cập nhật trạng thái biến thể. Vui lòng thử lại.', 'error');
        button.disabled = false;
    }
}

function bindVariantDefaultButtons() {
    document.querySelectorAll('[data-pv-default]').forEach(button => {
        button.addEventListener('click', () => setDefaultVariant(button));
    });
}

async function setDefaultVariant(button) {
    const id = button.dataset.pvId;
    if (!id || button.disabled) {
        return;
    }

    button.disabled = true;

    try {
        const response = await fetch(`/ProductVariants/SetDefault/${encodeURIComponent(id)}`, {
            method: 'POST',
            headers: {
                RequestVerificationToken: getAntiForgeryToken(document),
                'X-Requested-With': 'XMLHttpRequest',
            },
        });

        if (!response.ok) {
            throw new Error('Set default failed.');
        }

        await response.json();
        window.location.reload();
    } catch {
        showVariantNotice('Không thể đặt biến thể mặc định. Vui lòng thử lại.', 'error');
        button.disabled = false;
    }
}

function bindVariantDeleteConfirmation() {
    document.querySelectorAll('[data-pv-delete]').forEach(form => {
        form.addEventListener('submit', async event => {
            if (form.dataset.deleteChecked === 'true') {
                return;
            }

            event.preventDefault();

            const code = form.dataset.pvCode || 'biến thể này';
            const submitButton = form.querySelector('button[type="submit"]');
            submitButton?.setAttribute('disabled', 'disabled');

            try {
                const result = await checkVariantDelete(form);

                if (!result.canDelete) {
                    showVariantNotice(result.message || `Không thể xóa "${code}" vì còn dữ liệu liên quan.`, 'error');
                    return;
                }

                if (!window.confirm(`Bạn có chắc muốn xóa biến thể "${code}"?\nHành động này không thể hoàn tác.`)) {
                    return;
                }

                form.dataset.deleteChecked = 'true';
                if (typeof form.requestSubmit === 'function') {
                    form.requestSubmit();
                } else {
                    form.submit();
                }
            } catch {
                showVariantNotice('Không thể kiểm tra điều kiện xóa. Vui lòng thử lại.', 'error');
            } finally {
                if (form.dataset.deleteChecked !== 'true') {
                    submitButton?.removeAttribute('disabled');
                }
            }
        });
    });
}

async function checkVariantDelete(form) {
    const id = form.dataset.pvId;
    if (!id) {
        throw new Error('Missing variant id.');
    }

    const response = await fetch(`/ProductVariants/CheckDelete/${encodeURIComponent(id)}`, {
        method: 'POST',
        headers: {
            RequestVerificationToken: getAntiForgeryToken(form),
            'X-Requested-With': 'XMLHttpRequest',
        },
    });

    if (!response.ok) {
        throw new Error('Delete check failed.');
    }

    return response.json();
}

function showVariantNotice(message, type = 'success') {
    const root = document.querySelector('[data-pv-toast-root]');
    if (!root) {
        return;
    }

    const toast = document.createElement('div');
    toast.className = `pv-toast is-${type}`;

    const marker = document.createElement('span');
    marker.className = 'pv-toast-marker';

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
