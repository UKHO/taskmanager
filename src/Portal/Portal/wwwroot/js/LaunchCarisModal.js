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
        selectSource();
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

function selectSource() {
    alert("bbbds");
}

function deselectUsage(selectedUsageElement) {
    var usageName = selectedUsageElement.data("usage-name");
    selectedUsageElement.remove();

    var checkbox = $("#usagesSelection").DataTable().$("input[data-usage-name='" + usageName + "']");
    checkbox.prop('checked', false);

}