$(document).ready(function () {

    $(".taskNoteItem").on("click",
        function () {
            var processId = $(this).data("processid");
            $("#hdnProcessId").val(processId);
            $("#addTaskNoteModal").modal("show");
        });

    $("#btnPostTaskNote").click(function () {

        if ($("#txtNote").val() === "") {
            $("#AddTaskError")
                .html("<div class=\"alert alert-danger\" role=\"alert\">Please enter a task note.</div>");
            $("#txtNote").focus();
        } else {
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
                    $("#addTaskNoteModal").modal("hide");
                    $(".modal-backdrop").remove();
                    $("body").removeClass("modal-open");
                },
                error: function (error) {
                    console.log(error);
                }
            });
        }
    });

});