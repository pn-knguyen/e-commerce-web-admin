'use strict';

document.addEventListener('DOMContentLoaded', () => {
    bindToastDismiss();
    bindOrderStatusForm();
    bindShipmentHistoryToggle();
    bindShippingProviderSwitcher();
    bindShipmentProviderAddressPickers();
    bindShipmentQuoteForms();
    bindConfirmForms();
});

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

function bindShipmentHistoryToggle() {
    document.querySelectorAll('[data-shipment-history-more]').forEach(button => {
        const historyBox = button.closest('.order-shipment-history');
        if (!historyBox) {
            return;
        }

        button.addEventListener('click', () => {
            historyBox.querySelectorAll('[data-shipment-history-extra="true"][hidden]').forEach(item => {
                item.hidden = false;
            });
            button.remove();
        });
    });
}

function bindOrderStatusForm() {
    const form = document.querySelector('[data-order-status-form]');
    if (!form) {
        return;
    }

    form.setAttribute('novalidate', 'novalidate');

    const alertBox = form.querySelector('[data-order-form-alert]');
    const orderSelect = form.querySelector('[data-order-status-select]');
    const paymentSelect = form.querySelector('[data-payment-status-select]');
    const currentOrderStatus = form.dataset.currentOrderStatus || '';
    const currentPaymentStatus = form.dataset.currentPaymentStatus || '';
    const currentShipmentStatus = form.dataset.currentShipmentStatus || '';
    const isCodPayment = form.dataset.isCodPayment === 'true';
    let hasSubmitted = !alertBox?.classList.contains('hidden');

    const setAlertVisible = isVisible => {
        alertBox?.classList.toggle('hidden', !isVisible);
    };

    const labels = {
        order: {
            Pending: 'Chờ xác nhận',
            Confirmed: 'Đã xác nhận',
            Processing: 'Đang xử lý',
            Shipping: 'Đang giao',
            Completed: 'Đã giao',
            Cancelled: 'Đã hủy',
            Returned: 'Đã hoàn hàng',
        },
        payment: {
            Unpaid: 'Chưa thanh toán',
            Paid: 'Đã thanh toán',
            Failed: 'Thanh toán lỗi',
            Refunded: 'Đã hoàn tiền',
        },
    };

    const allowedOrderTransitions = {
        Pending: ['Pending', 'Confirmed', 'Cancelled'],
        Confirmed: ['Confirmed', 'Processing', 'Cancelled'],
        Processing: ['Processing', 'Shipping', 'Cancelled'],
        Shipping: ['Shipping', 'Completed', 'Returned'],
        Completed: ['Completed', 'Returned'],
        Cancelled: ['Cancelled'],
        Returned: ['Returned'],
    };

    const allowedPaymentTransitions = {
        Unpaid: ['Unpaid', 'Paid', 'Failed'],
        Failed: ['Failed', 'Unpaid', 'Paid'],
        Paid: ['Paid', 'Refunded'],
        Refunded: ['Refunded'],
    };

    const getLabel = (group, value) => labels[group]?.[value] || 'Không xác định';

    const getShipmentLabel = value => ({
        Draft: 'Nháp',
        Quoted: 'Đã báo giá',
        Booking: 'Đang tạo vận đơn',
        Booked: 'Chờ lấy hàng',
        ReadyToPick: 'Mới tạo đơn',
        PickingUp: 'Đang lấy hàng',
        Picking: 'Nhân viên đang lấy hàng',
        MoneyCollectPicking: 'Đang thu tiền người gửi',
        Picked: 'Đã lấy hàng',
        InTransit: 'Đang vận chuyển',
        Storing: 'Hàng đang ở kho',
        Transporting: 'Đang luân chuyển',
        Sorting: 'Đang phân loại',
        Delivering: 'Đang giao cho khách',
        MoneyCollectDelivering: 'Đang thu tiền người nhận',
        Delivered: 'Đã giao',
        Cancelled: 'Đã hủy',
        Failed: 'Lỗi giao hàng',
        DeliveryFail: 'Giao hàng thất bại',
        WaitingToReturn: 'Chờ hoàn hàng',
        Return: 'Đang chờ trả hàng',
        ReturnTransporting: 'Đang luân chuyển hàng hoàn',
        ReturnSorting: 'Đang phân loại hàng hoàn',
        Returning: 'Đang trả hàng',
        ReturnFail: 'Trả hàng thất bại',
        Returned: 'Đã hoàn hàng',
        Exception: 'Đơn ngoại lệ',
        Damage: 'Hàng hư hỏng',
        Lost: 'Hàng thất lạc',
        ProviderUnknown: 'Trạng thái GHN chưa xác định',
    })[value] || 'Không xác định';

    const getMessageElement = fieldName => Array.from(form.querySelectorAll('[data-valmsg-for]'))
        .find(element => element.dataset.valmsgFor === fieldName) ?? null;

    const setFieldError = (field, fieldName, message) => {
        const hasError = Boolean(message);
        const messageElement = getMessageElement(fieldName);

        field?.setAttribute('aria-invalid', hasError ? 'true' : 'false');
        field?.classList.toggle('input-validation-error', hasError);

        if (messageElement) {
            messageElement.textContent = message;
            messageElement.classList.toggle('field-validation-error', hasError);
            messageElement.classList.toggle('field-validation-valid', !hasError);
        }
    };

    const canChangeOrderStatus = (current, next) =>
        (allowedOrderTransitions[current] || []).includes(next);

    const canChangePaymentStatus = (current, next) =>
        (allowedPaymentTransitions[current] || []).includes(next);

    const getValidationMessages = () => {
        const nextOrderStatus = orderSelect?.value || '';
        const nextPaymentStatus = paymentSelect?.value || '';
        let orderMessage = '';
        let paymentMessage = '';

        if (!canChangeOrderStatus(currentOrderStatus, nextOrderStatus)) {
            orderMessage = `Không thể chuyển đơn từ "${getLabel('order', currentOrderStatus)}" sang "${getLabel('order', nextOrderStatus)}".`;
        }

        if (!canChangePaymentStatus(currentPaymentStatus, nextPaymentStatus)) {
            paymentMessage = `Không thể chuyển thanh toán từ "${getLabel('payment', currentPaymentStatus)}" sang "${getLabel('payment', nextPaymentStatus)}".`;
        }

        if (nextOrderStatus === 'Completed' &&
            currentShipmentStatus &&
            currentShipmentStatus !== 'Delivered') {
            orderMessage = `Chưa thể chuyển đơn sang đã giao vì vận đơn đang ở trạng thái "${getShipmentLabel(currentShipmentStatus)}".`;
        }

        if (nextPaymentStatus === 'Refunded' && !['Cancelled', 'Returned'].includes(nextOrderStatus)) {
            paymentMessage = 'Chỉ hoàn tiền cho đơn đã hủy hoặc đã trả hàng.';
        }

        if (['Cancelled', 'Returned'].includes(nextOrderStatus) && nextPaymentStatus === 'Paid') {
            paymentMessage = 'Đơn đã hủy hoặc trả hàng không thể giữ trạng thái đã thanh toán.';
        }

        if (['Cancelled', 'Returned'].includes(nextOrderStatus) &&
            currentPaymentStatus === 'Paid' &&
            nextPaymentStatus !== 'Refunded') {
            paymentMessage = 'Đơn đã thanh toán khi hủy hoặc trả hàng phải chuyển sang đã hoàn tiền.';
        }

        return { orderMessage, paymentMessage };
    };

    const validateForm = showErrors => {
        const { orderMessage, paymentMessage } = getValidationMessages();
        const isValid = !orderMessage && !paymentMessage;

        if (showErrors) {
            setFieldError(orderSelect, 'OrderStatus', orderMessage);
            setFieldError(paymentSelect, 'PaymentStatus', paymentMessage);
            setAlertVisible(!isValid);
        }

        return {
            isValid,
            firstInvalid: orderMessage ? orderSelect : paymentMessage ? paymentSelect : null,
        };
    };

    const syncPaymentWithOrderStatus = () => {
        if (orderSelect?.value === 'Completed' &&
            isCodPayment &&
            currentPaymentStatus !== 'Refunded' &&
            paymentSelect?.value !== 'Paid') {
            paymentSelect.value = 'Paid';
        }
    };

    orderSelect?.addEventListener('change', syncPaymentWithOrderStatus);

    [orderSelect, paymentSelect].forEach(field => {
        field?.addEventListener('change', () => {
            if (hasSubmitted) {
                validateForm(true);
            }
        });
    });

    form.addEventListener('submit', event => {
        hasSubmitted = true;

        const result = validateForm(true);
        if (!result.isValid) {
            event.preventDefault();
            result.firstInvalid?.focus();
        }
    });
}

