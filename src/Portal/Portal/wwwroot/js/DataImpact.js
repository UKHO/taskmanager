$(document).ready(function () {
    getDataImpacts();
});

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