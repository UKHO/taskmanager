$(document).ready(function () {
    getDataImpacts();
    
});


function updateDataImpact() {
    $(".dataImpact").each(function(index, element) {
        //Set Form Control Names
        $(element).find($(".dataImpactUsage")).prop("name", "DataImpacts[" + index + "].HpdUsageId");
        $(element).find($(".dataImpactEdited")).prop("name", "DataImpacts[" + index + "].Edited");
        $(element).find($(".dataImpactComments")).prop("name", "DataImpacts[" + index + "].Comments");
        $(element).find($(".dataImpactVerified")).prop("name", "DataImpacts[" + index + "].Verified");


        //if (index > 0) {
        //    setDeleteHandler($(element).find(".deleteAssignTask"));
        //}

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


function setVerified() {

    var pageIdentity = $("#pageIdentity").val();
    if (pageIdentity === "Assess") {
        $(".usageVerified").prop("disabled", true);
    } else if (pageIdentity === "Verify") {
        $(".usageVerified").prop("disabled", false);
    }
}