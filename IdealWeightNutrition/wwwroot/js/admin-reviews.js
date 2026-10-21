let reviewsTable;
let currentFilter = 'all';
let allReviewsData = [];
let verifiedOnlyFilter = false;
const t = window.reviewAdminTranslations || {};

function clearVerifiedFilter() {
    verifiedOnlyFilter = false;
    $.fn.dataTable.ext.search = $.fn.dataTable.ext.search.filter(function (fn) {
        return !fn.isVerifiedReviewFilter;
    });
}

function escapeHtml(text) {
    if (!text) return '';
    return text
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#039;');
}

function updateStatistics(data) {
    const total = data.length;
    const pending = data.filter(r => !r.isApproved).length;
    const approved = data.filter(r => r.isApproved).length;
    const verified = data.filter(r => r.isVerifiedPurchase).length;

    $('#totalReviews').text(total);
    $('#pendingReviews').text(pending);
    $('#approvedReviews').text(approved);
    $('#verifiedReviews').text(verified);
}

function setActiveReviewFilter(filter, sourceButton) {
    currentFilter = filter;
    if (filter !== 'verified') {
        clearVerifiedFilter();
    }

    document.querySelectorAll('#reviewStatusFilters .filter-tab').forEach(tab => {
        tab.classList.remove('active');
        tab.setAttribute('aria-pressed', 'false');
    });

    document.querySelectorAll('#reviewStatsContainer .order-stat-card').forEach(card => {
        card.classList.remove('is-active');
        card.setAttribute('aria-pressed', 'false');
    });

    if (sourceButton) {
        sourceButton.classList.add('active', 'is-active');
        sourceButton.setAttribute('aria-pressed', 'true');
    }

    const tabMatch = document.querySelector('#reviewStatusFilters .filter-tab[data-filter="' + filter + '"]');
    if (tabMatch && tabMatch !== sourceButton) {
        tabMatch.classList.add('active');
        tabMatch.setAttribute('aria-pressed', 'true');
    }

    const statMatch = document.querySelector('#reviewStatsContainer .order-stat-card[data-review-filter="' + filter + '"]');
    if (statMatch && statMatch !== sourceButton) {
        statMatch.classList.add('is-active');
        statMatch.setAttribute('aria-pressed', 'true');
    }

    loadReviewsTable(filter);
}

function loadReviewsTable(status) {
    if (reviewsTable) {
        reviewsTable.destroy();
    }

    const url = status === 'all'
        ? '/Admin/Review/GetAll'
        : `/Admin/Review/GetAll?status=${status}`;

    reviewsTable = $('#tblReviews').DataTable({
        ajax: {
            url: url,
            type: 'GET',
            dataSrc: function (json) {
                allReviewsData = json.data || [];
                updateStatistics(allReviewsData);
                return allReviewsData;
            }
        },
        columns: [
            {
                data: null,
                width: '8%',
                render: function (data, type, row) {
                    const isProduct = row.reviewType === 'Product';
                    return isProduct
                        ? '<span class="badge bg-primary"><i class="bi bi-box-seam me-1"></i>' + (t.product || 'Product') + '</span>'
                        : '<span class="badge bg-info"><i class="bi bi-briefcase me-1"></i>' + (t.service || 'Service') + '</span>';
                }
            },
            {
                data: null,
                width: '18%',
                render: function (data, type, row) {
                    return '<strong>' + escapeHtml(row.itemName || 'N/A') + '</strong>';
                }
            },
            {
                data: null,
                width: '13%',
                render: function (data, type, row) {
                    const verified = row.isVerifiedPurchase
                        ? '<br><small class="badge bg-success mt-1"><i class="bi bi-patch-check me-1"></i>' + (t.verified || 'Verified') + '</small>'
                        : '';
                    return '<div>' + escapeHtml(row.userName || t.anonymous || 'Anonymous') + verified + '</div>';
                }
            },
            {
                data: null,
                width: '10%',
                render: function (data, type, row) {
                    const stars = '★'.repeat(row.rating || 0) + '☆'.repeat(5 - (row.rating || 0));
                    return '<div class="review-rating-stars">' + stars + '</div>' +
                        '<div class="review-rating-value">' + (row.rating || 0) + '/5</div>';
                }
            },
            {
                data: null,
                width: '25%',
                render: function (data, type, row) {
                    const comment = row.comment || t.noComment || 'No comment';
                    const hasMore = comment.length > 100;
                    return '<div class="comment-cell">' +
                        '<div class="comment-preview">' + escapeHtml(comment) + '</div>' +
                        (hasMore ? '<button type="button" class="btn btn-sm btn-link p-0 mt-1" data-action="view-comment" data-item="' + escapeHtml(row.itemName || '') + '" data-comment="' + escapeHtml(row.fullComment || comment) + '">' +
                            '<i class="bi bi-eye me-1"></i>' + (t.viewFull || 'View full') + '</button>' : '') +
                        '</div>';
                }
            },
            {
                data: 'createdAtTimestamp',
                width: '11%',
                render: function (data, type, row) {
                    if (type === 'sort' || type === 'type') {
                        return data || 0;
                    }
                    return '<div class="review-date-cell"><i class="bi bi-calendar3 me-1"></i>' + escapeHtml(row.createdAt || 'N/A') + '</div>';
                }
            },
            {
                data: null,
                width: '9%',
                render: function (data, type, row) {
                    if (row.isApproved) {
                        return '<span class="badge bg-success"><i class="bi bi-check-circle me-1"></i>' + (t.approved || 'Approved') + '</span>';
                    }
                    return '<span class="badge bg-warning text-dark"><i class="bi bi-clock me-1"></i>' + (t.pending || 'Pending') + '</span>';
                }
            },
            {
                data: null,
                width: '11%',
                orderable: false,
                render: function (data, type, row) {
                    const toggleLabel = row.isApproved ? (t.hide || 'Hide') : (t.approve || 'Approve');
                    const toggleClass = row.isApproved ? 'btn-warning' : 'btn-success';
                    const toggleIcon = row.isApproved ? 'bi-eye-slash' : 'bi-check-circle';
                    const itemName = escapeHtml(row.itemName || '');

                    return '<div class="admin-row-actions flex-column">' +
                        '<button type="button" class="btn btn-sm ' + toggleClass + '" data-action="toggle-approval" data-id="' + row.id + '" aria-label="' + toggleLabel + '">' +
                        '<i class="bi ' + toggleIcon + ' me-1"></i>' + toggleLabel + '</button>' +
                        '<button type="button" class="btn btn-sm btn-danger" data-action="delete-review" data-id="' + row.id + '" data-item="' + itemName + '" aria-label="' + (t.delete || 'Delete') + '">' +
                        '<i class="bi bi-trash me-1"></i>' + (t.delete || 'Delete') + '</button>' +
                        '</div>';
                }
            }
        ],
        order: [[5, 'desc']],
        ordering: true,
        language: {
            emptyTable: t.emptyTable || 'No reviews found'
        },
        pageLength: 10,
        lengthMenu: [[10, 25, 50, 100], [10, 25, 50, 100]],
        responsive: true
    });
}

