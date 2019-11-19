$(document).ready(function () {

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
});