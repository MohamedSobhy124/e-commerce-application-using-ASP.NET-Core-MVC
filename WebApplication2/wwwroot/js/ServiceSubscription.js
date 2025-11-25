var dataTable;

$(document).ready(function () {
    loadDataTable();
});

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        "ajax": {
            "url": "/Admin/ServiceSubscription/GetAll"
        },
        "columns": [
            { "data": "id", "width": "5%" },
            {
                "data": "imageUrl",
                "width": "10%",
                "render": function (data) {
                    if (data) {
                        return `<img src="${data}" style="width: 60px; height: 60px; object-fit: cover; border-radius: 8px;" />`;
                    }
                    return `<div style="width: 60px; height: 60px; background: #f3f4f6; border-radius: 8px; display: flex; align-items: center; justify-content: center;">
                                <i class="bi bi-image" style="color: #9ca3af;"></i>
                             </div>`;
                }
            },
            { "data": "title", "width": "20%" },
            {
                "data": "serviceType",
                "width": "10%",
                "render": function (data) {
                    if (data == 1) {
                        return '<span class="badge bg-success">Online</span>';
                    } else {
                        return '<span class="badge bg-info">Offline</span>';
                    }
                }
            },
            {
                "data": "price",
                "width": "10%",
                "render": function (data) {
                    const currencySymbol = (typeof getCurrencySymbol === 'function' ? getCurrencySymbol() : (typeof getCurrentLanguage === 'function' && getCurrentLanguage() === 'ar' ? 'د.إ' : 'AED'));
                    return currencySymbol + ' ' + data.toFixed(2);
                }
            },
            {
                "data": "offlinePaymentPercent",
                "width": "10%",
                "render": function (data) {
                    if (data) {
                        return data + '%';
                    }
                    return '-';
                }
            },
            {
                "data": "isActive",
                "width": "10%",
                "render": function (data) {
                    if (data) {
                        return '<span class="badge bg-success">Active</span>';
                    } else {
                        return '<span class="badge bg-secondary">Inactive</span>';
                    }
                }
            },
            {
                "data": "purchaseCount",
                "width": "10%",
                "render": function (data) {
                    return '<span class="badge bg-primary">' + (data || 0) + '</span>';
                }
            },
            {
                "data": "id",
                "width": "15%",
                "render": function (data) {
                    return `
                        <div class="btn-group" role="group">
                            <a href="/Admin/ServiceSubscription/Details/${data}" class="btn btn-sm btn-info" title="Details">
                                <i class="bi bi-eye"></i>
                            </a>
                            <a href="/Admin/ServiceSubscription/Edit/${data}" class="btn btn-sm btn-warning" title="Edit">
                                <i class="bi bi-pencil"></i>
                            </a>
                            <button type="button" class="btn btn-sm btn-secondary toggle-active" data-id="${data}" title="Toggle Active">
                                <i class="bi bi-toggle-on"></i>
                            </button>
                            <a href="/Admin/ServiceSubscription/Delete/${data}" class="btn btn-sm btn-danger" title="Delete">
                                <i class="bi bi-trash"></i>
                            </a>
                        </div>
                    `;
                }
            }
        ]
    });
}

// Toggle active status
$(document).on('click', '.toggle-active', function () {
    var serviceId = $(this).data('id');
    var btn = $(this);
    
    $.ajax({
        url: '/Admin/ServiceSubscription/ToggleActive',
        type: 'POST',
        data: {
            id: serviceId,
            __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
        },
        success: function (response) {
            if (response.success) {
                dataTable.ajax.reload();
                toastr.success(response.message);
            } else {
                toastr.error(response.message);
            }
        },
        error: function () {
            toastr.error('An error occurred. Please try again.');
        }
    });
});

