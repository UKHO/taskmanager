$(document).ready(function () {

});

function initializeLaunchSourceEditorModal() {
    attachLaunchSourceEditorModalButtonHandler();
    attachUsagesSelectionCheckboxesHandler();
    attachSourcesSelectionCheckboxesHandler();

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

function deselectSource() {}