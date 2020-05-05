$(document).ready(function () {

    $('#historicalTasks').DataTable({
        "pageLength": 10,
        'sDom': 'ltipr',
        "lengthMenu": [5, 10, 25, 50],
        "scrollX": true,
        "order": [[9, 'desc']],
        "ordering": true
    });

    $("#frmSearchForHistoricalTasks").on("submit",
        function() {

            $("#modalWaitHistoricalTasks").modal("show");
        });

});