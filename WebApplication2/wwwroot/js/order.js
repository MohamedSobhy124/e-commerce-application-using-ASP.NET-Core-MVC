// Order Management DataTable
let dataTable;
let currentFilter = 'all';

$(document).ready(function () {
    // Load initial statistics
    $.ajax({
        url: '/Admin/Order/GetAll',
        type: 'GET',
        success: function(response) {
            if (response && response.data) {
                updateOrderStatistics(response.data);
            }
        },
        error: function() {
            console.error('Failed to load order statistics');
        }
    });
    
    // Load DataTable
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
                "width": "6%",
                "render": function (data, type, row) {
                    const id = row.id || row.Id;
                    return `<strong>${id}</strong>`;
                }
            },
            { 
                data: null,
                "width": "12%",
                "render": function (data, type, row) {
                    return row.name || row.Name || 'N/A';
                }
            },
            { 
                data: null,
                "width": "14%",
                "render": function (data, type, row) {
                    // For guest orders, show the email from OrderHeader
                    // For authenticated users, show ApplicationUser email
                    const isGuest = row.isGuestOrder || row.IsGuestOrder;
                    const appUser = row.applicationUser || row.ApplicationUser;
                    
                    const guestText = (window.orderTranslations && window.orderTranslations.guestOrder) || 'Guest Order';
                    const naText = (window.orderTranslations && window.orderTranslations.na) || 'N/A';
                    
                    if (isGuest || !appUser) {
                        return (row.email || row.Email) || `<span class="text-muted">${guestText}</span>`;
                    }
                    return (appUser.email || appUser.Email) || `<span class="text-muted">${naText}</span>`;
                }
            },
            { 
                data: null,
                "width": "10%",
                "render": function (data, type, row) {
                    return row.phoneNumber || row.PhoneNumber || 'N/A';
                }
            },
            { 
                data: null,
                "width": "10%",
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
                    const currencySymbol = (typeof getCurrencySymbol === 'function' ? getCurrencySymbol() : 'AED');
                    return `<strong style="color: #059669;">${currencySymbol} ${parseFloat(total).toFixed(2)}</strong>`;
                }
            },
            { 
                data: null,
                "width": "12%",
                "render": function (data, type, row) {
                    const status = (row.orderStatus || row.OrderStatus || '').toLowerCase();
                    const translations = window.orderTranslations || {};
                    let badgeClass = 'badge-secondary';
                    let icon = 'bi-hourglass';
                    let statusText = row.orderStatus || row.OrderStatus || '';
                    
                    // Map status to translated text
                    if (status.includes('delivered')) {
                        badgeClass = 'badge-success';
                        icon = 'bi-check-circle-fill';
                        statusText = translations.delivered || statusText;
                    } else if (status.includes('approved')) {
                        badgeClass = 'badge-success';
                        icon = 'bi-check-circle';
                        statusText = translations.approved || statusText;
                    } else if (status.includes('shipped')) {
                        badgeClass = 'badge-primary';
                        icon = 'bi-truck';
                        statusText = translations.shipped || statusText;
                    } else if (status.includes('cancel')) {
                        badgeClass = 'badge-danger';
                        icon = 'bi-x-circle';
                        statusText = translations.cancelled || statusText;
                    } else if (status.includes('process')) {
                        badgeClass = 'badge-info';
                        icon = 'bi-gear';
                        statusText = translations.processing || statusText;
                    } else if (status.includes('pending')) {
                        badgeClass = 'badge-warning';
                        icon = 'bi-clock';
                        statusText = translations.pending || statusText;
                    }
                    
                    return `<span class="order-badge ${badgeClass}"><i class="bi ${icon}"></i>${statusText}</span>`;
                }
            },
            {
                data: null,
                "width": "12%",
                "render": function (data, type, row) {
                    const paymentStatus = (row.paymentStatus || row.PaymentStatus || '').toLowerCase();
                    const translations = window.orderTranslations || {};
                    let badgeClass = 'payment-badge pending';
                    let icon = 'bi-hourglass';
                    let label = row.paymentStatus || row.PaymentStatus || (translations.pending || 'Pending');
                    
                    if (paymentStatus.includes('paid')) {
                        badgeClass = 'payment-badge paid';
                        icon = 'bi-check-circle-fill';
                        label = translations.paid || label;
                    } else if (paymentStatus.includes('pending')) {
                        badgeClass = 'payment-badge pending';
                        icon = 'bi-clock';
                        label = translations.pending || label;
                    } else if (paymentStatus.includes('approved')) {
                        badgeClass = 'payment-badge approved';
                        icon = 'bi-check-circle';
                        label = translations.approved || label;
                    } else if (paymentStatus.includes('refund')) {
                        badgeClass = 'payment-badge refunded';
                        icon = 'bi-arrow-counterclockwise';
                        label = translations.refunded || label;
                    } else if (paymentStatus.includes('reject')) {
                        badgeClass = 'payment-badge rejected';
                        icon = 'bi-x-circle';
                        label = translations.rejected || label;
                    }
                    
                    return `<span class="${badgeClass}"><i class="bi ${icon}"></i>${label}</span>`;
                }
            },
            {
                data: null,
                "width": "14%",
                "render": function (data, type, row) {
                    const orderId = row.id || row.Id;
                    
                    if (!orderId || orderId === 0) {
                        console.error('Invalid Order ID:', row);
                        return '';
                    }
                    
                    const translations = window.orderTranslations || {};
                    const detailsText = translations.details || 'Details';
                    
                    return `
                        <div class="btn-group" role="group">
                            <a href="/Admin/Order/Details/${orderId}" 
                               class="btn btn-sm btn-primary"
                               title="${detailsText}">
                                <i class="bi bi-eye me-1"></i>${detailsText}
                            </a>
                        </div>
                    `;
                }
            }
        ],
        "order": [[0, "desc"]],
        "language": {
            "emptyTable": (window.orderTranslations && window.orderTranslations.noOrdersFound) || "No orders found"
        },
        "responsive": true,
        "pageLength": 25
    });
}

// Update order statistics
function updateOrderStatistics(orders) {
    const stats = {
        all: orders.length,
        pending: 0,
        approved: 0,
        processing: 0,
        shipped: 0,
        delivered: 0,
        cancelled: 0
    };

    orders.forEach(function(order) {
        const status = (order.orderStatus || order.OrderStatus || '').toLowerCase();
        
        if (status.includes('pending')) {
            stats.pending++;
        } else if (status.includes('approved')) {
            stats.approved++;
        } else if (status.includes('process')) {
            stats.processing++;
        } else if (status.includes('shipped')) {
            stats.shipped++;
        } else if (status.includes('delivered')) {
            stats.delivered++;
        } else if (status.includes('cancel')) {
            stats.cancelled++;
        }
    });

    // Update DOM
    $('#statAll').text(stats.all);
    $('#statPending').text(stats.pending);
    $('#statApproved').text(stats.approved);
    $('#statProcessing').text(stats.processing);
    $('#statShipped').text(stats.shipped);
    $('#statDelivered').text(stats.delivered);
}

function filterOrders(status) {
    currentFilter = status;
    
    // Update active tab
    document.querySelectorAll('.filter-tab').forEach(tab => {
        tab.classList.remove('active');
    });
    
    // Find and activate the clicked tab
    const tabs = document.querySelectorAll('.filter-tab');
    tabs.forEach(tab => {
        const onclickAttr = tab.getAttribute('onclick');
        if (onclickAttr && onclickAttr.includes(`'${status}'`)) {
            tab.classList.add('active');
        }
    });
    
    // Reload table with filter
    loadDataTable(status);
}


