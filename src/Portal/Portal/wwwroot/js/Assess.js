$(document).ready(function () {

    setAssessDoneHandler();
    setAssessSaveHandler();

    var formChanged = false;
    $("#frmAssessPage").change(function () { formChanged = true; });

    window.onbeforeunload = function () {
        if (formChanged) {
            return "Changes detected";
        }
    };

    if ($("#assessDoneErrorMessage").html().trim().length > 0) {
        $("#modalWaitAssessDoneErrors").modal("show");
    }

    function completeAssess(action) {
        $("#assessDoneErrorMessage").html("");
        $("#btnDone").prop("disabled", true);
        $("#btnSave").prop("disabled", true);
        $("#modalWaitAssessDone").modal("show");

        var formData = $('#frmAssessPage').serialize();

        $.ajax({
            type: "POST",
            url: "Assess/?handler=Done&action=" + action,
            beforeSend: function (xhr) {
                xhr.setRequestHeader("RequestVerificationToken", $('input:hidden[name="__RequestVerificationToken"]').val());
            },
            data: formData,
            complete: function () {
                //Add a delay to account for the modalWaitReviewDone modal
                //not being fully shown, before trying to hide it
                window.setTimeout(function () {
                    $("#modalWaitAssessDone").modal("hide");
                    $("#btnDone").prop("disabled", false);
                    $("#btnSave").prop("disabled", false);
                }, 200);
            },
            success: function (result) {
                formChanged = false;
                if (action === "Done") {
                    window.location.replace("/Index");
                }
                console.log("success");
            },
            error: function (error) {
                var responseJson = error.responseJSON;

                if (responseJson != null) {
                    $("#assessDoneErrorMessage").append("<ul/>");
                    var unOrderedList = $("#assessDoneErrorMessage ul");

                    responseJson.forEach(function (item) {
                        unOrderedList.append("<li>" + item + "</li>");
                    });

                    $("#modalWaitAssessDoneErrors").modal("show");
                }

            }
        });
    }

    function setAssessDoneHandler() {
        $("#btnDone").prop("disabled", false);


        $("#btnDone").click(function (e) {
            completeAssess("Done");
        });
    }

    function setAssessSaveHandler() {
        $("#btnSave").prop("disabled", false);


        $("#btnSave").click(function (e) {
            completeAssess("Save");
        });
    }

});
