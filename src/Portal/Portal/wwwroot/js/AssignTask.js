$(document).ready(function () {

    var processId = Number($("#hdnProcessId").val());
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
                update();
            },
            error: function (error) {
                $("#assignTasksError")
                    .html("<div class=\"alert alert-danger\" role=\"alert\">Failed to load Assign Tasks. Please try again later.</div>");
            }
        });
    }

    function update() {
        $(".assignTask").each(function (index, element) {
            var id = index - 1;

            if (index > 0) {            //Set Form Control Names
                $(element).find($(".assignTaskAssessor")).prop("name", "AdditionalAssignedTasks[" + id + "].Assessor");
                $(element).find($(".assignTaskVerifier")).prop("name", "AdditionalAssignedTasks[" + id + "].Verifier");
                $(element).find($(".assignTaskSourceType")).prop("name", "AdditionalAssignedTasks[" + id + "].AssignedTaskSourceType");
                $(element).find($(".assignTaskWorkspaceAffected")).prop("name", "AdditionalAssignedTasks[" + id + "].WorkspaceAffected");
                $(element).find($(".assignTaskNotes")).prop("name", "AdditionalAssignedTasks[" + id + "].Notes");

                //Set Heading
                $(element).find("span").text("Assign Task " + (index + 1));

                setDeleteHandler($(element).find(".deleteAssignTask"));
            }

        });
    };

    function setCreateHandler() {
        $("#btnCreateTask").on("click", function (e) {
            var currentCount = $(".assignTask").length;
            var newThing = $($(".assignTask")[0]).clone();

            $(newThing).find(".assignTaskAssessor").val(0);
            $(newThing).find(".assignTaskVerifier").val(0);
            $(newThing).find(".assignTaskSourceType").val(0);
            $(newThing).find(".assignTaskWorkspaceAffected").val("");
            $(newThing).find(".assignTaskNotes").val("");

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