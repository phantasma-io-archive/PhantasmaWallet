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
