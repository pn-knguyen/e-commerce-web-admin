'use strict';

document.addEventListener('DOMContentLoaded', () => {
    bindToastDismiss();
    bindDeleteForms();
    bindStatusToggles();
    bindGhnAddressPickers();
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

function bindDeleteForms() {
    document.querySelectorAll('[data-location-delete]').forEach(form => {
        form.addEventListener('submit', event => {
            const name = form.dataset.locationName || 'diem lay hang nay';
            const isBlocked = form.dataset.deleteBlocked === 'true';
            const message = isBlocked
                ? `"${name}" da co van don. He thong se khong xoa duoc, ban nen tam tat thay vi xoa. Tiep tuc gui yeu cau xoa?`
                : `Xoa diem lay hang "${name}"?`;

            if (!window.confirm(message)) {
                event.preventDefault();
            }
        });
    });
}

function bindStatusToggles() {
    document.querySelectorAll('[data-location-toggle]').forEach(button => {
        button.addEventListener('click', async () => {
            const id = button.dataset.locationId;
            if (!id || button.disabled) {
                return;
            }

            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
            button.disabled = true;

            try {
                const response = await fetch(`/FulfillmentLocations/ToggleActive/${id}`, {
                    method: 'POST',
                    headers: {
                        RequestVerificationToken: token,
                    },
                });

                if (!response.ok) {
                    throw new Error('toggle failed');
                }

                window.location.reload();
            } catch {
                window.alert('Khong cap nhat duoc trang thai diem lay hang.');
                button.disabled = false;
            }
        });
    });
}

function bindGhnAddressPickers() {
    document.querySelectorAll('[data-ghn-address-picker]').forEach(picker => {
        const provinceSelect = picker.querySelector('[data-ghn-province-select]');
        const districtSelect = picker.querySelector('[data-ghn-district-select]');
        const wardSelect = picker.querySelector('[data-ghn-ward-select]');
        const status = picker.querySelector('[data-ghn-address-status]');
        const districtCodeLabel = picker.querySelector('[data-ghn-district-code-label]');
        const wardCodeLabel = picker.querySelector('[data-ghn-ward-code-label]');

        const provinceCodeInput = document.querySelector('[data-ghn-province-code]');
        const provinceNameInput = document.querySelector('[data-ghn-province-name]');
        const districtCodeInput = document.querySelector('[data-ghn-district-code]');
        const districtNameInput = document.querySelector('[data-ghn-district-name]');
        const wardCodeInput = document.querySelector('[data-ghn-ward-code]');
        const wardNameInput = document.querySelector('[data-ghn-ward-name]');

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

        const setStatus = (message, isError = false) => {
            if (!status) {
                return;
            }

            status.textContent = message || '';
            status.classList.toggle('is-error', isError);
        };

        const setCodeLabels = () => {
            if (districtCodeLabel) {
                districtCodeLabel.textContent = districtCodeInput.value
                    ? `Mã quận/huyện GHN: ${districtCodeInput.value}`
                    : '';
            }

            if (wardCodeLabel) {
                wardCodeLabel.textContent = wardCodeInput.value
                    ? `Mã phường/xã GHN: ${wardCodeInput.value}`
                    : '';
            }
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
                setOptions(
                    provinceSelect,
                    provinces,
                    'Chọn tỉnh/thành',
                    item => item.id,
                    item => item.name,
                    selectedProvince);
                provinceSelect.disabled = false;

                if (provinceSelect.value) {
                    const selected = provinceSelect.options[provinceSelect.selectedIndex];
                    provinceCodeInput.value = selected.value;
                    provinceNameInput.value = selected.dataset.name || selected.textContent || '';
                    await loadDistricts(provinceSelect.value, initial.districtCode);
                }

                setStatus('');
            } catch (error) {
                setOptions(provinceSelect, [], 'Không tải được tỉnh/thành');
                setStatus(error.message || 'Không tải được danh sách tỉnh/thành GHN.', true);
            }
        };

        const loadDistricts = async (provinceId, selectedDistrictId = '') => {
            clearDistrict();
            clearWard();

            if (!provinceId) {
                setCodeLabels();
                return;
            }

            districtSelect.disabled = true;
            setOptions(districtSelect, [], 'Đang tải quận/huyện...');

            try {
                const payload = await fetchJson(`/FulfillmentLocations/GhnDistricts?provinceId=${encodeURIComponent(provinceId)}&purpose=pickup`);
                const districts = payload.items || [];
                const selectedDistrict = findSelectedValue(
                    districts,
                    selectedDistrictId,
                    initial.districtName,
                    item => item.id,
                    item => item.name);
                setOptions(
                    districtSelect,
                    districts,
                    'Chọn quận/huyện',
                    item => item.id,
                    item => item.name,
                    selectedDistrict);
                districtSelect.disabled = false;

                if (districtSelect.value) {
                    const selected = districtSelect.options[districtSelect.selectedIndex];
                    districtCodeInput.value = selected.value;
                    districtNameInput.value = selected.dataset.name || selected.textContent || '';
                    await loadWards(districtSelect.value, initial.wardCode);
                }

                setStatus('');
            } catch (error) {
                setOptions(districtSelect, [], 'Không tải được quận/huyện');
                setStatus(error.message || 'Không tải được danh sách quận/huyện GHN.', true);
            } finally {
                setCodeLabels();
            }
        };

        const loadWards = async (districtId, selectedWardCode = '') => {
            clearWard();

            if (!districtId) {
                setCodeLabels();
                return;
            }

            wardSelect.disabled = true;
            setOptions(wardSelect, [], 'Đang tải phường/xã...');

            try {
                const payload = await fetchJson(`/FulfillmentLocations/GhnWards?districtId=${encodeURIComponent(districtId)}&purpose=pickup`);
                const wards = payload.items || [];
                const selectedWard = findSelectedValue(
                    wards,
                    selectedWardCode,
                    initial.wardName,
                    item => item.code,
                    item => item.name);
                setOptions(
                    wardSelect,
                    wards,
                    'Chọn phường/xã',
                    item => item.code,
                    item => item.name,
                    selectedWard);
                wardSelect.disabled = false;

                if (wardSelect.value) {
                    const selected = wardSelect.options[wardSelect.selectedIndex];
                    wardCodeInput.value = selected.value;
                    wardNameInput.value = selected.dataset.name || selected.textContent || '';
                }

                setStatus('');
            } catch (error) {
                setOptions(wardSelect, [], 'Không tải được phường/xã');
                setStatus(error.message || 'Không tải được danh sách phường/xã GHN.', true);
            } finally {
                setCodeLabels();
            }
        };

        provinceSelect.addEventListener('change', async () => {
            const selected = provinceSelect.options[provinceSelect.selectedIndex];
            provinceCodeInput.value = selected?.value || '';
            provinceNameInput.value = selected?.dataset.name || selected?.textContent || '';
            initial.districtCode = '';
            initial.wardCode = '';
            await loadDistricts(provinceCodeInput.value);
            setCodeLabels();
        });

        districtSelect.addEventListener('change', async () => {
            const selected = districtSelect.options[districtSelect.selectedIndex];
            districtCodeInput.value = selected?.value || '';
            districtNameInput.value = selected?.dataset.name || selected?.textContent || '';
            initial.wardCode = '';
            await loadWards(districtCodeInput.value);
            setCodeLabels();
        });

        wardSelect.addEventListener('change', () => {
            const selected = wardSelect.options[wardSelect.selectedIndex];
            wardCodeInput.value = selected?.value || '';
            wardNameInput.value = selected?.dataset.name || selected?.textContent || '';
            setCodeLabels();
        });

        loadProvinces();
        setCodeLabels();
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
        if (item.status !== undefined && item.status !== null) {
            option.dataset.status = String(item.status);
        }
        if (item.supportType !== undefined && item.supportType !== null) {
            option.dataset.supportType = String(item.supportType);
        }

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
        throw new Error(payload?.message || 'Không tải được dữ liệu GHN.');
    }

    return payload || {};
}