function bindShippingProviderSwitcher() {
    document.querySelectorAll('[data-shipping-provider-shell]').forEach(shell => {
        const select = shell.querySelector('[data-shipping-provider-select]');
        const emptyText = shell.querySelector('[data-shipping-provider-empty]');
        const body = shell.closest('.order-shipping-body') || document;
        const panels = Array.from(body.querySelectorAll('[data-shipping-provider-panel]') || []);
        const backdrop = getShippingModalBackdrop();
        let wasModalOpen = false;

        if (!select || panels.length === 0) {
            return;
        }

        const closeProvider = () => {
            select.value = '';
            syncProvider();
        };

        const syncProvider = () => {
            const selectedProvider = select.value;
            const hasSelectedModalForm = panels.some(panel =>
                panel.dataset.shippingProviderPanel === selectedProvider &&
                panel.hasAttribute('data-shipping-modal-form'));
            let modalForm = null;

            panels.forEach(panel => {
                const isModalForm = panel.hasAttribute('data-shipping-modal-form');
                const isSelected = panel.dataset.shippingProviderPanel === selectedProvider &&
                    (!hasSelectedModalForm || isModalForm);

                if (isSelected && isModalForm) {
                    modalForm = panel;
                }

                panel.classList.toggle('is-hidden', !isSelected);
                panel.toggleAttribute('hidden', !isSelected);
            });

            const isModalOpen = Boolean(modalForm);
            backdrop.hidden = !isModalOpen;
            backdrop.classList.toggle('is-hidden', !isModalOpen);
            document.body.classList.toggle('order-modal-open', isModalOpen);

            if (emptyText) {
                emptyText.hidden = Boolean(selectedProvider);
            }

            if (isModalOpen && !wasModalOpen) {
                requestAnimationFrame(() => {
                    modalForm.querySelector('select, input:not([type="hidden"]), textarea, button')?.focus();
                });
            }

            wasModalOpen = isModalOpen;
        };

        select.addEventListener('change', syncProvider);
        panels.forEach(panel => {
            panel.querySelectorAll('[data-close-shipping-modal]').forEach(button => {
                button.addEventListener('click', closeProvider);
            });
        });
        backdrop.addEventListener('click', closeProvider);
        document.addEventListener('keydown', event => {
            if (event.key === 'Escape' && !backdrop.hidden) {
                closeProvider();
            }
        });
        syncProvider();
    });
}

