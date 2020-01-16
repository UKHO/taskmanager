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

            $("#assignTaskModal").modal("show");
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