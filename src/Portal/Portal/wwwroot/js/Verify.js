$(document).ready(function () {

    var isReadOnly = $("#IsReadOnly").val() === "True";
    var customHttpStatusCodes = JSON.parse($("#SerialisedCustomHttpStatusCodes").val());

    $("#Reviewer").prop("disabled", true);

    setVerifyDoneHandler();
    setContinueVerifyProgressHandler();
    setVerifySaveHandler();
    setContinueVerifyDoneWarningHandler();
    setRejectEventsHandlers();
    setConfirmRejectHandler();
    setUnsavedChangesHandlers();
    setModalVerifyPopupHiddenHandler();
    initialisePopup();

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

    function setRejectEventsHandlers() {

        $("#btnReject").on("click", function () {
            mainButtonsEnabled(false);

            $("#modalVerifyPopup").modal("show");

            // check for unsaved changes
            if (hasUnsavedChanges()) {
                return;
            }

            populateAndShowWaitPopupForReject();
        });

        $("#txtRejectComment").keydown(function (event) {
            $("#ConfirmRejectError").html("");
            $("#btnConfirmReject").prop("disabled", false);
        });
    }

    function setConfirmRejectHandler() {
        $("#btnConfirmReject").on("click",
            function (event) {
                $("#btnConfirmReject").prop("disabled", true);
                $("#btnCancelReject").prop("disabled", true);


                if ($("#txtRejectComment").val() === "") {
                    $("#ConfirmRejectError").html("");
                    $("#ConfirmRejectError")
                        .html("<div class=\"alert alert-danger\" role=\"alert\">Please enter a comment.</div>");
                    $("#txtRejectComment").focus();


                    $("#btnConfirmReject").prop("disabled", false);
                    $("#btnCancelReject").prop("disabled", false);
                    return;
                }

                $("#ConfirmReject").modal("hide");

                populateAndShowWaitPopupForContinueReject();

                $("#modalVerifyPopup").modal("show");
                
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
                        processErrors(error);
                    }
                });
            });
    }

    function setVerifyDoneHandler() {
        $("#btnDone").click(function (e) {
            mainButtonsEnabled(false);

            $("#modalVerifyPopup").modal("show");

            // check for unsaved changes
            if (hasUnsavedChanges()) {
                return;
            }

            populateAndShowWaitPopupForDone();

        });
    }

    function setContinueVerifyProgressHandler() {
        $("#btnContinueVerifyProgress").on("click", function (e) {

            $("#btnCancelVerifyProgressWarning").prop("disabled", true);
            $("#btnContinueVerifyProgress").prop("disabled", true);
            $("#modalVerifyProgressWarning").hide();

            processVerifyDone("Done");
        });
    }

    function setVerifySaveHandler() {
        $("#btnSave").click(function (e) {
            processVerifySave();
        });
    }

    function setContinueVerifyDoneWarningHandler() {
        $("#btnVerifyWarningContinue").on("click", function (e) {

            $("#btnVerifyWarningCancel").prop("disabled", true);
            $("#btnVerifyWarningContinue").prop("disabled", true);

            $("#modalVerifyWarnings").hide();

            processVerifyDone("ConfirmedSignOff");
        });
    }
    
    function processVerifySave() {
        mainButtonsEnabled(false);

        populateAndShowWaitPopupForSave();
        $("#modalVerifyPopup").modal("show");

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
                $("#modalVerifyPopup").modal("hide");
            },
            error: function (error) {
                processErrors(error);
            }
        });
    }

    function processVerifyDone(action) {
        mainButtonsEnabled(false);
        populateAndShowWaitPopupForContinueDone();

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
                processErrors(error);
            }
        });
    }

    function processErrors(error) {
        var responseJson = error.responseJSON;
        var statusCode = error.status;

        $("#modalVerifyErrorMessage").html("");
        $("#modalVerifyWarningMessage").html("");

        $("#modalVerifyWait").hide();

        var ulTag = "<ul class=\"mb-0 pb-0\" />";

        if (responseJson == null) {
            $("#modalVerifyErrorMessage").append(ulTag);
            var unOrderedList = $("#modalVerifyErrorMessage ul");

            unOrderedList.append("<li class=\"pt-1 pb-1\" >System error. Please try again later</li>");
            $("#modalVerifyErrors").show();
            return;
        }

        if (statusCode === customHttpStatusCodes.WarningsDetected) {

            $("#modalVerifyWarningMessage").append(ulTag);
            var unOrderedList = $("#modalVerifyWarningMessage ul");

            responseJson.forEach(function (item) {
                unOrderedList.append("<li class=\"pt-1 pb-1\" >" + item + "</li>");
            });

            $("#btnVerifyWarningCancel").prop("disabled", false);
            $("#btnVerifyWarningContinue").prop("disabled", false);

            $("#modalVerifyWarnings").show();
            return;
        }

        if (statusCode === customHttpStatusCodes.FailedValidation) {
            $("#modalVerifyErrorMessage").append(ulTag);
            var unOrderedList = $("#modalVerifyErrorMessage ul");

            responseJson.forEach(function (item) {
                unOrderedList.append("<li class=\"pt-1 pb-1\" >" + item + "</li>");
            });

            $("#modalVerifyErrors").show();
            return;
        }

        $("#modalVerifyErrorMessage").append(ulTag);
        var unOrderedList = $("#modalVerifyErrorMessage ul");

        unOrderedList.append("<li class=\"pt-1 pb-1\" >" + responseJson + "</li>");

        $("#modalVerifyErrors").show();
    }

    function populateAndShowWaitPopupForDone() {
        $("#modalVerifyPopup h4.modal-title").text("Progressing task");

        $("#btnCancelVerifyProgressWarning").prop("disabled", false);
        $("#btnContinueVerifyProgress").prop("disabled", false);

        $("#modalVerifyProgressWarning").show();
    }
    
    function populateAndShowWaitPopupForContinueDone() {
        $("#modalVerifyPopup h4.modal-title").text("Signing off task");
        
        $("#modalVerifyWaitMessage").html("");

        var ulTag = "<ul class=\"mb-0 pb-0\" />";
        
        $("#modalVerifyWaitMessage").append(ulTag);
        var unOrderedList = $("#modalVerifyWaitMessage ul");

        unOrderedList.append("<li class=\"pt-1 pb-1\" >Verifying Data...</li>");
        unOrderedList.append("<li class=\"pt-1 pb-1\" >Signing-off Task...</li>");

        $("#modalVerifyWait").show();
    }

    function populateAndShowWaitPopupForSave() {

        $("#modalVerifyPopup h4.modal-title").text("Saving task data");

        $("#modalVerifyWaitMessage").html("");

        var ulTag = "<ul class=\"mb-0 pb-0\" />";
        
        $("#modalVerifyWaitMessage").append(ulTag);
        var unOrderedList = $("#modalVerifyWaitMessage ul");

        unOrderedList.append("<li class=\"pt-1 pb-1\" >Verifying Data...</li>");
        unOrderedList.append("<li class=\"pt-1 pb-1\" >Saving Data...</li>");

        $("#modalVerifyWait").show();
    }
    
    function populateAndShowWaitPopupForReject() {
        $("#modalVerifyPopup h4.modal-title").text("Rejecting task");

        $("#btnCancelReject").prop("disabled", false);
        $("#btnConfirmReject").prop("disabled", false);

        $("#ConfirmRejectError").html("");
        $("#txtRejectComment").val("");

        $("#ConfirmReject").show();

        $("#txtRejectComment").focus();

    }

    function populateAndShowWaitPopupForContinueReject() {

        $("#ConfirmReject").hide();

        $("#modalVerifyPopup h4.modal-title").text("Rejecting task");


        $("#modalVerifyWaitMessage").html("");

        var ulTag = "<ul class=\"mb-0 pb-0\" />";


        $("#modalVerifyWaitMessage").append(ulTag);
        var unOrderedList = $("#modalVerifyWaitMessage ul");

        unOrderedList.append("<li class=\"pt-1 pb-1\" >Verifying Data...</li>");
        unOrderedList.append("<li class=\"pt-1 pb-1\" >Rejecting Task...</li>");

        $("#modalVerifyWait").show();
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

    function setModalVerifyPopupHiddenHandler() {
        $("#modalVerifyPopup").on("hidden.bs.modal",
            function () {
                $("#modalVerifyWait").hide();
                $("#modalVerifyErrors").hide();
                $("#modalVerifyWarnings").hide();
                $("#modalVerifyProgressWarning").hide();
                $("#ConfirmReject").hide();

                mainButtonsEnabled(true);
            });
    }

    function initialisePopup() {
        $("#modalVerifyWait").hide();
        $("#modalVerifyErrors").hide();
        $("#modalVerifyWarnings").hide();
        $("#modalVerifyProgressWarning").hide();
        $("#ConfirmReject").hide();
    }
    
    function hasUnsavedChanges() {
        if (formChanged) {

            $("#modalVerifyErrorMessage").html("");

            var ulTag = "<ul class=\"mb-0 pb-0\" />";

            $("#modalVerifyErrorMessage").append(ulTag);

            var unOrderedList = $("#modalVerifyErrorMessage ul");
            unOrderedList.append("<li class=\"pt-1 pb-1\">Unsaved changes detected, please Save first.</li>");

            $("#modalVerifyErrors").show();

            mainButtonsEnabled(true);
            return true;
        }
        return false;
    }});