function toggleApproval(reviewId) {
    $.ajax({
        url: '/Admin/Review/ToggleApproval',
        type: 'POST',
        data: { id: reviewId },
        success: function (response) {
            if (response.success) {
                toastr.success(response.message);
                reviewsTable.ajax.reload(null, false);
            } else {
                toastr.error(response.message);
            }
        },
        error: function () {
            toastr.error(t.errorToggle || 'Error toggling approval status');
        }
    });
}

function deleteReview(reviewId, itemName) {
    const title = (t.deleteConfirmTitle || 'Delete review?').replace('{0}', itemName);
    const text = (t.deleteConfirmText || 'This action cannot be undone.').replace('{0}', itemName);

    Swal.fire({
        title: title,
        text: text,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#d33',
        cancelButtonColor: '#6c757d',
        confirmButtonText: t.confirmDelete || 'Yes, delete it!',
        cancelButtonText: t.cancel || 'Cancel'
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: '/Admin/Review/Delete',
                type: 'POST',
                data: { id: reviewId },
                success: function (response) {
                    if (response.success) {
                        toastr.success(response.message);
                        reviewsTable.ajax.reload(null, false);
                    } else {
                        toastr.error(response.message);
                    }
                },
                error: function () {
                    toastr.error(t.errorDelete || 'Error deleting review');
                }
            });
        }
    });
}

function viewFullComment(comment, itemName) {
    Swal.fire({
        title: itemName,
        html: '<div class="review-comment-modal">' + comment + '</div>',
        width: '600px',
        showCloseButton: true,
        confirmButtonText: t.close || 'Close',
        confirmButtonColor: '#6c757d'
    });
}

function exportReviews() {
    Swal.fire({
        title: t.exportTitle || 'Export reviews',
        text: t.exportPreparing || 'Preparing your export...',
        icon: 'info',
        timer: 2000,
        timerProgressBar: true,
        showConfirmButton: false,
        didOpen: () => {
            Swal.showLoading();
        }
    });

    setTimeout(() => {
        Swal.fire({
            title: t.exportComplete || 'Export complete',
            text: t.exportSuccess || 'Your reviews have been exported successfully.',
            icon: 'success',
            confirmButtonText: t.close || 'OK',
            confirmButtonColor: '#7BC043'
        });
    }, 2000);
}

$(document).ready(function () {
    loadReviewsTable(currentFilter);

    document.querySelectorAll('#reviewStatusFilters .filter-tab').forEach(btn => {
        btn.addEventListener('click', function () {
            setActiveReviewFilter(btn.getAttribute('data-filter'), btn);
        });
    });

    document.querySelectorAll('#reviewStatsContainer .order-stat-card').forEach(card => {
        card.addEventListener('click', function () {
            const filter = card.getAttribute('data-review-filter');
            if (filter === 'verified') {
                setActiveReviewFilter('all', card);
                clearVerifiedFilter();
                verifiedOnlyFilter = true;
                const verifiedFilterFn = function (settings, _data, dataIndex) {
                    if (!verifiedOnlyFilter || settings.nTable.id !== 'tblReviews') {
                        return true;
                    }
                    const row = reviewsTable.row(dataIndex).data();
                    return row && row.isVerifiedPurchase;
                };
                verifiedFilterFn.isVerifiedReviewFilter = true;
                $.fn.dataTable.ext.search.push(verifiedFilterFn);
                if (reviewsTable) {
                    reviewsTable.draw();
                }
                return;
            }
            setActiveReviewFilter(filter, card);
        });
    });

    document.getElementById('exportReviewsBtn')?.addEventListener('click', exportReviews);

    $('#tblReviews').on('click', '[data-action="toggle-approval"]', function () {
        toggleApproval(parseInt(this.getAttribute('data-id'), 10));
    });

    $('#tblReviews').on('click', '[data-action="delete-review"]', function () {
        deleteReview(parseInt(this.getAttribute('data-id'), 10), this.getAttribute('data-item') || '');
    });

    $('#tblReviews').on('click', '[data-action="view-comment"]', function () {
        viewFullComment(this.getAttribute('data-comment') || '', this.getAttribute('data-item') || '');
    });
});
