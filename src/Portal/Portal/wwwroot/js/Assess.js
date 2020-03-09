﻿$(document).ready(function () {

    $("#Reviewer").prop("disabled", true);


    initialiseOperatorsTypeaheads();

    setAssessDoneHandler();
    setAssessSaveHandler();

    var formChanged = false;
    $("#frmAssessPage").change(function () { formChanged = true; });

    window.onbeforeunload = function () {
        if (formChanged) {
            return "Changes detected";
        }
    };

    if ($("#assessDoneErrorMessage").html().trim().length > 0) {
        $("#modalWaitAssessDoneErrors").modal("show");
    }

    function completeAssess(action) {
        $("#assessDoneErrorMessage").html("");
        $("#btnDone").prop("disabled", true);
        $("#btnSave").prop("disabled", true);
        $("#modalWaitAssessDone").modal("show");

        var onHold = false;

        if (document.getElementById("onHoldToggle").checked) {
            onHold = true;
        }

        var formData = new FormData();
        formData.append("formData", $("#frmAssessPage").serialize());
        formData.append("onHold", onHold);

        $.ajax({
            type: "POST",
            url: "Assess/?handler=Done&action=" + action,
            beforeSend: function (xhr) {
                xhr.setRequestHeader("RequestVerificationToken", $('input:hidden[name="__RequestVerificationToken"]').val());
            },
            data: formData,
            contentType: false,
            processData: false,
            complete: function () {
                //Add a delay to account for the modalWaitReviewDone modal
                //not being fully shown, before trying to hide it
                window.setTimeout(function () {
                    $("#modalWaitAssessDone").modal("hide");
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
                    $("#assessDoneErrorMessage").append("<ul/>");
                    var unOrderedList = $("#assessDoneErrorMessage ul");

                    responseJson.forEach(function (item) {
                        unOrderedList.append("<li>" + item + "</li>");
                    });

                    $("#modalWaitAssessDoneErrors").modal("show");
                }

            }
        });
    }

    function setAssessDoneHandler() {
        $("#btnDone").prop("disabled", false);


        $("#btnDone").click(function (e) {
            completeAssess("Done");
        });
    }

    function setAssessSaveHandler() {
        $("#btnSave").prop("disabled", false);


        $("#btnSave").click(function (e) {
            completeAssess("Save");
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


    function removeOperatorsInitialiseErrors() {
        $("#operatorsErrorMessages").collapse("hide");
        $("#operatorsErrorList").empty();
    }

});
