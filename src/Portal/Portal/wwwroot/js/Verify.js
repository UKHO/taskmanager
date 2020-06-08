$(document).ready(function () {

    var isReadOnly = $("#IsReadOnly").val() === "True";
    var customHttpStatusCodes = JSON.parse($("#SerialisedCustomHttpStatusCodes").val());

    $("#Reviewer").prop("disabled", true);

    setVerifyDoneHandler();
    handlebtnContinueVerifyProgress();
    setVerifySaveHandler();
    handleContinueVerifyDoneWarning();
    handleConfirmReject();
    setUnsavedChangesHandlers();

    if (isReadOnly) {
        makeFormReadOnly($("#frmVerifyPage"));
    }

    var formChanged = false;

    function setUnsavedChangesHandlers() {
        $("#frmVerifyPage").change(function () {
            formChanged = true;
        });

        window.onbeforeunload = function () {
            if (formChanged) {
                return "Changes detected";
            }
        }
    }

    if ($("#verifyErrorMessage").html().trim().length > 0) {
        $("#modalWaitVerifyErrors").modal("show");
    }

    $("#btnCancelReject").on("click", function () {
        $("#btnReject").prop("disabled", false);
        $("#btnDone").prop("disabled", false);
        $("#btnSave").prop("disabled", false);
    });

    $("#btnContinueRejection").on("click",
        function () {
            $("#modalUnsavedWarning").modal("hide");
            $("#ConfirmReject").modal("show");
        });


    $("#btnReject").on("click", function () {
        $("#btnConfirmReject").prop("disabled", false);
        $("#ConfirmRejectError").html("");
        $("#txtRejectComment").val("");
        $("#btnReject").prop("disabled", true);
        $("#btnDone").prop("disabled", true);
        $("#btnSave").prop("disabled", true);

        if (formChanged) {
            $("#modalUnsavedWarning").modal("show");
        } else {
            $("#ConfirmReject").modal("show");
        }
    });

    $("#txtRejectComment").keydown(function (event) {
        $("#ConfirmRejectError").html("");
        $("#btnConfirmReject").prop("disabled", false);
    });

    function handleConfirmReject() {
        $("#btnConfirmReject").on("click",
            function(event) {
                $("#btnConfirmReject").prop("disabled", true);


                if ($("#txtRejectComment").val() === "") {
                    $("#ConfirmRejectError")
                        .html("<div class=\"alert alert-danger\" role=\"alert\">Please enter a comment.</div>");
                    $("#txtRejectComment").focus();
                    return;
                }

                // check unsaved changes

                $("#ConfirmReject").modal("hide");
                $("#ConfirmRejectError").html("");
                $("#verifyErrorMessage").html("");
                $("#modalWaitVerifyReject").modal("show");

                var processId = Number($("#ProcessId").val());
                var comment = $("#txtRejectComment").val();

                $.ajax({
                    type: "POST",
                    url: "Verify/?handler=RejectVerify",
                    beforeSend: function(xhr) {
                        xhr.setRequestHeader("RequestVerificationToken",
                            $('input:hidden[name="__RequestVerificationToken"]').val());
                    },
                    data: { "processId": processId, "comment": comment },
                    complete: function() {
                        //Add a delay to account for the modalWaitReviewDone modal
                        //not being fully shown, before trying to hide it
                        window.setTimeout(function() {
                                $("#modalWaitVerifyReject").modal("hide");
                                $("#btnDone").prop("disabled", false);
                                $("#btnSave").prop("disabled", false);
                                $("#ConfirmReject").prop("disabled", false);
                                $("#btnReject").prop("disabled", false);
                            },
                            200);
                    },
                    success: function(result) {
                        formChanged = false;
                        window.location.replace("/Index");
                    },
                    error: function(error) {
                        var responseJson = error.responseJSON;
                        var statusCode = error.status;

                        if (responseJson != null) {
                            if (statusCode === customHttpStatusCodes.FailedValidation) {
                                $("#verifyErrorMessage").append("<ul/>");
                                var unOrderedList = $("#verifyErrorMessage ul");

                                responseJson.forEach(function(item) {
                                    unOrderedList.append("<li>" + item + "</li>");
                                });

                                $("#modalWaitVerifyErrors").modal("show");
                            } else {
                                $("#verifyErrorMessage").append("<ul/>");
                                var unOrderedList = $("#verifyErrorMessage ul");

                                unOrderedList.append("<li>" + responseJson + "</li>");

                                $("#modalWaitVerifyErrors").modal("show");

                            }
                        } else {
                            $("#verifyErrorMessage").append("<ul/>");
                            var unOrderedList = $("#verifyErrorMessage ul");

                            unOrderedList.append("<li>System error. Please try again later</li>");

                            $("#modalWaitVerifyErrors").modal("show");
                        }


                    }
                });


            });
    }

    $("#ConfirmReject").on("shown.bs.modal",
        function () {
            $("#txtRejectComment").focus();
        });
    
    function setVerifyDoneHandler() {
        $("#btnDone").click(function (e) {
            mainButtonsEnabled(false);

            // check for unsaved changes
            if (formChanged) {

                $("#verifyErrorMessage").html("");

                $("#verifyErrorMessage").append("<ul/>");
                var unOrderedList = $("#verifyErrorMessage ul");
                unOrderedList.append("<li>Unsaved changes detected, please Save first.</li>");

                $("#modalWaitVerifyErrors").modal("show");

                mainButtonsEnabled(true);
                return;
            }

            // display: progress warning modal
            $("#btnContinueVerifyProgress").prop("disabled", false);
            $("#btnCancelVerifyProgressWarning").prop("disabled", false);
            $("#modalVerifyProgressWarning").modal("show");

            mainButtonsEnabled(true);

        });
    }

    function handlebtnContinueVerifyProgress() {
        $("#btnContinueVerifyProgress").on("click", function (e) {
            processVerifyDone("Done", $("#modalVerifyProgressWarning"));
        });
    }

    function setVerifySaveHandler() {
        $("#btnSave").click(function (e) {
            processVerifySave();
        });
    }

    function handleContinueVerifyDoneWarning() {
        $("#btnContinueVerifyDoneWarning").on("click", function (e) {
            processVerifyDone("ConfirmedSignOff", $("#modalVerifyDoneWarning"));
        });
    }

    function processVerifySave() {
        mainButtonsEnabled(false);
        $("#modalWaitVerifySave").modal("show");

        var formData = $('#frmVerifyPage').serialize();

        $.ajax({
            type: "POST",
            url: "Verify/?handler=Save",
            beforeSend: function (xhr) {
                xhr.setRequestHeader("RequestVerificationToken", $('input:hidden[name="__RequestVerificationToken"]').val());
            },
            data: formData,
            complete: function () {
                console.log("Save Complete");
                mainButtonsEnabled(true);
            },
            success: function (result) {
                formChanged = false;
                console.log("Save Success");
                $("#modalWaitVerifySave").modal("hide");
            },
            error: function (error) {
                processErrors(error, $("#modalWaitVerifySave"));
            }
        });
    }

    function processVerifyDone(action, modalWarningPopup) {
        mainButtonsEnabled(false);

        $("#btnContinueVerifyProgress").prop("disabled", true);
        $("#btnCancelVerifyProgressWarning").prop("disabled", true);

        hideOnePopupAndShowAnother(modalWarningPopup, $("#modalWaitVerifyDone"));

        var formData = $('#frmVerifyPage').serialize();

        $.ajax({
            type: "POST",
            url: "Verify/?handler=Done&action=" + action,
            beforeSend: function (xhr) {
                xhr.setRequestHeader("RequestVerificationToken", $('input:hidden[name="__RequestVerificationToken"]').val());
            },
            data: formData,
            complete: function () {
                console.log("Done complete");
                mainButtonsEnabled(true);
            },
            success: function (result) {
                window.location.replace("/Index");
                console.log("Done success");
            },
            error: function (error) {
                processErrors(error, $("#modalWaitVerifyDone"));
            }
        });
    }
    
    function processErrors(error, modalWaitPopup) {
        var responseJson = error.responseJSON;
        var statusCode = error.status;

        $("#verifyErrorMessage").html("");
        $("#verifyDoneWarningMessages").html("");

        if (responseJson == null) {
            $("#verifyErrorMessage").append("<ul/>");
            var unOrderedList = $("#verifyErrorMessage ul");

            unOrderedList.append("<li>System error. Please try again later</li>");

            hideOnePopupAndShowAnother(modalWaitPopup, $("#modalWaitVerifyErrors"));
            return;
        }

        if (statusCode === customHttpStatusCodes.WarningsDetected) {

            $("#verifyDoneWarningMessages").append("<ul/>");
            var unOrderedList = $("#verifyDoneWarningMessages ul");

            responseJson.forEach(function (item) {
                unOrderedList.append("<li>" + item + "</li>");
            });

            hideOnePopupAndShowAnother(modalWaitPopup, $("#modalVerifyDoneWarning"));
            return;
        }

        if (statusCode === customHttpStatusCodes.FailedValidation) {
            $("#verifyErrorMessage").append("<ul/>");
            var unOrderedList = $("#verifyErrorMessage ul");

            responseJson.forEach(function (item) {
                unOrderedList.append("<li>" + item + "</li>");
            });

            hideOnePopupAndShowAnother(modalWaitPopup, $("#modalWaitVerifyErrors"));
            return;
        }

        $("#verifyErrorMessage").append("<ul/>");
        var unOrderedList = $("#verifyErrorMessage ul");

        unOrderedList.append("<li>" + responseJson + "</li>");

        hideOnePopupAndShowAnother(modalWaitPopup, $("#modalWaitVerifyErrors"));
    }

    function makeFormReadOnly(formElement) {
        var fieldset = $(formElement).children("fieldset");
        fieldset.prop("disabled", true);
    }

    function mainButtonsEnabled(isEnabled) {
        $("#btnDone").prop("disabled", !isEnabled);
        $("#btnSave").prop("disabled", !isEnabled);
        $("#btnReject").prop("disabled", !isEnabled);
        $("#btnClose").prop("disabled", !isEnabled);
    }
    
    function hideOnePopupAndShowAnother(popupToHide, popupToShow) {
        popupToHide.one("hidden.bs.modal", function () {
            popupToShow.modal("show");
        });
        popupToHide.modal("hide");

    }
});
