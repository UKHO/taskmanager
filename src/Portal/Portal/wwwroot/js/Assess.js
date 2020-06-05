$(document).ready(function () {

    var customHttpStatusCodes = JSON.parse($("#SerialisedCustomHttpStatusCodes").val());

    $("#Reviewer").prop("disabled", true);


    initialiseOperatorsTypeaheads();

    setAssessDoneHandler();
    setAssessSaveHandler();
    handleContinueAssessDoneWarning();
    setUnsavedChangesHandlers();
    handlebtnContinueAssessProgress();

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

    if ($("#assessErrorMessage").html().trim().length > 0) {
        $("#modalWaitAssessErrors").modal("show");
    }

    function setAssessDoneHandler() {
        $("#btnDone").click(function (e) {

            mainButtonsEnabled(false);

            // check for unsaved changes
            if (formChanged) {

                $("#assessErrorMessage").html("");

                $("#assessErrorMessage").append("<ul/>");
                var unOrderedList = $("#assessErrorMessage ul");
                unOrderedList.append("<li>Unsaved changes detected, please Save first.</li>");

                $("#modalWaitAssessErrors").modal("show");

                mainButtonsEnabled(true);
                return;
            }

            // display: progress warning modal
            $("#btnContinueAssessProgress").prop("disabled", false);
            $("#btnCancelAssessProgressWarning").prop("disabled", false);
            $("#modalAssessProgressWarning").modal("show");

            mainButtonsEnabled(true);

        });
    }

    function handlebtnContinueAssessProgress() {
        $("#btnContinueAssessProgress").on("click", function (e) {
            processAssessDone("Done", $("#modalAssessProgressWarning"));
        });
    }

    function handleContinueAssessDoneWarning() {
        $("#btnContinueAssessDoneWarning").on("click", function (e) {
            processAssessDone("ConfirmedDone", $("#modalAssessDoneWarning"));
        });
    }
    function setAssessSaveHandler() {
        $("#btnSave").click(function (e) {
            processAssessSave();
        });
    }

    function processAssessSave() {
        mainButtonsEnabled(false);
        $("#assessErrorMessage").html("");
        $("#modalWaitAssessSave").modal("show");

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
                $("#modalWaitAssessSave").modal("hide");
            },
            error: function (error) {
                processErrors(error, $("#modalWaitAssessSave"));
            }
        });
    }

    function processAssessDone(action, modalWarningPopup) {
        mainButtonsEnabled(false);

        $("#btnContinueAssessProgress").prop("disabled", true);
        $("#btnCancelAssessProgressWarning").prop("disabled", true);

        $("#assessErrorMessage").html("");
        $("#assessDoneWarningMessages").html("");

        hideOnePopupAndShowAnother(modalWarningPopup, $("#modalWaitAssessDone"));

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
                processErrors(error, $("#modalWaitAssessDone"));
            }
        });
    }

    function processErrors(error, modalWaitPopup) {
        var responseJson = error.responseJSON;
        var statusCode = error.status;

        if (responseJson == null) {
            $("#assessErrorMessage").append("<ul/>");
            var unOrderedList = $("#assessErrorMessage ul");

            unOrderedList.append("<li>System error. Please try again later</li>");

            hideOnePopupAndShowAnother(modalWaitPopup, $("#modalWaitAssessErrors"));
            return;
        }

        if (statusCode === customHttpStatusCodes.WarningsDetected) {

            $("#assessDoneWarningMessages").append("<ul/>");
            var unOrderedList = $("#assessDoneWarningMessages ul");

            responseJson.forEach(function (item) {
                unOrderedList.append("<li>" + item + "</li>");
            });

            hideOnePopupAndShowAnother(modalWaitPopup, $("#modalAssessDoneWarning"));
            return;
        }

        if (statusCode === customHttpStatusCodes.FailedValidation) {
            $("#assessErrorMessage").append("<ul/>");
            var unOrderedList = $("#assessErrorMessage ul");

            responseJson.forEach(function (item) {
                unOrderedList.append("<li>" + item + "</li>");
            });

            hideOnePopupAndShowAnother(modalWaitPopup, $("#modalWaitAssessErrors"));
            return;
        }

        $("#assessErrorMessage").append("<ul/>");
        var unOrderedList = $("#assessErrorMessage ul");

        unOrderedList.append("<li>" + responseJson + "</li>");

        hideOnePopupAndShowAnother(modalWaitPopup, $("#modalWaitAssessErrors"));
    }

    function hideOnePopupAndShowAnother(popuptoHide, popupToShow) {
        popuptoHide.one("hidden.bs.modal", function () {
            popupToShow.modal("show");
        });
        popuptoHide.modal("hide");

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

});
