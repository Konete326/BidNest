window.onload = function () {
    // Function to display the welcome message in the navbar
    function displayWelcomeMessage(name) {
        var welcomeMessageElement = document.getElementById('welcomeMessage1');
        if (welcomeMessageElement) {
            welcomeMessageElement.textContent = "Welcome, " + name + "!";
        }
    }

    // Check if the current page is index.html
    if (window.location.pathname === '/index.html') {
        var nameDialog = new bootstrap.Modal(document.getElementById('nameDialog'), {
            backdrop: 'static',
            keyboard: false
        });
        var welcomeDialog = new bootstrap.Modal(document.getElementById('welcomeDialog'));

        nameDialog.show();

        // Function to close the nameDialog and show welcomeDialog
        document.getElementById('submitName').onclick = function () {
            var nameInput = document.getElementById('nameInput').value;
            if (nameInput.trim() !== "") {
                nameDialog.hide();
                document.getElementById('welcomeDialogContent').textContent = "Welcome, " + nameInput + "!";
                document.getElementById('welcomeMessage1').textContent = "Welcome, " + nameInput + "!";
                welcomeDialog.show();
                // Store name in localStorage
                localStorage.setItem('nameInput', nameInput);
            }
        };

        // Function to close the welcomeDialog
        document.getElementById('closeWelcome').onclick = function () {
            welcomeDialog.hide();
        };
    } else {
        // If not index.html, retrieve name from localStorage and display welcome message in navbar
        var nameInput = localStorage.getItem('nameInput');
        if (nameInput) {
            displayWelcomeMessage(nameInput);
        }
    }
};


    
