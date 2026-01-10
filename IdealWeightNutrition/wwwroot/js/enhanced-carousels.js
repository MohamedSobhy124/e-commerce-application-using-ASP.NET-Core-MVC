/**
 * Enhanced Carousel Functionality
 * On mobile: Show 2 products per slide for better browsing
 * On desktop: Keeps original structure with multiple products per slide
 */

// Function to enhance carousels (can be called multiple times)
function enhanceCarouselsForMobile() {
    const carousels = document.querySelectorAll('.product-carousel:not([data-mobile-restructured="true"]), .offers-carousel:not([data-mobile-restructured="true"])');
    
    carousels.forEach(function(carousel) {
        const carouselInner = carousel.querySelector('.carousel-inner');
        if (!carouselInner) return;
        
        // Store original HTML to restore later
        if (!carousel.dataset.originalHtml) {
            carousel.dataset.originalHtml = carouselInner.innerHTML;
        }
        
        // Get all product cards from all carousel-items
        const allProducts = carouselInner.querySelectorAll('.carousel-item .row [class*="col-"]');
        
        if (allProducts.length <= 1) return;
        
        // Create new carousel structure with 2 products per slide
        const newCarouselItems = [];
        for (let i = 0; i < allProducts.length; i += 2) {
            // Get current and next product (if exists)
            const product1 = allProducts[i];
            const product2 = allProducts[i + 1];
            
            if (!product1) continue;
            
            // Create new carousel-item
            const newCarouselItem = document.createElement('div');
            newCarouselItem.className = 'carousel-item' + (i === 0 ? ' active' : '');
            
            // Create row wrapper
            const row = document.createElement('div');
            row.className = 'row g-3';
            
            // Clone first product column
            const newCol1 = product1.cloneNode(true);
            newCol1.className = 'col-6';
            row.appendChild(newCol1);
            
            // Clone second product column if exists
            if (product2) {
                const newCol2 = product2.cloneNode(true);
                newCol2.className = 'col-6';
                row.appendChild(newCol2);
            } else {
                // If odd number, create empty spacer
                const spacer = document.createElement('div');
                spacer.className = 'col-6';
                spacer.style.visibility = 'hidden';
                row.appendChild(spacer);
            }
            
            newCarouselItem.appendChild(row);
            newCarouselItems.push(newCarouselItem);
        }
        
        if (newCarouselItems.length === 0) return;
        
        // Replace carousel inner content
        carouselInner.innerHTML = '';
        newCarouselItems.forEach(function(item) {
            carouselInner.appendChild(item);
        });
        
        // Update indicators if they exist
        const indicators = carousel.querySelector('.carousel-indicators');
        if (indicators && newCarouselItems.length > 1) {
            indicators.innerHTML = '';
            newCarouselItems.forEach(function(item, index) {
                const indicatorBtn = document.createElement('button');
                indicatorBtn.type = 'button';
                indicatorBtn.setAttribute('data-bs-target', '#' + carousel.id);
                indicatorBtn.setAttribute('data-bs-slide-to', index.toString());
                indicatorBtn.setAttribute('aria-label', 'Slide ' + (index + 1));
                if (index === 0) {
                    indicatorBtn.className = 'active';
                    indicatorBtn.setAttribute('aria-current', 'true');
                }
                indicators.appendChild(indicatorBtn);
            });
        }
        
        // Mark as restructured
        carousel.dataset.mobileRestructured = 'true';
        
        // Reinitialize Bootstrap carousel
        if (typeof bootstrap !== 'undefined') {
            // Dispose existing carousel if any
            const existingCarousel = bootstrap.Carousel.getInstance(carousel);
            if (existingCarousel) {
                existingCarousel.dispose();
            }
            
            // Get interval from data attribute or default
            const interval = carousel.dataset.bsInterval || carousel.getAttribute('data-bs-interval') || '8000';
            
            // Create new carousel instance with touch support
            const bsCarousel = new bootstrap.Carousel(carousel, {
                interval: parseInt(interval),
                wrap: true,
                touch: true
            });
            carousel._carousel = bsCarousel;
        }
    });
}

function restoreDesktopCarousels() {
    const carousels = document.querySelectorAll('.product-carousel[data-mobile-restructured="true"], .offers-carousel[data-mobile-restructured="true"]');
    
    carousels.forEach(function(carousel) {
        if (carousel.dataset.originalHtml) {
            const carouselInner = carousel.querySelector('.carousel-inner');
            if (carouselInner) {
                // Restore original HTML
                carouselInner.innerHTML = carousel.dataset.originalHtml;
                
                // Reset flag
                carousel.dataset.mobileRestructured = 'false';
                
                // Reinitialize Bootstrap carousel
                if (typeof bootstrap !== 'undefined') {
                    // Dispose existing carousel
                    const existingCarousel = bootstrap.Carousel.getInstance(carousel);
                    if (existingCarousel) {
                        existingCarousel.dispose();
                    }
                    
                    // Recreate carousel with original structure
                    const interval = carousel.dataset.bsInterval || carousel.getAttribute('data-bs-interval') || '8000';
                    const bsCarousel = new bootstrap.Carousel(carousel, {
                        interval: parseInt(interval),
                        wrap: true,
                        touch: true
                    });
                    carousel._carousel = bsCarousel;
                }
            }
        }
    });
}

// DISABLED: Using CSS-only approach for 2 items per slide on mobile
// The CSS handles showing 2 items per slide without restructuring the DOM
// This is more performant and doesn't break existing carousel functionality

// document.addEventListener('DOMContentLoaded', function() {
//     // Check if mobile and enhance carousels
//     if (window.innerWidth <= 767) {
//         enhanceCarouselsForMobile();
//     }
//     
//     // Re-enhance on resize
//     let resizeTimer;
//     window.addEventListener('resize', function() {
//         clearTimeout(resizeTimer);
//         resizeTimer = setTimeout(function() {
//             if (window.innerWidth <= 767) {
//                 enhanceCarouselsForMobile();
//             } else {
//                 restoreDesktopCarousels();
//             }
//         }, 250);
//     });
//     
//     // Listen for dynamically loaded content (lazy-loaded sections)
//     let mutationTimer;
//     const observer = new MutationObserver(function(mutations) {
//         // Debounce to avoid too many calls
//         clearTimeout(mutationTimer);
//         mutationTimer = setTimeout(function() {
//             if (window.innerWidth <= 767) {
//                 enhanceCarouselsForMobile();
//             }
//         }, 500);
//     });
//     
//     observer.observe(document.body, {
//         childList: true,
//         subtree: true
//     });
// });

// Expose function globally so it can be called after AJAX loads
window.enhanceCarouselsForMobile = enhanceCarouselsForMobile;
