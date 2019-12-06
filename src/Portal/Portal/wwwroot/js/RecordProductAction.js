$(document).ready(function () {
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
    }


    function update() {

        $(".recordProductAction").each(function (index, element) {
            if (index > 0) {
                $(element).find(".deleteAction").show();
            } else {
                $(element).find(".deleteAction").hide();
            }
            //Set Form Control Names
            $(element).find($(".productActionImpactedProduct")).prop("name", "RecordProductAction[" + index + "].ImpactedProduct");
            $(element).find($(".productActionType")).prop("name", "RecordProductAction[" + index + "].ProductActionTypeId");
            $(element).find($(".productActionVerified")).prop("name", "RecordProductAction[" + index + "].Verified");

            // Additional required markup settings for checkboxes...
            $(element).find($(".productActionVerified")).prop("id", "ProductActionVerified-" + index);
            $(element).find($(".productActionVerifiedLabel")).prop("id", "ProductActionVerified-" + index);
            $(element).find($(".productActionVerifiedLabel")).prop("for", "ProductActionVerified-" + index);

            if (index > 0) {
                setDeleteHandler($(element).find(".deleteAction"));
            }

            var pageIdentity = $("#pageIdentity").val();
            if (pageIdentity === "Assess") {
                $(".productActionVerified").prop("disabled", true);
            } else if (pageIdentity === "Verify") {
                $(".productActionVerified").prop("disabled", false);
            }

        });
    };

    function setCreateHandler() {
        $("#btnAddImpact").on("click", function (e) {
            var currentCount = $(".recordProductAction").length;
            var newThing = $($(".recordProductAction")[0]).clone();

            $(newThing).find(".productActionImpactedProduct").val("");
            $(newThing).find(".productActionType").val(0);
            $(newThing).find(".productActionVerified").prop('checked', false);

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
