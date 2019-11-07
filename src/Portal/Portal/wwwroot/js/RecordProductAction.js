$(document).ready(function () {

    setCreateHandler();

    update();

    function update() {
        $(".assignTask").each(function (index, element) {
            //Set Form Control Names
            $(element).find($(".assignTaskAssessor")).prop("name", "AssignTaskModel[" + index + "].Assessor.AssessorId");
            $(element).find($(".assignTaskVerifier")).prop("name", "AssignTaskModel[" + index + "].Verifier.VerifierId");
            $(element).find($(".assignTaskSourceType")).prop("name", "AssignTaskModel[" + index + "].SourceType.SourceTypeId");
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
        $("#btnAddImpact").on("click", function (e) {
            var currentCount = $(".recordProductAction").length;
            var newThing = $($(".recordProductAction")[0]).clone();

            $(newThing).find(".impactedProduct").val("");
            $(newThing).find(".productActionType").val("");

            $("#productActions").append(newThing);
            $(newThing).show();

            update();
        });
    }

    function setDeleteHandler(element) {
        $(element).off("click").click(function () {
            $(this).parent().parent().remove();

            update();
        }).show();
    }
});