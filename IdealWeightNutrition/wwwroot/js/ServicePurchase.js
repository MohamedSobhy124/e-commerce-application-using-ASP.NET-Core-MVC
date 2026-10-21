var dataTable;

$(document).ready(function () {
    loadDataTable();
});

function togglePurchaseEmptyState(count) {
    var empty = document.getElementById('servicePurchasesEmpty');
    var wrap = document.getElementById('servicePurchasesTableWrap');
    if (!empty || !wrap) return;
    empty.hidden = count !== 0;
    wrap.hidden = count === 0;
}

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        "ajax": {
            "url": "/Admin/ServiceSubscription/GetAllPurchases",
            "dataSrc": function (json) {
                var rows = json.data || [];
                togglePurchaseEmptyState(rows.length);
                return rows;
            }
        },
        "columns": [
            { 
                "data": "id", 
                "width": "80px",
                "render": function (data) {
                    return `<strong class="text-primary">#${data}</strong>`;
                }
            },
            {
                "data": "serviceTitle",
                "width": "200px",
                "render": function (data) {
                    if (!data || data === "N/A") {
                        return '<span class="text-muted">N/A</span>';
                    }
                    return `<span class="fw-semibold">${data}</span>`;
                }
            },
            {
                "data": "customerName",
                "width": "150px",
                "render": function (data) {
                    if (!data || data === "N/A") {
                        return '<span class="text-muted">N/A</span>';
                    }
                    return data;
                }
            },
            {
                "data": "email",
                "width": "180px",
                "render": function (data) {
                    if (!data || data === "N/A") {
                        return '<span class="text-muted">N/A</span>';
                    }
                    return `<a href="mailto:${data}" class="text-decoration-none">${data}</a>`;
                }
            },
            {
                "data": "phone",
                "width": "130px",
                "render": function (data) {
                    if (!data || data === "N/A") {
                        return '<span class="text-muted">N/A</span>';
                    }
                    return data;
                }
            },
            {
                "data": "totalAmount",
                "width": "120px",
                "className": "text-end",
                "render": function (data) {
                    const currencySymbol = (typeof getCurrencySymbol === 'function' ? getCurrencySymbol() : (typeof getCurrentLanguage === 'function' && getCurrentLanguage() === 'ar' ? 'د.إ' : 'AED'));
                    return `<strong class="text-dark">${currencySymbol} ${data.toFixed(2)}</strong>`;
                }
            },
            {
                "data": "amountPaid",
                "width": "120px",
                "className": "text-end",
                "render": function (data) {
                    const currencySymbol = (typeof getCurrencySymbol === 'function' ? getCurrencySymbol() : (typeof getCurrentLanguage === 'function' && getCurrentLanguage() === 'ar' ? 'د.إ' : 'AED'));
                    return `<strong class="text-success">${currencySymbol} ${data.toFixed(2)}</strong>`;
                }
            },
            {
                "data": "paymentStatus",
                "width": "130px",
                "render": function (data) {
                    if (data === "Approved") {
                        return '<span class="badge bg-success"><i class="bi bi-check-circle me-1"></i>' + data + '</span>';
                    } else if (data === "Pending") {
                        return '<span class="badge bg-warning"><i class="bi bi-clock me-1"></i>' + data + '</span>';
                    } else {
                        return '<span class="badge bg-danger"><i class="bi bi-x-circle me-1"></i>' + data + '</span>';
                    }
                }
            },
            {
                "data": "purchaseDate",
                "width": "160px",
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
                "data": "id",
                "width": "100px",
                "orderable": false,
                "render": function (data) {
                    return `
                        <div class="btn-group" role="group">
                            <a href="/Admin/ServiceSubscription/PurchaseDetails/${data}" 
                               class="btn btn-sm btn-primary" 
                               aria-label="Details">
                                <i class="bi bi-eye" aria-hidden="true"></i>
                            </a>
                        </div>
                    `;
                }
            }
        ],
        "order": [[0, "desc"]], // Sort by Purchase ID descending
        "pageLength": 25,
        "lengthMenu": [[10, 25, 50, 100, -1], [10, 25, 50, 100, "All"]],
        "responsive": true,
        "scrollX": true,
        "scrollCollapse": true,
        "autoWidth": false,
        "language": {
            "emptyTable": "No service purchases found",
            "search": "Search:",
            "lengthMenu": "Show _MENU_ entries",
            "info": "Showing _START_ to _END_ of _TOTAL_ purchases",
            "infoEmpty": "Showing 0 to 0 of 0 purchases",
            "infoFiltered": "(filtered from _MAX_ total purchases)",
            "paginate": {
                "first": "First",
                "last": "Last",
                "next": "Next",
                "previous": "Previous"
            }
        },
        "dom": '<"row"<"col-sm-12 col-md-6"l><"col-sm-12 col-md-6"f>>rt<"row"<"col-sm-12 col-md-5"i><"col-sm-12 col-md-7"p>>',
        "initComplete": function() {
            $('#tblData_wrapper').addClass('service-purchase-table-wrapper');
        }
    });
}
