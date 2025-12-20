// Common JavaScript functionality for Admin Panel

const btnCheckboxes = $('input[btn-checkbox]')
const btnCheckAll = $('#selectAll')
$(btnCheckboxes).each(function () {
    $(this).on('click', function () {
        console.log($(this).attr('data-id'))

        if (btnCheckboxes.length == $('input[btn-checkbox]:checked').length) {
            btnCheckAll.prop('checked', true)
        } else {
            btnCheckAll.prop('checked', false)
        }
    })
})

function getSelected() {
    let ids = $(btnCheckboxes.filter(':checked'))
    let idsArr = $(ids).map(function () {
        return $(this).attr('data-id')
    }).get()
    return idsArr;
}
// Toast notification system
function showToast(message, type = 'success') {
  const container = document.querySelector('.my-toast-container') || createToastContainer();
  const toast = document.createElement('div');
  toast.className = `my-toast ${type}`;
  toast.innerHTML = `
    <div style="display: flex; align-items: center; gap: 0.5rem;">
      <svg style="width: 20px; height: 20px;" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
        ${type === 'success' 
          ? '<path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path><polyline points="22 4 12 14.01 9 11.01"></polyline>'
          : '<circle cx="12" cy="12" r="10"></circle><line x1="12" y1="8" x2="12" y2="12"></line><line x1="12" y1="16" x2="12.01" y2="16"></line>'
        }
      </svg>
      <span>${message}</span>
    </div>
  `;
  container.appendChild(toast);
  
  setTimeout(() => {
    toast.style.opacity = '0';
    toast.style.transform = 'translateX(100%)';
    setTimeout(() => toast.remove(), 300);
  }, 3000);
}

function createToastContainer() {
  const container = document.createElement('div');
  container.className = 'my-toast-container';
  document.body.appendChild(container);
  return container;
}

// Sidebar toggle
function toggleSidebar() {
  const sidebar = document.querySelector('.sidebar');
  const mainContent = document.querySelector('.main-content');
  sidebar.classList.toggle('collapsed');
  sidebar.classList.toggle('show');
  mainContent.classList.toggle('expanded');
}

// Modal functions
function openModal(id, modalId) {
    const modal = document.getElementById(modalId);
    if (modal) {
        modal.classList.add('show');

        modal.setAttribute('data-id', id);
        const title = modal.querySelector('h3');

        // Lưu tiêu đề gốc nếu chưa có
        if (!title.dataset.originalText) {
            title.dataset.originalText = title.innerText;
        }

        // Đặt lại tiêu đề với ID mới
        title.innerText = `${title.dataset.originalText} #${id}`;

        document.body.style.overflow = 'hidden';
    }
}

function closeModal(modalId) {
  const modal = document.getElementById(modalId);
  if (modal) {
    modal.classList.remove('show');
    document.body.style.overflow = '';
  }
}

// Close modal on outside click
document.addEventListener('click', (e) => {
  if (e.target.classList.contains('modal')) {
    closeModal(e.target.id);
  }
});

// Form validation
function validateForm(formId) {
  const form = document.getElementById(formId);
  if (!form) return false;
  
  const inputs = form.querySelectorAll('[required]');
  let isValid = true;
  
  inputs.forEach(input => {
    if (!input.value.trim()) {
      input.style.borderColor = 'var(--destructive)';
      isValid = false;
    } else {
      input.style.borderColor = 'var(--border)';
    }
  });
  
  return isValid;
}

// Search functionality
function setupSearch(inputId, tableId) {
  // const input = document.getElementById(inputId);
  // const table = document.getElementById(tableId);
  //
  // if (!input || !table) return;
  //
  // input.addEventListener('input', (e) => {
  //   const searchTerm = e.target.value.toLowerCase();
  //   const rows = table.querySelectorAll('tbody tr');
  //  
  //   rows.forEach(row => {
  //     const text = row.textContent.toLowerCase();
  //     row.style.display = text.includes(searchTerm) ? '' : 'none';
  //   });
  // });
}

