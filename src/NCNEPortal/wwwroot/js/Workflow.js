$(document).ready(function () {


    var formChanged = false;
    $("#frmWorkflow").change(function () { formChanged = true; });

    $(".commentslider").click(function() {

        var stage = $(this).data("taskstage");
        var processid = $(this).data("processid");


        var check = $("#commentscheck-" + stage).prop('checked');
        if (check) {
           $("#comment-" + stage).collapse("hide");
        } else {
            $("#comment-" + stage).collapse("show");
        }

    });

    $(".btn-stage-rework").click(function() {
        var processId = $(this).data("processid");
        var stageId = $(this).data("taskstageid");
        var username = $(this).data("username");
        var stageName = $(this).data("stagename");
        var stageTypeId = $(this).data("stagetypeid");



        $.ajax({
            type: "POST",
            url: "Workflow/?handler=ValidateRework",
            beforeSend: function (xhr) {
                xhr.setRequestHeader("RequestVerificationToken", $('input:hidden[name="__RequestVerificationToken"]').val());
            },
            data: {
                "processId": processId,
                "stageId": stageId,
                "username": username,
                "stageTypeId": stageTypeId

            },

            success: function (result) {
                $("#hdnConfirmProcessId").val(processId);
                $("#hdnConfirmStageId").val(stageId);
                $("#hdnAssignedUser").val(username);
                $("#Rework").val(true);
                $("#msgComplete").html("Are you sure you want to send for <span style='font-weight:600'>" + stageName + "</span> Rework?");
                $("#ConfirmModal").modal("show");

            },
            error: function (error) {
                var responseJson = error.responseJSON;

                if (responseJson != null) {
                    $("#workflowSaveErrorMessage").html("");

                    $("#workflowSaveErrorMessage").append("<ul/>");
                    var unOrderedList = $("#workflowSaveErrorMessage ul");

                    responseJson.forEach(function (item) {
                        unOrderedList.append("<li>" + item + "</li>");
                    });

                    $("#modalSaveWorkflowErrors").modal("show");
                }

            }
        });

    });

    $(".btn-stage-complete").click(function () {
        var processId = $(this).data("processid");
        var stageId = $(this).data("taskstageid");
        var username = $(this).data("username");
        var stageName = $(this).data("stagename");
        var stageTypeId = $(this).data("stagetypeid");
        
        
        
        $.ajax({
            type: "POST",
            url: "Workflow/?handler=ValidateComplete",
            beforeSend: function (xhr) {
                xhr.setRequestHeader("RequestVerificationToken", $('input:hidden[name="__RequestVerificationToken"]').val());
            },
            data: {
                "processId": processId,
                "stageId": stageId,
                "username": username,
                "stageTypeId":stageTypeId

            },

            success: function (result) {
                $("#hdnConfirmProcessId").val(processId);
                $("#hdnConfirmStageId").val(stageId);
                $("#hdnAssignedUser").val(username);
                $("#Rework").val(false);
                $("#msgComplete").html("Are you sure you want to mark <span style='font-weight:600'>" + stageName + "</span> as complete?");
                $("#ConfirmModal").modal("show");

            },
            error: function (error) {
                var responseJson = error.responseJSON;

                if (responseJson != null) {
                    $("#workflowSaveErrorMessage").html("");

                    $("#workflowSaveErrorMessage").append("<ul/>");
                    var unOrderedList = $("#workflowSaveErrorMessage ul");

                    responseJson.forEach(function (item) {
                        unOrderedList.append("<li>" + item + "</li>");
                    });

                    $("#modalSaveWorkflowErrors").modal("show");
                }

            }
        });
    });

    $("#btnConfirm").click(function() {
        var processId = $("#hdnConfirmProcessId").val();
        var stageId = $("#hdnConfirmStageId").val();
        var username = $("#hdnAssignedUser").val();
        var rework = $("#Rework").val();

        $.ajax({
            type: "POST",
            url: "Workflow/?handler=Complete",
            beforeSend: function(xhr) {
                xhr.setRequestHeader("RequestVerificationToken",
                    $('input:hidden[name="__RequestVerificationToken"]').val());
            },
            data: {
                "processId": processId,
                "stageId": stageId,
                "username": username,
                "isRework" :rework
            },
            success: function(result) {
                formChanged = false;
                window.location.href = "/workflow?ProcessId=" + processId;
            }
        });

    });

    $(".btnAddTaskcomment").click(function() {
        var processId = $(this).data("processid");
        $("#hdnTaskCommentProcessId").val(processId);
        $("#btnPostTaskComment").prop("disabled", false);
        $("#editTaskCommentError").html("");


        $("#editTaskCommentModal").modal("show");
    });

    $("#editTaskCommentModal").on("shown.bs.modal",
        function () {
            $("#txtTaskComment").val("");
            $("#txtTaskComment").focus();
        });

    $("#btnClearTaskComment").click(function () {
        $("#txtTaskComment").val("");
        $("#txtTaskComment").focus();
    });


    $("#btnPostTaskComment").on("click",
        function() {
            var comment = $("#txtTaskComment").val();
            $("#editTaskCommentError").html("");
            if (comment.trim().length === 0) {
                $("#editTaskCommentError")
                    .html("<div class=\"alert alert-danger\" role=\"alert\">Please enter a comment.</div>");
                $('#txtTaskComment').focus();

            }
            else {

                $("#btnPostTaskComment").prop("disabled", true);
                var processId = $("#hdnTaskCommentProcessId").val();

                $.ajax({
                    type: "POST",
                    url: "Workflow/?handler=TaskComment",

                    beforeSend: function (xhr) {
                        xhr.setRequestHeader("RequestVerificationToken",
                            $('input:hidden[name="__RequestVerificationToken"]').val());
                    },
                    data: {
                        "txtComment": comment,
                        "commentProcessId": processId
                       
                    },
                    success: function (result) {

                        var tableRow = "<tr><td style='width:200px'><div  class='mb-2'><strong>" + result[0] + "</strong></div>" +
                            "<div>" + result[1] + "</div></td>" +
                            "<td style='width:20px'><span class='fas fa-comment-alt'></span></td>" +
                            "<td><div class='d-inline'>" + comment + "</div></td></tr> ";

                        var count = parseInt($("#commentCount").html());

                        $("#commentCount").html((count + 1).toString());

                        $('#TaskCommentsTable').append(tableRow);

                        $("#editTaskCommentModal").modal("hide");


                    },
                    error: function (error) {
                        var responseJson = error.responseJSON;
                        $("#editTaskCommentError").append(responseJson);

                    }
                });
            }

        });


    $(".btnAddcomment").click(function () {
        var stageid = $(this).data("taskstage");
        var processid = $(this).data("processid");
        $("#hdnCommentProcessId").val(processid);
        $("#hdnStageId").val(stageid);

        $("#btnPostComment").prop("disabled", false);
        $("#editStageCommentError").html("");


        $("#editStageCommentModal").modal("show");
    });



    $("#editStageCommentModal").on("shown.bs.modal",
        function () {
            $("#txtComment").val("");
            $("#txtComment").focus();
        });

    $("#btnClearComment").click(function () {
        $("#txtComment").val("");
        $("#txtComment").focus();
    });

    $("#btnPostComment").on("click", function () {
        var comment = $("#txtComment").val();
        $("#editStageCommentError").html("");
        if (comment.trim().length === 0) {
            $("#editStageCommentError").html("<div class=\"alert alert-danger\" role=\"alert\">Please enter a comment.</div>");
            $('#txtComment').focus();
        } else {

            $("#btnPostComment").prop("disabled", true);
            var stageid = $("#hdnStageId").val();
            var processid = $("#hdnCommentProcessId").val();

            $.ajax({
                type: "POST",
                url: "Workflow/?handler=StageComment",

                beforeSend: function(xhr) {
                    xhr.setRequestHeader("RequestVerificationToken",
                        $('input:hidden[name="__RequestVerificationToken"]').val());
                },
                data: {
                    "txtComment": comment,
                    "commentProcessId": processid,
                    "stageId": stageid
                },
                success: function(result) {
                    $('#container-' +
                        stageid.toString()).prepend(' <div class="row m-3"><div class= "col-2" ><div>' +
                        result[1] +
                        '</div > <br /> <div><strong>' +
                        result[0] +
                        '</strong ></div ></div > <div class="col-10">' +
                        comment +
                        '</div > </div ><br />');

                    $("#editStageCommentModal").modal("hide");


                },
                error: function(error) {
                    var responseJson = error.responseJSON;
                    $("#editStageCommentError").append(responseJson);

                }
            });
        }
    });

        
    window.onbeforeunload = function () {
        if (formChanged) {
            return "Changes detected";
        }
    };
    
    $("#RepromatDate").datepicker({
        autoclose: true,
        todayHighLight: true,
        format: 'dd/mm/yyyy'
    }).datepicker('update');

    $("#PublicationDate").datepicker({
        autoclose: true,
        todayHighLight: true,
        format: 'dd/mm/yyyy'
    }).datepicker('update' );

    $("#AnnounceDate").datepicker({
        autoclose: true,
        todayHighLight: true,
        format: 'dd/mm/yyyy'
    }).datepicker('update' );

    $("#CommitToPrintDate").datepicker({
        autoclose: true,
        todayHighLight: true,
        format: 'dd/mm/yyyy'
    }).datepicker('update' );

    $("#CISDate").datepicker({
        autoclose: true,
        todayHighLight: true,
        format: 'dd/mm/yyyy'
    }).datepicker('update');

    $("#SendDate3ps").datepicker({
        autoclose: true,
        todayHighLight: true,
        format: 'dd/mm/yyyy'
    }).datepicker('update');

    $("#ExpectedReturnDate3ps").datepicker({
        autoclose: true,
        todayHighLight: true,
        format: 'dd/mm/yyyy'
    }).datepicker('update');

    $("#ActualReturnDate3ps").datepicker({
        autoclose: true,
        todayHighLight: true,
        format: 'dd/mm/yyyy'
    }).datepicker('update');




    var chartType = $("#chartType").text();
    if (chartType.trim() === "Adoption") {
        $("#PublicationDate").prop("disabled", true);
    }
    else
    {

        $("#RepromatDate").hide();
         $("#lblRepDate").hide();
     }

    set3psStatus();

    $("#3psToggle").on("change",
        function() {
            set3psStatus();
        });


    function set3psStatus() {

        var sentto3ps = $("#3psToggle").prop("checked");

        if (sentto3ps) {
            $("#SendDate3ps").prop("disabled", false);
            $("#ExpectedReturnDate3ps").prop("disabled", false);
            $("#ActualReturnDate3ps").prop("disabled", false);
            
        }
        else {

            $("#SendDate3ps").val("").datepicker("update");
            $("#ExpectedReturnDate3ps").val("").datepicker("update");
            $("#ActualReturnDate3ps").val("").datepicker("update");
            
            $("#SendDate3ps").prop("disabled", true);
            $("#ExpectedReturnDate3ps").prop("disabled", true);
            $("#ActualReturnDate3ps").prop("disabled", true);

        }

    }


    $("#Dating").change(function() {

        var dtPublish = $("#PublicationDate").val();
        var deadLine = $(this).val();
        if (dtPublish !== "") {
            $.ajax({
                type: "POST",
                url: "Workflow/?handler=CalcMilestones",

                beforeSend: function (xhr) {
                    xhr.setRequestHeader("RequestVerificationToken",
                        $('input:hidden[name="__RequestVerificationToken"]').val());
                },
                data: {
                    "deadLine": deadLine,
                    "dtInput": dtPublish,
                    "IsPublish": true
                },
                success: function (result) {
                    $("#AnnounceDate").datepicker("setDate", result[0]);
                    $("#CommitToPrintDate").datepicker("setDate", result[1]);
                    $("#CISDate").datepicker("setDate", result[2]);

                },
                error: function (error) {
                    var responseJson = error.responseJSON;
                    displayAssignRoleErrors(responseJson);
                }


            });
        }
    }
    );

    $("#RepromatDate").change(function () {
        var dtRepromat = $(this).val();
        if (dtRepromat !== "") {
            $.ajax({
                type: "POST",
                url: "Workflow/?handler=CalcMilestones",

                beforeSend: function (xhr) {
                    xhr.setRequestHeader("RequestVerificationToken",
                        $('input:hidden[name="__RequestVerificationToken"]').val());
                },
                data: {
                    "deadLine" : 0,
                    "dtInput": dtRepromat,
                    "IsPublish" : false
                },
                success: function (result) {
                    $("#PublicationDate").datepicker("update", result[0]);

                },
                error: function (error) {
                    var responseJson = error.responseJSON;
                    displayAssignRoleErrors(responseJson);
                }


            });
        }
    });

    $("#PublicationDate").change(function () {
        var dtPublish = $(this).val();
        var deadLine = $("#Dating").val();
        if ((dtPublish !== "") && (deadLine>0)) {
            $.ajax({
                type: "POST",
                url: "Workflow/?handler=CalcMilestones",

                beforeSend: function (xhr) {
                    xhr.setRequestHeader("RequestVerificationToken",
                        $('input:hidden[name="__RequestVerificationToken"]').val());
                },
                data: {
                    "deadLine"  : deadLine,
                    "dtInput": dtPublish,
                    "IsPublish" : true
                },
                success: function (result) {
                    $("#AnnounceDate").datepicker("setDate", result[0]);
                    $("#CommitToPrintDate").datepicker("setDate", result[1]);
                    $("#CISDate").datepicker("setDate", result[2]);


                },
                error: function (error) {
                    var responseJson = error.responseJSON;
                    displayAssignRoleErrors(responseJson);
                }


            });
        }
    });



    $("#btnTerminate").on("click", function() {
            {
                $("#ConfirmTerminate").modal("show");
            }
    });

    $("#btnSave").on("click",
        function() {

            $("#workflowSaveErrorMessage").html("");
            $("#btnClose").prop("disabled", true);
            $("#btnSave").prop("disabled", true);

            var formData = $("#frmWorkflow").serialize();

            $.ajax({
                type: "POST",
                url: "Workflow/?handler=Save",
                beforeSend: function (xhr) {
                    xhr.setRequestHeader("RequestVerificationToken", $('input:hidden[name="__RequestVerificationToken"]').val());
                },
                data: formData,
                complete: function () {
                    window.setTimeout(function () {
                        //$("#modalWaitAssessDone").modal("hide");
                        $("#btnClose").prop("disabled", false);
                        $("#btnSave").prop("disabled", false);
                    }, 200);
                },
                success: function (result) {
                    formChanged = false;
                    var chartNo = $("#ChartNo").val();
                    var workflowType = $("#workflowType").text().trim();
                    $('h1').text(workflowType + " - " + chartNo);
                    //update Deadline dates
                    var dateIds = JSON.parse(result);
                    $("#DtExp-" + dateIds.FormsDate).html($("#AnnounceDate").val());
                    $("#DtExp-" + dateIds.CommitDate).html($("#CommitToPrintDate").val());
                    $("#DtExp-" + dateIds.CisDate).html($("#CISDate").val());
                    $("#DtExp-" + dateIds.PublishDate).html($("#PublicationDate").val());
                    var processId = $("#hdnProcessId").val();
                    window.location.href = "/workflow?ProcessId="+processId;

                },
                error: function (error) {
                    var responseJson = error.responseJSON;

                    if (responseJson != null) {
                        $("#workflowSaveErrorMessage").append("<ul/>");
                        var unOrderedList = $("#workflowSaveErrorMessage ul");

                        responseJson.forEach(function (item) {
                            unOrderedList.append("<li>" + item + "</li>");
                        });

                        $("#modalSaveWorkflowErrors").modal("show");
                    }

                }
            });



        });

    $("#btnClose").on("click",
        function() {
            window.location.href = '/Index';
        });

    $("#terminatingTask").submit(function(event) {
        if ($("#txtTerminateComment").val().trim() === "") {
            $("#ConfirmTerminateError")
                .html("<div class=\"alert alert-danger\" role=\"alert\">Please enter a comment.</div>");
            $("#txtTerminateComment").focus();
            event.preventDefault();
        }
    });

    $("#ConfirmTerminate").on("shown.bs.modal",
        function () {
            $("#txtTerminateComment").focus();
        });


    if ($("#IsCarisProjectCreated").val() === "True") {
        setControlState(false);
    } else {
        setControlState(true);
    }



    $("#btnCreateCarisProject").on("click", function () {

        $("#createCarisProjectSuccess").collapse("hide");
        $("#createCarisProjectError").collapse("hide");

        setControlState(false);

        $("#createCarisProjectSpinner").show();

        var processId = Number($("#hdnProcessId").val());
        var projectName = $("#txtCarisProject").val();

        $.ajax({
            type: "POST",
            url: "Workflow/?handler=CreateCarisProject",
            beforeSend: function (xhr) {
                xhr.setRequestHeader("RequestVerificationToken", $('input:hidden[name="__RequestVerificationToken"]').val());
            },
            data: {
                "processId": processId,
                "projectName": projectName,

            },
            success: function (data) {
                $("#createCarisProjectSuccess").collapse("show");
            },
            error: function (error) {
                setControlState(true);
                var errorMessage = error.getResponseHeader("Error");

                $("#createCarisProjectErrorMessage").text("Failed to complete Caris Project creation. " + errorMessage);
                $("#createCarisProjectError").collapse("show");
            },
            complete: function () {
                $("#createCarisProjectSpinner").hide();
            }
        });

    });


    function setControlState(enableCarisProject) {
        if (enableCarisProject) {
            $("#btnCreateCarisProject").prop("disabled", false);
            $("#txtCarisProject").prop("disabled", false);
        } else {
            $("#btnCreateCarisProject").prop("disabled", true);
            $("#txtCarisProject").prop("disabled", true);
        }
    }

    initDesignCustomSelect();

    initialiseAssignRoleTypeahead();


    function initialiseAssignRoleTypeahead() {
        $('#assignRoleTypeaheadError').collapse("hide");
        // Constructing the suggestion engine
        var users = new Bloodhound({
            datumTokenizer: Bloodhound.tokenizers.whitespace,
            queryTokenizer: Bloodhound.tokenizers.whitespace,
            prefetch: {
                url: "Workflow/?handler=Users",
                ttl: 600000
            },
            initialize: false
        });

        var promise = users.initialize();
        promise
            .done(function() {
                removedAssignRoleErrors();
            })
            .fail(function() {
                $('#assignRoleErrorMessages').collapse("show");
                var errorArray = ["Failed to look up users. Try refreshing the page"];
                displayAssignRoleErrors(errorArray);
            });

        // Initializing the typeahead
        $('.ta_compiler').add('.ta_v1').add('.ta_v2').add('.ta_publisher').typeahead({
                hint: true,
                highlight: true, /* Enable substring highlighting */

                minLength:
                    3 /* Specify minimum characters required for showing result */
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


    function removedAssignRoleErrors() {
        $("#assignRoleErrorList").empty();
        $("#assignRoleErrorMessages").collapse("hide");
    }

    function displayAssignRoleErrors(errorStringArray) {
        var orderedList = $("#assignRoleErrorList");

        if (errorStringArray == null)
            orderedList.append("<li> An unknown error has occured</li>");
        else {
            errorStringArray.forEach(function (item) {
                orderedList.append("<li>" + item + "</li>");
            });
        }
        $("#assignRoleErrorMessages").collapse("show");
    }


    $("#Compiler").on('focus', function () {
        if ($(this).val === "") {
            $('.ta_compiler').typeahead('val', "");
            $('.ta_compiler').typeahead('close');
        }
    });


    $("#Verifier1").on('focus', function () {
        if ($(this).val === "") {
            $('.ta_v1').typeahead('val', "");
            $('.ta_v1').typeahead('close');
        }
    });

    $("#Verifier2").on('focus', function () {
        if ($(this).val === "") {
            $('.ta_v2').typeahead('val', "");
            $('.ta_v2').typeahead('close');
        }
    });

    $("#Publisher").on('focus', function () {
        if ($(this).val === "") {
            $('.ta_publisher').typeahead('val', "");
            $('.ta_publisher').typeahead('close');
        }
    });

});