function getShippingModalBackdrop() {
    let backdrop = document.querySelector('[data-shipping-modal-backdrop]');
    if (!backdrop) {
        backdrop = document.createElement('div');
        backdrop.className = 'order-modal-screen is-hidden';
        backdrop.setAttribute('data-shipping-modal-backdrop', '');
        backdrop.hidden = true;
        document.body.appendChild(backdrop);
    }

    return backdrop;
}

function bindShipmentQuoteForms() {
    document.querySelectorAll('[data-shipment-quote-form]').forEach(form => {
        form.setAttribute('novalidate', 'novalidate');

        const getControl = fieldName => form.elements[fieldName] || null;
        const getMessageElement = fieldName => Array.from(form.querySelectorAll('[data-valmsg-for]'))
            .find(element => element.dataset.valmsgFor === fieldName) ?? null;

        const providerControls = {
            ProviderDropoffProvinceCode: form.querySelector('[data-order-provider-province-select]'),
            ProviderDropoffDistrictCode: form.querySelector('[data-order-provider-district-select]'),
            ProviderDropoffWardCode: form.querySelector('[data-order-provider-ward-select]'),
        };

        const setFieldError = (fieldName, message, control = null) => {
            const hasError = Boolean(message);
            const target = control || getControl(fieldName);
            const messageElement = getMessageElement(fieldName);

            target?.setAttribute('aria-invalid', hasError ? 'true' : 'false');
            target?.classList.toggle('input-validation-error', hasError);

            if (messageElement) {
                messageElement.textContent = message || '';
                messageElement.classList.toggle('field-validation-error', hasError);
                messageElement.classList.toggle('field-validation-valid', !hasError);
            }
        };

        const parseNumber = value => {
            const text = String(value || '').trim().replace(',', '.');
            if (!text) {
                return null;
            }

            const number = Number(text);
            return Number.isFinite(number) ? number : Number.NaN;
        };

        const validateText = (fieldName, label, maxLength) => {
            const field = getControl(fieldName);
            const value = String(field?.value || '').trim();
            if (!value) {
                return `${label} là bắt buộc.`;
            }

            if (value.length > maxLength) {
                return `${label} tối đa ${maxLength} ký tự.`;
            }

            return '';
        };

        const validateInteger = (fieldName, label, min, max) => {
            const field = getControl(fieldName);
            const value = parseNumber(field?.value);
            if (value === null) {
                return `${label} là bắt buộc.`;
            }

            if (!Number.isInteger(value) || value < min || value > max) {
                return `${label} phải từ ${min} đến ${max}.`;
            }

            return '';
        };

        const validateDecimal = (fieldName, label, min, max) => {
            const field = getControl(fieldName);
            const value = parseNumber(field?.value);
            if (value === null) {
                return `${label} là bắt buộc.`;
            }

            if (Number.isNaN(value) || value < min || value > max) {
                return `${label} phải từ ${min} đến ${max}.`;
            }

            return '';
        };

        const validateOptionalNonNegative = (fieldName, label) => {
            const field = getControl(fieldName);
            const value = parseNumber(field?.value);
            if (value === null) {
                return '';
            }

            if (Number.isNaN(value)) {
                return `${label} phải là số hợp lệ.`;
            }

            return value < 0 ? `${label} không được là số âm.` : '';
        };

        const validateRequiredHidden = (fieldName, label) => {
            const field = getControl(fieldName);
            return String(field?.value || '').trim() ? '' : `Vui lòng chọn ${label}.`;
        };

        const validateForm = showErrors => {
            const checks = [
                ['FulfillmentLocationId', validateRequiredHidden('FulfillmentLocationId', 'điểm lấy hàng')],
                ['ProviderDropoffProvinceCode', validateRequiredHidden('ProviderDropoffProvinceCode', 'tỉnh/thành giao đến'), providerControls.ProviderDropoffProvinceCode],
                ['ProviderDropoffDistrictCode', validateRequiredHidden('ProviderDropoffDistrictCode', 'quận/huyện giao đến'), providerControls.ProviderDropoffDistrictCode],
                ['ProviderDropoffWardCode', validateRequiredHidden('ProviderDropoffWardCode', 'phường/xã giao đến'), providerControls.ProviderDropoffWardCode],
                ['PackageDescription', validateText('PackageDescription', 'Mô tả kiện hàng', 500)],
                ['Quantity', validateInteger('Quantity', 'Số kiện', 1, 999)],
                ['WeightGrams', validateInteger('WeightGrams', 'Cân nặng', 1, 30000)],
                ['LengthCm', validateDecimal('LengthCm', 'Chiều dài', 1, 150)],
                ['WidthCm', validateDecimal('WidthCm', 'Chiều rộng', 1, 150)],
                ['HeightCm', validateDecimal('HeightCm', 'Chiều cao', 1, 150)],
                ['DeclaredValue', validateOptionalNonNegative('DeclaredValue', 'Giá trị khai báo')],
                ['Notes', String(getControl('Notes')?.value || '').length > 1000 ? 'Ghi chú tối đa 1000 ký tự.' : ''],
            ];

            const firstInvalid = checks.find(([, message]) => Boolean(message));
            if (showErrors) {
                checks.forEach(([fieldName, message, control]) => {
                    setFieldError(fieldName, message, control);
                });
            }

            return {
                isValid: !firstInvalid,
                firstInvalid: firstInvalid
                    ? (firstInvalid[2] || getControl(firstInvalid[0]))
                    : null,
            };
        };

        let hasSubmitted = false;
        form.addEventListener('submit', event => {
            hasSubmitted = true;
            const result = validateForm(true);
            if (!result.isValid) {
                event.preventDefault();
                result.firstInvalid?.focus();
                result.firstInvalid?.scrollIntoView({ behavior: 'smooth', block: 'center' });
            }
        });

        form.querySelectorAll('input, select, textarea').forEach(field => {
            field.addEventListener('input', () => {
                if (hasSubmitted) {
                    validateForm(true);
                }
            });
            field.addEventListener('change', () => {
                if (hasSubmitted) {
                    validateForm(true);
                }
            });
        });
    });
}

