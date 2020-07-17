$(document).ready(function () {

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
                $('.assignTaskAssessor').on('typeahead:selected', function (eventObject, suggestionObject) {
                    $('#assignTaskAssessorUpn').val(suggestionObject.userPrincipalName);
                });
                $('.assignTaskVerifier').on('typeahead:selected', function(eventObject, suggestionObject) {
                    $('#assignTaskVerifierUpn').val(suggestionObject.userPrincipalName);
                });
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

            if (index > 0) {

                // Assign unique id for inputs, and link its label 'for' to the new id
                assignNewIdForAdditionalAssignedTasks($(element), id);

                //Set Form Control Names
                $(element).find($(".assignTaskAssessor:not(.tt-hint)")).prop("name", "AdditionalAssignedTasks[" + id + "].Assessor.DisplayName");
                $(element).find($(".assignTaskVerifier:not(.tt-hint)")).prop("name", "AdditionalAssignedTasks[" + id + "].Verifier.DisplayName");                
                $(element).find($(".assignTaskAssessorUpn")).prop("name", "AdditionalAssignedTasks[" + id + "].Assessor.UserPrincipalName");
                $(element).find($(".assignTaskVerifierUpn")).prop("name", "AdditionalAssignedTasks[" + id + "].Verifier.UserPrincipalName");
                $(element).find($(".assignTaskType")).prop("name", "AdditionalAssignedTasks[" + id + "].TaskType");
                $(element).find($(".assignTaskWorkspaceAffected")).prop("name", "AdditionalAssignedTasks[" + id + "].WorkspaceAffected");
                $(element).find($(".assignTaskNotes")).prop("name", "AdditionalAssignedTasks[" + id + "].Notes");

                //Set Heading
                $(element).find("h6 > span").text("Assign Task " + (index + 1));

                setDeleteHandler($(element).find(".deleteAssignTask"));
            }

            // Assign aria-labelledBy to type-ahead tt-hint
            assignAriaLabelledBy($(element), ".assignTaskAssessor.tt-hint");
            assignAriaLabelledBy($(element), ".assignTaskVerifier.tt-hint");
        });
    };

    function assignAriaLabelledBy(element, selector) {
        var currentInput = $(element).find(selector)[0];
        var currentParent = $(currentInput).closest(".form-row")[0];
        var currentLabel = $(currentParent).find(".col-form-label").first();
        var labelForValue = $(currentLabel).attr("for");
        $(currentInput).attr("aria-labelledby", labelForValue);
    }

    function assignNewIdForAdditionalAssignedTasks(element, id) {
        $(element).find("[id]").each(function () {
            var newId = $(this).attr("id");

            if (newId.indexOf("AdditionalAssignedTask_") < 0) {
                newId = newId.replace("PrimaryAssignedTask_", "") + id;
                newId = "AdditionalAssignedTask_" + newId;
                $(this).attr("id", newId);
            }
        });
        $(element).find("[for]").each(function () {
            var newAttr = $(this).attr("for"); 

            if (newAttr.indexOf("AdditionalAssignedTask_") < 0) {
                newAttr = newAttr.replace("PrimaryAssignedTask_", "") + id;
                newAttr = "AdditionalAssignedTask_" + newAttr;
                $(this).attr("for", newAttr);
            }
        });
    }

    function setCreateHandler() {
        $("#btnCreateTask").on("click", function (e) {
            var currentCount = $(".assignTask").length;
            var newThing = $($(".assignTask")[0]).clone();

            var assessorElement = $(newThing).find(".assignTaskAssessor:not(.tt-hint)");
            var assessorUpnElement = $(newThing).find("#assignTaskAssessorUpn");
            
            var verifierElement = $(newThing).find(".assignTaskVerifier:not(.tt-hint)");
            var verifierUpnElement = $(newThing).find("#assignTaskVerifierUpn");

            assessorElement.val("");
            assessorUpnElement.val("");
            verifierElement.val("");
            assessorUpnElement.val("");

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

            assessorElement.on('typeahead:selected', function (eventObject, suggestionObject) {
                assessorUpnElement.val(suggestionObject.userPrincipalName);

            });            
            verifierElement.on('typeahead:selected', function (eventObject, suggestionObject) {
                verifierUpnElement.val(suggestionObject.userPrincipalName);

            });

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