$(document).ready(function () {

    var customHttpStatusCodes = JSON.parse($("#SerialisedCustomHttpStatusCodes").val());

    $("#Reviewer").prop("disabled", true);


    initialiseOperatorsTypeaheads();

    setAssessDoneHandler();
    setAssessSaveHandler();
    setAssessWarningContinueHandler();
    setUnsavedChangesHandlers();
    setContinueAssessProgressHandler();
    initialisePopup();
    setModalAssessPopupHiddenHandler();

    var formChanged = false;

    function setUnsavedChangesHandlers() {
        $("#frmAssessPage").change(function () {
            formChanged = true;
        });

        window.onbeforeunload = function () {
            if (formChanged) {
                return "Changes detected";
            }
        }
    }

    function setAssessDoneHandler() {
        $("#btnDone").click(function (e) {

            mainButtonsEnabled(false);

            $("#modalAssessPopup").modal("show");

            // check for unsaved changes
            if (hasUnsavedChanges()) {
                return;
            }

            populateAndShowWaitPopupForDone();

        });
    }

    function setContinueAssessProgressHandler() {
        $("#btnContinueAssessProgress").on("click", function (e) {

            $("#btnCancelAssessProgressWarning").prop("disabled", true);
            $("#btnContinueAssessProgress").prop("disabled", true);
            $("#modalAssessProgressWarning").hide();

            processAssessDone("Done");
        });
    }

    function setAssessWarningContinueHandler() {
        $("#btnAssessWarningContinue").on("click", function (e) {

            $("#btnAssessWarningCancel").prop("disabled", true);
            $("#btnAssessWarningContinue").prop("disabled", true);

            $("#modalAssessWarnings").hide();

            processAssessDone("ConfirmedDone");
        });
    }
    function setAssessSaveHandler() {
        $("#btnSave").click(function (e) {
            processAssessSave();
        });
    }

    function processAssessSave() {
        mainButtonsEnabled(false);

        populateAndShowWaitPopupForSave();

        $("#modalAssessPopup").modal("show");

        var formData = $("#frmAssessPage").serialize();

        $.ajax({
            type: "POST",
            url: "Assess/?handler=Save",
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
                $("#modalAssessPopup").modal("hide");
            },
            error: function (error) {
                processErrors(error);
            }
        });
    }

    function processAssessDone(action) {
        mainButtonsEnabled(false);

        populateAndShowWaitPopupForContinueDone();
        
        var formData = $("#frmAssessPage").serialize();

        $.ajax({
            type: "POST",
            url: "Assess/?handler=Done&action=" + action,
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

        $("#modalAssessErrorMessage").html("");
        $("#modalAssessWarningMessage").html("");

        $("#modalAssessWait").hide();

        var ulTag = "<ul class=\"mb-0 pb-0\" />";
        
        if (responseJson == null) {
            $("#modalAssessErrorMessage").append(ulTag);
            var unOrderedList = $("#modalAssessErrorMessage ul");

            unOrderedList.append("<li class=\"pt-1 pb-1\" >System error. Please try again later</li>");

            $("#modalAssessErrors").show();
            return;
        }

        if (statusCode === customHttpStatusCodes.WarningsDetected) {

            $("#modalAssessWarningMessage").append(ulTag);
            var unOrderedList = $("#modalAssessWarningMessage ul");

            responseJson.forEach(function (item) {
                unOrderedList.append("<li class=\"pt-1 pb-1\" >" + item + "</li>");
            });
            
            $("#btnAssessWarningCancel").prop("disabled", false);
            $("#btnAssessWarningContinue").prop("disabled", false);

            $("#modalAssessWarnings").show();
            return;
        }

        if (statusCode === customHttpStatusCodes.FailedValidation) {
            $("#modalAssessErrorMessage").append(ulTag);
            var unOrderedList = $("#modalAssessErrorMessage ul");

            responseJson.forEach(function (item) {
                unOrderedList.append("<li class=\"pt-1 pb-1\" >" + item + "</li>");
            });

            $("#modalAssessErrors").show();
            return;
        }

        $("#modalAssessErrorMessage").append("<ul/>");
        var unOrderedList = $("#modalAssessErrorMessage ul");

        unOrderedList.append("<li>" + responseJson + "</li>");

        $("#modalAssessErrors").show();
    }

    function initialisePopup() {
        $("#modalAssessWait").hide();
        $("#modalAssessErrors").hide();
        $("#modalAssessProgressWarning").hide();
        $("#modalAssessWarnings").hide();
    }

    function setModalAssessPopupHiddenHandler() {
        $("#modalAssessPopup").on("hidden.bs.modal",
            function () {
                $("#modalAssessWait").hide();
                $("#modalAssessErrors").hide();
                $("#modalAssessProgressWarning").hide();
                $("#modalAssessWarnings").hide();
                mainButtonsEnabled(true);
            });
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

    function removeOperatorsInitialiseErrors() {
        $("#operatorsErrorMessages").collapse("hide");
        $("#operatorsErrorList").empty();
    }

    function mainButtonsEnabled(isEnabled) {
        $("#btnDone").prop("disabled", !isEnabled);
        $("#btnSave").prop("disabled", !isEnabled);
        $("#btnClose").prop("disabled", !isEnabled);
    }
    
    function populateAndShowWaitPopupForDone() {
        $("#modalAssessPopup h4.modal-title").text("Progressing task");

        $("#btnCancelAssessProgressWarning").prop("disabled", false);
        $("#btnContinueAssessProgress").prop("disabled", false);

        $("#modalAssessProgressWarning").show();
    }
    
    function populateAndShowWaitPopupForContinueDone() {

        $("#modalAssessPopup h4.modal-title").text("Progressing task");


        $("#modalAssessWaitMessage").html("");

        var ulTag = "<ul class=\"mb-0 pb-0\" />";


        $("#modalAssessWaitMessage").append(ulTag);
        var unOrderedList = $("#modalAssessWaitMessage ul");

        unOrderedList.append("<li class=\"pt-1 pb-1\" >Verifying Data...</li>");
        unOrderedList.append("<li class=\"pt-1 pb-1\" >Progressing Task...</li>");

        $("#modalAssessWait").show();
    }

    function populateAndShowWaitPopupForSave() {

        $("#modalAssessPopup h4.modal-title").text("Saving task data");

        $("#modalAssessWaitMessage").html("");

        var ulTag = "<ul class=\"mb-0 pb-0\" />";


        $("#modalAssessWaitMessage").append(ulTag);
        var unOrderedList = $("#modalAssessWaitMessage ul");

        unOrderedList.append("<li class=\"pt-1 pb-1\" >Verifying Data...</li>");
        unOrderedList.append("<li class=\"pt-1 pb-1\" >Saving Data...</li>");

        $("#modalAssessWait").show();
    }
    
    function hasUnsavedChanges() {
        if (formChanged) {

            $("#modalAssessErrorMessage").html("");

            var ulTag = "<ul class=\"mb-0 pb-0\" />";

            $("#modalAssessErrorMessage").append(ulTag);

            var unOrderedList = $("#modalAssessErrorMessage ul");
            unOrderedList.append("<li class=\"pt-1 pb-1\">Unsaved changes detected, please Save first.</li>");

            $("#modalAssessErrors").show();

            mainButtonsEnabled(true);
            return true;
        }
        return false;
    }

});
