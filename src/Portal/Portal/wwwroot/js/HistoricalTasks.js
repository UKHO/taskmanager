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

            mainHistoricalButtonsEnabled(false);

            populateAndShowWaitPopupForHistoricalSearch();

            $("#modalHistoricalWait").show();
            $("#modalHistoricalPopup").modal("show");
        });

    attachClearHistoricalTasksSearchButtonHandler();
    initialiseHistoricalPopup();

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

function initialiseHistoricalPopup() {
    $("#modalHistoricalWait").hide();
}

function populateAndShowWaitPopupForHistoricalSearch() {
    $("#modalVerifyPopup h4.modal-title").text("Searching historical tasks");

    $("#modalHistoricalWaitMessage").html("");

    var ulTag = "<ul class=\"mb-0 pb-0\" />";

    $("#modalHistoricalWaitMessage").append(ulTag);
    var unOrderedList = $("#modalHistoricalWaitMessage ul");

    unOrderedList.append("<li class=\"pt-1 pb-1\" >Getting historical data from database ...</li>");
    unOrderedList.append("<li class=\"pt-1 pb-1\" >Populating Historical Tasks list...</li>");

    $("#modalVerifyWait").show();
}

function mainHistoricalButtonsEnabled(isEnabled) {
    $("#btnClose").prop("disabled", !isEnabled);
    $("#btnClearHistoricalTasksSearch").prop("disabled", !isEnabled);
    $("#btnSearch").prop("disabled", !isEnabled);
}