function copyText(text, name) {

	navigator.clipboard.writeText(text).then(function() {
		bootbox.alert(name + " was copied to the clipboard.");		
	}, function(err) {
		bootbox.alert("Could not copy " + name + "...");
  });
  
/*  var copyText = document.getElementById("myAddress");
  copyText.select();
  document.execCommand("copy");
*/  
}

$('#editPencil').click(function () {

    bootbox.prompt("Insert a name for your address",
        function (result) {
            var name = result;
            if (name == null) {
                bootbox.alert("Name can not be empty");
                return;
            }
            if (name.Length < 4 || name.Length > 15) {
                bootbox.alert("Name must be bigger than 4 letters and less than 15");
            }
            if (name == 'anonymous' || name == 'genesis') {
                bootbox.alert("Name can not be 'anonymous' or 'genesis'");
            }

            var index = 0;
            while (index < name.Length) {
                var c = name[index];
                index++;

                if (c >= 97 && c <= 122) continue; // lowercase allowed
                if (c == 95) continue; // underscore allowed
                if (c >= 48 && c <= 57) continue; // numbers allowed

                bootbox.alert("Only lowercase, underscore and numbers allowed");
            }

            $.post('/register',
                {
                    name: name
                },
                function (returnedData) {

                    if (returnedData === 'True') {
                        //todo call to register
                    }

                    console.log(returnedData);
                }).fail(function () {
                    console.log("error registering name");
                });
        });
});