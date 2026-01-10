// Testimonials Carousel Enhancement - Smooth Transitions

(function() {
    'use strict';

    document.addEventListener('DOMContentLoaded', function() {
        const carousel = document.getElementById('testimonialsCarousel');
        if (!carousel) return;

        // Enhance carousel with smooth transitions
        const carouselInstance = new bootstrap.Carousel(carousel, {
            interval: 5000,
            wrap: true,
            touch: true,
            pause: 'hover'
        });

        // Add smooth transition classes on slide events
        carousel.addEventListener('slide.bs.carousel', function(e) {
            const activeItem = carousel.querySelector('.carousel-item.active');
            const nextItem = e.relatedTarget;
            
            if (activeItem) {
                activeItem.classList.add('sliding-out');
            }
            if (nextItem) {
                nextItem.classList.add('sliding-in');
            }
        });

        carousel.addEventListener('slid.bs.carousel', function(e) {
            const items = carousel.querySelectorAll('.carousel-item');
            items.forEach(item => {
                item.classList.remove('sliding-out', 'sliding-in');
            });
        });

        // Improve touch/swipe on mobile
        let touchStartX = 0;
        let touchEndX = 0;
        const swipeThreshold = 50;

        carousel.addEventListener('touchstart', function(e) {
            touchStartX = e.changedTouches[0].screenX;
        }, { passive: true });

        carousel.addEventListener('touchend', function(e) {
            touchEndX = e.changedTouches[0].screenX;
            const diff = touchStartX - touchEndX;

            if (Math.abs(diff) > swipeThreshold) {
                if (diff > 0) {
                    carouselInstance.next();
                } else {
                    carouselInstance.prev();
                }
            }
        }, { passive: true });
    });

})();

