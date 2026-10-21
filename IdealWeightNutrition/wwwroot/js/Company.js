$(document).ready(function () {
    var table = document.getElementById('tblData');
    if (!table) {
        return;
    }

    var labels = table.dataset;
    var dataTable = $('#tblData').DataTable({
        ajax: { url: '/Admin/Companies/getall' },
        language: {
            emptyTable: labels.empty || ''
        },
        columns: [
            { data: 'id' },
            { data: 'name' },
            { data: 'phoneNumber' },
            { data: 'city' },
            { data: 'state' },
            {
                data: 'id',
                orderable: false,
                render: function (data, type, row) {
                    var name = row && row.name ? row.name : '';
                    return '<div class="admin-row-actions">' +
                        '<a href="/Admin/Companies/UpSert?id=' + data + '" class="btn btn-sm btn-primary" aria-label="' + labels.labelEdit + '"><i class="bi bi-pencil" aria-hidden="true"></i></a>' +
                        '<button type="button" class="btn btn-sm btn-danger js-delete-company" data-url="/Admin/Companies/Delete?id=' + data + '" data-name="' + name + '" aria-label="' + labels.labelDelete + '"><i class="bi bi-trash" aria-hidden="true"></i></button>' +
                        '</div>';
                }
            }
        ]
    });

    $('#tblData').on('click', '.js-delete-company', function () {
        var button = this;
        Swal.fire({
            title: labels.confirmTitle,
            text: (labels.confirmText || '') + ' ' + (button.dataset.name || ''),
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: labels.confirmYes
        }).then(function (result) {
            if (!result.isConfirmed) {
                return;
            }
            $.ajax({
                url: button.dataset.url,
                type: 'DELETE',
                success: function (data) {
                    if (data.success) {
                        toastr.success(data.massage || data.message);
                        dataTable.ajax.reload();
                    } else {
                        toastr.error(data.massage || data.message);
                    }
                }
            });
        });
    });
});
