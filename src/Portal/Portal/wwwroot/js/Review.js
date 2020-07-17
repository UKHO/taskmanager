$(document).ready(function () {
    var isReadOnly = $("#IsReadOnly").val() === "True";
    var customHttpStatusCodes = JSON.parse($("#SerialisedCustomHttpStatusCodes").val());

    $('#operators').find('div .operator:gt(0)').hide();

    initialiseOperatorsTypeaheads();

    setReviewSaveHandler();
    setReviewDoneHandler();
    setContinueProgressHandler();

    setTerminateHandlers();
    setUnsavedChangesHandlers();
    setModalReviewPopupHiddenHandler();
    initialisePopup();

    if (isReadOnly) {
        makeFormReadOnly($("#frmReviewPage"));
    }

    var formChanged = false;

    function setUnsavedChangesHandlers() {
        $("#frmReviewPage").change(function () {
            formChanged = true;
        });

        window.onbeforeunload = function () {
            if (formChanged) {
                return "Changes detected";
            }
        }
    }

    function setTerminateHandlers() {
        $("#btnTerminate").on("click", function () {
            mainButtonsEnabled(false);

            $("#modalReviewPopup").modal("show");

            // check for unsaved changes
            if (hasUnsavedChanges()) {
                return;
            }

            populateAndShowWaitPopupForTerminate();
        });

        $("#txtTerminateComment").keydown(function (e) {
            $("#ConfirmTerminateError").html("");
        });

        $("#btnConfirmTerminate").on("click", function () {

            $("#btnCancelTerminate").prop("disabled", true);
            $("#btnConfirmTerminate").prop("disabled", true);

            if ($("#txtTerminateComment").val() === "") {
                $("#ConfirmTerminateError")
                    .html("<div class=\"alert alert-danger\" role=\"alert\">Please enter a comment.</div>");
                $("#txtTerminateComment").focus();


                $("#btnCancelTerminate").prop("disabled", false);
                $("#btnConfirmTerminate").prop("disabled", false);
                return;
            }

            submitTerminateForm();
        });
    }

    function setReviewDoneHandler() {

        $("#btnDone").click(function (e) {
            mainButtonsEnabled(false);
            
            $("#modalReviewPopup").modal("show");

            // check for unsaved changes
            if (hasUnsavedChanges()) {
                return;
            }

            populateAndShowWaitPopupForDone();

        });
    }

    function setContinueProgressHandler() {
        $("#btnContinueReviewProgress").click(function (e) {

            $("#btnCancelReviewProgressWarning").prop("disabled", true);
            $("#btnContinueReviewProgress").prop("disabled", true);

            processReviewDone();
        });
    }

    function setReviewSaveHandler() {
        $("#btnSave").click(function (e) {
            processReviewSave();
        });
    }

    function processReviewSave() {
        mainButtonsEnabled(false);

        populateAndShowWaitPopupForSave();
        $("#modalReviewPopup").modal("show");

        var formData = $("#frmReviewPage").serialize();
        console.log(formData);
        $.ajax({
            type: "POST",
            url: "Review/?handler=Save",
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
                $("#modalReviewPopup").modal("hide");
            },
            error: function (error) {
                processErrors(error);
            }
        });
    }

    function processReviewDone() {
        mainButtonsEnabled(false);

        populateAndShowWaitPopupForContinueDone();

        var formData = $("#frmReviewPage").serialize();

        $.ajax({
            type: "POST",
            url: "Review/?handler=Done",
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

    function submitTerminateForm() {
        mainButtonsEnabled(false);

        populateAndShowWaitPopupForContinueTerminate();
        
        var processId = Number($("#ProcessId").val());
        var comment = $("#txtTerminateComment").val();

        $.ajax({
            type: "POST",
            url: "Review/?handler=Terminate",
            beforeSend: function (xhr) {
                xhr.setRequestHeader("RequestVerificationToken", $('input:hidden[name="__RequestVerificationToken"]').val());
            },
            data: { "processId": processId, "comment": comment },
            complete: function () {
                console.log("terminate complete");
                mainButtonsEnabled(true);

            },
            success: function (result) {
                console.log("terminate success");
                formChanged = false;

                window.location.replace("/Index");
            },
            error: function (error) {
                console.log("terminate error");
                processErrors(error);
            }
        });
    }
    
    function processErrors(error) {
        var responseJson = error.responseJSON;
        var statusCode = error.status;

        $("#modalReviewErrorMessage").html("");

        $("#modalReviewWait").hide();

        var ulTag = "<ul class=\"mb-0 pb-0\" />";

        if (responseJson == null) {
            $("#modalReviewErrorMessage").append(ulTag);
            var unOrderedList = $("#modalReviewErrorMessage ul");

            unOrderedList.append("<li class=\"pt-1 pb-1\" >System error. Please try again later</li>");
            $("#modalReviewErrors").show();
            return;
        }

        if (statusCode === customHttpStatusCodes.FailedValidation) {
            $("#modalReviewErrorMessage").append(ulTag);
            var unOrderedList = $("#modalReviewErrorMessage ul");

            responseJson.forEach(function (item) {
                unOrderedList.append("<li class=\"pt-1 pb-1\" >" + item + "</li>");
            });

            $("#modalReviewErrors").show();
            return;
        }

        $("#modalReviewErrorMessage").append(ulTag);
        var unOrderedList = $("#modalReviewErrorMessage ul");

        unOrderedList.append("<li class=\"pt-1 pb-1\" >" + responseJson + "</li>");

        $("#modalReviewErrors").show();
    }

    function initialisePopup() {
        $("#modalReviewWait").hide();
        $("#modalReviewErrors").hide();
        $("#modalReviewProgressWarning").hide();
        $("#ConfirmTerminate").hide();
    }

    function makeFormReadOnly(formElement) {
        var fieldset = $(formElement).children("fieldset");
        fieldset.prop("disabled", true);
    }

    function mainButtonsEnabled(isEnabled) {
        $("#btnDone").prop("disabled", !isEnabled);
        $("#btnSave").prop("disabled", !isEnabled);
        $("#btnTerminate").prop("disabled", !isEnabled);
        $("#btnClose").prop("disabled", !isEnabled);
    }

    function setModalReviewPopupHiddenHandler() {
        $("#modalReviewPopup").on("hidden.bs.modal",
            function () {
                $("#modalReviewWait").hide();
                $("#modalReviewErrors").hide();
                $("#modalReviewProgressWarning").hide();
                $("#ConfirmTerminate").hide();
                mainButtonsEnabled(true);
            });
    }

    function populateAndShowWaitPopupForTerminate() {
        $("#modalReviewPopup h4.modal-title").text("Terminating task");

        $("#btnConfirmTerminate").prop("disabled", false);
        $("#btnCancelTerminate").prop("disabled", false);

        $("#ConfirmTerminateError").html("");
        $("#txtTerminateComment").val("");

        $("#ConfirmTerminate").show();

        $("#txtTerminateComment").focus();
    }

    function populateAndShowWaitPopupForContinueTerminate() {

        $("#ConfirmTerminate").hide();

        $("#modalReviewPopup h4.modal-title").text("Terminating task");

        $("#modalReviewWaitMessage").html("");

        var ulTag = "<ul class=\"mb-0 pb-0\" />";


        $("#modalReviewWaitMessage").append(ulTag);
        var unOrderedList = $("#modalReviewWaitMessage ul");

        unOrderedList.append("<li class=\"pt-1 pb-1\" >Verifying Data...</li>");
        unOrderedList.append("<li class=\"pt-1 pb-1\" >Terminating Task...</li>");

        $("#modalReviewWait").show();
    }

    function populateAndShowWaitPopupForDone() {
        $("#modalReviewPopup h4.modal-title").text("Progressing task");

        $("#btnCancelReviewProgressWarning").prop("disabled", false);
        $("#btnContinueReviewProgress").prop("disabled", false);

        $("#modalReviewProgressWarning").show();
    }

    function populateAndShowWaitPopupForContinueDone() {
        $("#modalReviewProgressWarning").hide();

        $("#modalReviewPopup h4.modal-title").text("Progressing task");


        $("#modalReviewWaitMessage").html("");

        var ulTag = "<ul class=\"mb-0 pb-0\" />";
    }


    function displayOperatorsInitialiseErrors(errorStringArray) {

        $("#modalReviewWaitMessage").append(ulTag);
        var unOrderedList = $("#modalReviewWaitMessage ul");

        unOrderedList.append("<li class=\"pt-1 pb-1\" >Verifying Data...</li>");
        unOrderedList.append("<li class=\"pt-1 pb-1\" >Progressing Task...</li>");

        $("#modalReviewWait").show();
    }

    function populateAndShowWaitPopupForSave() {

        $("#modalReviewPopup h4.modal-title").text("Saving task data");

        $("#modalReviewWaitMessage").html("");

        var ulTag = "<ul class=\"mb-0 pb-0\" />";


        $("#modalReviewWaitMessage").append(ulTag);
        var unOrderedList = $("#modalReviewWaitMessage ul");

        unOrderedList.append("<li class=\"pt-1 pb-1\" >Verifying Data...</li>");
        unOrderedList.append("<li class=\"pt-1 pb-1\" >Saving Data...</li>");

        $("#modalReviewWait").show();
    }

    function hasUnsavedChanges() {
        if (formChanged) {

            $("#modalReviewErrorMessage").html("");

            var ulTag = "<ul class=\"mb-0 pb-0\" />";

            $("#modalReviewErrorMessage").append(ulTag);
            var unOrderedList = $("#modalReviewErrorMessage ul");
            unOrderedList.append("<li class=\"pt-1 pb-1\" >Unsaved changes detected, please Save first.</li>");

            $("#modalReviewErrors").show();

            mainButtonsEnabled(true);
            return true;
        }
        return false;
    }
});