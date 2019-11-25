$(document).ready(function () {

    setCreateHandler();

    update();

    function update() {

        $(".recordProductAction").each(function (index, element) {
            if (index > 0) {
                $(element).find(".deleteAction").show();
            } else {
                $(element).find(".deleteAction").hide();
            }
            //Set Form Control Names
            //$(element).find($(".assignTaskAssessor")).prop("name", "AssignTaskModel[" + index + "].Assessor.AssessorId");
            //$(element).find($(".assignTaskVerifier")).prop("name", "AssignTaskModel[" + index + "].Verifier.VerifierId");
            //$(element).find($(".assignTaskSourceType")).prop("name", "AssignTaskModel[" + index + "].SourceType.SourceTypeId");
            //$(element).find($(".assignTaskWorkspaceAffected")).prop("name", "AssignTaskModel[" + index + "].WorkspaceAffected");
            //$(element).find($(".assignTaskNotes")).prop("name", "AssignTaskModel[" + index + "].Notes");

            //Set Heading
            //$(element).find("span").text("Assign Task " + (index + 1));
            if (index > 0) {
                setDeleteHandler($(element).find(".deleteAction"));
            }

            var pageIdentity = $("#pageIdentity").val();
            if (pageIdentity === "Assess") {
                $(".verified").prop("disabled", true);
            } else if (pageIdentity === "Verify") {
                $(".verified").prop("disabled", false);
            }

        });
    };

    function setCreateHandler() {
        $("#btnAddImpact").on("click", function (e) {
            var currentCount = $(".recordProductAction").length;
            var newThing = $($(".recordProductAction")[0]).clone();

            $(newThing).find(".impactedProduct").val(0);
            $(newThing).find(".productActionType").val(0);

            $("#productActions").append(newThing);
            $(newThing).show();

            update();
        });
    }

    function setDeleteHandler(element) {
        $(element).on("click").click(function () {
            $(element).parents(".recordProductAction").remove();

            update();
        }).show();
    }
});