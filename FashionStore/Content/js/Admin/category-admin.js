// Hàm riêng để fill dữ liệu cho edit category
function fillEditCategoryModal(categoryId, categoryName, groupId) {
    const modal = document.getElementById('editCategoryModal');
    if (modal) {
        // Fill dữ liệu vào form
        const categoryNameInput = modal.querySelector('input[name="CategoryName"]');
        const categoryIdInput = modal.querySelector('input[name="CategoryID"]');
        const categoryGroupSelect = modal.querySelector('select[name="GroupID"]');

        if (categoryGroupSelect) {
            categoryGroupSelect.value = groupId;
        }
        
        if (categoryNameInput) {
            categoryNameInput.value = categoryName;
        }
        if (categoryIdInput) {
            categoryIdInput.value = categoryId;
        }

        // Mở modal sau khi fill xong
        openModal(categoryId,'editCategoryModal');
    }
}

function submitUpdateForm()
{
    const form = document.getElementById('editCategoryForm');
    if (form) {
        form.submit();
    }
}

function confirmDelete(id)
{
    const confirmation = confirm("Bạn có chắc chắn muốn xóa danh mục này không?");
    if(confirmation)
    {
        const form = document.getElementById('deleteCategoryForm');
        console.log(form);
        if (form) {
            const categoryId = document.querySelector('input[name="id"]');
            categoryId.value = id;
            form.submit();
        }
    }
}
