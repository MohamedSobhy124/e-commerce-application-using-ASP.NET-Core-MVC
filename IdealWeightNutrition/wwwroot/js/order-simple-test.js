// SIMPLE TEST VERSION - Use this to verify Tabulator works

document.addEventListener('DOMContentLoaded', function() {
    
    setTimeout(function() {
        
        if (typeof Tabulator === 'undefined') {
            console.error('❌ Tabulator NOT loaded');
            document.getElementById('orderTable').innerHTML = '<div class="alert alert-danger">Tabulator library failed to load from CDN. Check your internet connection.</div>';
            return;
        }
        
        
        try {
            var table = new Tabulator("#orderTable", {
                height: "500px",
                layout: "fitColumns",
                placeholder: "No Data",
                ajaxURL: "/Admin/Order/GetAll?status=&start=0&length=25",
                ajaxResponse: function(url, params, response) {
                    return {
                        data: response.data || []
                    };
                },
                columns: [
                    {title: "ID", field: "id", width: 80},
                    {title: "Name", field: "name"},
                    {title: "Email", field: "email"},
                    {title: "Phone", field: "phoneNumber"},
                    {title: "Total", field: "orderTotal"},
                    {title: "Status", field: "orderStatus"},
                    {title: "Actions", formatter: function(cell) {
                        var id = cell.getRow().getData().id;
                        return '<a href="/Admin/Order/Details/' + id + '" class="btn btn-sm btn-primary">Details</a>';
                    }}
                ]
            });
            
            table.on("tableBuilt", function(){
            });
            
            table.on("dataLoaded", function(data){
            });
            
            table.on("ajaxError", function(error){
                console.error('❌ AJAX Error:', error);
            });
            
        } catch (error) {
            console.error('❌ Error creating table:', error);
            document.getElementById('orderTable').innerHTML = '<div class="alert alert-danger">Error: ' + error.message + '</div>';
        }
    }, 1000);
});

