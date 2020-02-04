$(document).ready(function () {
    getEditDatabase();
});

function getEditDatabase() {

    var processId = { "processId": Number($("#hdnProcessId").val()) };

    $.ajax({
        type: "GET",
        url: "_EditDatabase",
        beforeSend: function (xhr) {
            xhr.setRequestHeader("XSRF-TOKEN", $('input:hidden[name="__RequestVerificationToken"]').val());
        },
        contentType: "application/json; charset=utf-8",
        data: processId,
        success: function (result) {
            $("#editDatabase").html(result);
            launchSourceEditorDownloadHandler();
            //setLaunchSourceEditorHref(processId.processId);
            initialiseWorkspaceTypeahead();
        },
        error: function (error) {
            $("#editDatabaseError")
                .html("<div class=\"alert alert-danger\" role=\"alert\">Failed to load Edit Database.</div>");
        }
    });
}

function launchSourceEditorDownloadHandler() {
    $("#btnLaunchSourceEditorDownload").on("click", function () {
        $("#btnLaunchSourceEditorDownload").prop("disabled", true);

        var processId = Number($("#hdnProcessId").val());
        var pageIdentity = $("#pageIdentity").val();

        $.ajax({
            type: "GET",
            xhrFields: {
                responseType: 'blob'
            },
            url: "_EditDatabase/?handler=LaunchSourceEditor",
            beforeSend: function (xhr) {
                xhr.setRequestHeader("XSRF-TOKEN", $('input:hidden[name="__RequestVerificationToken"]').val());
            },
            contentType: "application/json; charset=utf-8",
            data: {
                "processId": processId,
                "taskStage": pageIdentity
            },
            success: function (data) {

                var a = document.createElement('a');
                var url = window.URL.createObjectURL(data);
                a.href = url;
                a.download = 'myfile.xml';
                document.body.append(a);
                a.click();
                a.remove();
                window.URL.revokeObjectURL(url);
            },
            error: function (error, message) {
                var errorMessage = error.responseJSON;
                alert('Error - ' + errorMessage);
                $("#editDatabaseError")
                    .html("<div class=\"alert alert-danger\" role=\"alert\">Failed to load Edit Database.</div>");
            },
            complete: function() {

                $("#btnLaunchSourceEditorDownload").prop("disabled", false);
            }
        });

    });
}

//function setLaunchSourceEditorHref(processId) {
//    var pageIdentity = $("#pageIdentity").val();
//    var href = "_EditDatabase/?handler=LaunchSourceEditor" +
//        "&processId=" + processId +
//        "&taskStage=" + pageIdentity;

//    $("#launchSourceEditorLink").attr("href", href);
//}

function initialiseWorkspaceTypeahead() {
    $('#workspaceTypeaheadError').collapse("hide");
    // Constructing the suggestion engine
    var workspace = new Bloodhound({
        datumTokenizer: Bloodhound.tokenizers.whitespace,
        queryTokenizer: Bloodhound.tokenizers.whitespace,
        prefetch: {
            url: "_EditDatabase/?handler=Workspaces",
            ttl: 600000
        },
        initialize: false
    });

    var promise = workspace.initialize();
    promise.fail(function () {
        $('#workspaceTypeaheadError').collapse("show");
    });

    // Initializing the typeahead
    $('.typeahead').typeahead({
        hint: true,
        highlight: true, /* Enable substring highlighting */

        minLength:
            3 /* Specify minimum characters required for showing result */
    },
        {
            name: 'workspace',
            source: workspace,
            limit: 100,
            templates: {
                notFound: '<div>No results</div>'
            }
        });
}