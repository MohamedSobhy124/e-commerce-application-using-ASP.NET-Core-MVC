var dataTable;

$(document).ready(function () {
    loadDataTable();
});

function tableLabels() {
    var table = document.getElementById('tblData');
    return table ? table.dataset : {};
}

function toggleNewsletterEmptyState(count) {
    var empty = document.getElementById('newsletterEmpty');
    var wrap = document.getElementById('newsletterTableWrap');
    if (!empty || !wrap) {
        return;
    }
    empty.hidden = count !== 0;
    wrap.hidden = count === 0;
}

function loadDataTable() {
    var labels = tableLabels();
    dataTable = $('#tblData').DataTable({
        ajax: {
            url: '/Admin/NewsletterSubscription/GetAll',
            dataSrc: function (json) {
                var rows = json.data || [];
                toggleNewsletterEmptyState(rows.length);
                return rows;
            }
        },
        columns: [
            { data: 'id' },
            {
                data: 'email',
                render: function (data) {
                    return '<a href="mailto:' + data + '" class="text-decoration-none">' + data + '</a>';
                }
            },
            {
                data: 'subscribedDate',
                render: function (data) {
                    return data ? new Date(data).toLocaleString() : '—';
                }
            },
            { data: 'source' },
            {
                data: 'isActive',
                render: function (data) {
                    return data ? labels.labelActive : labels.labelInactive;
                }
            },
            {
                data: 'unsubscribedDate',
                render: function (data) {
                    return data ? new Date(data).toLocaleString() : '—';
                }
            },
            {
                data: 'id',
                orderable: false,
                render: function (data, type, row) {
                    return '<div class="admin-row-actions">' +
                        '<a href="/Admin/NewsletterSubscription/Details/' + data + '" class="btn btn-sm btn-outline-secondary" aria-label="' + labels.labelDetails + '"><i class="bi bi-eye" aria-hidden="true"></i></a>' +
                        '<button type="button" class="btn btn-sm btn-outline-primary toggle-active" data-id="' + data + '" aria-label="' + (row.isActive ? labels.labelDeactivate : labels.labelActivate) + '"><i class="bi bi-' + (row.isActive ? 'pause' : 'play') + '" aria-hidden="true"></i></button>' +
                        '<button type="button" class="btn btn-sm btn-danger delete-subscription" data-id="' + data + '" data-email="' + (row.email || '') + '" aria-label="' + labels.labelDelete + '"><i class="bi bi-trash" aria-hidden="true"></i></button>' +
                        '</div>';
                }
            }
        ],
        order: [[0, 'desc']],
        pageLength: 25,
        language: {
            emptyTable: labels.empty || ''
        }
    });
}

$(document).on('click', '.toggle-active', function () {
    var subscriptionId = $(this).data('id');
    var labels = tableLabels();
    var token = $('input[name="__RequestVerificationToken"]').val();
    Swal.fire({
        title: labels.confirmTitle,
        text: labels.confirmStatus,
        icon: 'question',
        showCancelButton: true,
        confirmButtonText: labels.confirmYes
    }).then(function (result) {
        if (!result.isConfirmed) {
            return;
        }
        var formData = new FormData();
        formData.append('id', subscriptionId);
        if (token) {
            formData.append('__RequestVerificationToken', token);
        }
        $.ajax({
            url: '/Admin/NewsletterSubscription/ToggleActive',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            success: function (response) {
                if (response.success) {
                    toastr.success(response.message);
                    dataTable.ajax.reload();
                } else {
                    toastr.error(response.message);
                }
            }
        });
    });
});

$(document).on('click', '.delete-subscription', function () {
    var subscriptionId = $(this).data('id');
    var email = $(this).data('email');
    var labels = tableLabels();
    var token = $('input[name="__RequestVerificationToken"]').val();
    Swal.fire({
        title: labels.confirmTitle,
        text: (labels.confirmDelete || '') + ' ' + (email || ''),
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: labels.confirmYes
    }).then(function (result) {
        if (!result.isConfirmed) {
            return;
        }
        var formData = new FormData();
        formData.append('id', subscriptionId);
        if (token) {
            formData.append('__RequestVerificationToken', token);
        }
        $.ajax({
            url: '/Admin/NewsletterSubscription/Delete',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            success: function (response) {
                if (response.success) {
                    toastr.success(response.message);
                    dataTable.ajax.reload();
                } else {
                    toastr.error(response.message);
                }
            }
        });
    });
});
