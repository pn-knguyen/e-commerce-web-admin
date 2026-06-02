'use strict';

bindAssignAttributeValidation();

function bindAssignAttributeValidation() {
    const assignForm = document.querySelector('form[action$="/Assign"], form[action*="/CategoryVariantAttributes/Assign"]')
        ?? Array.from(document.querySelectorAll('form')).find(form => form.querySelector('[name="AttributeId"]'));

    if (!assignForm) {
        return;
    }

    const alertBox = assignForm.closest('.surface-form-card')?.querySelector('[data-surface-form-alert]')
        ?? assignForm.querySelector('[data-surface-form-alert]');
    let hasSubmitted = !alertBox?.classList.contains('hidden');
    const setAlertVisible = isVisible => {
        alertBox?.classList.toggle('hidden', !isVisible);
    };

    const attributeSelect = assignForm.querySelector('[name="AttributeId"]');
    if (!attributeSelect) {
        return;
    }

    const validateSelect = showError => {
    const message = attributeSelect.value ? '' : 'Vui lòng chọn thuộc tính cần gán.';

        if (showError) {
            setInlineError(attributeSelect, message);
        }

        return message === '';
    };

    attributeSelect.addEventListener('change', () => {
        validateSelect(true);
        if (hasSubmitted) {
            setAlertVisible(!validateSelect(false));
        }
    });

    assignForm.addEventListener('submit', event => {
        hasSubmitted = true;
        const isValid = validateSelect(true);
        setAlertVisible(!isValid);

        if (!isValid) {
            event.preventDefault();
            attributeSelect.focus();
        }
    });
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

document.addEventListener('submit', e => {
    const form = e.target.closest('[data-cva-remove]');
    if (!form) return;

    const name = form.dataset.attrName || 'thuộc tính này';
    const categoryName = form.dataset.categoryName || 'danh mục này';
    const usage = Number.parseInt(form.dataset.usage || '0', 10);

    if (usage > 0) {
        e.preventDefault();
        alert(`Không thể bỏ gán "${name}" khỏi "${categoryName}" vì đang được dùng bởi ${usage} biến thể sản phẩm.`);
        return;
    }

    if (!confirm(`Bỏ gán thuộc tính "${name}" khỏi danh mục "${categoryName}"?\n\nHành động này không thể hoàn tác.`)) {
        e.preventDefault();
    }
});