function bindConfirmForms() {
    document.querySelectorAll('form[data-confirm]').forEach(form => {
        form.addEventListener('submit', event => {
            const message = form.dataset.confirm || 'Xac nhan thao tac nay?';
            if (!window.confirm(message)) {
                event.preventDefault();
            }
        });
    });
}

function bindShipmentProviderAddressPickers() {
    document.querySelectorAll('[data-order-provider-address]').forEach(picker => {
        const provinceSelect = picker.querySelector('[data-order-provider-province-select]');
        const districtSelect = picker.querySelector('[data-order-provider-district-select]');
        const wardSelect = picker.querySelector('[data-order-provider-ward-select]');
        const provinceCodeInput = document.querySelector('[data-order-provider-province-code]');
        const provinceNameInput = document.querySelector('[data-order-provider-province-name]');
        const districtCodeInput = document.querySelector('[data-order-provider-district-code]');
        const districtNameInput = document.querySelector('[data-order-provider-district-name]');
        const wardCodeInput = document.querySelector('[data-order-provider-ward-code]');
        const wardNameInput = document.querySelector('[data-order-provider-ward-name]');

        if (!provinceSelect || !districtSelect || !wardSelect ||
            !provinceCodeInput || !provinceNameInput ||
            !districtCodeInput || !districtNameInput ||
            !wardCodeInput || !wardNameInput) {
            return;
        }

        const initial = {
            provinceCode: picker.dataset.provinceCode || provinceCodeInput.value || '',
            provinceName: picker.dataset.provinceName || provinceNameInput.value || '',
            districtCode: picker.dataset.districtCode || districtCodeInput.value || '',
            districtName: picker.dataset.districtName || districtNameInput.value || '',
            wardCode: picker.dataset.wardCode || wardCodeInput.value || '',
            wardName: picker.dataset.wardName || wardNameInput.value || '',
        };

        const clearDistrict = () => {
            districtCodeInput.value = '';
            districtNameInput.value = '';
            setOptions(districtSelect, [], 'Chọn tỉnh/thành trước');
            districtSelect.disabled = true;
        };

        const clearWard = () => {
            wardCodeInput.value = '';
            wardNameInput.value = '';
            setOptions(wardSelect, [], 'Chọn quận/huyện trước');
            wardSelect.disabled = true;
        };

        const loadProvinces = async () => {
            provinceSelect.disabled = true;
            setOptions(provinceSelect, [], 'Đang tải tỉnh/thành...');

            try {
                const payload = await fetchJson('/FulfillmentLocations/GhnProvinces');
                const provinces = payload.items || [];
                const selectedProvince = findSelectedValue(
                    provinces,
                    initial.provinceCode,
                    initial.provinceName,
                    item => item.id,
                    item => item.name);

                setOptions(provinceSelect, provinces, 'Chọn tỉnh/thành', item => item.id, item => item.name, selectedProvince);
                provinceSelect.disabled = false;

                if (provinceSelect.value) {
                    const selected = provinceSelect.options[provinceSelect.selectedIndex];
                    provinceCodeInput.value = selected.value;
                    provinceNameInput.value = selected.dataset.name || selected.textContent || '';
                    await loadDistricts(provinceSelect.value, initial.districtCode);
                }
            } catch {
                setOptions(provinceSelect, [], 'Không tải được tỉnh/thành');
            }
        };

        const loadDistricts = async (provinceId, selectedDistrictId = '') => {
            clearDistrict();
            clearWard();

            if (!provinceId) {
                return;
            }

            districtSelect.disabled = true;
            setOptions(districtSelect, [], 'Đang tải quận/huyện...');

            try {
                const payload = await fetchJson(`/FulfillmentLocations/GhnDistricts?provinceId=${encodeURIComponent(provinceId)}&purpose=delivery`);
                const districts = payload.items || [];
                const selectedDistrict = findSelectedValue(
                    districts,
                    selectedDistrictId,
                    initial.districtName,
                    item => item.id,
                    item => item.name);

                setOptions(districtSelect, districts, 'Chọn quận/huyện', item => item.id, item => item.name, selectedDistrict);
                districtSelect.disabled = false;

                if (districtSelect.value) {
                    const selected = districtSelect.options[districtSelect.selectedIndex];
                    districtCodeInput.value = selected.value;
                    districtNameInput.value = selected.dataset.name || selected.textContent || '';
                    await loadWards(districtSelect.value, initial.wardCode);
                }
            } catch {
                setOptions(districtSelect, [], 'Không tải được quận/huyện');
            }
        };

        const loadWards = async (districtId, selectedWardCode = '') => {
            clearWard();

            if (!districtId) {
                return;
            }

            wardSelect.disabled = true;
            setOptions(wardSelect, [], 'Đang tải phường/xã...');

            try {
                const payload = await fetchJson(`/FulfillmentLocations/GhnWards?districtId=${encodeURIComponent(districtId)}&purpose=delivery`);
                const wards = payload.items || [];
                const selectedWard = findSelectedValue(
                    wards,
                    selectedWardCode,
                    initial.wardName,
                    item => item.code,
                    item => item.name);

                setOptions(wardSelect, wards, 'Chọn phường/xã', item => item.code, item => item.name, selectedWard);
                wardSelect.disabled = false;

                if (wardSelect.value) {
                    const selected = wardSelect.options[wardSelect.selectedIndex];
                    wardCodeInput.value = selected.value;
                    wardNameInput.value = selected.dataset.name || selected.textContent || '';
                }
            } catch {
                setOptions(wardSelect, [], 'Không tải được phường/xã');
            }
        };

        provinceSelect.addEventListener('change', async () => {
            const selected = provinceSelect.options[provinceSelect.selectedIndex];
            provinceCodeInput.value = selected?.value || '';
            provinceNameInput.value = selected?.dataset.name || selected?.textContent || '';
            initial.districtCode = '';
            initial.wardCode = '';
            await loadDistricts(provinceCodeInput.value);
        });

        districtSelect.addEventListener('change', async () => {
            const selected = districtSelect.options[districtSelect.selectedIndex];
            districtCodeInput.value = selected?.value || '';
            districtNameInput.value = selected?.dataset.name || selected?.textContent || '';
            initial.wardCode = '';
            await loadWards(districtCodeInput.value);
        });

        wardSelect.addEventListener('change', () => {
            const selected = wardSelect.options[wardSelect.selectedIndex];
            wardCodeInput.value = selected?.value || '';
            wardNameInput.value = selected?.dataset.name || selected?.textContent || '';
        });

        loadProvinces();
    });
}

