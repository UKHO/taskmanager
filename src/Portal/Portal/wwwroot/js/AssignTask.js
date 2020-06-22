﻿$(document).ready(function () {

    var processId = Number($("#hdnProcessId").val());
    var isReadOnly = $("#IsReadOnly").val() === "True";
    getAssignedTasks();

    function getAssignedTasks() {
        $.ajax({
            type: "GET",
            url: "_AssignTask",
            beforeSend: function (xhr) {
                xhr.setRequestHeader("XSRF-TOKEN", $('input:hidden[name="__RequestVerificationToken"]').val());
            },
            contentType: "application/json; charset=utf-8",
            data: { "processId": processId },
            success: function (result) {
                $("#assignTasks").html(result);

                setCreateHandler();
                initialiseTypeaheads($('.assignTaskAssessor, .assignTaskVerifier'));
                update();

                if (isReadOnly) {
                    $(".deleteAssignTask").off("click");
                    $(".btnCreateTask").off("click");
                }
            },
            error: function (error) {
                $("#assignTasksError")
                    .html("<div class=\"alert alert-danger\" role=\"alert\">Failed to load Assign Tasks. Please try again later.</div>");
            }
        });
    }

    function initialiseTypeaheads(typeaheadElements) {

        removeTypeaheadsInitialiseErrors();

        typeaheadElements.typeahead('val', "");
        typeaheadElements.typeahead('close');

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
                removeTypeaheadsInitialiseErrors();
            })
            .fail(function () {
                var errorArray = ["Failed to look up users. Try refreshing the page."];
                displayTypeaheadsInitialiseErrors(errorArray);
            });

        typeaheadElements.typeahead({
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

    function displayTypeaheadsInitialiseErrors(errorStringArray) {

        var orderedList = $(".assignTask .dialog ol.error-list");

        // == to catch undefined and null
        if (errorStringArray == null) {
            orderedList.append("<li>An unknown error has occurred</li>");

        } else {
            errorStringArray.forEach(function (item) {
                orderedList.append("<li>" + item + "</li>");
            });
        }

        $(".assignTask .dialog").show();
    }

    function removeTypeaheadsInitialiseErrors() {
        $(".assignTask .dialog").hide();
        $(".assignTask .dialog ol.error-list").empty();
    }

    function update() {
        $(".assignTask").each(function (index, element) {
            var id = index - 1;

            if (index > 0) {            //Set Form Control Names
                $(element).find($(".assignTaskAssessor:not(.tt-hint)")).prop("name", "AdditionalAssignedTasks[" + id + "].Assessor");
                $(element).find($(".assignTaskVerifier:not(.tt-hint)")).prop("name", "AdditionalAssignedTasks[" + id + "].Verifier");
                $(element).find($(".assignTaskType")).prop("name", "AdditionalAssignedTasks[" + id + "].TaskType");
                $(element).find($(".assignTaskWorkspaceAffected")).prop("name", "AdditionalAssignedTasks[" + id + "].WorkspaceAffected");
                $(element).find($(".assignTaskNotes")).prop("name", "AdditionalAssignedTasks[" + id + "].Notes");

                //Set Heading
                $(element).find("h6 > span").text("Assign Task " + (index + 1));

                setDeleteHandler($(element).find(".deleteAssignTask"));
            }

        });
    };

    function setCreateHandler() {
        $("#btnCreateTask").on("click", function (e) {
            var currentCount = $(".assignTask").length;
            var newThing = $($(".assignTask")[0]).clone();

            var assessorElement = $(newThing).find(".assignTaskAssessor:not(.tt-hint)");
            var verifierElement = $(newThing).find(".assignTaskVerifier:not(.tt-hint)");

            assessorElement.val("");
            verifierElement.val("");
            $(newThing).find(".assignTaskType").val(0);
            $(newThing).find(".assignTaskWorkspaceAffected").val("");
            $(newThing).find(".assignTaskNotes").val("");

            //Remove typeahead elements from cloned assign task
            //leaving just the input textbox remaining
            assessorElement.appendTo(assessorElement.parents("div")[0]);
            assessorElement.siblings("span").remove();
            verifierElement.appendTo(verifierElement.parents("div")[0]);
            verifierElement.siblings("span").remove();

            initialiseTypeaheads(assessorElement.add(verifierElement));

            $("#assignTasks").append(newThing);
            $(newThing).show();

            update();
        });
    }

    function setDeleteHandler(element) {
        $(element).off("click").click(function () {
            $(element).parents(".assignTask").remove();

            update();
        }).show();
    }
});