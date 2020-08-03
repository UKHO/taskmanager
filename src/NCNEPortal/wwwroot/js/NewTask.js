$(document).ready(function() {

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
    $('.ta_v2').on('typeahead:selected', function (eventObject, suggestionObject) {
        $('#Verifier2Upn').val(suggestionObject.userPrincipalName);
    });
    $('.ta_v2').on('typeahead:autocompleted', function (eventObject, suggestionObject) {
        $('#Verifier2Upn').val(suggestionObject.userPrincipalName);
    });
    $('.ta_100pCheck').on('typeahead:selected', function (eventObject, suggestionObject) {
        $('#HundredPCheckUpn').val(suggestionObject.userPrincipalName);
    });
    $('.ta_100pCheck').on('typeahead:autocompleted', function (eventObject, suggestionObject) {
        $('#HundredPCheckUpn').val(suggestionObject.userPrincipalName);
    });

    var tabIndex = 1;
    $("#frmNewTask *").filter(':visible').each(function (i) {

        var tagName = $(this).prop("tagName").toLowerCase();
        var designSystem = $(this).hasClass("design-custom-select");

        if (tagName === "input" || tagName === "button" || designSystem ) {
            $(this).attr('tabindex', tabIndex);
            tabIndex++;
        }
    });

    $("#PublicationDate").datepicker({
        autoclose: true,
        todayHighLight: true,
        format: 'dd/mm/yyyy'
    }).datepicker('update');

    $("#RepromatDate").datepicker({
        autoclose: true,
        todayHighLight: true,
        format: 'dd/mm/yyyy'
    }).datepicker('update');


    $("#RepromatDate").change(function () {
        var dtRepromat = $(this).val();
        if (dtRepromat !== "") {
            $.ajax({
                type: "POST",
                url: "NewTask/?handler=CalcPublishDate",

                beforeSend: function(xhr) {
                    xhr.setRequestHeader("RequestVerificationToken",
                        $('input:hidden[name="__RequestVerificationToken"]').val());
                },
                data: {
                    "dtRepromat": dtRepromat
                },
                success: function(result) {
                    $("#PublicationDate").val(result).datepicker.update();
                   
                },
                error: function(error) {
                    var responseJson = error.responseJSON;
                    displayAssignRoleErrors(responseJson);
                }


            });
        }
    });

    $("#btnClose").click(function() {
        window.location.href = '/Index';
    });

    $("#btnCreate").click(function() {


        removeAssignRoleErrors();
        var formData = $("#frmNewTask").serialize();

        $.ajax({
            type: "POST",
            url: "NewTask/?handler=Save",
            beforeSend: function(xhr) {
                xhr.setRequestHeader("RequestVerificationToken",
                    $('input:hidden[name="__RequestVerificationToken"]').val());
            },
            data: formData,
            success: function(result) {
                window.location.href = '/Index';
            },
            error: function(error) {
                var responseJson = error.responseJSON;

                if (responseJson != null) {
                    var responseJson = error.responseJSON;
                    displayAssignRoleErrors(responseJson);
                }

            }
        });

    });


    function removeAssignRoleErrors() {
        $("#assignRoleErrorList").empty();
        $("#assignRoleErrorMessages").collapse("hide");
    }

    function displayAssignRoleErrors(errorStringArray) {
        var orderedList = $("#assignRoleErrorList");

        if (errorStringArray == null)
            orderedList.append("<li> An unknown error has occured</li>");
        else {
            errorStringArray.forEach(function(item) {
                orderedList.append("<li>" + item + "</li>");
            });
        }
        $("#assignRoleErrorMessages").collapse("show");
    }


    $("#Compiler").on('focus', function() {
        if ($(this).val === "") {
            $('.ta_compiler').typeahead('val', "");
            $('.ta_compiler').typeahead('close');
        }
    });


    $("#Verifier1").on('focus', function() {
        if ($(this).val === "") {
            $('.ta_v1').typeahead('val', "");
            $('.ta_v1').typeahead('close');
        }
    });

    $("#Verifier2").on('focus',function() {
        if ($(this).val === "") {
            $('.ta_v2').typeahead('val', "");
            $('.ta_v2').typeahead('close');
        }
    });

    $("#100pCheck").on('focus',function() {
        if ($(this).val === "") {
            $('.ta_100pCheck').typeahead('val', "");
            $('.ta_100pCheck').typeahead('close');
        }
    });


    $("#ChartType").change(function() {

        if ($(this).val() === "Adoption") {
            $("#RepromatDate").prop("disabled", false);
            $("#PublicationDate").prop("disabled", true);
            $("#PublicationDate").val("").datepicker("update");
           
        } else {
            $("#RepromatDate").prop("disabled", true);
            $("#PublicationDate").prop("disabled", false);
            $("#RepromatDate").val("").datepicker("update");
        }
    });

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
                url: "/NewTask/?handler=Users",
                ttl: 600000
            },
            initialize: false
        });

        var promise = users.initialize();
        promise
            .done(function() {
                removeAssignRoleErrors();
            })
            .fail(function() {
                $('#assignRoleErrorMessages').collapse("show");
                var errorArray = ["Failed to look up users. Try refreshing the page"];
                displayAssignRoleErrors(errorArray);
            });

        // Initializing the typeahead
        $('.ta_compiler').add('.ta_v1').add('.ta_v2').add('.ta_100pCheck').typeahead({
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

});