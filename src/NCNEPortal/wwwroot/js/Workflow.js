$(document).ready(function() {

    $("#btnTerminate").on("click", function() {
            {
                $("#ConfirmTerminate").modal("show");
            }
    });

    $("#btnClose").on("click",
        function() {
            window.location.href = '/Index';
        });

    $("#terminatingTask").submit(function(event) {
        if ($("#txtTerminateComment").val().trim() === "") {
            $("#ConfirmTerminateError")
                .html("<div class=\"alert alert-danger\" role=\"alert\">Please enter a comment.</div>");
            $("#txtTerminateComment").focus();
            event.preventDefault();
        }
    });

    $("#ConfirmTerminate").on("shown.bs.modal",
        function () {
            $("#txtTerminateComment").focus();
        });


    if ($("#IsCarisProjectCreated").val() === "True") {
        setControlState(false);
    } else {
        setControlState(true);
    }



    $("#btnCreateCarisProject").on("click", function () {

        $("#createCarisProjectSuccess").collapse("hide");
        $("#createCarisProjectError").collapse("hide");

        setControlState(false);

        $("#createCarisProjectSpinner").show();

        var processId = Number($("#hdnProcessId").val());
        var projectName = $("#txtCarisProject").val();

        $.ajax({
            type: "POST",
            url: "Workflow/?handler=CreateCarisProject",
            beforeSend: function (xhr) {
                xhr.setRequestHeader("RequestVerificationToken", $('input:hidden[name="__RequestVerificationToken"]').val());
            },
            data: {
                "processId": processId,
                "projectName": projectName,

            },
            success: function (data) {
                $("#createCarisProjectSuccess").collapse("show");
            },
            error: function (error) {
                setControlState(true);
                var errorMessage = error.getResponseHeader("Error");

                $("#createCarisProjectErrorMessage").text("Failed to complete Caris Project creation. " + errorMessage);
                $("#createCarisProjectError").collapse("show");
            },
            complete: function () {
                $("#createCarisProjectSpinner").hide();
            }
        });

    });


    function setControlState(enableCarisProject) {
        if (enableCarisProject) {
            $("#btnCreateCarisProject").prop("disabled", false);
            $("#txtCarisProject").prop("disabled", false);
        } else {
            $("#btnCreateCarisProject").prop("disabled", true);
            $("#txtCarisProject").prop("disabled", true);
        }
    }


});