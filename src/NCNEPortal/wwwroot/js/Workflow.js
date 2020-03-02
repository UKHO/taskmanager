$(document).ready(function () {

    $("#RepromatDate").datepicker({
        autoclose: true,
        todayHighLight: true,
        format: 'dd/mm/yyyy'
    }).datepicker('update', $("#RepromatDate").val());

    $("#PublicationDate").datepicker({
        autoclose: true,
        todayHighLight: true,
        format: 'dd/mm/yyyy'
    }).datepicker('update' , $("#PublicationDate").val());

    $("#AnnounceDate").datepicker({
        autoclose: true,
        todayHighLight: true,
        format: 'dd/mm/yyyy'
    }).datepicker('update', $("#AnnounceDate").val() );

    $("#CommitToPrintDate").datepicker({
        autoclose: true,
        todayHighLight: true,
        format: 'dd/mm/yyyy'
    }).datepicker('update', $("#CommitToPrintDate").val());

    $("#CISDate").datepicker({
        autoclose: true,
        todayHighLight: true,
        format: 'dd/mm/yyyy'
    }).datepicker('update',  $("#CISDate").val());


    if ($("#ChartType").val() !== "Adoption") {

        $("#RepromatDate").hide();
        $("#lblRepDate").hide();
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
                url: "NewTask/?handler=Users",
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