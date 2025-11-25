// Order Management DataTable
let dataTable;
let currentFilter = 'all';

$(document).ready(function () {
    loadDataTable(currentFilter);
});

function loadDataTable(status) {
    if (dataTable) {
        dataTable.destroy();
    }

    const url = status === 'all' 
        ? '/Admin/Order/GetAll' 
        : `/Admin/Order/GetAll?status=${status}`;

    dataTable = $('#tblData').DataTable({
        "ajax": {
            url: url,
            type: "GET",
            dataSrc: "data"
        },
        "columns": [
            { 
                data: null,
                "width": "8%",
                "render": function (data, type, row) {
                    const id = row.id || row.Id;
                    return `<strong>#${id}</strong>`;
                }
            },
            { 
                data: null,
                "width": "15%",
                "render": function (data, type, row) {
                    return row.name || row.Name || 'N/A';
                }
            },
            { 
                data: null,
                "width": "15%",
                "render": function (data, type, row) {
                    // For guest orders, show the email from OrderHeader
                    // For authenticated users, show ApplicationUser email
                    const isGuest = row.isGuestOrder || row.IsGuestOrder;
                    const appUser = row.applicationUser || row.ApplicationUser;
                    
                    if (isGuest || !appUser) {
                        return (row.email || row.Email) || '<span class="text-muted">Guest Order</span>';
                    }
                    return (appUser.email || appUser.Email) || '<span class="text-muted">N/A</span>';
                }
            },
            { 
                data: null,
                "width": "12%",
                "render": function (data, type, row) {
                    return row.phoneNumber || row.PhoneNumber || 'N/A';
                }
            },
            { 
                data: null,
                "width": "12%",
                "render": function (data, type, row) {
                    const orderDate = row.orderDate || row.OrderDate;
                    const date = new Date(orderDate);
                    return date.toLocaleDateString('en-US', { 
                        month: 'short', 
                        day: 'numeric', 
                        year: 'numeric' 
                    });
                }
            },
            { 
                data: null,
                "width": "10%",
                "render": function (data, type, row) {
                    const total = row.orderTotal || row.OrderTotal || 0;
                    const currencySymbol = (typeof getCurrencySymbol === 'function' ? getCurrencySymbol() : (typeof getCurrentLanguage === 'function' && getCurrentLanguage() === 'ar' ? 'د.إ' : 'AED'));
                    return `<strong style="color: #059669;">${currencySymbol} ${total.toFixed(2)}</strong>`;
                }
            },
            { 
                data: null,
                "width": "12%",
                "render": function (data, type, row) {
                    const status = (row.orderStatus || row.OrderStatus || '').toLowerCase();
                    let badgeClass = 'badge-secondary';
                    let icon = 'bi-hourglass';
                    
                    if (status.includes('delivered')) {
                        badgeClass = 'badge-success';
                        icon = 'bi-check-circle-fill';
                    } else if (status.includes('approved')) {
                        badgeClass = 'badge-success';
                        icon = 'bi-check-circle';
                    } else if (status.includes('shipped')) {
                        badgeClass = 'badge-primary';
                        icon = 'bi-truck';
                    } else if (status.includes('cancel')) {
                        badgeClass = 'badge-danger';
                        icon = 'bi-x-circle';
                    } else if (status.includes('process')) {
                        badgeClass = 'badge-info';
                        icon = 'bi-gear';
                    } else if (status.includes('pending')) {
                        badgeClass = 'badge-warning';
                        icon = 'bi-clock';
                    }
                    
                    return `<span class="badge ${badgeClass}"><i class="bi ${icon} me-1"></i>${row.orderStatus || row.OrderStatus}</span>`;
                }
            },
            {
                data: null,
                "render": function (data, type, row) {
                    const orderId = row.id || row.Id;
                    
                    // Debug logging
                    if (!orderId || orderId === 0) {
                        console.error('Invalid Order ID:', row);
                    }
                    
                    return `
                        <div class="btn-group" role="group">
                            <a href="/Admin/Order/Details/${orderId}" 
                               class="btn btn-sm btn-primary"
                               title="Order ID: ${orderId}">
                                <i class="bi bi-eye me-1"></i>Details
                            </a>
                        </div>
                    `;
                },
                "width": "16%"
            }
        ],
        "order": [[0, "desc"]],
        "language": {
            "emptyTable": "No orders found"
        },
        "responsive": true
    });
}

function filterOrders(status) {
    currentFilter = status;
    
    // Update active tab
    document.querySelectorAll('.filter-tab').forEach(tab => {
        tab.classList.remove('active');
    });
    event.target.closest('.filter-tab').classList.add('active');
    
    // Reload table with filter
    loadDataTable(status);
}

// Add styles for badges
const style = document.createElement('style');
style.textContent = `
    .badge {
        padding: 0.5rem 1rem;
        border-radius: 50px;
        font-weight: 600;
        font-size: 0.85rem;
        display: inline-flex;
        align-items: center;
        white-space: nowrap;
    }
    .badge-success {
        background: linear-gradient(135deg, #10b981 0%, #059669 100%);
        color: white;
    }
    .badge-primary {
        background: linear-gradient(135deg, #7c3aed 0%, #6d28d9 100%);
        color: white;
    }
    .badge-danger {
        background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%);
        color: white;
    }
    .badge-warning {
        background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%);
        color: white;
    }
    .badge-info {
        background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%);
        color: white;
    }
    .badge-secondary {
        background: linear-gradient(135deg, #6b7280 0%, #4b5563 100%);
        color: white;
    }
    
    .order-filter-tabs {
        display: flex;
        gap: 0.75rem;
        flex-wrap: wrap;
        padding: 1.5rem;
        background: var(--gray-50);
        border-radius: var(--border-radius);
        margin-bottom: 1.5rem;
    }
    
    .filter-tab {
        padding: 0.75rem 1.5rem;
        background: var(--white);
        border: 2px solid var(--gray-300);
        border-radius: var(--border-radius);
        color: var(--gray-700);
        font-weight: 600;
        cursor: pointer;
        transition: all 0.3s ease;
        display: flex;
        align-items: center;
    }
    
    .filter-tab:hover {
        border-color: var(--primary-color);
        color: var(--primary-color);
        transform: translateY(-2px);
        box-shadow: 0 4px 12px rgba(124, 58, 237, 0.2);
    }
    
    .filter-tab.active {
        background: linear-gradient(135deg, var(--primary-color) 0%, var(--primary-dark) 100%);
        border-color: var(--primary-color);
        color: var(--white);
        box-shadow: 0 4px 12px rgba(124, 58, 237, 0.3);
    }
`;
document.head.appendChild(style);

