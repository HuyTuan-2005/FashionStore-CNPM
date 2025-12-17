const formChangeStatus = $('#form-change-status');
const btnChangeStatus = $('button[btn-change-status]');
const formDelete = $('#form-delete');

let url = window.location.href;

// Áp dụng cho các form cần chống submit lặp
function preventDoubleSubmit($form) {
    $form.on('submit', function (e) {
        const $f = $(this);
        if ($f.data('submitted')) {
            e.preventDefault();
            return false;
        }
        $f.data('submitted', true);
        // Khóa tất cả nút submit để tránh click lại
        $f.find('button[type="submit"], input[type="submit"]').prop('disabled', true);
    });
}

$(function () {
    preventDoubleSubmit($('#productForm'));
    preventDoubleSubmit($('#form-change-status'));
    preventDoubleSubmit($('#form-delete'));
});

$(btnCheckAll).click(function () {
    btnCheckboxes.prop('checked', $(this).prop('checked'))
})


if (btnChangeStatus.length > 0) {
    $(btnChangeStatus).each(function () {
        $(this).on('click', function () {
            let isActive = $(this).attr('data-status') === "True" ? "false" : "true";
            console.log($(this).attr('data-status'));

            formChangeStatus.find('input[name="status"]').val(isActive);
            formChangeStatus.find('input[name="productId"]').val($(this).attr('data-id'));

            showToast("Gửi yêu cầu thay đổi trạng thái thành công", 'success');
            setTimeout(function () {
                formChangeStatus.submit();
            }, 500)
        })
    })
}


function bulkUpdateStatus(action) {
    let status = $('input[name="status"]:checked').val()

    $(formChangeStatus).find('input[name="productId"]').val(getSelected())
    $(formChangeStatus).find('input[name="status"]').val(status)

    formChangeStatus.get(0).setAttribute('action', action);
    console.log(formChangeStatus)

    showToast("Gửi yêu cầu thay đổi trạng thái thành công", 'success');
    setTimeout(function () {
        formChangeStatus.submit();
    }, 1000)
}

// Delete confirmation
function confirmDelete(id, name, type = 'mục') {
    if (confirm(`Bạn có chắc chắn muốn xóa ${type} "${name}"?`)) {
        formDelete.find('input[name="id"]').val(id);
        showToast(`Đã xóa ${type} thành công`, 'success');
        formDelete.submit();
    }
}

function confirmBulkDelete(action) {
    if (confirm(`Bạn có chắc chắn muốn xóa những sản phẩm đang chọn?`)) {
        formDelete.find('input[name="id"]').val(getSelected());
        showToast(`Đã gửi yêu cầu xóa thành công`, 'success');

        formDelete.get(0).setAttribute('action', action);
        // console.log(formDelete.attr('action'));
        // console.log(formDelete.find('input[name="id"]').val());
        setTimeout(function () {
            formDelete.submit();
        }, 2000)
    }
}

let variants = [];

// Helper: lấy giá trị được chọn
function getCheckedValuesByName(name) {
    return Array.from(document.querySelectorAll(`input[name="${name}"]:checked`));
}

// Tạo variants: duyệt theo Màu -> Size
function updateVariants() {
    const colorInputs = getCheckedValuesByName('colors');
    const sizeInputs = getCheckedValuesByName('sizes');

    const container = document.getElementById('variantsContainer');

    if (colorInputs.length === 0 || sizeInputs.length === 0) {
        container.innerHTML = '<div style="text-align:center; padding:2rem; color:var(--muted-foreground);"><p>Chọn màu sắc và kích thước để tạo biến thể</p></div>';
        variants = [];
        return;
    }

    const colors = colorInputs.map(cb => {
        const id = cb.getAttribute('data-id');
        const colorHex = cb.parentElement.style.backgroundColor;
        return {id: id, name: cb.value, hex: colorHex};
    }); // duy trì cặp name/hex theo từng màu đã tick 

    const sizes = sizeInputs.map(cb => {
        const id = cb.getAttribute('data-id');
        return {id: id, name: cb.value};
    });

    // Xây mảng variants: màu trước, size sau
    variants = [];
    colors.forEach(c => {
        sizes.forEach(size => {
            variants.push({
                color: {id: c.id, name: c.name, hex: c.hex},
                size: {id: size.id, name: size.name},
                sku: '',
                stock: 0,
                status: 'Available'
            });
        });
    }); // đảm bảo thứ tự Color -> Size như yêu cầu [web:55]

    renderVariantsTable();
}


