$(document).ready(function() {

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

   
    $("#btnClose").click(function() {
        window.location.href = '/Index';
    });

    $("#btnCreate").click(function() {


        removedAssignRoleErrors();

        var compiler = $("#Compiler").val();
        var verifier1 = $("#Verifier1").val();
        var verifier2 = $("#Verifier2").val();
        var publisher = $("#Publisher").val();

        $.ajax({
            type: "POST",
            url: "NewTask/?handler=AssignRoleToUser",
            beforeSend: function(xhr) {
                xhr.setRequestHeader("RequestVerificationToken",
                    $('input:hidden[name="__RequestVerificationToken"]').val());
            },
            data: {
                "compiler": compiler,
                "verifierOne": verifier1,
                "verifierTwo": verifier2,
                "publisher": publisher
            },
            success: function(result) {
                $("form").submit();
            },
            error: function(error) {
                var responseJson = error.responseJSON;
                displayAssignRoleErrors(responseJson);
            }

        });

    });

    function removedAssignRoleErrors() {
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

    $("#Publisher").on('focus',function() {
        if ($(this).val === "") {
            $('.ta_publisher').typeahead('val', "");
            $('.ta_publisher').typeahead('close');
        }
    });


    $("#ChartType").change(function() {

        if (this.value === "Adoption") {
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

});