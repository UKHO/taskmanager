$(document).ready(function () {

    $.fn.dataTable.moment('DD/MM/YYYY');

    $('#historicalTasks').DataTable({
        "pageLength": 10,
        'sDom': 'ltipr',
        "lengthMenu": [5, 10, 25, 50],
        "scrollX": true,
        "order": [[9, 'desc']],
        "ordering": true
    });

    $("#frmSearchForHistoricalTasks").on("submit",
        function () {

            $("#modalWaitHistoricalTasks").modal("show");
        });

    attachClearHistoricalTasksSearchButtonHandler();
});

function attachClearHistoricalTasksSearchButtonHandler() {
    $("#btnClearHistoricalTasksSearch").click(function () {
        clearHistoricalTasksSearch();
    });
}

function clearHistoricalTasksSearch() {
    $(".historicalTaskSearchField").each(function () {
        $(this).val("");
    });

    $("#SearchParameters_ProcessId").focus();
}