var dataTable;
var currentReturnStatus = 'all';
var t = window.returnRequestTranslations || {};

function statusChipClass(status) {
    switch (status) {
        case 'Approved':
        case 'Completed':
            return 'success';
        case 'Rejected':
            return 'danger';
        case 'Processing':
            return 'info';
        case 'Pending':
            return 'warning';
        default:
            return 'secondary';
    }
}

function renderStatusChip(status) {
    var label = status;
    if (status === 'Pending') label = t.statusPending || status;
    else if (status === 'Approved') label = t.statusApproved || status;
    else if (status === 'Rejected') label = t.statusRejected || status;
    else if (status === 'Processing') label = t.statusProcessing || status;
    else if (status === 'Completed') label = t.statusCompleted || status;
    else if (status === 'Cancelled') label = t.statusCancelled || status;

    return '<span class="badge bg-' + statusChipClass(status) + ' return-status-chip">' + label + '</span>';
}

function loadDataTable() {
    if (dataTable) {
        dataTable.destroy();
        $('#returnRequestTable tbody').empty();
    }

    var url = currentReturnStatus === 'all'
        ? '/Admin/ReturnRequest/GetAll'
        : '/Admin/ReturnRequest/GetAll?status=' + encodeURIComponent(currentReturnStatus);

    dataTable = $('#returnRequestTable').DataTable({
        ajax: {
            url: url,
            type: 'GET',
            dataSrc: 'data'
        },
        columns: [
            { data: 'id', width: '5%' },
            { data: 'orderId', width: '8%' },
            { data: 'customerName', width: '15%' },
            { data: 'customerEmail', width: '15%' },
            {
                data: 'status',
                width: '12%',
                render: function (data) {
                    return renderStatusChip(data);
                }
            },
            {
                data: 'requestDate',
                width: '12%',
                type: 'date'
            },
            {
                data: 'refundAmount',
                width: '10%',
                render: function (data) {
                    return parseFloat(data || 0).toFixed(2);
                }
            },
            { data: 'reason', width: '20%' },
            {
                data: 'id',
                width: '10%',
                orderable: false,
                className: 'text-center',
                render: function (data) {
                    var viewLabel = t.view || 'View';
                    return '<div class="admin-row-actions">' +
                        '<a href="/Admin/ReturnRequest/Details/' + data + '" class="btn btn-sm btn-primary" aria-label="' + viewLabel + '">' +
                        '<i class="bi bi-eye" aria-hidden="true"></i>' +
                        '</a></div>';
                }
            }
        ],
        order: [[5, 'desc']],
        language: {
            emptyTable: t.emptyTable || 'No return requests found'
        }
    });
}

function setReturnStatusFilter(status, button) {
    currentReturnStatus = status;

    document.querySelectorAll('#returnStatusFilters .filter-tab').forEach(function (tab) {
        tab.classList.remove('active');
        tab.setAttribute('aria-pressed', 'false');
    });

    if (button) {
        button.classList.add('active');
        button.setAttribute('aria-pressed', 'true');
    }

    loadDataTable();
}

$(document).ready(function () {
    loadDataTable();

    document.querySelectorAll('#returnStatusFilters .filter-tab').forEach(function (btn) {
        btn.addEventListener('click', function () {
            setReturnStatusFilter(btn.getAttribute('data-status'), btn);
        });
    });
});
