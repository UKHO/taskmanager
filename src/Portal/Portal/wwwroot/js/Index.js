$(document).ready(function () {

    // Required unless we refactor behaviour around errors
    var usersFetched = false;

    var menuItem = 0;

    setMenuItems();

    var userFullName = $("#userFullName > strong").text();

    var unassignedTasksTable = setupUnassignedTasks();

    var inFlightTasksTable = setupInFlightTasks();
    var selectedTeams = getTeamSelection();

    applyDatatableFilter();

    handleDisplayTaskNotes();


    initialiseAssignTaskTypeahead();

    handleMyTaskList();
    handleTeamTasks();
    handleHistoricalTasks();

    handleAssignTaskToUser();

    handleSelectAllTeams();
    handleClearAllTeams();
    handleFilterTasksByTeam();
    setupFilterTasksByTeamButtonStyle();

    handleGlobalSearch();
    handleTaskNotes();
    handleAssignTask();
    handleSelectAllTeams();

    populateTasks();

    handleSelectTeam();

    function setMenuItems() {
        var menuItemPriosToHistoricalTaskView = sessionStorage.getItem('historicalMenuItem');

        if (menuItemPriosToHistoricalTaskView != null) {
            menuItem = Number(menuItemPriosToHistoricalTaskView);
            sessionStorage.setItem('historicalMenuItem', 0); // Set it back to default
        }
    }

    function setupUnassignedTasks() {
        return $('#unassignedTasks').DataTable({
            "pageLength": 5,
            'sDom': 'ltipr',
            "lengthMenu": [5, 10, 25, 50],
            'columnDefs': [
                {
                    'targets': [12],
                    'orderable': false,
                    'searchable': false
                },
                {
                    'targets': [13],
                    'visible': false,
                    'searchable': false
                }
            ],
            "scrollX": true,
            "order": [[1, 'asc']],
            "ordering": true
        });
    }

    function setupInFlightTasks() {
        return $('#inFlightTasks').DataTable({
            "pageLength": 10,
            "lengthMenu": [5, 10, 25, 50],
            'sDom': 'ltipr',
            'autoWidth': true,
            'columnDefs': [
                {
                    'targets': [0],
                    'orderable': false,
                    'searchable': false
                },
                {
                    'targets': [13],
                    'orderable': false,
                    'searchable': false
                },
                {
                    'targets': [14],
                    'visible': false,
                    'searchable': false
                }
            ],
            "order": [[2, 'asc']],
            "scrollX": true,
            "createdRow": function (row, data, dataIndex) {
                if (data[14] === "") {
                    $("td.details-control", row).removeClass("details-control");
                    $("td.details-control i", row).removeClass("fa");
                }
            }
        });
    }

    function format(data) {
        return '<span class="note-formatting">' + data[14] + '</span>';
    }

    function handleDisplayTaskNotes() {
        $('#inFlightTasks tbody').on('click',
            'td.details-control i',
            function () {

                var tr = $(this).closest('tr');
                var row = inFlightTasksTable.row(tr);

                if (row.child.isShown()) {
                    row.child.hide();
                    tr.removeClass('shown');
                } else {
                    row.child(format(row.data()), 'no-padding').show();
                    tr.addClass('shown');
                }
            });
    }

    function applyDatatableFilter() {
        //Datatables search plugin
        $.fn.dataTable.ext.search.push(
            function (settings, searchData, index, rowData, counter) {

                // My Tasks List filter
                if (menuItem === 0) {

                    if (settings.sTableId === "inFlightTasks") {
                        return filterMyAssignedTasksList(rowData);
                    }

                    return true;
                }

                // Team tasks list
                if (menuItem === 1) {
                    if (settings.sTableId === "unassignedTasks") {
                        return filterSelectedTeamsList(rowData[11]);
                    }

                    if (settings.sTableId === "inFlightTasks") {
                        return filterSelectedTeamsList(rowData[12]);
                    }

                    return true;
                }

                return true;

            }
        );
    }

    function filterMyAssignedTasksList(rowData) {

        var taskStage = rowData[8];

        switch (taskStage) {
            case "Review":
                var reviewer = rowData[9];

                if (reviewer !== userFullName) {
                    return false;
                }
                break;
            case "Assess":
                var assessor = rowData[10];

                if (assessor !== userFullName) {
                    return false;
                }
                break;
            case "Verify":
                var verifier = rowData[11];

                if (verifier !== userFullName) {
                    return false;
                }
                break;
        }
        return true;

    }

    function filterSelectedTeamsList(team) {

        if (selectedTeams.length === 0) {
            return true;
        }

        var exists = $.inArray(team, selectedTeams) !== -1;
        return exists;

    }

    function handleMyTaskList() {
        $("#btnMyTaskList").click(function () {
            menuItem = 0;
            setMenuItemSelection();

            $("#btnSelectTeam").hide();
            $('#txtGlobalSearch').val("");
            unassignedTasksTable.search("").draw();
            inFlightTasksTable.search("").draw();

        });
    }

    function handleTeamTasks() {
        $("#btnTeamTasks").click(function () {
            menuItem = 1;
            setMenuItemSelection();

            $("#btnSelectTeam").show();
            $('#txtGlobalSearch').val("");
            setupFilterTasksByTeamButtonStyle();

            selectedTeams = getTeamSelection();

            unassignedTasksTable.search("").draw();
            inFlightTasksTable.search("").draw();

        });
    }

    function handleHistoricalTasks() {
        $("#btnHistoricalTasks").click(function () {

            sessionStorage.setItem('historicalMenuItem', menuItem);
            window.location.href = '/DbAssessment/HistoricalTasks';

        });
    }

    function setMenuItemSelection() {
        $("#menuItemList button").each(function (index) {
            if (index === menuItem) {
                $(this).addClass("buttonSelected");
                $(this).removeClass("btn-primary");

            } else {
                $(this).removeClass("buttonSelected");
                $(this).addClass("btn-primary");
            }
        });
    }

    function setupFilterTasksByTeamButtonStyle() {
        var selectedTeamsArray = getTeamSelection();

        if (selectedTeamsArray.length > 0) {
            $("#btnSelectTeam").removeClass("btn-primary").addClass("btn-success");
            $(".filterIcon").show();
        } else {

            $("#btnSelectTeam").removeClass("btn-success").addClass("btn-primary");
            $(".filterIcon").hide();
        }
    }

    function handleGlobalSearch() {
        $('#txtGlobalSearch').keyup(function() {
            unassignedTasksTable.search($(this).val()).draw();
            inFlightTasksTable.search($(this).val()).draw();
        });
    }

    function handleTaskNotes() {
        $(".taskNoteItem").on("click",
            function() {

                $("#btnPostTaskNote").prop("disabled", false);
                $("#editTaskNoteError").html("");

                var processId = $(this).data("processid");
                $("#hdnProcessId").val(processId);

                var taskNote = $(this).data("tasknote");
                $("#txtNote").val(taskNote);

                $("#editTaskNoteModal").modal("show");
            });

        $("#editTaskNoteModal").on("shown.bs.modal",
            function() {
                $("#txtNote").focus();
            });

        $("#btnClearTaskNote").click(function() {
            $("#txtNote").val("");
            $("#txtNote").focus();
        });

        $("#btnPostTaskNote").on("submit",
            function() {
                $("#btnPostTaskNote").prop("disabled", true);

            });
    }

    function handleAssignTask() {
        $(".assignTaskItem").on("click",
            function() {
                //$("#btnAssignTaskToUser").prop("disabled", false);

                var processId = $(this).data("processid");
                $("#hdnAssignTaskProcessId").val(processId);

                var taskStage = $(this).data("taskstage");
                $("#hdnAssignTaskStage").val(taskStage);

                $("#assignTaskModal").modal("show");
            });

        $("#assignTaskModal").on("shown.bs.modal",
            function() {
                $("#assignTaskTypeaheadError").hide();
                $("#assignTaskErrorMsg").text("");
                $("#txtUsername").focus();
                $('.typeahead').typeahead('val', "");
                $('.typeahead').typeahead('close');
            });

        $("#btnCancelAssignTask").on("click",
            function() {
                if (usersFetched) removeAssignUserErrors();
            });
    }

    function handleAssignTaskToUser() {
        $("#btnAssignTaskToUser").on("click",
            function () {

                removeAssignUserErrors();

                if ($("#txtUsername").val() === "") {

                    var errorArray = ["Please enter a user"];
                    displayAssignUserErrors(errorArray);

                    return;
                }

                $("#btnAssignTaskToUser").prop("disabled", true);

                var processId = $("#hdnAssignTaskProcessId").val();
                var userName = $("#txtUsername").val();
                var taskStage = $("#hdnAssignTaskStage").val();

                $.ajax({
                    type: "POST",
                    url: "Index/?handler=AssignTaskToUser",
                    beforeSend: function (xhr) {
                        xhr.setRequestHeader("RequestVerificationToken",
                            $('input:hidden[name="__RequestVerificationToken"]').val());
                    },
                    data: {
                        "processId": processId,
                        "userName": userName,
                        "taskStage": taskStage
                    },
                    success: function (result) {
                        $("#assignTaskModal").modal("hide");
                        $("body").removeClass("modal-open");
                        $(".modal-backdrop").remove();

                        window.location.reload();
                    },
                    error: function (error) {
                        var responseJson = error.responseJSON;

                        displayAssignUserErrors(responseJson);

                        $("#btnAssignTaskToUser").prop("disabled", false);
                    }
                });

            });
    }

    function initialiseAssignTaskTypeahead() {

        $('#assignTaskErrorMessages').collapse("hide");

        var users = new Bloodhound({
            datumTokenizer: function (d) {
                return Bloodhound.tokenizers.nonword(d.displayName);
            }, 
            queryTokenizer: Bloodhound.tokenizers.whitespace,
            prefetch: {
                url: "/Index/?handler=Users",
                ttl: 60000
            },
            initialize: false
        });
        var promise = users.initialize();
        promise
            .done(function () {
                $("#btnAssignTaskToUser").prop("disabled", false);
                $("#txtUsername").prop("disabled", false);

                removeAssignUserErrors();

                usersFetched = true;
            })
            .fail(function () {
                $("#btnAssignTaskToUser").prop("disabled", true);
                $("#txtUsername").prop("disabled", true);

                var errorArray = ["Failed to look up users. Try refreshing the page."];
                displayAssignUserErrors(errorArray);

                usersFetched = false;
            });

        // Initializing the typeahead
        $('.typeahead').typeahead({
                hint: true,
                highlight: true, /* Enable substring highlighting */
                minLength: 3 /* Specify minimum characters required for showing result */
            },
            {
                name: 'users',
                source: users,
                limit: 100,
                displayKey: 'displayName',
                valueKey: 'userPrincipalName',
                templates: {
                    empty: '<div>No results</div>',
                    suggestion: function(users) {
                        return "<p><span class='displayName'>" + users.displayName + "</span><br/><span class='email'>" + users.userPrincipalName + "</span></p>";
                    }
                }
            });
    }


    function populateTasks() {
        switch (menuItem) {
        case 0:
            $("#btnMyTaskList").trigger("click");
            break;
        case 1:
                $("#btnTeamTasks").trigger("click");
            break;
        default:
            throw "Not Implemented";
        }
    }

    function handleSelectTeam() {
        $("#btnSelectTeam").click(function() {
            loadTeamSelectionFromSessionStorage();

            $("#selectTeamsModal").modal("show");
        });
    }

    function handleSelectAllTeams() {

        $("#btnSelectAllTeams").on('click',
            function () {
                $(".teamsCheckbox").each(function (index, item) {
                    $(item).prop('checked', true);
                });

            });
    }

    function handleClearAllTeams() {

        $("#btnClearAllTeams").on('click',
            function () {
                $(".teamsCheckbox").each(function (index, item) {
                    $(item).prop('checked', false);
                });

            });
    }

    function handleFilterTasksByTeam() {
        $("#btnFilterTasksByTeam").on("click", function () {

            $("#selectTeamsModal").modal("hide");
            saveTeamSelectionToSessionStorage();
            setupFilterTasksByTeamButtonStyle();

            selectedTeams = getTeamSelection();

            unassignedTasksTable.search("").draw();
            inFlightTasksTable.search("").draw();
        });
    }

    function getTeamSelection() {
        var checkBoxArray = [];
        var loadCheckBoxArray = JSON.parse(sessionStorage.getItem('teams'));

        if (loadCheckBoxArray != null) {
            $.each(loadCheckBoxArray, function (key, value) {
                if (value.id === 'team0') {

                    checkBoxArray.push("");
                } else {
                    checkBoxArray.push(value.name);
                }
            });
            
        }

        return checkBoxArray;
    }

    function saveTeamSelectionToSessionStorage() {
        var checkBoxArray = [];

        $(".teamsCheckbox").each(function () {
            if ($(this).prop('checked')) {

                var team = {
                    'id': $(this).attr('id'),
                    'name': $(this).siblings(".teamsCheckboxLabel").text()
                }
                checkBoxArray.push(team);
            }
        });

        sessionStorage.setItem('teams', JSON.stringify(checkBoxArray));
    }

    function loadTeamSelectionFromSessionStorage() {
        var loadCheckBoxArray = JSON.parse(sessionStorage.getItem('teams'));

        if (loadCheckBoxArray != null) {
            $(".teamsCheckbox").each(function () {
                var checkboxId = $(this).attr('id');
                var exist = $.grep(loadCheckBoxArray,
                    function (n, i) {
                        return n.id === checkboxId;
                    });
                $(this).prop('checked', (exist.length === 1));
            });
        }

    }
});

function displayAssignUserErrors(errorStringArray) {

    var orderedList = $("#assignTaskErrorList");

    // == to catch undefined and null
    if (errorStringArray == null) {
        orderedList.append("<li>An unknown error has occurred</li>");

    } else {
        errorStringArray.forEach(function (item) {
            orderedList.append("<li>" + item + "</li>");
        });
    }

    $("#assignTaskErrorMessages").collapse("show");
}

function removeAssignUserErrors() {
    $("#assignTaskErrorMessages").collapse("hide");
    $("#assignTaskErrorList").empty();
}