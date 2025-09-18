/**
 * WEBSITE: https://themefisher.com
 * TWITTER: https://twitter.com/themefisher
 * FACEBOOK: https://www.facebook.com/themefisher
 * GITHUB: https://github.com/themefisher/
 */

(function ($) {
	'use strict';

	// testimonial-wrap
	if ($('.testimonial-wrap').length !== 0) {
		$('.testimonial-wrap').slick({
			slidesToShow: 2,
			slidesToScroll: 2,
			infinite: true,
			dots: true,
			arrows: false,
			autoplay: true,
			autoplaySpeed: 6000,
			responsive: [{
					breakpoint: 1024,
					settings: {
						slidesToShow: 1,
						slidesToScroll: 1,
						infinite: true,
						dots: true
					}
				},
				{
					breakpoint: 900,
					settings: {
						slidesToShow: 1,
						slidesToScroll: 1
					}
				}, {
					breakpoint: 600,
					settings: {
						slidesToShow: 1,
						slidesToScroll: 1
					}
				},
				{
					breakpoint: 480,
					settings: {
						slidesToShow: 1,
						slidesToScroll: 1
					}
				}
			]
		});
	}

	// navbarDropdown
	if ($(window).width() < 992) {
		$('#navbar .dropdown-toggle').on('click', function () {
			$(this).siblings('.dropdown-menu').animate({
				height: 'toggle'
			}, 300);
		});
	}

	$(window).on('scroll', function () {
		//.Scroll to top show/hide
		if ($('#scroll-to-top').length) {
			var scrollToTop = $('#scroll-to-top'),
				scroll = $(window).scrollTop();
			if (scroll >= 200) {
				scrollToTop.fadeIn(200);
			} else {
				scrollToTop.fadeOut(100);
			}
		}
	});
	// scroll-to-top
	if ($('#scroll-to-top').length) {
		$('#scroll-to-top').on('click', function () {
			$('body,html').animate({
				scrollTop: 0
			}, 600);
			return false;
		});
	}

	// portfolio-gallery
	if ($('.portfolio-gallery').length !== 0) {
		$('.portfolio-gallery').each(function () {
			$(this).find('.popup-gallery').magnificPopup({
				type: 'image',
				gallery: {
					enabled: true
				}
			});
		});
	}

	// Counter
	if ($('.counter-stat').length !== 0) {
		$('.counter-stat').counterUp({
			delay: 10,
			time: 1000
		});
	}

})(jQuery);
function updateTicker() {
    var ticker = document.getElementById("ticker");
    var now = new Date();
    var dateString = now.toLocaleDateString();
    var timeString = now.toLocaleTimeString();
    
    // Fetch user's location using Geolocation API
    if (navigator.geolocation) {
      navigator.geolocation.getCurrentPosition(function(position) {
        var locationString = "Latitude: " + position.coords.latitude.toFixed(2) + ", Longitude: " + position.coords.longitude.toFixed(2);
        ticker.textContent = "Date: " + dateString + " | Time: " + timeString + " | Location: " + locationString + " | ";
      });
    } else {
      ticker.textContent = "Date: " + dateString + " | Time: " + timeString + " | Location: Location not available | ";
    }
  }
  
  // Update ticker content every second
  setInterval(updateTicker, 1000);
  
  // Initial update
  updateTicker();

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

