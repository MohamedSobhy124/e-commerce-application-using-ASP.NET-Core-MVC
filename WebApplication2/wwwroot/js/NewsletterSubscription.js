var dataTable;

$(document).ready(function () {
    loadDataTable();
});

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        "ajax": {
            "url": "/Admin/NewsletterSubscription/GetAll",
            "dataSrc": "data"
        },
        "columns": [
            { 
                "data": "id", 
                "width": "60px",
                "render": function (data) {
                    return `<strong class="text-primary">#${data}</strong>`;
                }
            },
            {
                "data": "email",
                "width": "250px",
                "render": function (data) {
                    return `<a href="mailto:${data}" class="text-decoration-none fw-semibold">${data}</a>`;
                }
            },
            {
                "data": "subscribedDate",
                "width": "180px",
                "render": function (data) {
                    if (data) {
                        var date = new Date(data);
                        return date.toLocaleDateString('en-AE', { 
                            year: 'numeric', 
                            month: 'short', 
                            day: 'numeric',
                            hour: '2-digit',
                            minute: '2-digit'
                        });
                    }
                    return '<span class="text-muted">N/A</span>';
                }
            },
            {
                "data": "source",
                "width": "120px",
                "render": function (data) {
                    if (!data || data === "N/A") {
                        return '<span class="text-muted">N/A</span>';
                    }
                    return `<span class="badge bg-info">${data}</span>`;
                }
            },
            {
                "data": "isActive",
                "width": "100px",
                "render": function (data) {
                    if (data) {
                        return '<span class="badge bg-success"><i class="bi bi-check-circle me-1"></i>Active</span>';
                    } else {
                        return '<span class="badge bg-secondary"><i class="bi bi-x-circle me-1"></i>Inactive</span>';
                    }
                }
            },
            {
                "data": "unsubscribedDate",
                "width": "180px",
                "render": function (data) {
                    if (data && data !== "") {
                        var date = new Date(data);
                        return date.toLocaleDateString('en-AE', { 
                            year: 'numeric', 
                            month: 'short', 
                            day: 'numeric',
                            hour: '2-digit',
                            minute: '2-digit'
                        });
                    }
                    return '<span class="text-muted">-</span>';
                }
            },
            {
                "data": "id",
                "width": "150px",
                "orderable": false,
                "render": function (data, type, row) {
                    return `
                        <div class="btn-group" role="group">
                            <a href="/Admin/NewsletterSubscription/Details/${data}" 
                               class="btn btn-sm btn-info" 
                               title="View Details">
                                <i class="bi bi-eye"></i>
                            </a>
                            <button type="button" 
                                    class="btn btn-sm ${row.isActive ? 'btn-warning' : 'btn-success'} toggle-active" 
                                    data-id="${data}" 
                                    title="${row.isActive ? 'Deactivate' : 'Activate'}">
                                <i class="bi bi-${row.isActive ? 'x-circle' : 'check-circle'}"></i>
                            </button>
                            <button type="button" 
                                    class="btn btn-sm btn-danger delete-subscription" 
                                    data-id="${data}"
                                    data-email="${row.email}"
                                    title="Delete">
                                <i class="bi bi-trash"></i>
                            </button>
                        </div>
                    `;
                }
            }
        ],
        "order": [[0, "desc"]], // Sort by ID descending
        "pageLength": 25,
        "lengthMenu": [[10, 25, 50, 100, -1], [10, 25, 50, 100, "All"]],
        "responsive": true,
        "scrollX": true,
        "scrollCollapse": true,
        "autoWidth": false,
        "language": {
            "emptyTable": "No newsletter subscriptions found",
            "search": "Search:",
            "lengthMenu": "Show _MENU_ entries",
            "info": "Showing _START_ to _END_ of _TOTAL_ subscriptions",
            "infoEmpty": "Showing 0 to 0 of 0 subscriptions",
            "infoFiltered": "(filtered from _MAX_ total subscriptions)",
            "paginate": {
                "first": "First",
                "last": "Last",
                "next": "Next",
                "previous": "Previous"
            }
        },
        "dom": '<"row"<"col-sm-12 col-md-6"l><"col-sm-12 col-md-6"f>>rt<"row"<"col-sm-12 col-md-5"i><"col-sm-12 col-md-7"p>>',
        "initComplete": function() {
            $('#tblData_wrapper').addClass('newsletter-subscription-table-wrapper');
        }
    });
}

