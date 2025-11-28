// Responsive Offers Carousel - Adjusts items per slide based on screen size
(function() {
    'use strict';
    
    function initResponsiveOffersCarousel() {
        const carousel = document.getElementById('offersCarousel');
        if (!carousel) return;
        
        const carouselInner = carousel.querySelector('.carousel-inner');
        if (!carouselInner) return;
        
        // Store original HTML structure
        const originalHTML = carouselInner.innerHTML;
        if (!originalHTML) return;
        
        // Extract all offer cards from the original structure
        const tempDiv = document.createElement('div');
        tempDiv.innerHTML = originalHTML;
        
        // Find all offer cards - they are inside column divs (col-12 col-sm-6 col-lg-3)
        // We need to get the actual .offer-card elements, not the column wrappers
        let allCards = Array.from(tempDiv.querySelectorAll('.offer-card'));
        
        // If no cards found in temp div, try current DOM
        if (allCards.length === 0) {
            allCards = Array.from(carouselInner.querySelectorAll('.offer-card'));
        }
        
        if (allCards.length === 0) {
            return;
        }
        
        let currentItemsPerSlide = getItemsPerSlide();
        
        function getItemsPerSlide() {
            const width = window.innerWidth;
            // Mobile (< 576px): 1 product per slide
            if (width < 576) return 1;
            // Small tablet (576-768px): 1 product per slide
            if (width < 768) return 1;
            // Tablet (768-992px): 2 items
            if (width < 992) return 2;
            // Small desktop (992-1200px): 3 items
            if (width < 1200) return 3;
            // Desktop (>= 1200px): 4 items
            return 4;
        }
        
        function reorganizeSlides() {
            const newItemsPerSlide = getItemsPerSlide();
            
            // Always reorganize on mobile/tablet to ensure proper structure
            const shouldReorganize = newItemsPerSlide !== currentItemsPerSlide || 
                                    !carouselInner.querySelector('.carousel-item') ||
                                    window.innerWidth < 768;
            
            if (!shouldReorganize && carouselInner.querySelector('.carousel-item')) {
                return;
            }
            
            currentItemsPerSlide = newItemsPerSlide;
            
            // Use the original cards array (stored at initialization)
            // Don't try to re-extract from DOM as it will be empty after clearing
            const cardsToUse = Array.from(allCards);
            
            if (cardsToUse.length === 0) {
                return;
            }
            
            // Clear existing carousel items
            carouselInner.innerHTML = '';
            
            // Group cards into slides
            const totalSlides = Math.ceil(cardsToUse.length / currentItemsPerSlide);
            
            for (let slideIndex = 0; slideIndex < totalSlides; slideIndex++) {
                const slideCards = cardsToUse.slice(
                    slideIndex * currentItemsPerSlide,
                    (slideIndex + 1) * currentItemsPerSlide
                );
                
                if (slideCards.length === 0) continue;
                
                // Create carousel item
                const carouselItem = document.createElement('div');
                carouselItem.className = 'carousel-item' + (slideIndex === 0 ? ' active' : '');
                
                // Create row
                const row = document.createElement('div');
                row.className = 'row g-3 g-md-4';
                
                // Add cards to row with appropriate column classes
                slideCards.forEach((card, cardIndex) => {
                    const cardClone = card.cloneNode(true);
                    
                    // Create a column wrapper div
                    const colWrapper = document.createElement('div');
                    
                    // Add responsive column classes based on items per slide
                    // Force 1 item per slide for mobile (< 576px) and small tablet (576-768px)
                    if (currentItemsPerSlide === 1) {
                        // Mobile and small tablet: 1 item per slide - full width
                        colWrapper.className = 'col-12';
                    } else if (currentItemsPerSlide === 2) {
                        // Tablet: 2 items per slide
                        colWrapper.className = 'col-12 col-md-6';
                    } else if (currentItemsPerSlide === 3) {
                        // Small desktop: 3 items per slide
                        colWrapper.className = 'col-12 col-md-6 col-lg-4';
                    } else {
                        // Desktop: 4 items per slide
                        colWrapper.className = 'col-12 col-md-6 col-lg-3';
                    }
                    
                    // Append the cloned card to the column wrapper
                    colWrapper.appendChild(cardClone);
                    
                    // Append the column wrapper to the row
                    row.appendChild(colWrapper);
                });
                
                carouselItem.appendChild(row);
                carouselInner.appendChild(carouselItem);
            }
            
            // Update indicators
            updateIndicators(totalSlides);
            
            // Reinitialize Bootstrap carousel if needed
            const bsCarousel = bootstrap.Carousel.getInstance(carousel);
            if (bsCarousel) {
                // Reset to first slide
                bsCarousel.to(0);
            } else {
                // Initialize carousel if it doesn't exist
                new bootstrap.Carousel(carousel, {
                    interval: 5000,
                    ride: 'carousel',
                    pause: 'hover'
                });
            }
        }
        
        function updateIndicators(totalSlides) {
            let indicatorsContainer = carousel.querySelector('.offers-carousel-indicators');
            
            if (!indicatorsContainer && totalSlides > 1) {
                // Create indicators container if it doesn't exist
                indicatorsContainer = document.createElement('div');
                indicatorsContainer.className = 'carousel-indicators offers-carousel-indicators';
                carousel.appendChild(indicatorsContainer);
            }
            
            if (!indicatorsContainer) return;
            
            // Clear existing indicators
            indicatorsContainer.innerHTML = '';
            
            // Create new indicators
            for (let i = 0; i < totalSlides; i++) {
                const button = document.createElement('button');
                button.type = 'button';
                button.setAttribute('data-bs-target', '#offersCarousel');
                button.setAttribute('data-bs-slide-to', i.toString());
                button.setAttribute('aria-label', `Slide ${i + 1}`);
                if (i === 0) {
                    button.className = 'active';
                    button.setAttribute('aria-current', 'true');
                }
                indicatorsContainer.appendChild(button);
            }
        }
        
        // Initial organization - reorganize immediately
        reorganizeSlides();
        
        // Reorganize on resize with debounce
        let resizeTimeout;
        window.addEventListener('resize', () => {
            clearTimeout(resizeTimeout);
            resizeTimeout = setTimeout(() => {
                const newItemsPerSlide = getItemsPerSlide();
                if (newItemsPerSlide !== currentItemsPerSlide) {
                    reorganizeSlides();
                }
            }, 250);
        });
    }
    
    // Initialize when DOM is ready
    function initWhenReady() {
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', () => {
                setTimeout(initResponsiveOffersCarousel, 300);
            });
        } else {
            // DOM already loaded, wait a bit more for Bootstrap to initialize
            setTimeout(initResponsiveOffersCarousel, 300);
        }
    }
    
    // Also try on window load as fallback
    window.addEventListener('load', () => {
        setTimeout(initResponsiveOffersCarousel, 500);
    });
    
    initWhenReady();
})();