// Select all checkboxes
function setupSelectAll(selectAllId, checkboxClass) {
  const selectAll = document.getElementById(selectAllId);
  if (!selectAll) return;
  
  selectAll.addEventListener('change', (e) => {
    const checkboxes = document.querySelectorAll(`.${checkboxClass}`);
    checkboxes.forEach(cb => cb.checked = e.target.checked);
    updateBulkActionsBar();
  });
}

// Update bulk actions bar
function updateBulkActionsBar() {
  const checkboxes = document.querySelectorAll('.row-checkbox:checked');
  const bulkBar = document.getElementById('bulkActionsBar');
  const count = document.getElementById('selectedCount');
  
  if (bulkBar && count) {
    if (checkboxes.length > 0) {
      bulkBar.classList.remove('hidden');
      count.textContent = checkboxes.length;
    } else {
      bulkBar.classList.add('hidden');
    }
  }
}


// Export to Excel (mock)
function exportToExcel() {
  showToast('Đang xuất dữ liệu Excel...', 'success');
}

// Print function
function printPage() {
  window.print();
}

// Filter functionality
function setupFilter(selectId, tableId, columnIndex) {
  // const select = document.getElementById(selectId);
  // const table = document.getElementById(tableId);
  //
  // if (!select || !table) return;
  //
  // select.addEventListener('change', (e) => {
  //   const filterValue = e.target.value.toLowerCase();
  //   const rows = table.querySelectorAll('tbody tr');
  //  
  //   rows.forEach(row => {
  //     const cell = row.querySelectorAll('td')[columnIndex];
  //     if (!cell) return;
  //    
  //     const text = cell.textContent.toLowerCase();
  //     if (filterValue === 'all' || text.includes(filterValue)) {
  //       row.style.display = '';
  //     } else {
  //       row.style.display = 'none';
  //     }
  //   });
  // });
}

// submit form filters
function submitFilters() {
    const filterForm = $('#filterForm');

    filterForm.submit();
}

// Clear filters
function clearFilters() {
    // Reset search input
    const inputTag = document.querySelectorAll('.form-control');
    if (inputTag.length > 0)
        inputTag.forEach(input => {
            input.value = '';
        })

    // Reset select filters
    const selects = document.querySelectorAll('select[data-filter]');
    selects.forEach(select => select.value = "");

    // Show all table rows
    // const rows = document.querySelectorAll('tbody tr');
    // rows.forEach(row => row.style.display = '');

    showToast('Đã xóa bộ lọc', 'success');
    setTimeout(() => {
        submitFilters()
    }, 500);
}

// Format currency
function formatCurrency(amount) {
  return new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND'
  }).format(amount);
}

// Format date
function formatDate(date) {
  return new Intl.DateTimeFormat('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric'
  }).format(new Date(date));
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', () => {
  // Setup search if search input exists
  const searchInput = document.querySelector('input[type="search"]');
  const table = document.querySelector('table');
  if (searchInput && table) {
    setupSearch(searchInput.id || 'searchInput', table.id || 'dataTable');
  }
  
  // Setup select all if exists
  const selectAll = document.getElementById('selectAll');
  if (selectAll) {
    setupSelectAll('selectAll', 'row-checkbox');
  }
  
  // Add change listeners to row checkboxes
  document.querySelectorAll('.row-checkbox').forEach(cb => {
    cb.addEventListener('change', updateBulkActionsBar);
  });
  
  // Mobile menu toggle
  const menuToggle = document.getElementById('menuToggle');
  if (menuToggle) {
    menuToggle.addEventListener('click', toggleSidebar);
  }
});

// Set active navigation item
function setActiveNav(page) {
  document.querySelectorAll('.nav-item').forEach(item => {
    item.classList.remove('active');
    if (item.getAttribute('data-page') === page) {
      item.classList.add('active');
    }
  });
}

function saveDraft() {
    showToast('Đã lưu bản nháp!', 'success');
}

function previewImage(input, previewId) {
    const preview = document.getElementById(previewId);
    if (input.files && input.files[0]) {

        preview.parentElement.style.padding = '0.5rem';
        
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

        preview.parentElement.style.padding = '0.5rem';
        
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