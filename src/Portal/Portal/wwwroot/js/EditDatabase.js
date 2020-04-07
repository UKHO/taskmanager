$(document).ready(function () {
    getEditDatabase();
});

function setControlState(enableCarisProject, enableLaunchSource) {
    $("#editDatabase > .card *").prop("disabled", true);

    if (enableCarisProject) {
        $("#SelectedCarisWorkspace")
            .add("#ProjectName")
            .add("#btnCreateCarisProject")
            .prop("disabled", false);
    }
    if (enableLaunchSource) {
        $("#btnOpenLaunchCarisSelectionModal").prop("disabled", false);
    }
}

function getEditDatabase() {

    var processId = Number($("#hdnProcessId").val());
    var pageIdentity = $("#pageIdentity").val();

    $('#SelectedCarisWorkspace').typeahead('val', "");
    $('#SelectedCarisWorkspace').typeahead('close');

    $.ajax({
        type: "GET",
        url: "_EditDatabase",
        beforeSend: function (xhr) {
            xhr.setRequestHeader("XSRF-TOKEN", $('input:hidden[name="__RequestVerificationToken"]').val());
        },
        contentType: "application/json; charset=utf-8",
        data: {
            "processId": processId,
            "taskStage": pageIdentity
        },
        success: function (result) {
            $("#editDatabase").html(result);
            initializeLaunchSourceEditorModal();
            createCarisProjectHandler();
            initialiseWorkspaceTypeahead();

            if (pageIdentity === 'Assess') {
                if ($("#IsCarisProjectCreated").val() === "True") {
                    setControlState(false, true);
                } else {
                    setControlState(true, false);
                }
            }
            else if (pageIdentity === 'Verify') {
                if ($("#IsCarisProjectCreated").val() === "True") {
                    setControlState(false, true);
                } else {
                    setControlState(false, false);
                }
            }

        },
        error: function (error) {

            var errorMessage = error.getResponseHeader("Error");

            $("#editDatabaseError")
                .html("<div class=\"alert alert-danger\" role=\"alert\">Failed to load Edit Database. "
                    + errorMessage
                    + "</div>");
        }
    });
}

function createCarisProjectHandler() {
    $("#btnCreateCarisProject").on("click", function () {
        hideDialogBoxes();
        setControlState(false, false);
        $("#createCarisProjectSpinner").show();

        var processId = Number($("#hdnProcessId").val());
        var pageIdentity = $("#pageIdentity").val();
        var projectName = $("#ProjectName").val();
        var carisWorkspace = $("#SelectedCarisWorkspace").val();

        $.ajax({
            type: "POST",
            url: "_EditDatabase/?handler=CreateCarisProject",
            beforeSend: function (xhr) {
                xhr.setRequestHeader("RequestVerificationToken", $('input:hidden[name="__RequestVerificationToken"]').val());
            },
            data: {
                "processId": processId,
                "taskStage": pageIdentity,
                "projectName": projectName,
                "carisWorkspace": carisWorkspace
            },
            success: function (data) {
                setControlState(false, true);
                $("#createCarisProjectSuccess").collapse("show");
            },
            error: function (error) {
                setControlState(true, false);

                var errorMessage = error.getResponseHeader("Error");

                $("#createCarisProjectErrorMessage").text("Failed to complete Caris Project creation. " + errorMessage);
                $("#createCarisProjectError").collapse("show");
            },
            complete: function () {
                $("#createCarisProjectSpinner").hide();
            }
        });

    });
}



function hideDialogBoxes() {

    $("#createCarisProjectSuccess").collapse("hide");
    $("#createCarisProjectError").collapse("hide");

    $("#launchSourceEditorDownloadSuccess").collapse("hide");
    $("#launchSourceEditorDownloadError").collapse("hide");

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
    $('#SelectedCarisWorkspace').typeahead({
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