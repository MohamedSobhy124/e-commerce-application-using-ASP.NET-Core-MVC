var dataTable;

$(document).ready(function () {
    loadDataTable();
});

function loadDataTable() {
    dataTable = $('#returnRequestTable').DataTable({
        "ajax": {
            "url": "/Admin/ReturnRequest/GetAll",
            "type": "GET",
            "datatype": "json"
        },
        "columns": [
            { "data": "id", "width": "5%" },
            { "data": "orderId", "width": "8%" },
            { "data": "customerName", "width": "15%" },
            { "data": "customerEmail", "width": "15%" },
            {
                "data": "status",
                "width": "12%",
                "render": function (data) {
                    var badgeClass = "secondary";
                    if (data === "Approved") badgeClass = "success";
                    else if (data === "Rejected") badgeClass = "danger";
                    else if (data === "Processing") badgeClass = "info";
                    else if (data === "Completed") badgeClass = "success";
                    else if (data === "Cancelled") badgeClass = "secondary";
                    
                    return `<span class="badge bg-${badgeClass}">${data}</span>`;
                }
            },
            { 
                "data": "requestDate", 
                "width": "12%",
                "type": "date",
                "render": function (data) {
                    return data; // Display formatted date
                }
            },
            {
                "data": "refundAmount",
                "width": "10%",
                "render": function (data) {
                    return "AED " + parseFloat(data).toFixed(2).replace('.', ',');
                }
            },
            { "data": "reason", "width": "20%" },
            {
                "data": "id",
                "width": "13%",
                "orderable": false,
                "render": function (data) {
                    return `
                        <div class="btn-group" role="group">
                            <a href="/Admin/ReturnRequest/Details/${data}" class="btn btn-primary btn-sm">
                                <i class="bi bi-eye"></i> View
                            </a>
                        </div>
                    `;
                }
            }
        ],
        "order": [[5, "desc"]], // Sort by requestDate column (index 5) descending (latest first)
        "language": {
            "emptyTable": "No return requests found"
        },
        "width": "100%"
    });
}

