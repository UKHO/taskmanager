$(document).ready(function () {

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