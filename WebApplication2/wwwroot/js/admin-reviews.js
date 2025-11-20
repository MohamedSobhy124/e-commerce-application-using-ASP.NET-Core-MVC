// Admin Reviews Management DataTable
let reviewsTable;
let currentFilter = 'all';

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
            dataSrc: "data"
        },
        "columns": [
            { 
                data: null,
                "width": "15%",
                "render": function (data, type, row) {
                    return row.productName || 'N/A';
                }
            },
            { 
                data: null,
                "width": "12%",
                "render": function (data, type, row) {
                    const verified = row.isVerifiedPurchase 
                        ? '<br><small class="badge bg-success">✓ Verified</small>' 
                        : '';
                    return (row.userName || 'N/A') + verified;
                }
            },
            { 
                data: null,
                "width": "10%",
                "render": function (data, type, row) {
                    const stars = '★'.repeat(row.rating || 0) + '☆'.repeat(5 - (row.rating || 0));
                    return `<span style="color: #FFB800; font-size: 1.2rem;">${stars}</span>`;
                }
            },
            { 
                data: null,
                "width": "30%",
                "render": function (data, type, row) {
                    return `<div title="${row.fullComment || ''}">${row.comment || 'N/A'}</div>`;
                }
            },
            { 
                data: null,
                "width": "12%",
                "render": function (data, type, row) {
                    return row.createdAt || 'N/A';
                }
            },
            { 
                data: null,
                "width": "10%",
                "render": function (data, type, row) {
                    if (row.isApproved) {
                        return '<span class="badge badge-success"><i class="bi bi-check-circle me-1"></i>Approved</span>';
                    } else {
                        return '<span class="badge badge-warning"><i class="bi bi-clock me-1"></i>Pending</span>';
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
                        <div class="btn-group" role="group">
                            <button onclick="toggleApproval(${row.id})" 
                                    class="btn btn-sm ${approveClass}" 
                                    title="${approveText}">
                                <i class="bi ${approveIcon}"></i>
                            </button>
                            <button onclick="deleteReview(${row.id})" 
                                    class="btn btn-sm btn-danger" 
                                    title="Delete">
                                <i class="bi bi-trash"></i>
                            </button>
                        </div>
                    `;
                },
                "width": "11%"
            }
        ],
        "order": [[4, "desc"]], // Sort by date descending
        "language": {
            "emptyTable": "No reviews found"
        },
        "responsive": true
    });
}

function filterReviews(status) {
    currentFilter = status;
    
    // Update active tab
    document.querySelectorAll('.filter-tab').forEach(tab => {
        tab.classList.remove('active');
    });
    event.target.closest('.filter-tab').classList.add('active');
    
    // Reload table with filter
    loadReviewsTable(status);
}

function toggleApproval(reviewId) {
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

function deleteReview(reviewId) {
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

