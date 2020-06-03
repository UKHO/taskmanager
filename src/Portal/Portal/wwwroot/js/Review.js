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

    if (isReadOnly) {
        makeFormReadOnly($("#frmReviewPage"));
    }

    var formChanged = false;

    if ($("#reviewErrorMessage").html().trim().length > 0) {
        $("#modalWaitReviewErrors").modal("show");
    }
    
    function setUnsavedChangesHandlers() {
        $("#frmReviewPage").change(function() {
             formChanged = true;
        });

        window.onbeforeunload = function() {
            if (formChanged) {
                return "Changes detected";
            }
        }
    }
    function attachTerminateHandlers() {
        $("#btnTerminate").on("click", function () {

            var processId = $("#hdnProcessId").serialize();

            $.ajax({
                type: "POST",
                url: "Review/?handler=ValidateTerminate",
                beforeSend: function (xhr) {
                    xhr.setRequestHeader("RequestVerificationToken", $('input:hidden[name="__RequestVerificationToken"]').val());
                },
                data: processId,
                success: function () {
                    $("#txtTerminateComment").val("");
                    $("#ConfirmTerminateError").html("");
                    $("#ConfirmTerminate").modal("show");
                    console.log("success");
                },
                error: function (error) {
                    $("#reviewTerminateErrorMessage").html("");

                    var responseJson = error.responseJSON;
                    var statusCode = error.status;

                    if (responseJson != null) {
                        if (statusCode === customHttpStatusCodes.FailedValidation) {

                            $("#reviewTerminateErrorMessage").append("<ul/>");
                            var validateTerminateErrorList = $("#reviewTerminateErrorMessage ul");

                            responseJson.forEach(function (item) {
                                validateTerminateErrorList.append("<li>" + item + "</li>");
                            });

                            $("#modalWaitReviewTerminateErrors").modal("show");

                        } 
                    } else {

                        $("#reviewTerminateErrorMessage").append("<ul/>");
                        var unOrderedList = $("#reviewTerminateErrorMessage ul");

                        unOrderedList.append("<li>System error. Please try again later</li>");

                        $("#modalWaitReviewTerminateErrors").modal("show");
                    }
                }
            });
        });

        $("#txtTerminateComment").keydown(function (e) {
            $("#ConfirmTerminateError").html("");
        });

        $("#btnConfirmTerminate").on("click", function () {
            if ($("#txtTerminateComment").val() === "") {
                $("#ConfirmTerminateError")
                    .html("<div class=\"alert alert-danger\" role=\"alert\">Please enter a comment.</div>");
                $("#txtTerminateComment").focus();
                return;
            }
            submitTerminateForm();
        });

        $("#ConfirmTerminate").on("shown.bs.modal", function () {
            $("#txtTerminateComment").focus();
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

                mainButtonsEnabled(true);
                return;
            }

            // display: progress warning modal
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
        $("#reviewErrorMessage").html("");
        $("#modalWaitReviewSave").modal("show");

        var formData = $("#frmReviewPage").serialize();

        $.ajax({
            type: "POST",
            url: "Review/?handler=Save",
            beforeSend: function (xhr) {
                xhr.setRequestHeader("RequestVerificationToken", $('input:hidden[name="__RequestVerificationToken"]').val());
            },
            data: formData,
            complete: function () {
                //Add a delay to account for the modalWaitReviewDone modal
                //not being fully shown, before trying to hide it
                window.setTimeout(function () {
                    $("#modalWaitReviewSave").modal("hide");
                    mainButtonsEnabled(true);
                }, 200);
            },
            success: function (result) {
                formChanged = false;
                console.log("success");
            },
            error: function (error) {
                var responseJson = error.responseJSON;

                if (responseJson != null) {
                    $("#reviewErrorMessage").append("<ul/>");
                    var unOrderedList = $("#reviewErrorMessage ul");

                    responseJson.forEach(function (item) {
                        unOrderedList.append("<li>" + item + "</li>");
                    });

                    $("#modalWaitReviewErrors").modal("show");
                }

            }
        });
    }

    function processReviewDone() {
        mainButtonsEnabled(false);

        $("#modalReviewProgressWarning").modal("hide");
        $("#reviewErrorMessage").html("");
        $("#modalWaitReviewDone").modal("show");

        var formData = $("#frmReviewPage").serialize();

        $.ajax({
            type: "POST",
            url: "Review/?handler=Done",
            beforeSend: function (xhr) {
                xhr.setRequestHeader("RequestVerificationToken", $('input:hidden[name="__RequestVerificationToken"]').val());
            },
            data: formData,
            complete: function () {
                //Add a delay to account for the modalWaitReviewDone modal
                //not being fully shown, before trying to hide it
                window.setTimeout(function () {
                    $("#modalWaitReviewDone").modal("hide");
                    mainButtonsEnabled(true);
                }, 200);
            },
            success: function (result) {
                formChanged = false;
                window.location.replace("/Index");
                console.log("success");
            },
            error: function (error) {
                var responseJson = error.responseJSON;

                if (responseJson != null) {
                    $("#reviewErrorMessage").append("<ul/>");
                    var unOrderedList = $("#reviewErrorMessage ul");

                    responseJson.forEach(function (item) {
                        unOrderedList.append("<li>" + item + "</li>");
                    });

                    $("#modalWaitReviewErrors").modal("show");
                }

            }
        });
    }

    function submitTerminateForm() {
        var formData = $("#terminatingReview").serialize();

        $("#btnConfirmTerminate").prop("disabled", true);
        $("#btnCancelTerminate").prop("disabled", true);

        $("#reviewTerminateErrorMessage").html("");

        //anonymous function to allow chaining for modal show/hide
        var reviewTerminateAjax = function () {
            $.ajax({
                type: "POST",
                url: "Review/?handler=ReviewTerminate",
                beforeSend: function (xhr) {
                    xhr.setRequestHeader("RequestVerificationToken", $('input:hidden[name="__RequestVerificationToken"]').val());
                },
                data: formData,
                complete: function () {
                    console.log("terminate complete");

                },
                success: function (result) {
                    console.log("terminate success");
                    formChanged = false;

                    window.location.replace("/Index");
                },
                error: function (error) {
                    console.log("terminate error");
                    $("#btnConfirmTerminate").prop("disabled", false);
                    $("#btnCancelTerminate").prop("disabled", false);

                    var responseJson = error.responseJSON;
                    var statusCode = error.status;

                    $("#reviewTerminateErrorMessage").append("<ul/>");
                    var unOrderedList = $("#reviewTerminateErrorMessage ul");

                    if (responseJson == null) {
                        unOrderedList.append("<li>System error. Please try again later</li>");
                    } else if (statusCode === customHttpStatusCodes.FailuresDetected) {
                        responseJson.forEach(function (item) {
                            unOrderedList.append("<li>" + item + "</li>");
                        });
                    } else {
                        unOrderedList.append("<li>" + responseJson + "</li>");
                    }

                    //Hide modalWaitReviewTerminate modal and show modalWaitReviewTerminateErrors modal
                    $("#modalWaitReviewTerminate").one("hidden.bs.modal", function () {
                        $("#modalWaitReviewTerminateErrors").modal("show");
                    });
                    $("#modalWaitReviewTerminate").modal("hide");
                }
            });
        }

        //Hide ConfirmTerminate modal and show modalWaitReviewTerminate modal,
        //when modalWaitReviewTerminate modal shows, initiate AJAX
        $("#ConfirmTerminate").one("hidden.bs.modal", function () {
            $("#modalWaitReviewTerminate").modal("show");
        });
        $("#modalWaitReviewTerminate").one("shown.bs.modal", function () {
            reviewTerminateAjax();
        });
        $("#ConfirmTerminate").modal("hide");
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
                    suggestion: function(users) {
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

    function mainButtonsEnabled(isEnabled)
    {
        $("#btnDone").prop("disabled", !isEnabled);
        $("#btnSave").prop("disabled", !isEnabled);
        $("#btnTerminate").prop("disabled", !isEnabled);
        $("#btnClose").prop("disabled", !isEnabled);
    }
});