const formChangeStatus = $('#form-change-status');

// Single status update
function updateStatus(id, status) {
    formChangeStatus.find('input[name="id"]').val(id);
    formChangeStatus.find('input[name="status"]').val(status);

    showToast(`Đang cập nhật trạng thái sang ${status}`, 'success');
    setTimeout(function () {
        formChangeStatus.submit();
    }, 1000);
}

// Bulk status update
function bulkUpdateStatus(status) {
    const checkboxes = document.querySelectorAll('.row-checkbox:checked');
    if (checkboxes.length === 0) {
        showToast('Vui lòng chọn ít nhất một mục', 'error');
        return;
    }

    $(formChangeStatus).find('input[name="id"]').val(getSelected())
    $(formChangeStatus).find('input[name="status"]').val(status)

    formChangeStatus.get(0).setAttribute('action', "/Admin/Order/EditManyStatus");
    console.log(formChangeStatus)

    showToast("Gửi yêu cầu thay đổi trạng thái thành công", 'success');
    setTimeout(function () {
        formChangeStatus.submit();
    }, 1000)
}

function confirmDelete(button) {
    // console.log($(button).attr('data-id'))
    if (confirm("Bạn có chắc chắn muốn hủy đơn hàng này không?")) {
        updateStatus($(button).attr('data-id'), 'Cancelled');
    }
}