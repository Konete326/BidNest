window.onload = function () {
    // Function to display the welcome message in the navbar
    function displayWelcomeMessage(name) {
        var welcomeMessageElement = document.getElementById('welcomeMessage');
        if (welcomeMessageElement) {
            welcomeMessageElement.textContent = "Welcome, " + name + "!";
        }
    }

    // Check if the current page is index.html
    if (window.location.pathname === '/index.html') {
        var hasNameBeenAsked = sessionStorage.getItem('nameAsked');

        // If the name hasn't been asked before or the page is reloaded, show the name dialog
        if (!hasNameBeenAsked || window.performance && window.performance.navigation.type === window.performance.navigation.TYPE_RELOAD) {
            var nameDialog = new bootstrap.Modal(document.getElementById('nameDialog'), {
                backdrop: 'static',
                keyboard: false
            });
            nameDialog.show();

            // Function to close the nameDialog and show welcomeDialog
            document.getElementById('submitName').onclick = function () {
                var nameInput = document.getElementById('nameInput').value;
                if (nameInput.trim() !== "") {
                    nameDialog.hide();
                    document.getElementById('welcomeDialogContent').textContent = "Welcome, " + nameInput + "!";
                    document.getElementById('welcomeMessage').textContent = "Welcome, " + nameInput + "!";
                    sessionStorage.setItem('nameAsked', true); // Set that the name has been asked
                    // Store name in localStorage
                    localStorage.setItem('nameInput', nameInput);
                }
            };
        } else {
            // If the name has been asked before and the page is not reloaded, retrieve name from localStorage and display welcome message in navbar
            var nameInput = localStorage.getItem('nameInput');
            if (nameInput) {
                displayWelcomeMessage(nameInput);
            }
        }

        // Function to close the welcomeDialog
        document.getElementById('closeWelcome').onclick = function () {
            var welcomeDialog = new bootstrap.Modal(document.getElementById('welcomeDialog'));
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