function setOptions(select, items, placeholder, getValue, getText, selectedValue = '') {
    select.replaceChildren();
    select.append(new Option(placeholder, ''));

    items.forEach(item => {
        const value = String(getValue ? getValue(item) : item.value);
        const text = String(getText ? getText(item) : item.text);
        const option = new Option(text, value);
        option.dataset.name = text;
        if (selectedValue && value === String(selectedValue)) {
            option.selected = true;
        }

        select.append(option);
    });
}

function findSelectedValue(items, currentValue, currentName, getValue, getText) {
    if (currentValue) {
        const byValue = items.find(item => String(getValue(item)) === String(currentValue));
        if (byValue) {
            return String(getValue(byValue));
        }
    }

    if (currentName) {
        const normalizedName = normalizeText(currentName);
        const byName = items.find(item => normalizeText(getText(item)) === normalizedName) ||
            items.find(item => {
                const optionName = normalizeText(getText(item));
                return optionName.includes(normalizedName) || normalizedName.includes(optionName);
            });
        if (byName) {
            return String(getValue(byName));
        }
    }

    return '';
}

function normalizeText(value) {
    return String(value || '')
        .normalize('NFD')
        .replace(/[\u0300-\u036f]/g, '')
        .replace(/\s+/g, ' ')
        .trim()
        .toLowerCase();
}

async function fetchJson(url) {
    const response = await fetch(url, {
        headers: {
            Accept: 'application/json',
        },
    });

    let payload = null;
    try {
        payload = await response.json();
    } catch {
        payload = null;
    }

    if (!response.ok) {
        throw new Error(payload?.message || 'Không tải được dữ liệu địa chỉ.');
    }

    return payload || {};
}
