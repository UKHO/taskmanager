$(document).ready(function () {
    var processId = { "processId": $("#hdnProcessId").val() };

    getAssignTasks();
    getComments();
    getSourceDocuments();

    // Create child div under assignTasks and insert partial into it
    $("#btnCreateTask").click(function () {
        getAssignTasks();
    });

    $('#btnTerminate').on('click', function () {
        $('#ConfirmTerminate').modal('show');
    });
    
    $("#terminatingReview").submit(function (event) {
        if ($('#txtTerminateComment').val() === "") {
            $("#ConfirmTerminateError")
                .html("<div class=\"alert alert-danger\" role=\"alert\">Please enter a comment.</div>");
            $('#txtTerminateComment').focus();
        event.preventDefault();
        }
    });

    function getAssignTasks() {
        $.ajax({
            type: "GET",
            url: "Review/?handler=RetrieveAssignTasks",
            beforeSend: function (xhr) {
                xhr.setRequestHeader("XSRF-TOKEN", $('input:hidden[name="__RequestVerificationToken"]').val());
            },
            contentType: "application/json; charset=utf-8",
            data: processId,
            success: function (result) {
                $("#assignTasks").append(result);
            },
            error: function (error) {
                $("#assignTasksError")
                    .html("<div class=\"alert alert-danger\" role=\"alert\">Failed to create new assign task section.</div>");
            }
        });
    }

    function getComments() {

        $.ajax({
            type: "GET",
            url: "Review/?handler=RetrieveComments",
            beforeSend: function(xhr) {
                xhr.setRequestHeader("XSRF-TOKEN", $('input:hidden[name="__RequestVerificationToken"]').val());
            },
            contentType: "application/json; charset=utf-8",
            data: processId,
            success: function(result) {
                $("#existingComments").html(result);
            },
            error: function(error) {
                $("#AddCommentError")
                    .html("<div class=\"alert alert-danger\" role=\"alert\">Failed to load comments.</div>");
            }
        });
    }

    function getSourceDocuments() {

        $.ajax({
            type: "GET",
            url: "Review/?handler=RetrieveSourceDocuments",
            beforeSend: function (xhr) {
                xhr.setRequestHeader("XSRF-TOKEN", $('input:hidden[name="__RequestVerificationToken"]').val());
            },
            contentType: "application/json; charset=utf-8",
            data: processId,
            success: function (result) {
                $("#sourceDocuments").html(result);
                applyCollapseIconHandler();
            },
            error: function (error) {
                $("#sourceDocumentsError")
                    .html("<div class=\"alert alert-danger\" role=\"alert\">Failed to load Source Documents.</div>");
            }
        });
    }

    function applyCollapseIconHandler() {
        $(".collapse").on("show.bs.collapse", function (e) {
            var el = $(e.currentTarget).prev("[data-toggle='collapse']");
            var icon = $(el).find("i.fa.fa-plus");
            icon.removeClass("fa-plus").addClass("fa-minus");
        });

        $(".collapse").on("hide.bs.collapse", function (e) {
            var el = $(e.currentTarget).prev("[data-toggle='collapse']");
            var icon = $(el).find("i.fa.fa-minus");
            icon.removeClass("fa-minus").addClass("fa-plus");
        });
    }
});