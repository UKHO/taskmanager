$(document).ready(function () {
    var processId = { "processId": $("#hdnProcessId").val() };

    getComments();

    $("#OnHold").submit(function (event) {
        $("#btnPutOnHold").prop("disabled", "disabled");

        $.ajax({
            type: "POST",
            url: "Review/?handler=OnPostOnHoldAsync",
            beforeSend: function (xhr) {
                xhr.setRequestHeader("XSRF-TOKEN", $('input:hidden[name="__RequestVerificationToken"]').val());
            },
            contentType: "application/json; charset=utf-8",
            data: processId,
            success: function (result) {
                $("#OnHoldError").modal("hide");
            },
            error: function (error) {
                $("#OnHoldError").modal("show");
            }
        });
    });

    $("#OffHold").submit(function (event) {
        $("#btnTakeOffHold").prop("disabled", "disabled");

        $.ajax({
            type: "POST",
            url: "Review/?handler=OnPostOffHoldAsync",
            beforeSend: function (xhr) {
                xhr.setRequestHeader("XSRF-TOKEN", $('input:hidden[name="__RequestVerificationToken"]').val());
            },
            contentType: "application/json; charset=utf-8",
            data: processId,
            success: function (result) {
                $("#OnHoldError").modal("hide");
            },
            error: function (error) {
                $("#OnHoldError").modal("show");
            }
        });
    });

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

    function getComments() {

        $.ajax({
            type: "GET",
            url: "Review/?handler=RetrieveComments",
            beforeSend: function (xhr) {
                xhr.setRequestHeader("XSRF-TOKEN", $('input:hidden[name="__RequestVerificationToken"]').val());
            },
            contentType: "application/json; charset=utf-8",
            data: processId,
            success: function (result) {
                $("#existingComments").html(result);
            },
            error: function (error) {
                $("#AddCommentError")
                    .html("<div class=\"alert alert-danger\" role=\"alert\">Failed to load comments.</div>");
            }
        });
    }
});