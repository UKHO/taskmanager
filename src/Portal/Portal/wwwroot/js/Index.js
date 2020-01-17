$(document).ready(function () {

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

    $("#btnPostTaskNote").on("submit", function () {
        $("#btnPostTaskNote").prop("disabled", true);

    });

    $(".assignTaskItem").on("click",
        function () {

            $("#btnAssignTaskToUser").prop("disabled", false);
            $("#AssignTaskError").html("");

            var processId = $(this).data("processid");
            $("#hdnAssignTaskProcessId").val(processId);

            var taskStage = $(this).data("taskstage");
            $("#hdnAssignTaskStage").val(taskStage);

            $("#assignTaskModal").modal("show");
        });

    $("#assignTaskModal").on("shown.bs.modal",
        function () {
            $("#txtUsername").focus();
            $('.typeahead').typeahead('val', "");
            $('.typeahead').typeahead('close');
        });

    $("#btnAssignTaskToUser").on("click", function () {
        $("#btnAssignTaskToUser").prop("disabled", true);

        var processId = $("#hdnAssignTaskProcessId").val();
        var userName = $("#txtUsername").val();
        var taskStage = $("#hdnAssignTaskStage").val();

        $.ajax({
            type: "POST",
            url: "Index/?handler=AssignTaskToUser",
            beforeSend: function (xhr) {
                xhr.setRequestHeader("RequestVerificationToken", $('input:hidden[name="__RequestVerificationToken"]').val());
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
                //getComments();
            },
            error: function (error) {
                console.log(error);

                //$("#AddCommentError")
                //    .html("<div class=\"alert alert-danger\" role=\"alert\">Error adding comment. Please try again later.</div>");

                //$("#btnPostComment").prop("disabled", false);
            }
        });

    });

    initialiseAssignTaskTypeahead();

    function initialiseAssignTaskTypeahead() {
        $('#assignTaskTypeaheadError').collapse("hide");
        // Constructing the suggestion engine
        var users = new Bloodhound({
            datumTokenizer: Bloodhound.tokenizers.whitespace,
            queryTokenizer: Bloodhound.tokenizers.whitespace,
            prefetch: {
                url: "Index/?handler=Users",
                ttl: 600000
            },
            initialize: false
        });

        var promise = users.initialize();
        promise.fail(function () {
            $('#assignTaskTypeaheadError').collapse("show");
        });

        // Initializing the typeahead
        $('.typeahead').typeahead({
            hint: true,
            highlight: true, /* Enable substring highlighting */

            minLength:
                3 /* Specify minimum characters required for showing result */
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