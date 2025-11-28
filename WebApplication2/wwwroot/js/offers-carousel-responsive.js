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
        const allCards = tempDiv.querySelectorAll('.offer-card');
        
        if (allCards.length === 0) return;
        
        let currentItemsPerSlide = getItemsPerSlide();
        
        function getItemsPerSlide() {
            const width = window.innerWidth;
            if (width < 576) return 1;      // Mobile: 1 item
            if (width < 768) return 1;      // Small tablet: 1 item  
            if (width < 992) return 2;      // Tablet: 2 items
            if (width < 1200) return 3;     // Small desktop: 3 items
            return 4;                       // Desktop: 4 items
        }
        
        function reorganizeSlides() {
            const newItemsPerSlide = getItemsPerSlide();
            
            // Only reorganize if items per slide changed
            if (newItemsPerSlide === currentItemsPerSlide && carouselInner.querySelector('.carousel-item')) {
                return;
            }
            
            currentItemsPerSlide = newItemsPerSlide;
            
            // Clear existing carousel items
            carouselInner.innerHTML = '';
            
            // Group cards into slides
            const cardsArray = Array.from(allCards);
            const totalSlides = Math.ceil(cardsArray.length / currentItemsPerSlide);
            
            for (let slideIndex = 0; slideIndex < totalSlides; slideIndex++) {
                const slideCards = cardsArray.slice(
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
                slideCards.forEach((card) => {
                    const cardClone = card.cloneNode(true);
                    
                    // Remove existing column classes
                    cardClone.className = cardClone.className.replace(/col-\w+-\d+/g, '').trim();
                    
                    // Add responsive column classes based on items per slide
                    if (currentItemsPerSlide === 1) {
                        cardClone.className += ' col-12';
                    } else if (currentItemsPerSlide === 2) {
                        cardClone.className += ' col-12 col-md-6';
                    } else if (currentItemsPerSlide === 3) {
                        cardClone.className += ' col-12 col-md-6 col-lg-4';
                    } else {
                        cardClone.className += ' col-12 col-sm-6 col-lg-3';
                    }
                    
                    row.appendChild(cardClone);
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
        
        // Initial organization
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
            document.addEventListener('DOMContentLoaded', initResponsiveOffersCarousel);
        } else {
            setTimeout(initResponsiveOffersCarousel, 100);
        }
    }
    
    initWhenReady();
})();

