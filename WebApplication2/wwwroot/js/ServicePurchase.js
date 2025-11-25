var dataTable;

$(document).ready(function () {
    loadDataTable();
});

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        "ajax": {
            "url": "/Admin/ServiceSubscription/GetAllPurchases",
            "dataSrc": "data"
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
                               title="View Details">
                                <i class="bi bi-eye me-1"></i>View
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
            // Add custom styling after table initialization
            $('#tblData_wrapper').addClass('service-purchase-table-wrapper');
        }
    });
}

// Add custom styles for better table appearance
// Check if style already exists to avoid duplicate declaration
let styleElement = document.getElementById('service-purchase-table-styles');
if (!styleElement) {
    styleElement = document.createElement('style');
    styleElement.id = 'service-purchase-table-styles';
    styleElement.textContent = `
    .service-purchase-table-wrapper {
        padding: 1rem;
    }
    
    .service-purchase-table-wrapper .dataTables_wrapper .dataTables_filter input {
        border: 2px solid #e5e7eb;
        border-radius: 8px;
        padding: 0.5rem 1rem;
        margin-left: 0.5rem;
        transition: all 0.3s ease;
    }
    
    .service-purchase-table-wrapper .dataTables_wrapper .dataTables_filter input:focus {
        border-color: #7c3aed;
        outline: none;
        box-shadow: 0 0 0 3px rgba(124, 58, 237, 0.1);
    }
    
    .service-purchase-table-wrapper .dataTables_wrapper .dataTables_length select {
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
        .service-purchase-table-wrapper {
            overflow-x: auto;
        }
        
        #tblData {
            min-width: 1200px;
        }
    }
`;
    document.head.appendChild(styleElement);
}

