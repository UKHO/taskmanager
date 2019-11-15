$(document).ready(function () {

    $(".taskNoteItem").on("click",
        function () {
            var processId = $(this).data("processid");

            $("#addTaskNoteModal").modal("show");
        });

});