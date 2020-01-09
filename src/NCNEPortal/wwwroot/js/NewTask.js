$(document).ready(function () {
    
    $("#PublicationDate").datepicker({
        autoclose: true,
        todayHighLight: true,
        format: 'dd/mm/yyyy'
    }).datepicker('update', new Date());

    $("#btnClose").click(function() {
        window.location.href = '/Index';
    });

});