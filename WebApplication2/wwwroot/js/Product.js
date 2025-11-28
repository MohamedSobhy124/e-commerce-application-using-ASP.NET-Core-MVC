var dataTable;

// Helper function to get localized string
function getLocalizedString(key, defaultValue) {
    if (typeof window.Localizer !== 'undefined' && window.Localizer[key]) {
        return window.Localizer[key];
    }
    return defaultValue || key;
}

$(document).ready(function () {
    loadDataTable();
});


function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        "ajax": { 
            url: '/Admin/Product/getall',
            dataSrc: 'data'
        },
        "order": [[0, "desc"]], // Order by first column (ID) descending
        "ordering": true, // Enable sorting
        "orderMulti": false, // Disable multi-column ordering
        "stateSave": false, // Don't save state to override any saved sorting
        "rowCallback": function(row, data) {
            // Mark deleted products in red
            if (data.isDeleted) {
                $(row).css('background-color', '#ffebee');
                $(row).css('color', '#c62828');
                $(row).find('td').css('color', '#c62828');
            }
        },
        "columns": [
            { 
                data: 'id', 
                "width": "15%",
                "render": function(data, type, row) {
                    var html = data;
                    if (row.isDeleted) {
                        html += ' <span class="badge bg-danger">' + getLocalizedString('Deleted', 'Deleted') + '</span>';
                    }
                    return html;
                }
            },
            { 
                data: 'title',
                "width": "15%",
                "render": function(data, type, row) {
                    if (row.isDeleted) {
                        return '<span style="text-decoration: line-through; color: #c62828;">' + data + '</span>';
                    }
                    return data;
                }
            },
            { data: 'isbn', "width": "10%" },
            { data: 'price', "width": "10%" },
            { data: 'author', "width": "10%" }, 
            { data: 'categry.name', "width": "10%" },
            {
                data: 'id',
                "render": function (data, type, row) {
                    // Remove actions for deleted products
                    if (row.isDeleted) {
                        return '<span class="text-muted" style="color: #c62828 !important;"><i class="bi bi-lock-fill"></i> ' + getLocalizedString('NoActions', 'No Actions') + '</span>';
                    }
                    return `
                    <div class=" btn-group" role="group">
                    <a href="/Admin/Product/UpSert?id=${data}" class="btn btn-dark"><i class="bi bi-pencil-square"></i>${getLocalizedString('Edit', 'Edit')}</a>

                    <a Onclick="Delete('/Admin/Product/Delete?id=${data}')" class="btn btn-danger "><i class="bi bi-trash-fill"></i>${getLocalizedString('Delete', 'Delete')}</a>
                    </div>`
                },
                "width": "25%"
            }

        ]
    });
};
function Delete(url) {
    Swal.fire({
        title: getLocalizedString('AreYouSure', 'Are you sure?'),
        text: getLocalizedString('DeleteConfirmMessage', "You won't be able to revert this!"),
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#3085d6',
        cancelButtonColor: '#d33',
        confirmButtonText: getLocalizedString('YesDeleteIt', 'Yes, delete it!'),
        cancelButtonText: getLocalizedString('Cancel', 'Cancel')
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: url,
                type: 'DELETE',
                success: function (data) {
                    if (data.success) {
                        toastr.success(data.massage || data.message || getLocalizedString('ProductDeletedSuccessfully', 'Product deleted successfully'));
                        // Reload the DataTable
                        dataTable.ajax.reload(null, false); // false = don't reset paging
                    } else {
                        toastr.error(data.massage || data.message || getLocalizedString('ErrorDeletingProduct', 'Error deleting product'));
                    }
                },
                error: function (xhr, status, error) {
                    toastr.error(getLocalizedString('ErrorDeletingProduct', 'Error deleting product') + ': ' + error);
                }
            });
        }
    });
}

