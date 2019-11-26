$(document).ready(function () {
    var pageIdentity = $("#pageIdentity").val();
    if (pageIdentity === "Assess") {
        $(".usageVerified").prop("disabled", true);
    } else if (pageIdentity === "Verify") {
        $(".usageVerified").prop("disabled", false);
    }
});