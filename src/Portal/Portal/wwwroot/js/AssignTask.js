$(document).ready(function () {

    $(".deleteAssignTask").click(function () {
        var ordinal = $(this).data("ordinal");
        var assignTaskId = $(this).data("assignTaskId");

        alert(ordinal);
        alert(assignTaskId);
    });
});