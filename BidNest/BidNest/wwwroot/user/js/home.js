let items = document.querySelectorAll('.slider .list .item');
let next = document.getElementById('next');
let prev = document.getElementById('prev');
let thumbnails = document.querySelectorAll('.thumbnail .item');

// config param
let countItem = items.length;
let itemActive = 0;
// event next click
next.onclick = function(){
    itemActive = itemActive + 1;
    if(itemActive >= countItem){
        itemActive = 0;
    }
    showSlider();
}
//event prev click
prev.onclick = function(){
    itemActive = itemActive - 1;
    if(itemActive < 0){
        itemActive = countItem - 1;
    }
    showSlider();
}
// auto run slider
let refreshInterval = setInterval(() => {
    next.click();
}, 5000)
function showSlider(){
    // remove item active old
    let itemActiveOld = document.querySelector('.slider .list .item.active');
    let thumbnailActiveOld = document.querySelector('.thumbnail .item.active');
    itemActiveOld.classList.remove('active');
    thumbnailActiveOld.classList.remove('active');

    // active new item
    items[itemActive].classList.add('active');
    thumbnails[itemActive].classList.add('active');

    // clear auto time run slider
    clearInterval(refreshInterval);
    refreshInterval = setInterval(() => {
        next.click();
    }, 5000)
}

// click thumbnail
thumbnails.forEach((thumbnail, index) => {
    thumbnail.addEventListener('click', () => {
        itemActive = index;
        showSlider();
    })
})


  
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

// Update ticker content with date, time, and location
function updateTicker() {
  var ticker = document.getElementById("ticker");
  var now = new Date();
  var dateString = now.toLocaleDateString();
  var timeString = now.toLocaleTimeString();

  // If user location is available, display it
  if (userLocation) {
    var locationString = "Latitude: " + userLocation.latitude + ", Longitude: " + userLocation.longitude;
    ticker.textContent = "Date: " + dateString + " | Time: " + timeString + " | Location: " + locationString + " | ";
  } else {
    ticker.textContent = "Date: " + dateString + " | Time: " + timeString + " | Location: Location not available | ";
  }
}

// Update ticker content every second
setInterval(updateTicker, 1000);

// Initial update
fetchLocation();
updateTicker();



