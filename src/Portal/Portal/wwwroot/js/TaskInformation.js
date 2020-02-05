$(document).ready(function () {
    var processId = Number($("#hdnProcessId").val());

    getTaskInformation();

    function getTaskInformation() {

        var data = {
            "processId": Number($("#hdnProcessId").val()),
            "taskStage": $("#pageIdentity").val()
        };

        $.ajax({
            type: "GET",
            url: "_TaskInformation",
            beforeSend: function (xhr) {
                xhr.setRequestHeader("XSRF-TOKEN", $('input:hidden[name="__RequestVerificationToken"]').val());
            },
            contentType: "application/json; charset=utf-8",
            data: data,
            success: function (result) {
                $("#taskInformation").html(result);

                setTaskTypeState();
            },
            error: function (error) {
                $("#taskInformationError")
                    .html("<div class=\"alert alert-danger\" role=\"alert\">Failed to load Task Information.</div>");
            }
        });
    }


    $("#btnPutOnHold").on("click", function () {
        $("#btnPutOnHold").prop("disabled", true);

        $.ajax({
            type: "POST",
            url: "_TaskInformation/?handler=OnHold",
            beforeSend: function (xhr) {
                xhr.setRequestHeader("RequestVerificationToken", $('input:hidden[name="__RequestVerificationToken"]').val());
            },
            data: { "processId": processId },
            success: function (result) {

                $('#putOnHold').attr("hidden", true);

                $('#takeOffHold').attr("hidden", false);
                $("#btnTakeOffHold").prop("disabled", false);

                $("#taskInformation").html(result);
                getComments();
            },
            error: function (error) {
                $("#OnHoldErrorMessage").text("Error putting task on hold. Please try again later.");
                    //.text("Error Putting task on hold. Please try again later");

                $("#OnHoldError").modal("show");
                $("#btnPutOnHold").prop("disabled", false);
            }
        });

    });

    $("#btnTakeOffHold").on("click", function () {
        $("#btnTakeOffHold").prop("disabled", true);

        $.ajax({
            type: "POST",
            url: "_TaskInformation/?handler=OffHold",
            beforeSend: function (xhr) {
                xhr.setRequestHeader("RequestVerificationToken", $('input:hidden[name="__RequestVerificationToken"]').val());
            },
            data: { "processId": processId },
            success: function (result) {

                $('#takeOffHold').attr("hidden", true);

                $('#putOnHold').attr("hidden", false);
                $("#btnPutOnHold").prop("disabled", false);

                $("#taskInformation").html(result);
                getComments();
            },
            error: function (error) {
                $("#OnHoldErrorMessage").text("Error taking task off hold. Please try again later.");
                    //.text("Error Taking task off hold. Please try again later");
                $("#OnHoldError").modal("show");
                $("#btnTakeOffHold").prop("disabled", false);
            }
        });
    });

    function setTaskTypeState() {
        var pageIdentity = $("#pageIdentity").val();
        if (pageIdentity === "Assess") {
            $(".taskType").prop("disabled", true);
        } else if (pageIdentity === "Verify") {
            $(".productActionVerified").prop("disabled", false);
        }
    }

});