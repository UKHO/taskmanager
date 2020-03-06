$(document).ready(function () {

    $("#Reviewer").prop("disabled", true);

    setVerifyDoneHandler();
    setVerifySaveHandler();
    handleContinueChildTaskWarning();

    var formChanged = false;
    $("#frmVerifyPage").change(function () { formChanged = true; });

    window.onbeforeunload = function () {
        if (formChanged) {
            return "Changes detected";
        }
    };

    if ($("#verifyDoneErrorMessage").html().trim().length > 0) {
        $("#modalWaitVerifyDoneErrors").modal("show");
    }

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


    function completeVerify(action) {
        $("#modalOpenChildTaskWarning").modal("hide");
        $("#verifyDoneErrorMessage").html("");
        $("#childTaskWarningMessages").html("");
        $("#btnDone").prop("disabled", true);
        $("#btnSave").prop("disabled", true);
        $("#modalWaitVerifyDone").modal("show");

        var formData = $('#frmVerifyPage').serialize();

        $.ajax({
            type: "POST",
            url: "Verify/?handler=Done&action=" + action,
            beforeSend: function (xhr) {
                xhr.setRequestHeader("RequestVerificationToken", $('input:hidden[name="__RequestVerificationToken"]').val());
            },
            data: formData,
            complete: function () {
                //Add a delay to account for the modalWaitReviewDone modal
                //not being fully shown, before trying to hide it
                window.setTimeout(function () {
                    $("#modalWaitVerifyDone").modal("hide");
                    $("#btnDone").prop("disabled", false);
                    $("#btnSave").prop("disabled", false);
                }, 200);
            },
            success: function (result) {
                formChanged = false;
                if (action === "Done" || action === "ConfirmedSignOff") {
                    window.location.replace("/Index");
                }
                console.log("success");
            },
            error: function (error) {
                var responseJson = error.responseJSON;
                var statusCode = error.status;

                if (responseJson != null) {
                    if (statusCode === 406) {
                        $("#childTaskWarningMessages").append("<ul/>");
                        var unOrderedList = $("#childTaskWarningMessages ul");

                        responseJson.forEach(function (item) {
                            unOrderedList.append("<li>" + item + "</li>");
                        });

                        $("#modalOpenChildTaskWarning").modal("show");
                    } else {
                        $("#verifyDoneErrorMessage").append("<ul/>");
                        var unOrderedList = $("#verifyDoneErrorMessage ul");

                        responseJson.forEach(function(item) {
                            unOrderedList.append("<li>" + item + "</li>");
                        });

                        $("#modalWaitVerifyDoneErrors").modal("show");
                    }
                } else {
                    $("#verifyDoneErrorMessage").html("<div class=\"alert alert-danger\" role=\"alert\">System error. Please try again later.</div>");
                    $("#modalWaitVerifyDoneErrors").modal("show");
                }
                
                
            }
        });
    }

    function setVerifyDoneHandler() {
        $("#btnDone").prop("disabled", false);


        $("#btnDone").click(function (e) {
            completeVerify("Done");
        });
    }

    function setVerifySaveHandler() {
        $("#btnSave").prop("disabled", false);


        $("#btnSave").click(function (e) {
            completeVerify("Save");
        });
    }

    function handleContinueChildTaskWarning() {
        $("#btnContinueChildTaskWarning").on("click", function(e) {
            completeVerify("ConfirmedSignOff");
        });
    }


});
