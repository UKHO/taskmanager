$(document).ready(function () {
    var id = 100; // TODO: Temporary solution for ADDED Record Product Action

    getProductActions();

    function getProductActions() {
        var processId = { "processId": Number($("#hdnProcessId").val()) };

        $.ajax({
            type: "GET",
            url: "_RecordProductAction",
            beforeSend: function (xhr) {
                xhr.setRequestHeader("XSRF-TOKEN", $('input:hidden[name="__RequestVerificationToken"]').val());
            },
            contentType: "application/json; charset=utf-8",
            data: processId,
            success: function (result) {
                $("#recordProductAction").html(result);

                setCreateHandler();
                update();
            },
            error: function (error) {
                $("#recordProductActionError")
                    .html("<div class=\"alert alert-danger\" role=\"alert\">Failed to load Product Actions.</div>");
            }
        });


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
                    $(".verifiedProduct").prop("disabled", true);
                } else if (pageIdentity === "Verify") {
                    $(".verifiedProduct").prop("disabled", false);
                }

            });
        };

        function setCreateHandler() {
            $("#btnAddImpact").on("click", function (e) {
                id += 1; // TODO: Temporary solution for ADDED Record Product Action
                var currentCount = $(".recordProductAction").length;
                var newThing = $($(".recordProductAction")[0]).clone();

                $(newThing).find(".impactedProduct").val(0);
                $(newThing).find(".productActionType").val(0);
                $(newThing).find(".verifiedProduct").attr('id', 'newImpact-' + id); // TODO: Temporary solution for ADDED Record Product Action
                $(newThing).find(".verifiedProductLabel").attr('for', 'newImpact-' + id); // TODO: Temporary solution for ADDED Record Product Action

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