// Render bảng: cột Màu là ô tròn theo hex
function renderVariantsTable() {
    const container = document.getElementById('variantsContainer');

    let html = `
    <div style="overflow-x:auto;">
      <table class="table">
        <thead>
          <tr>
            <th>Màu sắc</th>
            <th>Size</th>
            <th>SKU</th>
            <th>Tồn kho</th>
            <th>Trạng thái</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
  `;

    variants.forEach((v, index) => {
        html += `
      <tr>
        <td>
          <label title="Đen">
                                            <span class="swatch" style="background:${v.color.hex}">
                                            <input name="Variants[${index}].ColorID" value="${v.color.id}" type="hidden">
                                            </span>
                                        </label>
        </td>
        <td><strong>${v.size.name}</strong> <input name="Variants[${index}].SizeID" value="${v.size.id}" type="hidden"></td>
        <td>
          <input name="Variants[${index}].SKU" type="text" class="form-control" value="${v.sku}"
                 onchange="updateVariantField(${index}, 'sku', this.value)" placeholder="Auto"
                 style="max-width: 150px">
        </td>
        <td>
          <input name="Variants[${index}].Stock" type="number" class="form-control" value="${v.stock}" min="0"
                 onchange="updateVariantField(${index}, 'stock', this.value)"
                 placeholder="0" style="width:100px;">
        </td>
        <td>
        <select class="form-control" name="Variants[${index}].Status" id="${index}" onchange="updateVariantField(${index}, 'status', this.value)">
            <option value="Available" ${v.status === 'Available' ? 'selected' : ''}>Còn hàng</option>
            <option value="OutOfStock" ${v.status === 'OutOfStock' ? 'selected' : ''}>Hết hàng</option>
</select>
          
        </td>
        <td>
          <button class="btn btn-danger" onclick="removeVariantRow(${index}, this)">
            <svg style="width:16px; height:16px;" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <polyline points="3 6 5 6 21 6"></polyline>
              <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path>
            </svg>
          </button>
        </td>
      </tr>
    `;
    });

    html += `
        </tbody>
      </table>
    </div>
    <div style="margin-top:1rem; padding:1rem; background:var(--muted); border-radius:var(--radius);">
      <p style="font-size:12px; color:var(--muted-foreground);">
        <strong>Tổng: ${variants.length} biến thể</strong><br>
        SKU sẽ tự động tạo nếu để trống. Giá để trống sẽ dùng giá chung của sản phẩm.
      </p>
    </div>
  `;

    container.innerHTML = html;
}

// Cập nhật field 1 biến thể
function updateVariantField(index, field, value) {
    variants[index][field] = value;
}

// Xóa 1 dòng và phần tử tương ứng trong mảng để giữ đồng bộ index
function removeVariantRow(index, btn) {
    // Xóa khỏi DOM
    btn.closest('tr').remove();
    // Xóa khỏi dữ liệu
    variants.splice(index, 1);
    // Re-render để cập nhật index, binding onchange đúng dòng
    renderVariantsTable();
} // dùng closest('tr') để tìm dòng cha và splice để đồng bộ dữ liệu [web:117][web:113]

function handleSubmit(e) {
    const productForm = $("#productForm").get(0);
    if (productForm.checkValidity()) {
        variantsPush = variants.map(v => ({
            colorId: v.color?.id,
            sizeId: v.size?.id,
            sku: v.sku ?? '',
            stock: v.stock ?? 0,
            price: v.price ?? ''
        }));


        productForm.querySelectorAll('input[name="sizes"]').forEach(el => el.disabled = true);
        productForm.querySelectorAll('input[name="colors"]').forEach(el => el.disabled = true);

        // const fd = new FormData(productForm);
        // for (const [k, v] of fd.entries()) {
        //     console.log(k, '=>', v);
        // }

        // console.log('Variants:', variants);
        showToast('Đã thêm sản phẩm với ' + variants.length + ' biến thể!', 'success');
        setTimeout(() => {
            $("#productForm").submit();
        }, 500);
    } else {
        // Hiển thị lỗi cho user
        productForm.reportValidity();
    }
}


function previewImage(input, previewId) {
    const preview = document.getElementById(previewId);
    if (input.files && input.files[0]) {
        const reader = new FileReader();
        reader.onload = function (e) {
            preview.innerHTML = `<img src="${e.target.result}" style="max-width: 100%; border-radius: var(--radius);">`;
        };
        reader.readAsDataURL(input.files[0]);
    }
}

function previewMultipleImages(input, previewId) {
    const preview = document.getElementById(previewId);
    if (input.files && input.files.length > 0) {
        preview.innerHTML = '';
        const grid = document.createElement('div');
        grid.style.display = 'grid';
        grid.style.gridTemplateColumns = 'repeat(3, 1fr)';
        grid.style.gap = '0.5rem';

        Array.from(input.files).slice(0, 10).forEach(file => {
            const reader = new FileReader();
            reader.onload = function (e) {
                const img = document.createElement('img');
                img.src = e.target.result;
                img.style.width = '100%';
                img.style.height = '80px';
                img.style.objectFit = 'cover';
                img.style.borderRadius = 'var(--radius)';
                grid.appendChild(img);
            };
            reader.readAsDataURL(file);
        });
        preview.appendChild(grid);
    }
}


// Set active navigation item
function setActiveNav(page) {
    document.querySelectorAll('.nav-item').forEach(item => {
        item.classList.remove('active');
        if (item.getAttribute('data-page') === page) {
            item.classList.add('active');
        }
    });
}

// Toggle size pill khi click
document.querySelectorAll('.size-pill').forEach(pill => {
    pill.addEventListener('click', function () {
        this.classList.toggle('active');
        const checkbox = this.querySelector('input[type="checkbox"]');
        checkbox.checked = this.classList.contains('active');
    });
});

//const btnCheckboxes = document.querySelectorAll('input[btn-checkbox]')
//const btnCheckAll = document.querySelector('input[btn-checkAll]')

//btnCheckAll.addEventListener('click', () => {
//    btnCheckboxes.forEach(cb => cb.checked = btnCheckAll.checked)
//})

//btnCheckboxes.forEach((btn) => {
//    btn.addEventListener('click', () => {
//        console.log(btn.getAttribute('data-id'))

//        if ([...btnCheckboxes].every(c => c.checked)) {
//            btnCheckAll.checked = true
//        }
//        else {

//            btnCheckAll.checked = false
//        }
//    })

//})


//console.log(btnCheckboxes)