// Toggle active status
$(document).on('click', '.toggle-active', function () {
    var subscriptionId = $(this).data('id');
    var btn = $(this);
    var token = $('input[name="__RequestVerificationToken"]').val();
    
    Swal.fire({
        title: 'Change Status',
        text: 'Are you sure you want to change the subscription status?',
        icon: 'question',
        showCancelButton: true,
        confirmButtonColor: '#667eea',
        cancelButtonColor: '#6b7280',
        confirmButtonText: 'Yes, Change It',
        cancelButtonText: 'Cancel'
    }).then((result) => {
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
            headers: {
                'RequestVerificationToken': token || ''
            },
            success: function (response) {
                if (response.success) {
                    dataTable.ajax.reload();
                    Swal.fire({
                        icon: 'success',
                        title: 'Success',
                        text: response.message,
                        timer: 1500,
                        showConfirmButton: false
                    });
                } else {
                    Swal.fire({
                        icon: 'error',
                        title: 'Error',
                        text: response.message
                    });
                }
            },
            error: function () {
                Swal.fire({
                    icon: 'error',
                    title: 'Error',
                    text: 'An error occurred. Please try again.'
                });
            }
        });
    });
});

// Delete subscription
$(document).on('click', '.delete-subscription', function () {
    var subscriptionId = $(this).data('id');
    var email = $(this).data('email');
    var btn = $(this);
    var token = $('input[name="__RequestVerificationToken"]').val();
    
    Swal.fire({
        title: 'Delete Subscription',
        text: 'Are you sure you want to delete subscription for ' + email + '? This action cannot be undone.',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#dc2626',
        cancelButtonColor: '#6b7280',
        confirmButtonText: 'Yes, Delete It',
        cancelButtonText: 'Cancel',
        dangerMode: true
    }).then((result) => {
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
            headers: {
                'RequestVerificationToken': token || ''
            },
            success: function (response) {
                if (response.success) {
                    dataTable.ajax.reload();
                    Swal.fire({
                        icon: 'success',
                        title: 'Success',
                        text: response.message,
                        timer: 1500,
                        showConfirmButton: false
                    });
                } else {
                    Swal.fire({
                        icon: 'error',
                        title: 'Error',
                        text: response.message
                    });
                }
            },
            error: function () {
                Swal.fire({
                    icon: 'error',
                    title: 'Error',
                    text: 'An error occurred. Please try again.'
                });
            }
        });
    });
});

// Add custom styles for better table appearance
let styleElement = document.getElementById('newsletter-subscription-table-styles');
if (!styleElement) {
    styleElement = document.createElement('style');
    styleElement.id = 'newsletter-subscription-table-styles';
    styleElement.textContent = `
    .newsletter-subscription-table-wrapper {
        padding: 1rem;
    }
    
    .newsletter-subscription-table-wrapper .dataTables_wrapper .dataTables_filter input {
        border: 2px solid #e5e7eb;
        border-radius: 8px;
        padding: 0.5rem 1rem;
        margin-left: 0.5rem;
        transition: all 0.3s ease;
    }
    
    .newsletter-subscription-table-wrapper .dataTables_wrapper .dataTables_filter input:focus {
        border-color: #7c3aed;
        outline: none;
        box-shadow: 0 0 0 3px rgba(124, 58, 237, 0.1);
    }
    
    .newsletter-subscription-table-wrapper .dataTables_wrapper .dataTables_length select {
        border: 2px solid #e5e7eb;
        border-radius: 8px;
        padding: 0.5rem;
        margin: 0 0.5rem;
    }
    
    #tblData {
        width: 100% !important;
        border-collapse: separate;
        border-spacing: 0;
    }
    
    #tblData thead th {
        background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        color: white;
        font-weight: 600;
        padding: 1rem;
        text-align: left;
        border: none;
        position: sticky;
        top: 0;
        z-index: 10;
    }
    
    #tblData tbody td {
        padding: 0.875rem 1rem;
        border-bottom: 1px solid #e5e7eb;
        vertical-align: middle;
    }
    
    #tblData tbody tr {
        transition: all 0.2s ease;
    }
    
    #tblData tbody tr:hover {
        background-color: #f9fafb;
        transform: scale(1.01);
        box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
    }
    
    #tblData tbody tr:nth-child(even) {
        background-color: #fafafa;
    }
    
    #tblData tbody tr:nth-child(even):hover {
        background-color: #f3f4f6;
    }
    
    .dataTables_wrapper .dataTables_paginate .paginate_button {
        padding: 0.5rem 0.75rem;
        margin: 0 0.25rem;
        border-radius: 6px;
        border: 1px solid #e5e7eb;
        transition: all 0.3s ease;
    }
    
    .dataTables_wrapper .dataTables_paginate .paginate_button:hover {
        background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        color: white !important;
        border-color: #667eea;
    }
    
    .dataTables_wrapper .dataTables_paginate .paginate_button.current {
        background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        color: white !important;
        border-color: #667eea;
    }
    
    .dataTables_wrapper .dataTables_info {
        padding: 0.75rem 0;
        color: #6b7280;
        font-weight: 500;
    }
    
    @media (max-width: 768px) {
        .newsletter-subscription-table-wrapper {
            overflow-x: auto;
        }
        
        #tblData {
            min-width: 1000px;
        }
    }
`;
    document.head.appendChild(styleElement);
}

