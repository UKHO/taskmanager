$(document).ready(function () {

    setVerifySaveHandler();

    $("#btnReject").on("click", function () {
        $("#ConfirmReject").modal("show");
    });

    $("#rejectingVerify").submit(function (event) {
        if ($("#txtRejectComment").val() === "") {
            $("#ConfirmRejectError")
                .html("<div class=\"alert alert-danger\" role=\"alert\">Please enter a comment.</div>");
            $("#txtRejectComment").focus();
            event.preventDefault();
        }
    });

    $("#ConfirmReject").on("shown.bs.modal",
        function () {
            $("#txtRejectComment").focus();
        });


    function completeVerify(action) {
        //$("#assessDoneErrorMessage").html("");
        //$("#btnDone").prop("disabled", true);
        //$("#btnSave").prop("disabled", true);
        //$("#modalWaitAssessDone").modal("show");

        var formData = $('#frmVerifyPage').serialize();

        $.ajax({
            type: "POST",
            url: "Verify/?handler=Done&action=" + action,
            beforeSend: function (xhr) {
                xhr.setRequestHeader("RequestVerificationToken", $('input:hidden[name="__RequestVerificationToken"]').val());
            },
            data: formData,
            complete: function () {
                //Add a delay to account for the modalWaitReviewDone modal
                //not being fully shown, before trying to hide it
                //window.setTimeout(function () {
                //    $("#modalWaitAssessDone").modal("hide");
                //    $("#btnDone").prop("disabled", false);
                //    $("#btnSave").prop("disabled", false);
                //}, 200);
            },
            success: function (result) {
                //formChanged = false;
                //if (action === "Done") {
                //    window.location.replace("/Index");
                //}
                console.log("success");
            },
            error: function (error) {
                //var responseJson = error.responseJSON;

                //if (responseJson != null) {
                //    $("#assessDoneErrorMessage").append("<ul/>");
                //    var unOrderedList = $("#assessDoneErrorMessage ul");

                //    responseJson.forEach(function (item) {
                //        unOrderedList.append("<li>" + item + "</li>");
                //    });

                //    $("#modalWaitAssessDoneErrors").modal("show");
                //}

            }
        });
    }


    function setVerifySaveHandler() {
        $("#btnSave").prop("disabled", false);


        $("#btnSave").click(function (e) {
            completeVerify("Save");
        });
    }


});
