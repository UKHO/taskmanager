$(document).ready(function () {

});

function initializeLaunchSourceEditorModal() {
    attachLaunchSourceEditorModalButtonHandler();
    attachUsagesSelectionCheckboxesHandler();
    attachUsagesSelectionTextClickHandler();
    attachSourcesSelectionCheckboxesHandler();
    attachSourcesSelectionTextClickHandler();
    attachClearLaunchSourceEditorModalButtonHandler();
    attachLaunchSourceEditorDownloadHandler();

    $("#usagesSelection").DataTable({
        "pageLength": 6,
        "pagingType": "simple",
        "dom": "tip"
    });

    $("#sourcesSelection").DataTable({
        "pageLength": 6,
        "pagingType": "simple",
        "dom": "tip"
    });
}

function attachLaunchSourceEditorModalButtonHandler() {
    $("#btnOpenLaunchCarisSelectionModal").click(function () {
        clearLaunchSourceEditorModal();
        $("#LaunchCarisSelectionModal").modal("show");
    });
}

function attachUsagesSelectionCheckboxesHandler() {
    $("#usagesSelection input[type='checkbox']").change(function () {
        var usageName = $(this).data("usage-name");

        if (!$(this).prop('checked')) {
            
            var selectedUsageElement = $(".selectedUsage[data-usage-name='" + usageName + "']");
            deselectUsage(selectedUsageElement);
            return;
        }

        selectUsage(usageName);
    });
}

function attachUsagesSelectionTextClickHandler() {
    $("#usagesSelection .hpdUsageName").click(function () {
        var thisUsageCheckbox = $(this).siblings("td").find("input[type='checkbox']");
        var usageName = thisUsageCheckbox.data("usage-name");

        if (thisUsageCheckbox.is(":checked")) {

            var selectedUsageElement = $(".selectedUsage[data-usage-name='" + usageName + "']");
            deselectUsage(selectedUsageElement);
            return;
        }

        thisUsageCheckbox.prop('checked', true);
        selectUsage(usageName);
    });
}

function attachSourcesSelectionCheckboxesHandler() {
    $("#sourcesSelection input[type='checkbox']").change(function () {
        var sourceName = $(this).data("source-filename");

        if (!$(this).prop('checked')) {
            var selectedSourceElement = $(".selectedSource[data-source-filename='" + sourceName + "']");
            deselectSource(selectedSourceElement);
            return;
        }

        selectSource(sourceName);
    });
}

function attachSourcesSelectionTextClickHandler() {
    $("#sourcesSelection .sourceDocumentName").click(function () {
        var thisSourceCheckbox = $(this).siblings("td").find("input[type='checkbox']");
        var sourceName = thisSourceCheckbox.data("source-filename");

        if (thisSourceCheckbox.is(":checked")) {

            var selectedSourceElement = $(".selectedSource[data-source-filename='" + sourceName + "']");
            deselectSource(selectedSourceElement);
            return;
        }

        thisSourceCheckbox.prop('checked', true);
        selectSource(sourceName);
    });
}

function attachClearLaunchSourceEditorModalButtonHandler() {
    $("#btnClearLaunchCarisSelections").click(function () {
        clearLaunchSourceEditorModal();
    });
}

