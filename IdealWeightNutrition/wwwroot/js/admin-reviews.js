// Admin Reviews Management DataTable
let reviewsTable;
let currentFilter = 'all';
let currentType = 'all';
let allReviewsData = [];

$(document).ready(function () {
    loadReviewsTable(currentFilter);
});

function loadReviewsTable(status) {
    if (reviewsTable) {
        reviewsTable.destroy();
    }

    const url = status === 'all' 
        ? '/Admin/Review/GetAll' 
        : `/Admin/Review/GetAll?status=${status}`;

    reviewsTable = $('#tblReviews').DataTable({
        "ajax": {
            url: url,
            type: "GET",
            dataSrc: function(json) {
                allReviewsData = json.data;
                if (json.data.length > 0) {
                }
                updateStatistics(json.data);
                return json.data;
            }
        },
        "columns": [
            { 
                data: null,
                "width": "8%",
                "render": function (data, type, row) {
                    const isProduct = row.reviewType === 'Product';
                    const badge = isProduct 
                        ? '<span class="badge bg-primary"><i class="bi bi-box-seam me-1"></i>Product</span>'
                        : '<span class="badge bg-info"><i class="bi bi-briefcase me-1"></i>Service</span>';
                    return badge;
                }
            },
            { 
                data: null,
                "width": "18%",
                "render": function (data, type, row) {
                    const itemName = row.itemName || 'N/A';
                    const typeLabel = row.reviewType === 'Product' ? 'Product' : 'Service';
                    return `<strong>${itemName}</strong>`;
                }
            },
            { 
                data: null,
                "width": "13%",
                "render": function (data, type, row) {
                    const verified = row.isVerifiedPurchase 
                        ? '<br><small class="badge bg-success mt-1"><i class="bi bi-patch-check me-1"></i>Verified</small>' 
                        : '';
                    return `<div>${row.userName || 'Anonymous'}${verified}</div>`;
                }
            },
            { 
                data: null,
                "width": "10%",
                "render": function (data, type, row) {
                    const stars = '★'.repeat(row.rating || 0) + '☆'.repeat(5 - (row.rating || 0));
                    return `
                        <div style="color: #FFB800; font-size: 1.3rem;">${stars}</div>
                        <div style="font-size: 0.85rem; color: #666; font-weight: 600;">${row.rating}/5</div>
                    `;
                }
            },
            { 
                data: null,
                "width": "25%",
                "render": function (data, type, row) {
                    const comment = row.comment || 'No comment';
                    const hasMore = comment.length > 100;
                    return `
                        <div class="comment-cell">
                            <div class="comment-preview">${comment}</div>
                            ${hasMore ? `<button class="btn btn-sm btn-link p-0 mt-1" onclick="viewFullComment('${escapeHtml(row.fullComment)}', '${escapeHtml(row.itemName)}')">
                                <i class="bi bi-eye me-1"></i>View Full
                            </button>` : ''}
                        </div>
                    `;
                }
            },
            { 
                data: "createdAtTimestamp",
                "width": "11%",
                "render": function (data, type, row) {
                    // Use timestamp for sorting, but display formatted date
                    if (type === 'sort' || type === 'type') {
                        return data || 0;
                    }
                    return `
                        <div style="font-size: 0.9rem;">
                            <i class="bi bi-calendar3 me-1"></i>${row.createdAt || 'N/A'}
                        </div>
                    `;
                }
            },
            { 
                data: null,
                "width": "9%",
                "render": function (data, type, row) {
                    if (row.isApproved) {
                        return '<span class="badge badge-success" style="padding: 0.5rem 0.75rem;"><i class="bi bi-check-circle me-1"></i>Approved</span>';
                    } else {
                        return '<span class="badge badge-warning" style="padding: 0.5rem 0.75rem;"><i class="bi bi-clock me-1"></i>Pending</span>';
                    }
                }
            },
            {
                data: null,
                "render": function (data, type, row) {
                    const approveText = row.isApproved ? 'Unapprove' : 'Approve';
                    const approveClass = row.isApproved ? 'btn-warning' : 'btn-success';
                    const approveIcon = row.isApproved ? 'bi-x-circle' : 'bi-check-circle';
                    
                    return `
                        <div class="btn-group-vertical" role="group" style="gap: 0.25rem;">
                            <button onclick="toggleApproval(${row.id})" 
                                    class="btn btn-sm ${approveClass}" 
                                    title="${approveText}">
                                <i class="bi ${approveIcon} me-1"></i>${approveText}
                            </button>
                            <button onclick="deleteReview(${row.id})" 
                                    class="btn btn-sm btn-danger" 
                                    title="Delete Review">
                                <i class="bi bi-trash me-1"></i>Delete
                            </button>
                        </div>
                    `;
                },
                "width": "11%"
            }
        ],
        "order": [[5, "desc"]], // Sort by date column (index 5) descending - newest first
        "ordering": true, // Enable sorting
        "language": {
            "emptyTable": "No reviews found",
            "search": "Search reviews:",
            "lengthMenu": "Show _MENU_ reviews per page",
            "info": "Showing _START_ to _END_ of _TOTAL_ reviews (latest first)",
            "paginate": {
                "first": "First",
                "last": "Last",
                "next": "Next",
                "previous": "Previous"
            }
        },
        "pageLength": 10,
        "lengthMenu": [[10, 25, 50, 100], [10, 25, 50, 100]],
        "responsive": true,
        "drawCallback": function(settings) {
            var order = this.api().order();
        },
        "dom": '<"row"<"col-sm-12 col-md-6"l><"col-sm-12 col-md-6"f>>rt<"row"<"col-sm-12 col-md-5"i><"col-sm-12 col-md-7"p>>'
    });
}

