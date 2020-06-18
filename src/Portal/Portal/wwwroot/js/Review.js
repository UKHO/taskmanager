$(document).ready(function () {

    var isReadOnly = $("#IsReadOnly").val() === "True";
    var customHttpStatusCodes = JSON.parse($("#SerialisedCustomHttpStatusCodes").val());

    $('#operators').find('div .operator:gt(0)').hide();

    initialiseOperatorsTypeaheads();

    setReviewSaveHandler();
    setReviewDoneHandler();
    setContinueProgressHandler();

    attachTerminateHandlers();
    setUnsavedChangesHandlers();
    setModalReviewPopupHiddenHandler();

    if (isReadOnly) {
        makeFormReadOnly($("#frmReviewPage"));
    }

    var formChanged = false;

    if ($("#reviewErrorMessage").html().trim().length > 0) {
        $("#modalWaitReviewErrors").modal("show");
    }

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

    function attachTerminateHandlers() {
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

            $("#btnConfirmReject").prop("disabled", true);
            $("#btnConfirmTerminate").prop("disabled", true);

            if ($("#txtTerminateComment").val() === "") {
                $("#ConfirmTerminateError")
                    .html("<div class=\"alert alert-danger\" role=\"alert\">Please enter a comment.</div>");
                $("#txtTerminateComment").focus();


                $("#btnConfirmReject").prop("disabled", false);
                $("#btnConfirmTerminate").prop("disabled", false);
                return;
            }

            submitTerminateForm();
        });

        $("#ConfirmTerminate").on("shown.bs.modal", function () {
            $("#txtTerminateComment").focus();
        });

        $("#ConfirmTerminate").on("hidden.bs.modal", function () {
            mainButtonsEnabled(true);
        });

    }

    function setReviewDoneHandler() {

        $("#btnDone").click(function (e) {
            mainButtonsEnabled(false);

            // check for unsaved changes
            if (formChanged) {

                $("#reviewErrorMessage").html("");

                $("#reviewErrorMessage").append("<ul/>");
                var unOrderedList = $("#reviewErrorMessage ul");
                unOrderedList.append("<li>Unsaved changes detected, please Save first.</li>");

                $("#modalWaitReviewErrors").modal("show");

                $("#modalWaitReviewErrors").one("hidden.bs.modal", function () {
                    mainButtonsEnabled(true);
                });

                return;
            }

            // display: progress warning modal
            $("#btnContinueReviewProgress").prop("disabled", false);
            $("#btnCancelReviewProgressWarning").prop("disabled", false);
            $("#modalReviewProgressWarning").modal("show");

            mainButtonsEnabled(true);

        });
    }

    function setContinueProgressHandler() {
        $("#btnContinueReviewProgress").click(function (e) {
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

        $("#btnContinueReviewProgress").prop("disabled", true);
        $("#btnCancelReviewProgressWarning").prop("disabled", true);


        $("#reviewErrorMessage").html("");

        //Hide modalReviewProgressWarning modal and show modalWaitReviewDone modal
        hideOnePopupAndShowAnother($("#modalReviewProgressWarning"), $("#modalWaitReviewDone"));

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
                var responseJson = error.responseJSON;

                if (responseJson != null) {
                    $("#reviewErrorMessage").append("<ul/>");
                    var unOrderedList = $("#reviewErrorMessage ul");

                    responseJson.forEach(function (item) {
                        unOrderedList.append("<li>" + item + "</li>");
                    });

                    //Hide modalWaitReviewDone modal and show modalWaitReviewErrors modal
                    hideOnePopupAndShowAnother($("#modalWaitReviewDone"), $("#modalWaitReviewErrors"));
                }

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

        $("#modalReviewWait").collapse("hide");

        var ulTag = "<ul class=\"mb-0 pb-0\" />";

        if (responseJson == null) {
            $("#modalReviewErrorMessage").append(ulTag);
            var unOrderedList = $("#modalReviewErrorMessage ul");

            unOrderedList.append("<li class=\"pt-1 pb-1\" >System error. Please try again later</li>");
            $("#modalReviewErrors").collapse("show");
            return;
        }

        if (statusCode === customHttpStatusCodes.FailedValidation) {
            $("#modalReviewErrorMessage").append(ulTag);
            var unOrderedList = $("#modalReviewErrorMessage ul");

            responseJson.forEach(function (item) {
                unOrderedList.append("<li class=\"pt-1 pb-1\" >" + item + "</li>");
            });

            $("#modalReviewErrors").collapse("show");
            return;
        }

        $("#modalReviewErrorMessage").append(ulTag);
        var unOrderedList = $("#modalReviewErrorMessage ul");

        unOrderedList.append("<li class=\"pt-1 pb-1\" >" + responseJson + "</li>");

        $("#modalReviewErrors").collapse("show");
    }

    function hideOnePopupAndShowAnother(popupToHide, popupToShow) {
        popupToHide.one("hidden.bs.modal", function () {
            popupToShow.modal("show");
        });
        popupToHide.modal("hide");

    }

    function initialiseOperatorsTypeaheads() {

        removeOperatorsInitialiseErrors();

        $('#Reviewer, #Assessor, #Verifier').typeahead('val', "");
        $('#Reviewer, #Assessor, #Verifier').typeahead('close');

        var users = new Bloodhound({
            datumTokenizer: function (d) {
                return Bloodhound.tokenizers.nonword(d.displayName);
            },
            queryTokenizer: Bloodhound.tokenizers.whitespace,
            prefetch: {
                url: "/Index/?handler=Users",
                ttl: 60000
            },
            initialize: false
        });

        var promise = users.initialize();
        promise
            .done(function () {
                removeOperatorsInitialiseErrors();
            })
            .fail(function () {
                var errorArray = ["Failed to look up users. Try refreshing the page."];
                displayOperatorsInitialiseErrors(errorArray);
            });

        $('#Reviewer, #Assessor, #Verifier').typeahead({
            hint: true,
            highlight: true, /* Enable substring highlighting */
            minLength: 3 /* Specify minimum characters required for showing result */
        },
            {
                name: 'users',
                source: users,
                limit: 100,
                displayKey: 'displayName',
                valueKey: 'userPrincipalName',
                templates: {
                    empty: '<div>No results</div>',
                    suggestion: function (users) {
                        return "<p><span class='displayName'>" + users.displayName + "</span><br/><span class='email'>" + users.userPrincipalName + "</span></p>";
                    }
                }
            });
    }

    function displayOperatorsInitialiseErrors(errorStringArray) {

        var orderedList = $("#operatorsErrorList");

        // == to catch undefined and null
        if (errorStringArray == null) {
            orderedList.append("<li>An unknown error has occurred</li>");

        } else {
            errorStringArray.forEach(function (item) {
                orderedList.append("<li>" + item + "</li>");
            });
        }

        $("#operatorsErrorMessages").collapse("show");
    }

    function removeOperatorsInitialiseErrors() {
        $("#operatorsErrorMessages").collapse("hide");
        $("#operatorsErrorList").empty();
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
                $("#modalReviewWait").collapse("hide");
                $("#modalReviewErrors").collapse("hide");
                $("#modalReviewProgressWarning").collapse("hide");
                $("#ConfirmTerminate").collapse("hide");
                mainButtonsEnabled(true);
            });
    }

    function populateAndShowWaitPopupForTerminate() {
        $("#modalReviewPopup h4.modal-title").text("Terminating task");

        $("#btnConfirmTerminate").prop("disabled", false);
        $("#btnCancelTerminate").prop("disabled", false);

        $("#ConfirmTerminateError").html("");
        $("#txtTerminateComment").val("");

        $("#ConfirmTerminate").collapse("show");
    }

    function populateAndShowWaitPopupForContinueTerminate() {

        $("#ConfirmTerminate").collapse("hide");

        $("#modalReviewPopup h4.modal-title").text("Terminating task");

        $("#modalReviewWaitMessage").html("");

        ulTag = "<ul class=\"mb-0 pb-0\" />";


        $("#modalReviewWaitMessage").append(ulTag);
        var unOrderedList = $("#modalReviewWaitMessage ul");

        unOrderedList.append("<li class=\"pt-1 pb-1\" >Verifying Data...</li>");
        unOrderedList.append("<li class=\"pt-1 pb-1\" >Terminating Task...</li>");

        $("#modalReviewWait").collapse("show");
    }

    function populateAndShowWaitPopupForSave() {

        $("#modalReviewPopup h4.modal-title").text("Saving task data");

        $("#modalReviewWaitMessage").html("");

        ulTag = "<ul class=\"mb-0 pb-0\" />";


        $("#modalReviewWaitMessage").append(ulTag);
        var unOrderedList = $("#modalReviewWaitMessage ul");

        unOrderedList.append("<li class=\"pt-1 pb-1\" >Reviewing Data...</li>");
        unOrderedList.append("<li class=\"pt-1 pb-1\" >Saving Data...</li>");

        $("#modalReviewWait").collapse("show");
    }

    function hasUnsavedChanges() {
        if (formChanged) {

            $("#modalReviewErrorMessage").html("");

            $("#modalReviewErrorMessage").append("<ul/>");
            var unOrderedList = $("#modalReviewErrorMessage ul");
            unOrderedList.append("<li>Unsaved changes detected, please Save first.</li>");

            $("#modalReviewErrors").collapse("show");

            mainButtonsEnabled(true);
            return true;
        }
        return false;
    }
});