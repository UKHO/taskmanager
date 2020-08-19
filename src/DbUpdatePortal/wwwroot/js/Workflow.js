$(document).ready(function () {
    var isReadOnly = $("#IsReadOnly").val() === "True";

    if (isReadOnly) {
        makeFormReadOnly($("#frmWorkflow"));
    }

    $('.ta_compiler').on('typeahead:selected', function (eventObject, suggestionObject) {
        $('#CompilerUpn').val(suggestionObject.userPrincipalName);
    });
    $('.ta_compiler').on('typeahead:autocompleted', function (eventObject, suggestionObject) {
        $('#CompilerUpn').val(suggestionObject.userPrincipalName);
    });
    $('.ta_v1').on('typeahead:selected', function (eventObject, suggestionObject) {
        $('#Verifier1Upn').val(suggestionObject.userPrincipalName);
    });
    $('.ta_v1').on('typeahead:autocompleted', function (eventObject, suggestionObject) {
        $('#Verifier1Upn').val(suggestionObject.userPrincipalName);
    });

    var formChanged = false;
    $("#frmWorkflow").change(function () { formChanged = true; });
    

    $(".allcommentslider").click(function() {
        var check = $("#allcommentscheck").prop('checked');

        if (check) {

            $('[id^="commentscheck-"]').collapse("hide");
            $('[id^="comment-"]').collapse("hide");
            $('[id^="commentscheck-"]').prop('checked', false);

        } else {

            $('[id^="commentscheck-"]').collapse("show");
            $('[id^="comment-"]').collapse("show");
            $('[id^="commentscheck-"]').prop('checked', true);
        }

    });

    $(".commentslider").click(function() {

        var stage = $(this).data("taskstage");
        var processid = $(this).data("processid");


        var check = $("#commentscheck-" + stage).prop('checked');
        if (check) {
            $("#comment-" + stage).collapse("hide");
            $("#allcommentscheck").prop('checked', false);
        } else {
            $("#comment-" + stage).collapse("show");
        }

    });
    

    

    function publishCarisChart(versionNo, processId, stageId) {
        $.ajax({
            type: "POST",
            url: "Workflow/?handler=PublishCarisChart",
            beforeSend: function (xhr) {
                xhr.setRequestHeader("RequestVerificationToken",
                    $('input:hidden[name="__RequestVerificationToken"]').val());
            },
            data: {
                "versionNumber": versionNo,
                "processId": processId,
                "stageId": stageId
            },
            success: function (result) {
                formChanged = false;
                $("#PublishChartModal").modal("hide");
                window.location.href = "/workflow?ProcessId=" + processId;

            },
            error: function (error) {
                var responseJson = error.responseJSON;
                if (responseJson != null) {
                    $("#publishChartErrorMessage").text(responseJson);
                    $("#publishChartError").collapse("show");
                }
                $("#PublishConfirmModal").modal("hide");
                $("#btnPublish").prop("disabled", true);
                $("#PublishChartModal").modal("show");
            }
        });
    }

    function validateCompleteRework(url,  processId,stageId,
          username, stageTypeId,stageName, rework, publish ) {
        $.ajax({
            type: "POST",
            url: url,
            beforeSend: function (xhr) {
                xhr.setRequestHeader("RequestVerificationToken", $('input:hidden[name="__RequestVerificationToken"]').val());
            },
            data: {
                "processId": processId,
                "username": username,
                "stageTypeId": stageTypeId,
                "publish" : publish
            },

            success: function (result) {
                if (publish) {
                    $("#hdnPublishProcessId").val(processId);
                    $("#hdnPublishStageId").val(stageId);
                    $("#hdnPublishUser").val(username);

                    $("#PublishChartModal").modal("show");

                } else {

                    $("#hdnConfirmProcessId").val(processId);
                    $("#hdnConfirmStageId").val(stageId);
                    $("#hdnAssignedUser").val(username);
                    $("#Rework").val(rework);

                    if (rework) {
                        $("#msgComplete").html("Are you sure you want to send for <span id=stageName>" +
                            stageName +
                            "</span> Rework?");
                    } else {

                        $("#msgComplete").html("Are you sure you want to mark <span id=stageName>" +
                            stageName +
                            "</span> as complete?");
                    }
                    $("#ConfirmModal").modal("show");
                }

            },
            error: function (error) {
                var responseJson = error.responseJSON;
                (this).checked = false;

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
    }


    $(".btn-stage-rework").click(function() {
        var processId = $(this).data("processid");
        var stageId = $(this).data("taskstageid");
        var username = $(this).data("username");
        var stageName = $(this).data("stagename");
        var stageTypeId = $(this).data("stagetypeid");

        var url = "Workflow/?handler=ValidateRework";

        validateCompleteRework(url, processId, stageId, username, stageTypeId, stageName, true,false);


    });

    $(".btn-stage-complete").click(function () {
        var processId = $(this).data("processid");
        var stageId = $(this).data("taskstageid");
        var username = $(this).data("username");
        var stageName = $(this).data("stagename");
        var stageTypeId = $(this).data("stagetypeid");

        var url = "Workflow/?handler=ValidateComplete";

        validateCompleteRework(url, processId, stageId, username, stageTypeId, stageName, false);
        
    });

    $("#btnConfirm").click(function() {
        $(this).prop('disabled', true);
        var processId = $("#hdnConfirmProcessId").val();
        var stageId = $("#hdnConfirmStageId").val();
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
    
    $("#TargetDate").datepicker({
        autoclose: true,
        todayHighLight: true,
        format: 'dd/mm/yyyy'
    }).datepicker('update');




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

            $("#hdnProductAction").val($("#ProductAction").val());

            var formData = $("#frmWorkflow").serialize();
            
            $.ajax({
                type: "POST",
                url: "Workflow/?handler=Save",
                beforeSend: function (xhr) {
                    xhr.setRequestHeader("RequestVerificationToken", $('input:hidden[name="__RequestVerificationToken"]').val());
                },
                data:  formData,
                complete: function () {
                    window.setTimeout(function () {
                        //$("#modalWaitAssessDone").modal("hide");
                        $("#btnClose").prop("disabled", false);
                        $("#btnSave").prop("disabled", false);
                    }, 200);
                },
                success: function (result) {
                    formChanged = false;
                    //var chartNo = $("#ChartNo").val();
                    //var workflowType = $("#workflowType").text().trim();
                    //$('h1').text(workflowType + " - " + chartNo);
                    ////update Deadline dates
                    //var dateIds = JSON.parse(result);
                    //$("#DtExp-" + dateIds.FormsDate).html($("#AnnounceDate").val());
                    //$("#DtExp-" + dateIds.CommitDate).html($("#CommitToPrintDate").val());
                    //$("#DtExp-" + dateIds.CisDate).html($("#CISDate").val());
                    //$("#DtExp-" + dateIds.PublishDate).html($("#PublicationDate").val());
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
        function () {
            if (isReadOnly === true)
                window.location.href = '/HistoricalTasks';
                else
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
    
    $("#btnCreateCarisProject").on("click",
        function() {

            $("#createCarisProjectSuccess").collapse("hide");
            $("#createCarisProjectError").collapse("hide");


            setControlState(false);

            $("#createCarisProjectSpinner").show();

            var processId = Number($("#hdnProcessId").val());
            var projectName = $("#txtCarisProject").val();

            $.ajax({
                type: "POST",
                url: "Workflow/?handler=CreateCarisProject",
                beforeSend: function(xhr) {
                    xhr.setRequestHeader("RequestVerificationToken",
                        $('input:hidden[name="__RequestVerificationToken"]').val());
                },
                data: {
                    "processId": processId,
                    "projectName": projectName

                },
                success: function(data) {
                    $("#createCarisProjectSuccess").collapse("show");
                    
                },
                error: function(error) {
                    setControlState(true);
                    var errorMessage = error.getResponseHeader("Error");

                    $("#createCarisProjectErrorMessage")
                        .text("Failed to complete Caris Project creation. " + errorMessage);
                    $("#createCarisProjectError").collapse("show");
                },
                complete: function() {
                    $("#createCarisProjectSpinner").hide();
                }
            });

        });


    function setControlState(enableCarisProject) {
        if (enableCarisProject) {
            $("#btnCreateCarisProject").prop("disabled", false);
            $("#txtCarisProject").prop("disabled", false);
            $("#ChartNo").prop("disabled", false);
        } else {
            $("#btnCreateCarisProject").prop("disabled", true);
            $("#txtCarisProject").prop("disabled", true);
            $("#ChartNo").prop("disabled", true);
        }
    }

    initDesignCustomSelect();

    initialiseAssignRoleTypeahead();


    function initialiseAssignRoleTypeahead() {
        $('#assignRoleTypeaheadError').collapse("hide");
        // Constructing the suggestion engine
        var users = new Bloodhound({ 
            datumTokenizer: function (d) {
                return Bloodhound.tokenizers.nonword(d.displayName);
            },             
            queryTokenizer: Bloodhound.tokenizers.whitespace,
            prefetch: {
                url: "/Workflow/?handler=Users",
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
        $('.ta_compiler').add('.ta_v1').typeahead({
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

    function makeFormReadOnly(formElement) {
        var fieldset = $(formElement).children("fieldset");
        fieldset.prop("disabled", true);
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


    $("#btnPublishConfirm").click(function () {
        var complete = $("#Complete").val();
        var processId = $("#hdnProcessId").val();
        if (complete === "true") {
            completeWorkflow(processId);
        } else {

            var versionNo = $("#chartVersionNo").val();
            processId = $("#hdnPublishProcessId").val();
            var stageId = $("#hdnPublishStageId").val();
            publishCarisChart(versionNo, processId, stageId);
        }

    });

    function completeWorkflow(processId) {
        $.ajax({
            type: "POST",
            url: "workflow/?handler=CompleteWorkflow",
            beforeSend: function (xhr) {
                xhr.setRequestHeader("RequestVerificationToken",
                    $('input:hidden[name="__RequestVerificationToken"]').val());
            },
            data: {
                "processId": processId
            },
            success: function () {
                $("#PublishConfirmModal").modal("hide");
                window.location.href = '/Index';

            },
            error: function (error) {
                var responseJson = error.responseJSON;
                (this).checked = false;

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
    }

    $("#btnComplete").click(function () {
       
        var userName = $("#Verifier1Upn").val();

        $.ajax({
            type: "POST",
            url: "Workflow/?handler=ValidateCompleteWorkflow",
            beforeSend: function(xhr) {
                xhr.setRequestHeader("RequestVerificationToken",
                    $('input:hidden[name="__RequestVerificationToken"]').val());
            },
            data: {
                "userName" : userName

            },

            success: function (result) {
                $("#Complete").val(true);
                $("#msgPublishComplete").html("Are you sure you want to complete this workflow ?");
                $("#PublishConfirmModal").modal("show");
                }
            ,
            error: function(error) {
                var responseJson = error.responseJSON;
                (this).checked = false;

                if (responseJson != null) {
                    $("#workflowSaveErrorMessage").html("");

                    $("#workflowSaveErrorMessage").append("<ul/>");
                    var unOrderedList = $("#workflowSaveErrorMessage ul");

                    responseJson.forEach(function(item) {
                        unOrderedList.append("<li>" + item + "</li>");
                    });

                    $("#modalSaveWorkflowErrors").modal("show");
                }

            }

        });

    });
});