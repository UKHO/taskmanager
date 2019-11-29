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
        $(element).find($(".dataImpactVerified")).prop("name", "DataImpacts[" + index + "].Verified");

        // Additional required markup settings for checkboxes...
        $(element).find($(".dataImpactEdited")).prop("id", "UsageEdited-" + index);
        $(element).find($(".dataImpactVerified")).prop("id", "UsageVerified-" + index);
        $(element).find($(".dataImpactEditedLabel")).prop("id", "UsageEdited-" + index);
        $(element).find($(".dataImpactVerifiedLabel")).prop("id", "UsageVerified-" + index);
        $(element).find($(".dataImpactEditedLabel")).prop("for", "UsageEdited-" + index);
        $(element).find($(".dataImpactVerifiedLabel")).prop("for", "UsageVerified-" + index);

        if (index > 0) {
            setDeleteHandler($(element).find(".deleteUsage"));
        }

        setChangedHandler($(element).find(".dataImpactUsage"));
    });
}

function setChangedHandler(element) {
    $(element).on("change", function () {
        //getDataImpacts();

        var selectedItem = $(this).val();

        // For each usage drop down that isn't the one that the user has just selected a value on, remove the selectedItem from the drop down options
        // TODO - Something like this...
        $("select[name='DataImpacts[1].HpdUsageId'] > option[value=" + selectedItem + "]").remove();
    });
}

function setAddUsageHandler() {
    $("#btnAddUsage").on("click", function (e) {
        var newUsage = $($(".dataImpact")[0]).clone();

        $(newUsage).find(".dataImpactUsage").val(0);
        $(newUsage).find(".dataImpactEdited").removeAttr("checked");
        $(newUsage).find(".dataImpactComments").val("");
        $(newUsage).find(".dataImpactVerified").removeAttr("checked");

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
        $(".dataImpactVerified").prop("disabled", true);
    } else if (pageIdentity === "Verify") {
        $(".dataImpactVerified").prop("disabled", false);
    }
}