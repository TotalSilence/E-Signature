$(document).ready(function () {
    function starter() {
        $("#div1").show();
        $("#div2").hide();
        $("#div3").hide();
    }

    function submitadno() {
        var re = new RegExp("^[0-9]{12}$");
        var term = document.getElementById("anumber").value;
        if (re.test(term)) {
            $("div1").hide("fast");
            $("div2").show();
            $("div3").hide("fast");
            return true;
        }
        else {
            alert("This is an invalid aadhar number");
            return false;
        }
    }
});