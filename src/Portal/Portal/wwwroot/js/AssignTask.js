$(document).ready(function () {

    $(".deleteAssignTask").click(function () {
        var ordinal = $(this).data("ordinal");
        var assignTaskId = $(this).data("assigntaskid"); // TODO: use when calling via ajax to update DB
        $("#assignTask" + ordinal).remove();
    });
});