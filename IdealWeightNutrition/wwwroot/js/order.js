// Order Management with Tabulator (Modern, Free, Excellent Pagination)
let orderTable;
let currentFilter = 'all';
const translations = window.orderTranslations || {};

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    
    // Wait a bit for everything to load
    setTimeout(function() {
        // Check if Tabulator is loaded
        if (typeof Tabulator === 'undefined') {
            console.error('Tabulator library not loaded!');
            alert('Table library not loaded. Please refresh the page.');
            return;
        }
        
        // Check if container exists
        const container = document.getElementById('orderTable');
        if (!container) {
            console.error('Order table container not found!');
            return;
        }
        
        
        // Load initial statistics
        loadOrderStatistics();
        
        // Initialize Tabulator table
        initializeOrderTable(currentFilter);
    }, 500); // Wait 500ms for Tabulator CDN to load
});

// Load order statistics
function loadOrderStatistics() {
    fetch('/Admin/Order/GetOrderStatistics')
        .then(response => response.json())
        .then(data => {
            if (data.success && data.stats) {
                updateOrderStatistics(data.stats);
            }
        })
        .catch(error => {
            console.error('Failed to load order statistics:', error);
        });
}

// Initialize Tabulator table with server-side pagination
function initializeOrderTable(status) {
    const currentStatus = status === 'all' ? '' : status;
    
    
    try {
        orderTable = new Tabulator("#orderTable", {
        layout: "fitDataStretch",
        responsiveLayout: "collapse",
        pagination: true,
        paginationMode: "remote",
        paginationSize: 25,
        paginationSizeSelector: [10, 25, 50, 100],
        paginationCounter: "rows",
        ajaxURL: "/Admin/Order/GetAll",
        ajaxParams: { status: currentStatus },
        ajaxConfig: "GET",
        ajaxContentType: "json",
        ajaxFiltering: true,
        ajaxSorting: true,
        filterMode: "remote",
        sortMode: "remote",
        
        // Map server response to Tabulator format
        ajaxResponse: function(url, params, response) {
            
            return {
                last_page: response.last_page || Math.ceil((response.recordsTotal || 0) / params.size),
                data: response.data || []
            };
        },
        
        // Error handling
        ajaxError: function(error) {
            console.error('Tabulator AJAX Error:', error);
            alert('Error loading orders. Please check console for details.');
        },
        
        // Custom request parameters
        ajaxURLGenerator: function(url, config, params) {
            let fullUrl = url;
            const queryParams = [];
            
            // Status filter
            if (currentStatus) {
                queryParams.push(`status=${currentStatus}`);
            }
            
            // Pagination
            queryParams.push(`start=${(params.page - 1) * params.size}`);
            queryParams.push(`length=${params.size}`);
            
            // Sorting
            if (params.sort && params.sort.length > 0) {
                const sort = params.sort[0];
                queryParams.push(`sortColumn=${sort.field}`);
                queryParams.push(`sortDirection=${sort.dir}`);
            }
            
            // Search
            if (params.filter && params.filter.length > 0) {
                const searchValue = params.filter[0].value;
                if (searchValue) {
                    queryParams.push(`searchValue=${encodeURIComponent(searchValue)}`);
                }
            }
            
            if (queryParams.length > 0) {
                fullUrl += '?' + queryParams.join('&');
            }
            
            return fullUrl;
        },
        
        // Column definitions
        columns: [
            {
                title: translations.orderID || "Order ID",
                field: "id",
                width: 100,
                sorter: "number",
                headerSort: true,
                formatter: function(cell) {
                    return `<strong style="color: #667eea;">#${cell.getValue()}</strong>`;
                }
            },
            {
                title: translations.customer || "Customer",
                field: "name",
                sorter: "string",
                headerSort: true,
                responsive: 0,
                formatter: function(cell) {
                    const value = cell.getValue();
                    return value || '<span style="color: #9ca3af;">N/A</span>';
                }
            },
            {
                title: translations.email || "Email",
                field: "email",
                sorter: "string",
                headerSort: true,
                responsive: 1,
                formatter: function(cell) {
                    const row = cell.getRow().getData();
                    const isGuest = row.isGuestOrder;
                    const appUser = row.applicationUser;
                    
                    if (isGuest || !appUser) {
                        return row.email || `<span style="color: #9ca3af;">${translations.guestOrder || "Guest"}</span>`;
                    }
                    return appUser.email || `<span style="color: #9ca3af;">${translations.na || "N/A"}</span>`;
                }
            },
            {
                title: translations.phone || "Phone",
                field: "phoneNumber",
                sorter: "string",
                headerSort: false,
                responsive: 2,
                formatter: function(cell) {
                    const value = cell.getValue();
                    return value || '<span style="color: #9ca3af;">N/A</span>';
                }
            },
            {
                title: translations.orderDate || "Order Date",
                field: "orderDate",
                sorter: "datetime",
                headerSort: true,
                responsive: 0,
                formatter: function(cell) {
                    const date = new Date(cell.getValue());
                    return date.toLocaleDateString('en-US', { 
                        month: 'short', 
                        day: 'numeric', 
                        year: 'numeric',
                        hour: '2-digit',
                        minute: '2-digit'
                    });
                }
            },
            { 
                title: translations.total || "Total",
                field: "orderTotal",
                sorter: "number",
                headerSort: true,
                responsive: 1,
                formatter: function(cell) {
                    const value = cell.getValue() || 0;
                    const currencySymbol = (typeof getCurrencySymbol === 'function' ? getCurrencySymbol() : 'AED');
                    return `<strong style="color: #059669; font-size: 1.05rem;">${currencySymbol} ${parseFloat(value).toFixed(2)}</strong>`;
                },
                hozAlign: "right"
            },
            {
                title: translations.orderStatus || "Order Status",
                field: "orderStatus",
                sorter: "string",
                headerSort: true,
                responsive: 0,
                formatter: function(cell) {
                    const status = (cell.getValue() || '').toLowerCase();
                    let badgeClass = 'tabulator-badge badge-secondary';
                    let icon = 'bi-hourglass';
                    let statusText = cell.getValue() || '';
                    
                    if (status.includes('delivered')) {
                        badgeClass = 'tabulator-badge badge-success';
                        icon = 'bi-check-circle-fill';
                        statusText = translations.delivered || statusText;
                    } else if (status.includes('approved')) {
                        badgeClass = 'tabulator-badge badge-success';
                        icon = 'bi-check-circle';
                        statusText = translations.approved || statusText;
                    } else if (status.includes('shipped')) {
                        badgeClass = 'tabulator-badge badge-primary';
                        icon = 'bi-truck';
                        statusText = translations.shipped || statusText;
                    } else if (status.includes('cancel')) {
                        badgeClass = 'tabulator-badge badge-danger';
                        icon = 'bi-x-circle';
                        statusText = translations.cancelled || statusText;
                    } else if (status.includes('process')) {
                        badgeClass = 'tabulator-badge badge-info';
                        icon = 'bi-gear';
                        statusText = translations.processing || statusText;
                    } else if (status.includes('pending')) {
                        badgeClass = 'tabulator-badge badge-warning';
                        icon = 'bi-clock';
                        statusText = translations.pending || statusText;
                    }
                    
                    return `<span class="${badgeClass}"><i class="bi ${icon}"></i> ${statusText}</span>`;
                }
            },
            {
                title: translations.paymentStatus || "Payment Status",
                field: "paymentStatus",
                sorter: "string",
                headerSort: true,
                responsive: 1,
                formatter: function(cell) {
                    const paymentStatus = (cell.getValue() || '').toLowerCase();
                    let badgeClass = 'tabulator-badge payment-pending';
                    let icon = 'bi-hourglass';
                    let label = cell.getValue() || (translations.pending || 'Pending');
                    
                    if (paymentStatus.includes('paid')) {
                        badgeClass = 'tabulator-badge payment-paid';
                        icon = 'bi-check-circle-fill';
                        label = translations.paid || label;
                    } else if (paymentStatus.includes('pending')) {
                        badgeClass = 'tabulator-badge payment-pending';
                        icon = 'bi-clock';
                        label = translations.pending || label;
                    } else if (paymentStatus.includes('approved')) {
                        badgeClass = 'tabulator-badge payment-approved';
                        icon = 'bi-check-circle';
                        label = translations.approved || label;
                    } else if (paymentStatus.includes('refund')) {
                        badgeClass = 'tabulator-badge payment-refunded';
                        icon = 'bi-arrow-counterclockwise';
                        label = translations.refunded || label;
                    } else if (paymentStatus.includes('reject')) {
                        badgeClass = 'tabulator-badge payment-rejected';
                        icon = 'bi-x-circle';
                        label = translations.rejected || label;
                    }
                    
                    return `<span class="${badgeClass}"><i class="bi ${icon}"></i> ${label}</span>`;
                }
            },
            {
                title: translations.actions || "Actions",
                field: "actions",
                width: 120,
                headerSort: false,
                formatter: function(cell) {
                    const orderId = cell.getRow().getData().id;
                    const detailsText = translations.details || 'Details';
                    
                    return `
                            <a href="/Admin/Order/Details/${orderId}" 
                           class="tabulator-btn btn-details"
                               title="${detailsText}">
                            <i class="bi bi-eye"></i> ${detailsText}
                        </a>
                    `;
                },
                hozAlign: "center"
            }
        ],
        
        // Initial sort
        initialSort: [
            {column: "id", dir: "desc"}
        ],
        
        // Header filter (search)
        headerFilterPlaceholder: translations.search || "Search...",
        
        // Locale
        langs: {
            "default": {
                "pagination": {
                    "page_size": "Orders per page:",
                    "first": "First",
                    "first_title": "First Page",
                    "last": "Last",
                    "last_title": "Last Page",
                    "prev": "Previous",
                    "prev_title": "Previous Page",
                    "next": "Next",
                    "next_title": "Next Page",
                    "counter": {
                        "showing": "Showing",
                        "of": "of",
                        "rows": "orders"
                    }
                }
            }
        },
        
        // Placeholder for empty table
        placeholder: translations.noOrdersFound || "No orders found",
        
        // Loading message
        ajaxLoader: true,
        ajaxLoaderLoading: "<div class='tabulator-loader'><i class='bi bi-arrow-repeat spin'></i> Loading orders...</div>",
        
        // Row click
        rowClick: function(e, row) {
            // Don't navigate if clicking on the details button
            if (!e.target.closest('.tabulator-btn')) {
                const orderId = row.getData().id;
                window.location.href = `/Admin/Order/Details/${orderId}`;
            }
        }
    });
    
        // Add header filter (search box)
        orderTable.on("tableBuilt", function(){
            orderTable.setHeaderFilterValue("name", ""); // Initialize header filters
        });
        
        orderTable.on("dataLoaded", function(data){
        });
        
        
    } catch (error) {
        console.error('Error creating Tabulator instance:', error);
        alert('Error initializing table: ' + error.message);
    }
}

