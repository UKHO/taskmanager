$(document).ready(function () {

    setReviewDoneHandler();
    setReviewSaveHandler();

    var formChanged = false;
    $("#frmReviewPage").change(function () { formChanged = true; });

    window.onbeforeunload = function() {
        if (formChanged) {
            return "Changes detected";
        }
    };

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

    function completeReview(action) {
        $("#reviewDoneErrorMessage").html("");
        $("#btnDone").prop("disabled", true);
        $("#btnSave").prop("disabled", true);
        $("#modalWaitReviewDone").modal("show");

        var formData = $('#frmReviewPage').serialize();

        $.ajax({
            type: "POST",
            url: "Review/?handler=Done&action=" + action,
            beforeSend: function (xhr) {
                xhr.setRequestHeader("RequestVerificationToken", $('input:hidden[name="__RequestVerificationToken"]').val());
            },
            data: formData,
            complete: function () {
                $("#modalWaitReviewDone").modal("hide");
                $("#btnDone").prop("disabled", false);
                $("#btnSave").prop("disabled", false);
            },
            success: function (result) {
                formChanged = false;
                if (action === "Done") {
                    window.location.replace("/Index");
                }
                console.log("success");
            },
            error: function (error) {
                var responseJson = error.responseJSON;

                if (responseJson != null) {
                    $("#reviewDoneErrorMessage").append("<ul/>");
                    var unOrderedList = $("#reviewDoneErrorMessage ul");

                    responseJson.forEach(function (item) {
                        unOrderedList.append("<li>" + item + "</li>");
                    });

                    $("#modalWaitReviewDoneErrors").modal("show");
                }

            }
        });
    }

    function setReviewDoneHandler() {
        $("#btnDone").prop("disabled", false);


        $("#btnDone").click(function (e) {
            completeReview("Done");
        });
    }

    function setReviewSaveHandler() {
        $("#btnSave").prop("disabled", false);


        $("#btnSave").click(function (e) {
            completeReview("Save");
        });
    }
});