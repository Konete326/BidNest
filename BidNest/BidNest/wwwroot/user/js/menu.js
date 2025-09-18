document.addEventListener("DOMContentLoaded", function () {
    let viewCount = localStorage.getItem('viewCount');
    if (viewCount === null) {
      viewCount = 0;
    } else {
      viewCount = parseInt(viewCount);
    }
    document.getElementById('viewCount').textContent = viewCount;
    viewCount++;
    localStorage.setItem('viewCount', viewCount.toString());
  });
// Store the user's location
var userLocation = null;

// Function to fetch user's location using Geolocation API
function fetchLocation() {
  if (navigator.geolocation) {
    navigator.geolocation.getCurrentPosition(function(position) {
      userLocation = {
        latitude: position.coords.latitude.toFixed(2),
        longitude: position.coords.longitude.toFixed(2)
      };
    }, function(error) {
      console.error('Error fetching location:', error);
    });
  } else {
    console.error('Geolocation is not supported.');
  }
}