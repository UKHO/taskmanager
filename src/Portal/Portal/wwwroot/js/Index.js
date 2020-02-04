$(document).ready(function () {

    // Required unless we refactor behaviour around errors
    var usersFetched = false;

    $(".taskNoteItem").on("click",
        function () {

            $("#btnPostTaskNote").prop("disabled", false);
            $("#editTaskNoteError").html("");

            var processId = $(this).data("processid");
            $("#hdnProcessId").val(processId);

            var taskNote = $(this).data("tasknote");
            $("#txtNote").val(taskNote);

            $("#editTaskNoteModal").modal("show");
        });

    $("#editTaskNoteModal").on("shown.bs.modal",
        function () {
            $("#txtNote").focus();
        });

    $("#btnClearTaskNote").click(function () {
        $("#txtNote").val("");
        $("#txtNote").focus();
    });

    $("#btnPostTaskNote").on("submit",
        function () {
            $("#btnPostTaskNote").prop("disabled", true);

        });

    $(".assignTaskItem").on("click",
        function () {
            //$("#btnAssignTaskToUser").prop("disabled", false);

            var processId = $(this).data("processid");
            $("#hdnAssignTaskProcessId").val(processId);

            var taskStage = $(this).data("taskstage");
            $("#hdnAssignTaskStage").val(taskStage);

            $("#assignTaskModal").modal("show");
        });

    $("#assignTaskModal").on("shown.bs.modal",
        function () {
            $("#assignTaskTypeaheadError").hide();
            $("#assignTaskErrorMsg").text("");
            $("#txtUsername").focus();
            $('.typeahead').typeahead('val', "");
            $('.typeahead').typeahead('close');
        });

    $("#btnCancelAssignTask").on("click",
        function () {
            if (usersFetched) removeAssignUserErrors();
        });

    $("#btnAssignTaskToUser").on("click",
        function () {

            removeAssignUserErrors();            

            if ($("#txtUsername").val() === "") {
                
                var errorArray = ["Please enter a user"];
                displayAssignUserErrors(errorArray);

                return;
            }

            $("#btnAssignTaskToUser").prop("disabled", true);

            var processId = $("#hdnAssignTaskProcessId").val();
            var userName = $("#txtUsername").val();
            var taskStage = $("#hdnAssignTaskStage").val();

            $.ajax({
                type: "POST",
                url: "Index/?handler=AssignTaskToUser",
                beforeSend: function (xhr) {
                    xhr.setRequestHeader("RequestVerificationToken",
                        $('input:hidden[name="__RequestVerificationToken"]').val());
                },
                data: {
                    "processId": processId,
                    "userName": userName,
                    "taskStage": taskStage
                },
                success: function (result) {
                    $("#assignTaskModal").modal("hide");
                    $("body").removeClass("modal-open");
                    $(".modal-backdrop").remove();

                    window.location.reload();
                },
                error: function (error) {
                    var responseJson = error.responseJSON;

                    displayAssignUserErrors(responseJson);

                    $("#btnAssignTaskToUser").prop("disabled", false);
                }
            });

        });

    initialiseAssignTaskTypeahead();

    function initialiseAssignTaskTypeahead() {

        $('#assignTaskErrorMessages').collapse("hide");

        // Constructing the suggestion engine
        var users = new Bloodhound({
            datumTokenizer: Bloodhound.tokenizers.whitespace,
            queryTokenizer: Bloodhound.tokenizers.whitespace,
            prefetch: {
                url: "Index/?handler=Users",
                ttl: 60000
            },
            initialize: false
        });

        var promise = users.initialize();
        promise
            .done(function () {
                $("#btnAssignTaskToUser").prop("disabled", false);
                $("#txtUsername").prop("disabled", false);

                removeAssignUserErrors();

                usersFetched = true;
            })
            .fail(function () {
                $("#btnAssignTaskToUser").prop("disabled", true);
                $("#txtUsername").prop("disabled", true);

                var errorArray = ["Failed to look up users. Try refreshing the page."];
                displayAssignUserErrors(errorArray);

                usersFetched = false;
            });

        // Initializing the typeahead
        $('.typeahead').typeahead({
            hint: true,
            highlight: true,    /* Enable substring highlighting */

            minLength:
                3               /* Specify minimum characters required for showing result */
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
});

function displayAssignUserErrors(errorStringArray) {

    // == to catch undefined and null
    if (errorStringArray == null) return;

    var orderedList = $("#assignTaskErrorList");

    errorStringArray.forEach(function (item) {
        orderedList.append("<li>" + item + "</li>");
    });

    $("#assignTaskErrorMessages").collapse("show");
}

function removeAssignUserErrors() {
    $("#assignTaskErrorMessages").collapse("hide");
    $("#assignTaskErrorList").empty();
}