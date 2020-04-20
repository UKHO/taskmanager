﻿$(document).ready(function () {

    $('#operators').find('div .operator:gt(0)').hide();
    
    initialiseOperatorsTypeaheads();
    
    setReviewDoneHandler();
    setReviewSaveHandler();

    attachTerminateHandlers();

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

    function attachTerminateHandlers() {
        $("#btnTerminate").on("click", function () {
            $("#ConfirmTerminate").modal("show");
        });

        $("#txtTerminateComment").keydown(function (e) {
            if (e.keyCode !== 13) {
                $("#ConfirmTerminateError").html("");
                return;
            }
            $("#btnConfirmTerminate").trigger("click");
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

    function completeReview(action) {
        $("#reviewDoneErrorMessage").html("");
        $("#btnDone").prop("disabled", true);
        $("#btnSave").prop("disabled", true);
        $("#modalWaitReviewDone").modal("show");

        var formData = $("#frmReviewPage").serialize();

        $.ajax({
            type: "POST",
            url: "Review/?handler=Done&action=" + action,
            beforeSend: function (xhr) {
                xhr.setRequestHeader("RequestVerificationToken", $('input:hidden[name="__RequestVerificationToken"]').val());
            },
            data: formData,
            complete: function () {
                //Add a delay to account for the modalWaitReviewDone modal
                //not being fully shown, before trying to hide it
                window.setTimeout(function () {
                    $("#modalWaitReviewDone").modal("hide");
                    $("#btnDone").prop("disabled", false);
                    $("#btnSave").prop("disabled", false);
                }, 200);
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

    function submitTerminateForm() {
        var formData = $("#terminatingReview").serialize();

        $("#btnConfirmTerminate").prop("disabled", true);
        $("#btnCancelTerminate").prop("disabled", true);

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

                //TODO: error functionality
            }
        });
    }

    function initialiseOperatorsTypeaheads() {

        removeOperatorsInitialiseErrors();

        $('#Reviewer, #Assessor, #Verifier').typeahead('val', "");
        $('#Reviewer, #Assessor, #Verifier').typeahead('close');

        var users = new Bloodhound({
            datumTokenizer: Bloodhound.tokenizers.whitespace,
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
                highlight: true,    /* Enable substring highlighting */
                minLength: 3        /* Specify minimum characters required for showing result */
            },
            {
                name: 'users',
                source: users,
                limit: 100,
                templates: {
                    notFound: '<div>No results</div>'
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
});