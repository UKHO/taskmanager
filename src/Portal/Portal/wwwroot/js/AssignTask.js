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
            //Set Form Control Names
            $(element).find($(".assignTaskAssessor")).prop("name", "AssignTaskModel[" + index + "].Assessor.AssessorId");
            $(element).find($(".assignTaskVerifier")).prop("name", "AssignTaskModel[" + index + "].Verifier.VerifierId");
            $(element).find($(".assignTaskSourceType")).prop("name", "AssignTaskModel[" + index + "].AssignedTaskSourceType.AssignedTaskSourceTypeId");
            $(element).find($(".assignTaskWorkspaceAffected")).prop("name", "AssignTaskModel[" + index + "].WorkspaceAffected");
            $(element).find($(".assignTaskNotes")).prop("name", "AssignTaskModel[" + index + "].Notes");

            //Set Heading
            $(element).find("span").text("Assign Task " + (index + 1));

            if (index > 0) {
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