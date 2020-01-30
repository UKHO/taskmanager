$(document).ready(function () {
    getEditDatabase();
    launchSourceEditorHandler();
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
            launchSourceEditorHandler();
            initialiseWorkspaceTypeahead();
        },
        error: function (error) {
            $("#editDatabaseError")
                .html("<div class=\"alert alert-danger\" role=\"alert\">Failed to load Edit Database.</div>");
        }
    });
}

function launchSourceEditorHandler() {

    $("#btnLaunchSourceEditor").on("click", function () {
        $("#btnLaunchSourceEditor").prop("disabled", true);
        var processId = Number($("#hdnProcessId").val());

        $.ajax({
            type: "POST",
            url: "_EditDatabase/?handler=LaunchSourceEditor",
            beforeSend: function (xhr) {
                xhr.setRequestHeader("RequestVerificationToken", $('input:hidden[name="__RequestVerificationToken"]').val());
            },
            data: {
                "processId": processId
            },
            success: function (result) {
                $("#btnLaunchSourceEditor").prop("disabled", false);
            },
            error: function (error) {
                console.log(error);

                $("#launchSourceEditorError")
                    .html("<div class=\"alert alert-danger\" role=\"alert\">Error launching Source Editor. Please try again later.</div>");

                $("#btnLaunchSourceEditor").prop("disabled", false);
            }
        });
    });
}

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