// Update order statistics
function updateOrderStatistics(stats) {
    if (stats && typeof stats === 'object') {
        document.getElementById('statAll').textContent = stats.all || stats.total || 0;
        document.getElementById('statPending').textContent = stats.pending || 0;
        document.getElementById('statApproved').textContent = stats.approved || 0;
        document.getElementById('statProcessing').textContent = stats.processing || 0;
        document.getElementById('statShipped').textContent = stats.shipped || 0;
        document.getElementById('statDelivered').textContent = stats.delivered || 0;
    }
}

// Filter orders by status
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
    
    // Reload table with new filter
    if (orderTable) {
        // Destroy and reinitialize with new status
        orderTable.destroy();
        initializeOrderTable(status);
    }
}

// Custom CSS for Tabulator
const style = document.createElement('style');
style.textContent = `
    /* Tabulator Custom Styling */
    .tabulator {
        border: none;
        border-radius: 12px;
        overflow: hidden;
        box-shadow: 0 4px 15px rgba(0,0,0,0.1);
        font-family: inherit;
        background: white;
    }
    
    .tabulator .tabulator-header {
        background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        color: white;
        border: none;
        font-weight: 600;
    }
    
    .tabulator .tabulator-header .tabulator-col {
        background: transparent;
        border-right: 1px solid rgba(255,255,255,0.2);
    }
    
    .tabulator .tabulator-header .tabulator-col:last-child {
        border-right: none;
    }
    
    .tabulator .tabulator-header .tabulator-col .tabulator-col-content {
        padding: 12px 8px;
    }
    
    .tabulator .tabulator-header .tabulator-col .tabulator-col-title {
        color: white;
        font-weight: 600;
    }
    
    .tabulator .tabulator-header .tabulator-col.tabulator-sortable:hover {
        background: rgba(255,255,255,0.1);
    }
    
    .tabulator .tabulator-header .tabulator-col .tabulator-arrow {
        border-bottom-color: white;
        border-top-color: white;
    }
    
    .tabulator .tabulator-tableHolder {
        background: white;
    }
    
    .tabulator .tabulator-row {
        border-bottom: 1px solid #e9ecef;
        transition: all 0.2s ease;
    }
    
    .tabulator .tabulator-row:hover {
        background: #f8f9fa;
        cursor: pointer;
    }
    
    .tabulator .tabulator-row .tabulator-cell {
        border-right: 1px solid #f0f0f0;
        padding: 12px 8px;
    }
    
    .tabulator .tabulator-row.tabulator-row-even {
        background: #fafbfc;
    }
    
    .tabulator .tabulator-row.tabulator-row-even:hover {
        background: #f8f9fa;
    }
    
    /* Pagination */
    .tabulator .tabulator-footer {
        background: #f8f9fa;
        border: none;
        padding: 12px;
        border-radius: 0 0 12px 12px;
    }
    
    .tabulator .tabulator-footer .tabulator-page {
        background: white;
        border: 1px solid #dee2e6;
        color: #667eea;
        margin: 0 2px;
        border-radius: 6px;
        padding: 6px 12px;
        transition: all 0.2s ease;
    }
    
    .tabulator .tabulator-footer .tabulator-page:hover {
        background: #667eea;
        color: white;
        transform: translateY(-2px);
    }
    
    .tabulator .tabulator-footer .tabulator-page.active {
        background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        color: white;
        font-weight: 600;
        box-shadow: 0 2px 8px rgba(102, 126, 234, 0.3);
    }
    
    .tabulator .tabulator-footer .tabulator-page[disabled] {
        opacity: 0.5;
        cursor: not-allowed;
    }
    
    /* Badges */
    .tabulator-badge {
        display: inline-flex;
        align-items: center;
        gap: 0.375rem;
        padding: 0.375rem 0.75rem;
        border-radius: 20px;
        font-size: 0.875rem;
        font-weight: 600;
        white-space: nowrap;
    }
    
    .tabulator-badge.badge-success {
        background: linear-gradient(135deg, #10b981 0%, #059669 100%);
        color: white;
    }
    
    .tabulator-badge.badge-primary {
        background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%);
        color: white;
    }
    
    .tabulator-badge.badge-info {
        background: linear-gradient(135deg, #06b6d4 0%, #0891b2 100%);
        color: white;
    }
    
    .tabulator-badge.badge-warning {
        background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%);
        color: white;
    }
    
    .tabulator-badge.badge-danger {
        background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%);
        color: white;
    }
    
    .tabulator-badge.badge-secondary {
        background: linear-gradient(135deg, #6b7280 0%, #4b5563 100%);
        color: white;
    }
    
    .tabulator-badge.payment-paid {
        background: #d1fae5;
        color: #065f46;
        border: 2px solid #10b981;
    }
    
    .tabulator-badge.payment-pending {
        background: #fef3c7;
        color: #92400e;
        border: 2px solid #f59e0b;
    }
    
    .tabulator-badge.payment-approved {
        background: #dbeafe;
        color: #1e40af;
        border: 2px solid #3b82f6;
    }
    
    .tabulator-badge.payment-rejected {
        background: #fee2e2;
        color: #991b1b;
        border: 2px solid #ef4444;
    }
    
    .tabulator-badge.payment-refunded {
        background: #e0e7ff;
        color: #4338ca;
        border: 2px solid #6366f1;
    }
    
    /* Buttons */
    .tabulator-btn {
        display: inline-flex;
        align-items: center;
        gap: 0.375rem;
        padding: 0.5rem 1rem;
        border-radius: 8px;
        font-size: 0.875rem;
        font-weight: 600;
        text-decoration: none;
        transition: all 0.2s ease;
        border: none;
    }
    
    .tabulator-btn.btn-details {
        background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        color: white;
    }
    
    .tabulator-btn.btn-details:hover {
        transform: translateY(-2px);
        box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
        color: white;
    }
    
    /* Loading Spinner */
    .tabulator-loader {
        padding: 2rem;
        text-align: center;
        font-size: 1.25rem;
        color: #667eea;
    }
    
    .tabulator-loader i.spin {
        animation: spin 1s linear infinite;
        font-size: 2rem;
        display: block;
        margin-bottom: 0.5rem;
    }
    
    @keyframes spin {
        from { transform: rotate(0deg); }
        to { transform: rotate(360deg); }
    }
    
    /* Mobile Responsive */
    @media (max-width: 768px) {
        .tabulator {
            font-size: 0.875rem;
        }
        
        .tabulator .tabulator-cell {
            padding: 8px 4px;
        }
        
        .tabulator-badge {
            font-size: 0.75rem;
            padding: 0.25rem 0.5rem;
        }
        
        .tabulator-btn {
            padding: 0.375rem 0.75rem;
            font-size: 0.75rem;
        }
        
        .tabulator .tabulator-footer {
            font-size: 0.8rem;
        }
        
        .tabulator .tabulator-footer .tabulator-page {
            padding: 4px 8px;
            font-size: 0.75rem;
        }
    }
    
    /* RTL Support */
    [dir="rtl"] .tabulator .tabulator-header .tabulator-col {
        border-right: none;
        border-left: 1px solid rgba(255,255,255,0.2);
    }
    
    [dir="rtl"] .tabulator .tabulator-header .tabulator-col:first-child {
        border-left: none;
    }
`;
document.head.appendChild(style);
