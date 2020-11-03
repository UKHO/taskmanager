$(document).ready(function () {
    var isReadOnly = $("#IsReadOnly").val() === "True";

    getSncActions();

    function getSncActions() {
        var data = {
            "processId": Number($("#hdnProcessId").val()),
            "taskStage": $("#pageIdentity").val()
        };

        $.ajax({
            type: "GET",
            url: "_RecordSncAction",
            beforeSend: function (xhr) {
                xhr.setRequestHeader("XSRF-TOKEN", $('input:hidden[name="__RequestVerificationToken"]').val());
            },
            contentType: "application/json; charset=utf-8",
            data: data,
            success: function (result) {
                $("#recordSncAction").html(result);

                if (isReadOnly) {
                    return;
                }
                
                setCreateHandler();
                setSncActionedCheckboxHandler();
                update();

                setControlState();

                setImpactedProductHandler();
            },
            error: function (error) {
                $("#recordSncActionError")
                    .html("<div class=\"alert alert-danger\" role=\"alert\">Failed to load Product Actions.</div>");
            }
        });
    }

    function update() {

        $(".recordSncAction").each(function (index, element) {
            

            if (index > 0) {
                $(element).find(".deleteSncAction").show();
            } else {
                $(element).find(".deleteSncAction").hide();
            }

            //Set Form Control Names
            $(element).find($(".sncActionImpactedProduct"))
                .prop("name", "recordSncAction[" + index + "].ImpactedProduct");
            $(element).find($(".sncActionType"))
                .prop("name", "recordSncAction[" + index + "].SncActionTypeId");
            $(element).find($(".sncActionVerified")).prop("name", "recordSncAction[" + index + "].Verified");

            // Assign unique id
            assignUniqueIds($(element), index);

            if (index > 0) {
                setDeleteHandler($(element).find(".deleteSncAction"));
                setImpactedProductHandler();
            }

            var pageIdentity = $("#pageIdentity").val();
            if (pageIdentity === "Assess") {
                $(".sncActionVerified").prop("disabled", true);
            } else if (pageIdentity === "Verify") {
                $(".sncActionVerified").prop("disabled", false);
            }

        });
    };

    function assignUniqueIds(element, index) {
        $(element).find($(".sncActionImpactedProduct")).prop("id", "impactedSnc-" + index);
        $(element).find($(".sncActionImpactedProductLabel")).prop("id", "impactedSncLabel-" + index);
        $(element).find($(".sncActionImpactedProductLabel")).prop("for", "impactedSnc-" + index);

        $(element).find($(".sncActionType")).prop("id", "sncActionType-" + index);
        $(element).find($(".sncActionTypeLabel")).prop("id", "sncActionTypeLabel-" + index);
        $(element).find($(".sncActionTypeLabel")).prop("for", "sncActionType-" + index);

        $(element).find($(".sncActionVerified")).prop("id", "sncActionVerified-" + index);
        $(element).find($(".sncActionVerifiedLabel")).prop("id", "sncActionVerifiedLabel-" + index);
        $(element).find($(".sncActionVerifiedLabel")).prop("for", "sncActionVerified-" + index);

    }

    function setCreateHandler() {
        $("#btnAddSncImpact").on("click",
            function (e) {

                var currentCount = $(".recordSncAction").length;
                var newThing = $($(".recordSncAction")[0]).clone();

                $(newThing).find(".sncActionImpactedProduct").val("");
                $(newThing).find(".sncActionType").val(0);
                $(newThing).find(".sncActionVerified").prop('checked', false);

                $("#sncActions").append(newThing);
                $(newThing).show();

                update();

                setControlState();
            });
    }

    function setDeleteHandler(element) {
        $(element).on("click").click(function () {
            $(element).parents(".recordSncAction").remove();

            update();

            setControlState();
        }).show();
    }


    function setSncActionedCheckboxHandler() {
        $("#SncActioned").change(function () {
            setControlState();
        });
    }

    function setImpactedProductHandler() {
        $(".sncActionImpactedProduct").on("input",
            function (e) {

                var currentImpactedProduct = $(e.currentTarget);
                if ($(currentImpactedProduct).val() === "") {
                    var currentSncActionType = $(currentImpactedProduct).parents(".recordSncAction").find(".sncActionType");
                    $(currentSncActionType).prop("selectedIndex", 0).change();
                }

                setControlState();
            });

        $(".productActionType").on("change",
            function () {
                setControlState();
            });
    }

    function setControlState() {

        // disable Action if:
        // 1) Any Impacted Product is populated
        // 2) multiple rows

        $("#hdnSncActioned").val($("#SncActioned").prop("checked"));

        var impactedProductRows = $(".recordSncAction").length;
        var isActionChecked = $("#SncActioned").prop("checked");
       
        if (!isActionChecked) {
            $("#SncActioned").prop("disabled", false);
            $("#btnAddSncImpact").prop("disabled", true);
            $(".sncActionImpactedProduct").prop("disabled", true);
            $(".sncActionType").prop("disabled", true);

            return;
        }

        if (impactedProductRows > 1) {
            $("#SncActioned").prop("disabled", true);
            $("#btnAddSncImpact").prop("disabled", false);
            $(".sncActionImpactedProduct").prop("disabled", false);
            $(".sncActionType").prop("disabled", false);

            return;
        }
        
        var impactedProductHasValue = false;

        $(".sncActionImpactedProduct").each(function() {
            if ($(this).val() !== "") {
                impactedProductHasValue = true;
                return false;
            }
        });

        var impactedProductTypeHasValue = false;

        $(".sncActionType").each(function () {
            if ($(this).val() !== "") {
                impactedProductTypeHasValue = true;
                return false;
            }
        });

        if (impactedProductHasValue || impactedProductTypeHasValue) {
            $("#SncActioned").prop("disabled", true);
            $("#btnAddSncImpact").prop("disabled", false);
            $(".sncActionImpactedProduct").prop("disabled", false);
            $(".sncActionType").prop("disabled", false);

            return;
        }

        if (isActionChecked) {
            $("#SncActioned").prop("disabled", false);
            $("#btnAddSncImpact").prop("disabled", false);
            $(".sncActionImpactedProduct").prop("disabled", false);
            $(".sncActionType").prop("disabled", false);

            return;
        }
        
        $("#SncActioned").prop("disabled", false);
        $("#btnAddSncImpact").prop("disabled", true);
        $(".sncActionImpactedProduct").prop("disabled", true);
        $(".sncActionType").prop("disabled", true);
    }

});