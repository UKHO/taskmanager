$(document).ready(function () {
    var isReadOnly = $("#IsReadOnly").val() === "True";

    getProductActions();

    function getProductActions() {
        var data = {
            "processId": Number($("#hdnProcessId").val()),
            "taskStage": $("#pageIdentity").val()
        };

        $.ajax({
            type: "GET",
            url: "_RecordProductAction",
            beforeSend: function (xhr) {
                xhr.setRequestHeader("XSRF-TOKEN", $('input:hidden[name="__RequestVerificationToken"]').val());
            },
            contentType: "application/json; charset=utf-8",
            data: data,
            success: function (result) {
                $("#recordProductAction").html(result);

                if (isReadOnly) {
                    return;
                }

                setCreateHandler();
                setProductActionedCheckboxHandler();
                update();

                setControlState();

                setImpactedProductHandler();

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
            $(element).find($(".productActionImpactedProduct"))
                .prop("name", "RecordProductAction[" + index + "].ImpactedProduct");
            $(element).find($(".productActionType"))
                .prop("name", "RecordProductAction[" + index + "].ProductActionTypeId");
            $(element).find($(".productActionVerified")).prop("name", "RecordProductAction[" + index + "].Verified");

            // Assign unique id
            assignUniqueIds($(element), index);

            if (index > 0) {
                setDeleteHandler($(element).find(".deleteAction"));
                setImpactedProductHandler();
            }

            var pageIdentity = $("#pageIdentity").val();
            if (pageIdentity === "Assess") {
                $(".productActionVerified").prop("disabled", true);
            } else if (pageIdentity === "Verify") {
                $(".productActionVerified").prop("disabled", false);
            }

        });
    };

    function assignUniqueIds(element, index) {
        $(element).find($(".productActionImpactedProduct")).prop("id", "impactedProduct-" + index);
        $(element).find($(".productActionImpactedProductLabel")).prop("id", "impactedProductLabel-" + index);
        $(element).find($(".productActionImpactedProductLabel")).prop("for", "impactedProduct-" + index);

        $(element).find($(".productActionType")).prop("id", "productActionType-" + index);
        $(element).find($(".productActionTypeLabel")).prop("id", "productActionTypeLabel-" + index);
        $(element).find($(".productActionTypeLabel")).prop("for", "productActionType-" + index);

        $(element).find($(".productActionVerified")).prop("id", "productActionVerified-" + index);
        $(element).find($(".productActionVerifiedLabel")).prop("id", "productActionVerifiedLabel-" + index);
        $(element).find($(".productActionVerifiedLabel")).prop("for", "productActionVerified-" + index);

    }

    function setCreateHandler() {
        $("#btnAddImpact").on("click",
            function (e) {
                var currentCount = $(".recordProductAction").length;
                var newThing = $($(".recordProductAction")[0]).clone();

                $(newThing).find(".productActionImpactedProduct").val("");
                $(newThing).find(".productActionType").val(0);
                $(newThing).find(".productActionVerified").prop('checked', false);

                $("#productActions").append(newThing);
                $(newThing).show();

                update();

                setControlState();
            });
    }

    function setDeleteHandler(element) {
        $(element).on("click").click(function () {
            $(element).parents(".recordProductAction").remove();

            update();

            setControlState();
        }).show();
    }


    function setProductActionedCheckboxHandler() {
        $("#ProductActioned").change(function () {
            setControlState();
        });
    }

    function setImpactedProductHandler() {
        $(".productActionImpactedProduct").on("input",
            function (e) {

                var currentImpactedProduct = $(e.currentTarget);
                if ($(currentImpactedProduct).val() === "") {
                    var currentProductActionType = $(currentImpactedProduct).parents(".recordProductAction").find(".productActionType");
                    $(currentProductActionType).prop("selectedIndex", 0).change();
                }

                setControlState();
            });
    }

    function setControlState() {

        // disable Action if:
        // 1) Any Impacted Product is populated
        // 2) multiple rows

        $("#hdnProductActioned").val($("#ProductActioned").prop("checked"));

        var impactedProductRows = $(".recordProductAction").length;
        var isActionChecked = $("#ProductActioned").prop("checked");
       
        if (!isActionChecked) {
            $("#ProductActioned").prop("disabled", false);
            $("#btnAddImpact").prop("disabled", true);
            $(".productActionImpactedProduct").prop("disabled", true);
            $(".productActionType").prop("disabled", true);

            return;
        }

        if (impactedProductRows > 1) {
            $("#ProductActioned").prop("disabled", true);
            $("#btnAddImpact").prop("disabled", false);
            $(".productActionImpactedProduct").prop("disabled", false);
            $(".productActionType").prop("disabled", false);

            return;
        }
        
        var impactedProductHasValue = false;

        $(".productActionImpactedProduct").each(function() {
            if ($(this).val() !== "") {
                impactedProductHasValue = true;
                return false;
            }
        });

        if (impactedProductHasValue) {
            $("#ProductActioned").prop("disabled", true);
            $("#btnAddImpact").prop("disabled", false);
            $(".productActionImpactedProduct").prop("disabled", false);
            $(".productActionType").prop("disabled", false);

            return;
        }

        if (isActionChecked) {
            $("#ProductActioned").prop("disabled", false);
            $("#btnAddImpact").prop("disabled", false);
            $(".productActionImpactedProduct").prop("disabled", false);
            $(".productActionType").prop("disabled", false);

            return;
        }
        
        $("#ProductActioned").prop("disabled", false);
        $("#btnAddImpact").prop("disabled", true);
        $(".productActionImpactedProduct").prop("disabled", true);
        $(".productActionType").prop("disabled", true);
    }

});