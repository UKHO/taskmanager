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
                applyOnHoldHandler();
            },
            error: function (error) {
                $("#taskInformationError")
                    .html("<div class=\"alert alert-danger\" role=\"alert\">Failed to load Task Information.</div>");
            }
        });
    }

    function applyOnHoldHandler() {
        $("#onHoldToggle").on("change",
            function () {
                if (this.checked) {
                    $("#onHoldToggle").prop("disabled", true);

                    $.ajax({
                        type: "POST",
                        url: "_TaskInformation/?handler=OnHold",
                        beforeSend: function (xhr) {
                            xhr.setRequestHeader("RequestVerificationToken", $('input:hidden[name="__RequestVerificationToken"]').val());
                        },
                        data: { "processId": processId },
                        success: function (result) {
                            $("#onHoldToggle").prop("disabled", false);
                            $("#taskInformation").html(result);
                        },
                        error: function (error) {
                            $("#OnHoldErrorMessage").text("Error putting task on hold. Please try again later.");
                            $("#OnHoldError").modal("show");
                            $("#onHoldToggle").prop("disabled", false);
                        }
                    });

                } else {
                    $("#onHoldToggle").prop("disabled", true);

                    $.ajax({
                        type: "POST",
                        url: "_TaskInformation/?handler=OffHold",
                        beforeSend: function (xhr) {
                            xhr.setRequestHeader("RequestVerificationToken", $('input:hidden[name="__RequestVerificationToken"]').val());
                        },
                        data: { "processId": processId },
                        success: function (result) {
                            $("#onHoldToggle").prop("disabled", false);
                            $("#taskInformation").html(result);
                        },
                        error: function (error) {
                            $("#OnHoldErrorMessage").text("Error taking task off hold. Please try again later.");
                            $("#OnHoldError").modal("show");
                            $("#onHoldToggle").prop("disabled", false);
                        }
                    });
                }
            });
    }

    function setTaskTypeState() {
        var pageIdentity = $("#pageIdentity").val();

        if (pageIdentity === "Review") {
            $(".taskTypeLabel").hide();
            $(".taskTypeDropdown").hide();
        } else if (pageIdentity === "Assess") {
            $(".taskTypeLabel").show();
            $(".taskTypeDropdown").show();
            $(".taskTypeDropdown").prop("disabled", false);
        } else if (pageIdentity === "Verify") {
            $(".taskTypeLabel").show();
            $(".taskTypeDropdown").show();
            $(".taskTypeDropdown").prop("disabled", true);
        }
    }

});