'use strict';

// ═══════════════════════════════════════════════════════════════
//  Categories JS — slug auto-gen, image preview, toggle active
// ═══════════════════════════════════════════════════════════════

// ── Slug generator ──────────────────────────────────────────────
const VI_MAP = {
    à:'a',á:'a',ả:'a',ã:'a',ạ:'a',
    ă:'a',ắ:'a',ằ:'a',ẳ:'a',ẵ:'a',ặ:'a',
    â:'a',ấ:'a',ầ:'a',ẩ:'a',ẫ:'a',ậ:'a',
    đ:'d',
    è:'e',é:'e',ẻ:'e',ẽ:'e',ẹ:'e',
    ê:'e',ế:'e',ề:'e',ể:'e',ễ:'e',ệ:'e',
    ì:'i',í:'i',ỉ:'i',ĩ:'i',ị:'i',
    ò:'o',ó:'o',ỏ:'o',õ:'o',ọ:'o',
    ô:'o',ố:'o',ồ:'o',ổ:'o',ỗ:'o',ộ:'o',
    ơ:'o',ớ:'o',ờ:'o',ở:'o',ỡ:'o',ợ:'o',
    ù:'u',ú:'u',ủ:'u',ũ:'u',ụ:'u',
    ư:'u',ứ:'u',ừ:'u',ử:'u',ữ:'u',ự:'u',
    ỳ:'y',ý:'y',ỷ:'y',ỹ:'y',ỵ:'y',
};

function toSlug(text) {
    return text
        .toLowerCase()
        .split('').map(c => VI_MAP[c] ?? c).join('')
        .replace(/[^a-z0-9\s-]/g, '')
        .replace(/\s+/g, '-')
        .replace(/-+/g, '-')
        .replace(/^-+|-+$/g, '');
}

// ── Init on DOM ready ────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', () => {

    // --- Auto slug from name ---
    const nameInput  = document.getElementById('categoryName');
    const slugInput  = document.getElementById('categorySlug');
    let slugEdited   = slugInput && slugInput.value.trim() !== '';  // đã có giá trị → không ghi đè

    if (nameInput && slugInput) {
        nameInput.addEventListener('input', () => {
            if (!slugEdited) {
                slugInput.value = toSlug(nameInput.value);
            }
        });

        // Khi người dùng tự sửa slug → ngừng auto
        slugInput.addEventListener('input', () => {
            slugEdited = slugInput.value.trim() !== '';
        });

        // Khi xoá slug trống → auto lại
        slugInput.addEventListener('blur', () => {
            if (slugInput.value.trim() === '') {
                slugEdited = false;
                slugInput.value = toSlug(nameInput.value);
            }
        });
    }

    // --- Image preview ---
    const imgInput   = document.getElementById('imagePathInput');
    const previewBox = document.getElementById('imagePreview');
    const previewImg = document.getElementById('previewImg');

    if (imgInput && previewBox && previewImg) {
        let debounce;
        imgInput.addEventListener('input', () => {
            clearTimeout(debounce);
            debounce = setTimeout(() => {
                const url = imgInput.value.trim();
                if (url) {
                    previewImg.src = url;
                    previewBox.classList.remove('hidden');
                } else {
                    previewBox.classList.add('hidden');
                }
            }, 400);
        });

        previewImg.addEventListener('error', () => {
            previewBox.classList.add('hidden');
        });
    }

    // --- Auto-dismiss toast after 4s ---
    ['toastSuccess', 'toastError'].forEach(id => {
        const el = document.getElementById(id);
        if (el) setTimeout(() => el.remove(), 4000);
    });
});

// ── Toggle Active (inline, no page reload) ──────────────────────
async function toggleActive(id, btn) {
    btn.disabled = true;
    try {
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value
            ?? document.cookie.match(/XSRF-TOKEN=([^;]+)/)?.[1]
            ?? '';

        const res = await fetch(`/Categories/ToggleActive/${id}`, {
            method : 'POST',
            headers: {
                'RequestVerificationToken': token,
                'X-Requested-With': 'XMLHttpRequest',
            },
        });

        if (!res.ok) throw new Error('Server error');

        const { isActive } = await res.json();

        // Cập nhật badge
        const dot   = btn.querySelector('span:first-child');
        const label = btn.querySelector('.status-label');

        if (isActive) {
            btn.classList.replace('bg-slate-100','bg-emerald-100');
            btn.classList.replace('text-slate-500','text-emerald-700');
            btn.classList.replace('hover:bg-slate-200','hover:bg-emerald-200');
            dot.classList.replace('bg-slate-400','bg-emerald-500');
            label.textContent = 'Hoạt động';
        } else {
            btn.classList.replace('bg-emerald-100','bg-slate-100');
            btn.classList.replace('text-emerald-700','text-slate-500');
            btn.classList.replace('hover:bg-emerald-200','hover:bg-slate-200');
            dot.classList.replace('bg-emerald-500','bg-slate-400');
            label.textContent = 'Đã tắt';
        }

        btn.dataset.active = isActive.toString();
    } catch {
        alert('Không thể cập nhật trạng thái. Vui lòng thử lại.');
    } finally {
        btn.disabled = false;
    }
}

// ── Delete confirm ───────────────────────────────────────────────
function confirmDelete(name, productCount, childCount) {
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
