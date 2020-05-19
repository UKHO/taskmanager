$(document).ready(function () {

    var menuItem = 0;

    var userFullName = $("#userFullName > strong").text();

    var inFlightTasksTable = setupInFlightTasks();

    applyDatatableFilter();

    handleMyTaskList();
    handleTeamTasks();
   
    setMenuItemSelection();

    function setupInFlightTasks() {

        return inFlightTasksTable = $('#inFlightTasks').DataTable({
            "pageLength": 10,
            "lengthMenu": [10, 15, 20, 25],
            'sDom': 'ltipr',
            'autoWidth': true,
            'columnDefs': [
                {
                    'targets': [0],
                    'orderable': false,
                    'searchable': false
                },
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
            "order": [[1, 'asc']],
            "scrollX": true,
            "createdRow": function(row, data, dataIndex) {
                if (data[13] === "") {
                    $("td.details-control", row).removeClass("details-control");
                    $("td.details-control i", row).removeClass("fa");
                }
            }
        });


    }

    function format(data) {
        return '<span class="note-formatting">' + data[13] + '</span>';
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

    $('#txtGlobalSearch').keyup(function () {
        inFlightTasksTable.search($(this).val()).draw();
    });

    $("#btnNewTask").click(function () { window.location.href = '/NewTask'; });



    function applyDatatableFilter() {
        //Datatables search plugin
        $.fn.dataTable.ext.search.push(
            function(settings, searchData, index, rowData, counter) {

                // My Tasks List filter
                if (menuItem === 0) {

                    if (settings.sTableId === "inFlightTasks") {
                        return filterMyAssignedTasksList(rowData);
                    }

                    return true;
                } else {

                    if (settings.sTableId === "inFlightTasks") {
                        return true;
                    }
                }

            }
        );
    }


    function filterMyAssignedTasksList(rowData) {

        var username = rowData[5];

        if (username !== userFullName) {
            return false;
        }
        return true;
    }



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

    
    function handleMyTaskList() {
        $("#btnMyTaskList").click(function () {
            menuItem = 0;
            setMenuItemSelection();

            $('#txtGlobalSearch').val("");
            inFlightTasksTable.search("").draw();

        });
    }

    function handleTeamTasks() {
        $("#btnTeamTasks").click(function () {
            menuItem = 1;
            setMenuItemSelection();

            $('#txtGlobalSearch').val("");

            inFlightTasksTable.search("").draw();

        });
    }
    

    $("#editTaskNoteModal").on("shown.bs.modal",
        function () {
            $("#txtNote").focus();
        });

    $("#btnClearTaskNote").click(function () {
        $("#txtNote").val("");
        $("#txtNote").focus();
    });

    $("#btnPostTaskNote").on("submit", function () {
        $("#btnPostTaskNote").prop("disabled", true);

    });

    $(".assignTaskItem").on("click",
        function () {

            $("#btnAssignTaskToUser").prop("disabled", false);
            $("#AssignTaskError").html("");

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

    $("#btnAssignTaskToUser").on("click", function () {

        if ($("#txtUsername").val() === "") {
            $("#assignTaskTypeaheadError").show();
            $("#assignTaskErrorMsg").text("Please enter a user.");
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
                xhr.setRequestHeader("RequestVerificationToken", $('input:hidden[name="__RequestVerificationToken"]').val());
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
                //getComments();
            },
            error: function (error) {
                console.log(error);

                //$("#AddCommentError")
                //    .html("<div class=\"alert alert-danger\" role=\"alert\">Error adding comment. Please try again later.</div>");

                //$("#btnPostComment").prop("disabled", false);
            }
        });

    });

    initialiseAssignTaskTypeahead();

    function initialiseAssignTaskTypeahead() {

        $('#assignTaskTypeaheadError').collapse("hide");

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

                $('#assignTaskTypeaheadError').collapse("hide");

                usersFetched = true;
            })
            .fail(function () {
                $("#btnAssignTaskToUser").prop("disabled", true);
                $("#txtUsername").prop("disabled", true);

                $('#assignTaskTypeaheadError').collapse("show");

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
    //Show MyTaskList
    $("#btnMyTaskList").trigger("click");
});


