﻿$(document).ready(function () {

    var isReadOnly = $("#IsReadOnly").val() === "True";
    var customHttpStatusCodes = JSON.parse($("#SerialisedCustomHttpStatusCodes").val());

    $("#Reviewer").prop("disabled", true);

    setVerifyDoneHandler();
    handlebtnContinueVerifyProgress();
    setVerifySaveHandler();
    handleContinueVerifyDoneWarning();
    handleRejectEvents();
    handleConfirmReject();
    setUnsavedChangesHandlers();

    if (isReadOnly) {
        makeFormReadOnly($("#frmVerifyPage"));
    }

    if ($("#verifyErrorMessage").html().trim().length > 0) {
        $("#modalWaitVerifyErrors").modal("show");
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

    function handleRejectEvents() {

        $("#btnReject").on("click", function () {
            mainButtonsEnabled(false);

            // check for unsaved changes
            if (hasUnsavedChanges()) {
                return;
            }

            $("#btnConfirmReject").prop("disabled", false);
            $("#btnCancelReject").prop("disabled", false);
            $("#txtRejectComment").val("");
            $("#ConfirmReject").modal("show");
        });

        $("#txtRejectComment").keydown(function (event) {
            $("#ConfirmRejectError").html("");
            $("#btnConfirmReject").prop("disabled", false);
        });

        $("#ConfirmReject").on("shown.bs.modal", function () {
            $("#txtRejectComment").focus();
        });

        $("#ConfirmReject").on("hidden.bs.modal", function () {
            mainButtonsEnabled(true);
        });

    }

    function handleConfirmReject() {
        $("#btnConfirmReject").on("click",
            function (event) {
                $("#btnConfirmReject").prop("disabled", true);
                $("#btnCancelReject").prop("disabled", true);


                if ($("#txtRejectComment").val() === "") {
                    $("#ConfirmRejectError").html("");
                    $("#ConfirmRejectError")
                        .html("<div class=\"alert alert-danger\" role=\"alert\">Please enter a comment.</div>");
                    $("#txtRejectComment").focus();
                    return;
                }

                hideOnePopupAndShowAnother($("#ConfirmReject"), $("#modalWaitVerifyReject"));

                var processId = Number($("#ProcessId").val());
                var comment = $("#txtRejectComment").val();

                $.ajax({
                    type: "POST",
                    url: "Verify/?handler=RejectVerify",
                    beforeSend: function (xhr) {
                        xhr.setRequestHeader("RequestVerificationToken",
                            $('input:hidden[name="__RequestVerificationToken"]').val());
                    },
                    data: { "processId": processId, "comment": comment },
                    complete: function () {
                        console.log("Reject complete");
                        mainButtonsEnabled(true);
                    },
                    success: function (result) {
                        window.location.replace("/Index");
                        console.log("Reject success");
                    },
                    error: function (error) {
                        processErrors(error, $("#modalWaitVerifyReject"));
                    }
                });
            });
    }

    function setVerifyDoneHandler() {
        $("#btnDone").click(function (e) {
            mainButtonsEnabled(false);

            // check for unsaved changes
            if (hasUnsavedChanges()) {
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

            $("#modalVerifyProgressWarning").modal("hide");

            $("#modalVerifyPopup").modal("show");

            processVerifyDone("Done");
        });
    }

    function setVerifySaveHandler() {
        $("#btnSave").click(function (e) {
            processVerifySave();
        });
    }

    function handleContinueVerifyDoneWarning() {
        $("#btnContinueVerifyDoneWarning").on("click", function (e) {


            $("#modalVerifyDoneWarning").modal("hide");
            processVerifyDone("ConfirmedSignOff");
        });

        $("#btnVerifyWarningContinue").on("click", function (e) {

            $("#btnVerifyWarningCancel").prop("disabled", true);
            $("#btnVerifyWarningContinue").prop("disabled", true);

            $("#modalVerifyWarnings").collapse("hide");

            processVerifyDone("ConfirmedSignOff");
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

    function processVerifyDone(action) {
        mainButtonsEnabled(false);
        populateAndShowWaitPopupForDone();

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
                processErrors1(error);
            }
        });
    }

    function processErrors1(error) {
        var responseJson = error.responseJSON;
        var statusCode = error.status;

        $("#modalVerifyErrorMessage").html("");
        $("#modalVerifyWarningMessage").html("");

        $("#modalVerifyWait").collapse("hide");

        ulTag = "<ul class=\"mb-0 pb-0\" />";

        if (responseJson == null) {
            $("#modalVerifyErrorMessage").append(ulTag);
            var unOrderedList = $("#modalVerifyErrorMessage ul");

            unOrderedList.append("<li class=\"pt-1 pb-1\" >System error. Please try again later</li>");
            $("#modalVerifyErrors").collapse("show");
            return;
        }

        if (statusCode === customHttpStatusCodes.WarningsDetected) {

            $("#modalVerifyWarningMessage").append(ulTag);
            var unOrderedList = $("#modalVerifyWarningMessage ul");

            responseJson.forEach(function (item) {
                unOrderedList.append("<li class=\"pt-1 pb-1\" >" + item + "</li>");
            });

            $("#modalVerifyWarnings").collapse("show");
            return;
        }

        if (statusCode === customHttpStatusCodes.FailedValidation) {
            $("#modalVerifyErrorMessage").append(ulTag);
            var unOrderedList = $("#modalVerifyErrorMessage ul");

            responseJson.forEach(function (item) {
                unOrderedList.append("<li class=\"pt-1 pb-1\" >" + item + "</li>");
            });

            $("#modalVerifyErrors").collapse("show");
            return;
        }

        $("#modalVerifyErrorMessage").append(ulTag);
        var unOrderedList = $("#modalVerifyErrorMessage ul");

        unOrderedList.append("<li class=\"pt-1 pb-1\" >" + responseJson + "</li>");

        $("#modalVerifyErrors").collapse("show");
    }
    
    function populateAndShowWaitPopupForDone() {

        $("#modalVerifyWaitMessage").html("");

        ulTag = "<ul class=\"mb-0 pb-0\" />";


        $("#modalVerifyWaitMessage").append(ulTag);
        var unOrderedList = $("#modalVerifyWaitMessage ul");

        unOrderedList.append("<li class=\"pt-1 pb-1\" >Verifying Data...</li>");
        unOrderedList.append("<li class=\"pt-1 pb-1\" >Signing-off Task...</li>");

        $("#modalVerifyWait").collapse("show");
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

    $("#modalVerifyPopup").on("hidden.bs.modal", function () {
        $("#modalVerifyWait").collapse("hide");
        $("#modalVerifyErrors").collapse("hide");
        $("#modalVerifyWarnings").collapse("hide");
        
    });

    function hasUnsavedChanges() {
        if (formChanged) {

            $("#verifyErrorMessage").html("");

            $("#verifyErrorMessage").append("<ul/>");
            var unOrderedList = $("#verifyErrorMessage ul");
            unOrderedList.append("<li>Unsaved changes detected, please Save first.</li>");

            $("#modalWaitVerifyErrors").modal("show");

            mainButtonsEnabled(true);
            return true;
        }
        return false;
    }
});
