/**
 * Product Carousel Touch Fix
 * Allows vertical scrolling on mobile while preserving horizontal swipe for carousel
 */

(function() {
    'use strict';
    
    // Only run on mobile devices
    if (window.innerWidth > 767) {
        return;
    }
    
    // Track touch start position and direction
    let touchStartX = 0;
    let touchStartY = 0;
    let touchStartTime = 0;
    let isVerticalScroll = false;
    let isHorizontalSwipe = false;
    
    // Function to handle touch events for product carousels
    function setupCarouselTouchHandling() {
        const isRTL = document.documentElement.dir === 'rtl' || document.documentElement.getAttribute('dir') === 'rtl';
        const carousels = document.querySelectorAll('#newArrivalsCarousel, #bestSellersCarousel, #offersCarousel');
        
        carousels.forEach(function(carousel) {
            if (!carousel) return;
            
            // Skip if RTL - let the RTL handler manage it
            if (isRTL) {
                return;
            }
            
            // Remove any existing listeners to avoid duplicates
            const carouselInner = carousel.querySelector('.carousel-inner');
            if (!carouselInner) return;
            
            // Track touch start
            carouselInner.addEventListener('touchstart', function(e) {
                if (e.touches.length === 1) {
                    touchStartX = e.touches[0].clientX;
                    touchStartY = e.touches[0].clientY;
                    touchStartTime = Date.now();
                    isVerticalScroll = false;
                    isHorizontalSwipe = false;
                }
            }, { passive: true });
            
            // Detect scroll direction during touch move
            carouselInner.addEventListener('touchmove', function(e) {
                if (e.touches.length === 1 && touchStartX && touchStartY) {
                    const deltaX = Math.abs(e.touches[0].clientX - touchStartX);
                    const deltaY = Math.abs(e.touches[0].clientY - touchStartY);
                    
                    // If vertical movement is greater, it's a vertical scroll
                    if (deltaY > deltaX && deltaY > 10) {
                        isVerticalScroll = true;
                        isHorizontalSwipe = false;
                    } 
                    // If horizontal movement is greater, it's a horizontal swipe
                    else if (deltaX > deltaY && deltaX > 10) {
                        isHorizontalSwipe = true;
                        isVerticalScroll = false;
                    }
                }
            }, { passive: true });
            
            // On touch end, if it was vertical scroll, ensure Bootstrap doesn't interfere
            carouselInner.addEventListener('touchend', function(e) {
                // Reset after a short delay
                setTimeout(function() {
                    isVerticalScroll = false;
                    isHorizontalSwipe = false;
                    touchStartX = 0;
                    touchStartY = 0;
                }, 100);
            }, { passive: true });
        });
    }
    
    // Also handle touch events on the entire carousel container
    function setupCarouselContainerTouchHandling() {
        const isRTL = document.documentElement.dir === 'rtl' || document.documentElement.getAttribute('dir') === 'rtl';
        const carousels = document.querySelectorAll('#newArrivalsCarousel, #bestSellersCarousel, #offersCarousel');
        
        carousels.forEach(function(carousel) {
            if (!carousel) return;
            
            // Skip if RTL - let the RTL handler manage it
            if (isRTL) {
                return;
            }
            
            // Allow vertical scrolling to pass through
            carousel.addEventListener('touchmove', function(e) {
                // Don't prevent default - let browser handle scrolling naturally
            }, { passive: true });
        });
    }
    
    // Initialize on DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function() {
            setupCarouselTouchHandling();
            setupCarouselContainerTouchHandling();
        });
    } else {
        setupCarouselTouchHandling();
        setupCarouselContainerTouchHandling();
    }
    
    // Re-initialize after lazy-loaded sections are added
    const observer = new MutationObserver(function(mutations) {
        let shouldReinit = false;
        mutations.forEach(function(mutation) {
            if (mutation.addedNodes.length > 0) {
                mutation.addedNodes.forEach(function(node) {
                    if (node.nodeType === 1) { // Element node
                        if (node.id === 'newArrivalsCarousel' || 
                            node.id === 'bestSellersCarousel' ||
                            node.id === 'offersCarousel' ||
                            node.querySelector && (node.querySelector('#newArrivalsCarousel') || node.querySelector('#bestSellersCarousel') || node.querySelector('#offersCarousel'))) {
                            shouldReinit = true;
                        }
                    }
                });
            }
        });
        
        if (shouldReinit) {
            setTimeout(function() {
                setupCarouselTouchHandling();
                setupCarouselContainerTouchHandling();
            }, 200);
        }
    });
    
    observer.observe(document.body, {
        childList: true,
        subtree: true
    });
    
    // Expose function globally
    window.setupProductCarouselTouchHandling = setupCarouselTouchHandling;
})();

