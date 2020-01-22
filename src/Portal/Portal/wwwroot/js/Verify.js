$(document).ready(function () {
    $("#btnReject").on("click", function () {
        $("#ConfirmReject").modal("show");
    });

    $("#rejectingVerify").submit(function (event) {
        if ($("#txtRejectComment").val() === "") {
            $("#ConfirmRejectError")
                .html("<div class=\"alert alert-danger\" role=\"alert\">Please enter a comment.</div>");
            $("#txtRejectComment").focus();
            event.preventDefault();
        }
    });

    $("#ConfirmReject").on("shown.bs.modal",
        function () {
            $("#txtRejectComment").focus();
        });


});
