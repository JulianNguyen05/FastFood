// to get current year
function getYear() {
    var currentDate = new Date();
    var currentYear = currentDate.getFullYear();
    var displayYearElement = document.querySelector("#displayYear");

    // [FIX] Chỉ chạy nếu tìm thấy element #displayYear
    if (displayYearElement) {
        displayYearElement.innerHTML = currentYear;
    }
}

getYear();


// isotope js
$(window).on('load', function () {
    // Chỉ chạy isotope nếu có class .filters_menu
    if ($('.filters_menu').length) {
        $('.filters_menu li').click(function () {
            $('.filters_menu li').removeClass('active');
            $(this).addClass('active');

            var data = $(this).attr('data-filter');
            $grid.isotope({
                filter: data
            })
        });

        var $grid = $(".grid").isotope({
            itemSelector: ".all",
            percentPosition: false,
            masonry: {
                columnWidth: ".all"
            }
        })

        $(document).ready(function () {
            // Read a page's GET URL variables & return them as an associative array.
            function getUrlVars() {
                var vars = [], hash;
                if (window.location.href.indexOf('?') > -1) {
                    var hashes = window.location.href.slice(window.location.href.indexOf('?') + 1).split('&');
                    for (var i = 0; i < hashes.length; i++) {
                        hash = hashes[i].split('=');
                        vars.push(hash[0]);
                        vars[hash[0]] = hash[1];
                    }
                }
                return vars;
            };

            var id = getUrlVars()["id"];
            if (id > 0) {
                $('.filters_menu li').removeClass('active');
            }

            $('.filters_menu li').each(function () {
                // [FIX] Sử dụng jQuery attr để lấy data-id an toàn hơn
                var dataId = $(this).attr("data-id");

                // Checks if it is the same on the address bar
                if (dataId && id == dataId) {
                    $(this).closest("li").addClass("active");

                    var data = $(this).attr('data-filter');
                    $grid.isotope({
                        filter: data
                    })

                    return false; // Break loop
                }
            });
        });
    }
});

// nice select
$(document).ready(function () {
    if ($('select').length) {
        $('select').niceSelect();
    }
});

/** google_map js **/
function myMap() {
    // [FIX] Kiểm tra xem thẻ googleMap có tồn tại không trước khi khởi tạo
    var mapElement = document.getElementById("googleMap");

    if (mapElement) {
        var mapProp = {
            center: new google.maps.LatLng(40.712775, -74.005973),
            zoom: 18,
        };
        var map = new google.maps.Map(mapElement, mapProp);
    }
}

// client section owl carousel
$(document).ready(function () {
    if ($(".client_owl-carousel").length) {
        $(".client_owl-carousel").owlCarousel({
            loop: true,
            margin: 0,
            dots: false,
            nav: true,
            autoplay: true,
            autoplayHoverPause: true,
            navText: [
                '<i class="fa fa-angle-left" aria-hidden="true"></i>',
                '<i class="fa fa-angle-right" aria-hidden="true"></i>'
            ],
            responsive: {
                0: {
                    items: 1
                },
                768: {
                    items: 2
                },
                1000: {
                    items: 2
                }
            }
        });
    }
});

//'use strict';

(function ($) {
    /*-----------------------
        Quantity change
    -------------------------*/
    var proQty = $('.pro-qty');
    if (proQty.length) {
        proQty.prepend('<span class="dec qtybtn">-</span>');
        proQty.append('<span class="inc qtybtn">+</span>');

        proQty.on('click', '.qtybtn', function () {
            var $button = $(this);
            var $input = $button.parent().find('input');
            var oldValue = $input.val();

            var newVal = 0;

            if ($button.hasClass('inc')) {
                // var newVal = parseFloat(oldValue) + 1;
                if (oldValue >= 10) {
                    newVal = parseFloat(oldValue);
                } else {
                    newVal = parseFloat(oldValue) + 1;
                }
            } else {
                // Don't allow decrementing below zero
                if (oldValue > 1) {
                    newVal = parseFloat(oldValue) - 1;
                } else {
                    newVal = 1;
                }
            }
            $input.val(newVal);
        });
    }

})(jQuery);

/*For Quantity Change*/