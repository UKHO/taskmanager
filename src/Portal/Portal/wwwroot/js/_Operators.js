$(document).ready(function () {
    $('#Reviewer').on('typeahead:selected', function (eventObject, suggestionObject) {
        $('#hdnReviewerOperatorUpn').val(suggestionObject.userPrincipalName);
    });
    $('#Assessor').on('typeahead:selected', function (eventObject, suggestionObject) {
        $('#hdnAssessorOperatorUpn').val(suggestionObject.userPrincipalName);
    });
    $('#Verifier').on('typeahead:selected',
        function(eventObject, suggestionObject) {
            $('#hdnVerifierOperatorUpn').val(suggestionObject.userPrincipalName);
        });
});

function initialiseOperatorsTypeaheads() {

    removeOperatorsInitialiseErrors();

    $('#Reviewer, #Assessor, #Verifier').typeahead('val', "");
    $('#Reviewer, #Assessor, #Verifier').typeahead('close');

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
            removeOperatorsInitialiseErrors();
        })
        .fail(function () {
            var errorArray = ["Failed to look up users. Try refreshing the page."];
            displayOperatorsInitialiseErrors(errorArray);
        });

    $('#Reviewer, #Assessor, #Verifier').typeahead({
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
                suggestion: function (users) {
                    return "<p><span class='displayName'>" + users.displayName + "</span><br/><span class='email'>" + users.userPrincipalName + "</span></p>";
                }
            }
        });
}

function displayOperatorsInitialiseErrors(errorStringArray) {

    var orderedList = $("#operatorsErrorList");

    // == to catch undefined and null
    if (errorStringArray == null) {
        orderedList.append("<li>An unknown error has occurred</li>");

    } else {
        errorStringArray.forEach(function (item) {
            orderedList.append("<li>" + item + "</li>");
        });
    }

    $("#operatorsErrorMessages").show();
}

function removeOperatorsInitialiseErrors() {
    $("#operatorsErrorMessages").hide();
    $("#operatorsErrorList").empty();
}