$(document).ready(function() {
    setAssessDoneHandler();

    if ($("#assessDoneErrorMessage").html().trim().length > 0) {
        $("#modalWaitAssessDoneErrors").modal("show");
    }
});

function setAssessDoneHandler() {
    $("#btnDone").prop("disabled", false);

    $("#assessDone").on("submit", function (e) {
        $("#btnDone").prop("disabled", true);
        $("#modalWaitAssessDone").modal("show");

        //e.preventDefault();

    });
}