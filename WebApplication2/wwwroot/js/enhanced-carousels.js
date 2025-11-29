/**
 * Enhanced Carousel Functionality
 * On mobile: Each product becomes its own carousel-item for smooth one-by-one sliding
 * On desktop: Keeps original structure with multiple products per slide
 */

// Function to enhance carousels (can be called multiple times)
function enhanceCarouselsForMobile() {
    const carousels = document.querySelectorAll('.product-carousel:not([data-mobile-restructured="true"])');
    
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
        
        // Create new carousel structure with one product per slide
        const newCarouselItems = [];
        allProducts.forEach(function(productCol, index) {
            // Get the product card element
            const productCard = productCol.querySelector('.product-card-simple, .offer-card');
            if (!productCard) return;
            
            // Clone the entire column structure to preserve all attributes and classes
            const newCol = productCol.cloneNode(true);
            
            // Create new carousel-item
            const newCarouselItem = document.createElement('div');
            newCarouselItem.className = 'carousel-item' + (index === 0 ? ' active' : '');
            
            // Create row wrapper
            const row = document.createElement('div');
            row.className = 'row g-3';
            
            // Make the column full width
            newCol.className = 'col-12';
            
            // Add column to row
            row.appendChild(newCol);
            newCarouselItem.appendChild(row);
            newCarouselItems.push(newCarouselItem);
        });
        
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
            
            // Create new carousel instance
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
    const carousels = document.querySelectorAll('.product-carousel[data-mobile-restructured="true"]');
    
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

document.addEventListener('DOMContentLoaded', function() {
    // Check if mobile and enhance carousels
    if (window.innerWidth <= 767) {
        enhanceCarouselsForMobile();
    }
    
    // Re-enhance on resize
    let resizeTimer;
    window.addEventListener('resize', function() {
        clearTimeout(resizeTimer);
        resizeTimer = setTimeout(function() {
            if (window.innerWidth <= 767) {
                enhanceCarouselsForMobile();
            } else {
                restoreDesktopCarousels();
            }
        }, 250);
    });
    
    // Listen for dynamically loaded content (lazy-loaded sections)
    let mutationTimer;
    const observer = new MutationObserver(function(mutations) {
        // Debounce to avoid too many calls
        clearTimeout(mutationTimer);
        mutationTimer = setTimeout(function() {
            if (window.innerWidth <= 767) {
                enhanceCarouselsForMobile();
            }
        }, 500);
    });
    
    observer.observe(document.body, {
        childList: true,
        subtree: true
    });
});

// Expose function globally so it can be called after AJAX loads
window.enhanceCarouselsForMobile = enhanceCarouselsForMobile;
