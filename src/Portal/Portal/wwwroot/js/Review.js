$(document).ready(function () {

    setReviewDoneHandler();

    if ($("#reviewDoneErrorMessage").html().trim().length > 0) {
        $("#modalWaitReviewDoneErrors").modal("show");
    }

    $("#btnTerminate").on("click", function () {
        $("#ConfirmTerminate").modal("show");
    });

    $("#terminatingReview").submit(function (event) {
        if ($("#txtTerminateComment").val() === "") {
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

    function setReviewDoneHandler() {
        $("#btnDone").prop("disabled", false);



        $("#btnDone").click(function (e) {

            $("#btnDone").prop("disabled", true);
            $("#modalWaitReviewDone").modal("show");

            var formData = $('#frmReviewPage').serialize();


            $.ajax({
                type: "POST",
                url: "Review/?handler=Done",
                beforeSend: function (xhr) {
                    xhr.setRequestHeader("RequestVerificationToken", $('input:hidden[name="__RequestVerificationToken"]').val());
                },
                data: formData,
                complete: function() {
                    $("#modalWaitReviewDone").modal("hide");
                    $("#btnDone").prop("disabled", false);
                },
                success: function (result) {
                    console.log("success");
                },
                error: function (error) {
                    var responseJson = error.responseJSON;

                    if (responseJson != null) {
                        $("#reviewDoneErrorMessage").append("<ul/>");
                        var unOrderedList = $("#reviewDoneErrorMessage ul");

                        responseJson.forEach(function(item) {
                            unOrderedList.append("<li>" + item + "</li>");
                        });

                        $("#modalWaitReviewDoneErrors").modal("show");
                    }

                }
            });
            //
        });
    }
});