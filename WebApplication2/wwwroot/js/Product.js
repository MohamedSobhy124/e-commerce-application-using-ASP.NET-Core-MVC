var dataTable;
$(document).ready(function () {
    loadDataTable();
});


function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        "ajax": { url: '/Admin/Product/getall' },
        "columns": [
            { data: 'id', "width": "15%" },
            { data: 'title',"width":"15%" },
            { data: 'isbn', "width": "10%" },
            { data: 'price', "width": "10%" },
            { data: 'author', "width": "10%" }, 
            { data: 'categry.name', "width": "10%" },
            {
                data: 'id',
                "render": function (data) {
                    return `
                    <div class=" btn-group" role="group">
                    <a href="/Admin/Product/UpSert?id=${data}" class="btn btn-dark"><i class="bi bi-pencil-square"></i>Edit</a>

                    <a Onclick="Delete('/Admin/Product/Delete?id=${data}')" class="btn btn-danger "><i class="bi bi-trash-fill"></i>Delete</a>
                    </div>`
                },
                "width": "25%"
            }

        ]
    });
};
function Delete(url) {
    Swal.fire({
        title: 'Are you sure?',
        text: "You won't be able to revert this!",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#3085d6',
        cancelButtonColor: '#d33',
        confirmButtonText: 'Yes, delete it!'
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: url,
                    type: 'DELETE',
                Success: function (data) {
                    
                    toastr.Success(data.message);
                    
                    }
            })
        }
    }).then(function () {
        location.reload();
    });

}

