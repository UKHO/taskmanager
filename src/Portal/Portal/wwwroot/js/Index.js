﻿$(document).ready(function () {

    var menuItem = 0;
    var userFullName = $("#userFullName > strong").text();

    var unassignedTasksTable = $('#unassignedTasks').DataTable({
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
    var inFlightTasksTable = $('#inFlightTasks').DataTable({
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

    function format(data) {
        return '<span class="note-formatting">' + data[14] + '</span>';
    }

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

    //Datatables search plugin
    $.fn.dataTable.ext.search.push(
        function (settings, searchData, index, rowData, counter) {
            if (settings.sTableId !== "inFlightTasks" ||
                menuItem !== 0) {
                return true;
            }

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
    );

    $("#btnMyTaskList").click(function() {
        menuItem = 0;
        setMenuItemSelection();

        $('#txtGlobalSearch').val("");
        unassignedTasksTable.search("").draw();
        inFlightTasksTable.search("").draw();

    });

    $("#btnTeamTasks").click(function() {
        menuItem = 1;
        setMenuItemSelection();

        $('#txtGlobalSearch').val("");
        unassignedTasksTable.search("").draw();
        inFlightTasksTable.search("").draw();
        
    });

    function setMenuItemSelection() {
        console.log("hello");
        $("#menuItemList button").each(function (index) {
            console.log("hello2");
            if (index === menuItem) {
                $(this).addClass("btn-info");
                $(this).removeClass("btn-primary");
            } else {
                $(this).removeClass("btn-info");
                $(this).addClass("btn-primary");
            }
        });
    }

    $('#txtGlobalSearch').keyup(function () {
        unassignedTasksTable.search($(this).val()).draw();
        inFlightTasksTable.search($(this).val()).draw();
    });


    // Required unless we refactor behaviour around errors
    var usersFetched = false;

    $(".taskNoteItem").on("click",
        function () {

            $("#btnPostTaskNote").prop("disabled", false);
            $("#editTaskNoteError").html("");

            var processId = $(this).data("processid");
            $("#hdnProcessId").val(processId);

            var taskNote = $(this).data("tasknote");
            $("#txtNote").val(taskNote);

            $("#editTaskNoteModal").modal("show");
        });

    $("#editTaskNoteModal").on("shown.bs.modal",
        function () {
            $("#txtNote").focus();
        });

    $("#btnClearTaskNote").click(function () {
        $("#txtNote").val("");
        $("#txtNote").focus();
    });

    $("#btnPostTaskNote").on("submit",
        function () {
            $("#btnPostTaskNote").prop("disabled", true);

        });

    $(".assignTaskItem").on("click",
        function () {
            //$("#btnAssignTaskToUser").prop("disabled", false);

            var processId = $(this).data("processid");
            $("#hdnAssignTaskProcessId").val(processId);

            var taskStage = $(this).data("taskstage");
            $("#hdnAssignTaskStage").val(taskStage);

            $("#assignTaskModal").modal("show");
        });

    $("#assignTaskModal").on("shown.bs.modal",
        function () {
            $("#assignTaskTypeaheadError").hide();
            $("#assignTaskErrorMsg").text("");
            $("#txtUsername").focus();
            $('.typeahead').typeahead('val', "");
            $('.typeahead').typeahead('close');
        });

    $("#btnCancelAssignTask").on("click",
        function () {
            if (usersFetched) removeAssignUserErrors();
        });

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

    initialiseAssignTaskTypeahead();

    function initialiseAssignTaskTypeahead() {

        $('#assignTaskErrorMessages').collapse("hide");

        // Constructing the suggestion engine
        var users = new Bloodhound({
            datumTokenizer: Bloodhound.tokenizers.whitespace,
            queryTokenizer: Bloodhound.tokenizers.whitespace,
            prefetch: {
                url: "Index/?handler=Users",
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
            highlight: true,    /* Enable substring highlighting */

            minLength:
                3               /* Specify minimum characters required for showing result */
        },
            {
                name: 'users',
                source: users,
                limit: 100,
                templates: {
                    notFound: '<div>No results</div>'
                }
            });
    }

    $("#btnMyTaskList").trigger("click");
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