function attachLaunchSourceEditorDownloadHandler() {
    $("#btnLaunchSourceEditorDownload").on("click", function () {
        hideDialogBoxes();
        $("#btnLaunchSourceEditorDownload").prop("disabled", true);

        var processId = Number($("#hdnProcessId").val());
        var pageIdentity = $("#pageIdentity").val();
        var sessionFilename = $(this).data("sessionfilename");
        var selectedHpdUsages = [];
        var selectedSources = [];

        $(".selectedUsage").each(function () {
            selectedHpdUsages.push($(this).data("usage-name"));
        });

        $(".selectedSource").each(function () {
            selectedSources.push($(this).data("source-filename"));
        });

        $.ajax({
            type: "GET",
            xhrFields: {
                responseType: 'blob'
            },
            url: "_EditDatabase/?handler=LaunchSourceEditor",
            beforeSend: function (xhr) {
                xhr.setRequestHeader("XSRF-TOKEN", $('input:hidden[name="__RequestVerificationToken"]').val());
            },
            traditional: true,
            contentType: "application/json; charset=utf-8",
            data: {
                "processId": processId,
                "taskStage": pageIdentity,
                "sessionFilename": sessionFilename,
                "selectedHpdUsages": selectedHpdUsages,
                "selectedSources": selectedSources
            },
            success: function (data) {

                var url = window.URL.createObjectURL(data);
                $("#hdnDownloadLink").attr("href", url);
                $("#hdnDownloadLink").attr("download", sessionFilename);
                $("#hdnDownloadLink")[0].click();

                $("#launchSourceEditorDownloadSuccess").collapse("show");
            },
            error: function (error) {
                var errorMessage = error.getResponseHeader("Error");

                $("#launchSourceEditorDownloadErrorMessage").text("Failed to generate Session File. " + errorMessage);
                $("#launchSourceEditorDownloadError").collapse("show");
            },
            complete: function () {
                $("#btnLaunchSourceEditorDownload").prop("disabled", false);
                $("#hdnDownloadLink").removeAttr("href");
                $("#hdnDownloadLink").removeAttr("download");

                $("#LaunchCarisSelectionModal").modal("hide");
            }
        });

    });
}


function selectUsage(usageName) {
    var containerElement = $("#selectedUsagesContainer");

    var selectedUsageElement = $(document.createElement("div"));
    selectedUsageElement.addClass("selectedUsage d-flex mt-1 mb-1 p-2");
    selectedUsageElement.attr("data-usage-name", usageName);
    containerElement.append(selectedUsageElement);

    var checkElement = $(document.createElement("i"));
    checkElement.addClass("fa fa-check pl-1 pr-1");
    selectedUsageElement.append(checkElement);

    var usageNameElement = $(document.createElement("span"));
    usageNameElement.addClass("pl-1 pr-1");
    usageNameElement.text(usageName);
    selectedUsageElement.append(usageNameElement);

    var crossElement = $(document.createElement("i"));
    crossElement.addClass("fa fa-times-circle pl-1 pr-1");
    crossElement.click(function () {
        deselectUsage($(this).parent());
    });
    selectedUsageElement.append(crossElement);
}

function selectSource(sourceName) {
    var containerElement = $("#selectedSourcesContainer");

    var selectedSourceElement = $(document.createElement("div"));
    selectedSourceElement.addClass("selectedSource d-flex mt-1 mb-1 p-2");
    selectedSourceElement.attr("data-source-filename", sourceName);
    containerElement.append(selectedSourceElement);

    var checkElement = $(document.createElement("i"));
    checkElement.addClass("fa fa-check pl-1 pr-1");
    selectedSourceElement.append(checkElement);

    var sourceNameElement = $(document.createElement("span"));
    sourceNameElement.addClass("pl-1 pr-1");
    sourceNameElement.text(sourceName);
    selectedSourceElement.append(sourceNameElement);

    var crossElement = $(document.createElement("i"));
    crossElement.addClass("fa fa-times-circle pl-1 pr-1");
    crossElement.click(function () {
        deselectSource($(this).parent());
    });
    selectedSourceElement.append(crossElement);
}

function deselectUsage(selectedUsageElement) {
    var usageName = selectedUsageElement.data("usage-name");
    selectedUsageElement.remove();

    var checkbox = $("#usagesSelection").DataTable().$("input[data-usage-name='" + usageName + "']");
    checkbox.prop('checked', false);

}

function deselectSource(selectedSourceElement) {
    var sourceName = selectedSourceElement.data("source-filename");
    selectedSourceElement.remove();

    var checkbox = $("#sourcesSelection").DataTable().$("input[data-source-filename='" + sourceName + "']");
    checkbox.prop('checked', false);
}

function clearLaunchSourceEditorModal() {
    $(".selectedUsage").each(function () {
        deselectUsage($(this));
    });

    $(".selectedSource").each(function () {
        deselectSource($(this));
    });

    $("#usagesSelection").DataTable().page("first").draw();
    $("#sourcesSelection").DataTable().page("first").draw();
}
