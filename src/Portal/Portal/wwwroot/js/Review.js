$(document).ready(function () {

    setReviewDoneHandler();

    if ($("#reviewDoneErrorMessage").html().trim().length > 0) {
        $("#modalWaitReviewDoneErrors").modal("show");
    }

    $("#btnTerminate").on("click", function () {
        $("#ConfirmTerminate").modal("show");
    });

    $("#terminatingReview").submit(function (event) {
        if ($("#txtTerminateComment").val() === "") {
            $("#ConfirmTerminateError")
                .html("<div class=\"alert alert-danger\" role=\"alert\">Please enter a comment.</div>");
            $("#txtTerminateComment").focus();
            event.preventDefault();
        }
    });

    $("#ConfirmTerminate").on("shown.bs.modal",
        function () {
            $("#txtTerminateComment").focus();
        });

    function setReviewDoneHandler() {
        $("#btnDone").prop("disabled", false);

        $("#frmReviewPage").on("submit", function (e) {
            $("#btnDone").prop("disabled", true);
            $("#modalWaitReviewDone").modal("show");
        });
    }
});