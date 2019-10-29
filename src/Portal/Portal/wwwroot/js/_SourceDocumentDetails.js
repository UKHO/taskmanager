$(document).ready(function () {
    var processId = { "processId": $("#hdnProcessId").val() };

    getSourceDocuments();

    function getSourceDocuments() {

        $.ajax({
            type: "GET",
            url: "_SourceDocumentDetails",
            beforeSend: function (xhr) {
                xhr.setRequestHeader("XSRF-TOKEN", $('input:hidden[name="__RequestVerificationToken"]').val());
            },
            contentType: "application/json; charset=utf-8",
            data: processId,
            success: function (result) {
                $("#sourceDocuments").html(result);
                applyCollapseIconHandler();
                applyAttachLinkedDocumentHandlers();
            },
            error: function (error) {
                $("#sourceDocumentsError")
                    .html("<div class=\"alert alert-danger\" role=\"alert\">Failed to load Source Documents.</div>");
            }
        });
    }

    function applyAttachLinkedDocumentHandlers() {
        $(".attachLinkedDocument").on("click", function (e) {
            var linkedSdocId = $(this).data("linkedsdocid");

            $.ajax({
                type: "POST",
                url: "_SourceDocumentDetails/?handler=AttachLinkedDocument",
                beforeSend: function (xhr) {
                    xhr.setRequestHeader("RequestVerificationToken", $('input:hidden[name="__RequestVerificationToken"]').val());
                },
                data: {
                    linkedSdocId: linkedSdocId
                },
                success: function (result) {
                    alert("success");
                    //$("#assignTasks").append(result);
                },
                error: function (error) {
                    alert("error");
                    //$("#assignTasksError")
                    //    .html("<div class=\"alert alert-danger\" role=\"alert\">Failed to create new assign task section.</div>");
                }
            });
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