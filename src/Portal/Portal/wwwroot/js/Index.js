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

    $("#btnClearTaskNote").click(function() {
        $("#txtNote").val("");
    });

    function getTasks() {

        $.ajax({
            type: "GET",
            beforeSend: function (xhr) {
                xhr.setRequestHeader("XSRF-TOKEN", $('input:hidden[name="__RequestVerificationToken"]').val());
            },
            success: function (result) {
                location.reload();
            },
            error: function (error) {
                //$("#AddCommentError")
                //    .html("<div class=\"alert alert-danger\" role=\"alert\">Failed to load comments.</div>");
            }
        });
    }

    $("#btnPostTaskNote").click(function () {
        $("#btnPostTaskNote").prop("disabled", true);

        $.ajax({
            type: "POST",
            url: "Index/?handler=TaskNote",
            beforeSend: function (xhr) {
                xhr.setRequestHeader("RequestVerificationToken", $('input:hidden[name="__RequestVerificationToken"]').val());
            },
            data: {
                "taskNote": $("#txtNote").val(),
                "processId": $("#hdnProcessId").val()
            },
            success: function (result) {
                $("#editTaskNoteModal").modal("hide");
                $(".modal-backdrop").remove();
                $("body").removeClass("modal-open");
                getTasks();
            },
            error: function (error) {
                console.log(error);
                $("#editTaskNoteError")
                    .html("<div class=\"alert alert-danger\" role=\"alert\">Error updating task note. Please try again later.</div>");

            }
        });
    });

});