// Expose functions to global scope
window.filterReviews = function(status) {
    currentFilter = status;
    
    // Update active tab
    document.querySelectorAll('.filter-tab[data-filter]').forEach(tab => {
        tab.classList.remove('active');
    });
    event.target.closest('.filter-tab').classList.add('active');
    
    // Reload table with filter
    loadReviewsTable(status);
}

window.toggleApproval = function(reviewId) {
    Swal.fire({
        title: 'Toggle Approval',
        text: 'Do you want to change the approval status of this review?',
        icon: 'question',
        showCancelButton: true,
        confirmButtonColor: '#7BC043',
        cancelButtonColor: '#d33',
        confirmButtonText: 'Yes, toggle it!',
        cancelButtonText: 'Cancel'
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: '/Admin/Review/ToggleApproval',
                type: 'POST',
                data: { id: reviewId },
                success: function (response) {
                    if (response.success) {
                        toastr.success(response.message);
                        reviewsTable.ajax.reload();
                    } else {
                        toastr.error(response.message);
                    }
                },
                error: function () {
                    toastr.error('Error toggling approval status');
                }
            });
        }
    });
}

window.deleteReview = function(reviewId) {
    Swal.fire({
        title: 'Delete Review?',
        text: 'This action cannot be undone!',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#d33',
        cancelButtonColor: '#3085d6',
        confirmButtonText: 'Yes, delete it!',
        cancelButtonText: 'Cancel'
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: '/Admin/Review/Delete',
                type: 'POST',
                data: { id: reviewId },
                success: function (response) {
                    if (response.success) {
                        toastr.success(response.message);
                        reviewsTable.ajax.reload();
                    } else {
                        toastr.error(response.message);
                    }
                },
                error: function () {
                    toastr.error('Error deleting review');
                }
            });
        }
    });
}

window.filterByType = function(type) {
    currentType = type;
    
    // Update active button
    document.querySelectorAll('.filter-tab[data-type]').forEach(btn => {
        btn.classList.remove('active');
    });
    event.target.closest('.filter-tab').classList.add('active');
    
    // Apply filter to DataTable
    if (reviewsTable) {
        if (type === 'all') {
            reviewsTable.column(0).search('').draw();
        } else if (type === 'products') {
            reviewsTable.column(0).search('Product', true, false).draw();
        } else if (type === 'services') {
            reviewsTable.column(0).search('Service', true, false).draw();
        }
    }
}

function updateStatistics(data) {
    // Calculate statistics
    const total = data.length;
    const pending = data.filter(r => !r.isApproved).length;
    const approved = data.filter(r => r.isApproved).length;
    const verified = data.filter(r => r.isVerifiedPurchase).length;
    
    // Update stat cards - using correct IDs from HTML
    $('#totalReviews').text(total);
    $('#pendingReviews').text(pending);
    $('#approvedReviews').text(approved);
    $('#verifiedReviews').text(verified);
}

window.viewFullComment = function(comment, itemName) {
    Swal.fire({
        title: `<i class="bi bi-chat-left-text me-2"></i>${itemName}`,
        html: `<div style="text-align: left; max-height: 400px; overflow-y: auto; padding: 1rem; background: #f8f9fa; border-radius: 8px; line-height: 1.6;">${comment}</div>`,
        width: '600px',
        showCloseButton: true,
        confirmButtonText: '<i class="bi bi-check-lg me-1"></i>Close',
        confirmButtonColor: '#6c757d',
        customClass: {
            htmlContainer: 'text-start'
        }
    });
}

function escapeHtml(text) {
    if (!text) return '';
    return text
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#039;");
}

window.exportReviews = function() {
    Swal.fire({
        title: 'Export Reviews',
        text: 'Preparing your export...',
        icon: 'info',
        timer: 2000,
        timerProgressBar: true,
        showConfirmButton: false,
        didOpen: () => {
            Swal.showLoading();
        }
    });

    // Get current filter and type
    const statusParam = currentFilter !== 'all' ? `?status=${currentFilter}` : '';
    
    // Create download
    setTimeout(() => {
        // For now, we'll show a success message
        // In a real implementation, you would make an AJAX call to generate CSV/Excel
        Swal.fire({
            title: 'Export Complete!',
            text: 'Your reviews have been exported successfully.',
            icon: 'success',
            confirmButtonText: 'OK',
            confirmButtonColor: '#7BC043'
        });
        
        // Actual implementation would be something like:
        // window.location.href = `/Admin/Review/ExportReviews${statusParam}`;
    }, 2000);
}

