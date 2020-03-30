$(document).ready(function () {
    getDataImpacts();
});

function updateDataImpact() {
    $(".dataImpact").each(function (index, element) {
        if (index > 0) {
            $(element).find(".deleteUsage").show();
        } else {
            $(element).find(".deleteUsage").hide();
        }

        // Set Form Control Names
        $(element).find($(".dataImpactUsage")).prop("name", "DataImpacts[" + index + "].HpdUsageId");
        $(element).find($(".dataImpactEdited")).prop("name", "DataImpacts[" + index + "].Edited");
        $(element).find($(".dataImpactComments")).prop("name", "DataImpacts[" + index + "].Comments");
        $(element).find($(".dataImpactFeaturesSubmitted")).prop("name", "DataImpacts[" + index + "].FeaturesSubmitted");
        $(element).find($(".dataImpactFeaturesVerified")).prop("name", "DataImpacts[" + index + "].FeaturesVerified");

        // Additional required markup settings for checkboxes...
        $(element).find($(".dataImpactEdited")).prop("id", "UsageEdited-" + index);
        $(element).find($(".dataImpactEditedLabel")).prop("id", "UsageEdited-" + index);
        $(element).find($(".dataImpactEditedLabel")).prop("for", "UsageEdited-" + index);

        $(element).find($(".dataImpactFeaturesSubmitted")).prop("id", "usageFeaturesSubmitted-" + index);
        $(element).find($(".dataImpactFeaturesSubmittedLabel")).prop("id", "usageFeaturesSubmitted-" + index);
        $(element).find($(".dataImpactFeaturesSubmittedLabel")).prop("for", "usageFeaturesSubmitted-" + index);

        $(element).find($(".dataImpactFeaturesVerified")).prop("id", "usageFeaturesVerified-" + index);
        $(element).find($(".dataImpactFeaturesVerifiedLabel")).prop("id", "usageFeaturesVerified-" + index);
        $(element).find($(".dataImpactFeaturesVerifiedLabel")).prop("for", "usageFeaturesVerified-" + index);

        if (index > 0) {
            setDeleteHandler($(element).find(".deleteUsage"));
        }
    });
}

function setAddUsageHandler() {
    $("#btnAddUsage").on("click", function (e) {
        var newUsage = $($(".dataImpact")[0]).clone();

        $(newUsage).find(".dataImpactUsage").val(0);
        $(newUsage).find(".dataImpactEdited").prop('checked', false);
        $(newUsage).find(".dataImpactComments").val("");
        $(newUsage).find(".dataImpactFeaturesSubmitted").prop('checked', false);
        $(newUsage).find(".dataImpactFeaturesVerified").prop('checked', false);

        $("#dataImpactContainer").append(newUsage);
        $(newUsage).show();

        updateDataImpact();
    });

};

function getDataImpacts() {
    var processId = { "processId": Number($("#hdnProcessId").val()) };

    $.ajax({
        type: "GET",
        url: "_DataImpact",
        beforeSend: function (xhr) {
            xhr.setRequestHeader("XSRF-TOKEN", $('input:hidden[name="__RequestVerificationToken"]').val());
        },
        contentType: "application/json; charset=utf-8",
        data: processId,
        success: function (result) {
            $("#existingDataImpacts").html(result);

            setVerified();
            setAddUsageHandler();
            updateDataImpact();
        },
        error: function (error) {
            $("#AddDataImpactsError")
                .html("<div class=\"alert alert-danger\" role=\"alert\">Failed to load Data Impacts.</div>");
        }
    });
}

function setDeleteHandler(element) {
    $(element).on("click").click(function () {
        $(element).parents(".dataImpact").remove();

        updateDataImpact();
    }).show();
}

function setVerified() {

    var pageIdentity = $("#pageIdentity").val();
    if (pageIdentity === "Assess") {
        $(".dataImpactFeaturesVerified").bind("click", false);
    } else if (pageIdentity === "Verify") {
        $(".dataImpactFeaturesSubmitted").bind("click", false);
    }
}