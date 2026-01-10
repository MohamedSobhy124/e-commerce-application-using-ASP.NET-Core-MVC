// Responsive Carousel - Adjusts items per slide based on screen size
// Supports both offers and category carousels
(function() {
    'use strict';
    
    // Carousel configurations
    // Note: offersCarousel is now handled server-side like bestSellersCarousel, so it's excluded from JavaScript reorganization
    const carouselConfigs = [
        {
            carouselId: 'categoryCarousel',
            cardSelector: '.category-card',
            indicatorsClass: 'category-carousel-indicators',
            containerId: 'categoryCarouselContainer' // Category carousel needs container for button conversion
        }
    ];
    
    function initResponsiveCarousel(config) {
        // Skip offersCarousel - it's handled server-side like bestSellersCarousel
        if (config.carouselId === 'offersCarousel') {
            return;
        }
        
        const carouselContainer = config.containerId ? document.getElementById(config.containerId) : null;
        const carousel = document.getElementById(config.carouselId);
        if (!carousel) return;
        
        // Check if already converted to Bootstrap carousel
        let carouselInner = carousel.querySelector('.carousel-inner');
        let allCards = [];
        
        // If no carousel-inner, we need to convert the existing structure
        if (!carouselInner) {
            // Extract all cards from the existing structure
            allCards = Array.from(carousel.querySelectorAll(config.cardSelector));
            
            if (allCards.length === 0) {
                return;
            }
            
            // Store original card HTML
            allCards = allCards.map(card => card.cloneNode(true));
            
            // Create Bootstrap carousel structure
            carousel.className = 'carousel slide';
            carousel.setAttribute('data-bs-ride', 'carousel');
            carousel.setAttribute('data-bs-interval', '8000');
            carousel.setAttribute('data-bs-pause', 'hover');
            
            // Clear existing content
            carousel.innerHTML = '';
            
            // Create carousel-inner
            carouselInner = document.createElement('div');
            carouselInner.className = 'carousel-inner';
            carousel.appendChild(carouselInner);
            
            // Update navigation buttons to use Bootstrap carousel (only for category carousel)
            if (carouselContainer) {
                const prevBtn = carouselContainer.querySelector('.carousel-nav.prev');
                const nextBtn = carouselContainer.querySelector('.carousel-nav.next');
                
                if (prevBtn) {
                    prevBtn.setAttribute('type', 'button');
                    prevBtn.setAttribute('data-bs-target', `#${config.carouselId}`);
                    prevBtn.setAttribute('data-bs-slide', 'prev');
                    prevBtn.removeAttribute('onclick');
                    // Preserve existing icon if it exists
                    if (!prevBtn.querySelector('i')) {
                        prevBtn.innerHTML = '<i class="bi bi-chevron-left"></i>';
                    }
                }
                
                if (nextBtn) {
                    nextBtn.setAttribute('type', 'button');
                    nextBtn.setAttribute('data-bs-target', `#${config.carouselId}`);
                    nextBtn.setAttribute('data-bs-slide', 'next');
                    nextBtn.removeAttribute('onclick');
                    // Preserve existing icon if it exists
                    if (!nextBtn.querySelector('i')) {
                        nextBtn.innerHTML = '<i class="bi bi-chevron-right"></i>';
                    }
                }
            }
        } else {
            // Already Bootstrap carousel, extract cards from carousel-inner
            const tempDiv = document.createElement('div');
            tempDiv.innerHTML = carouselInner.innerHTML;
            let extractedCards = Array.from(tempDiv.querySelectorAll(config.cardSelector));
            
            // If no cards found in temp div, try current DOM
            if (extractedCards.length === 0) {
                extractedCards = Array.from(carouselInner.querySelectorAll(config.cardSelector));
            }
            
            // Deduplicate cards by extracting product ID from href (for offer cards)
            const uniqueCards = [];
            const seenIds = new Set();
            
            extractedCards.forEach(card => {
                // Try to get product ID from the card's link
                const link = card.querySelector('a[href*="productId"]');
                if (link) {
                    const href = link.getAttribute('href');
                    const productIdMatch = href.match(/productId[=:](\d+)/);
                    if (productIdMatch) {
                        const productId = productIdMatch[1];
                        if (!seenIds.has(productId)) {
                            seenIds.add(productId);
                            uniqueCards.push(card);
                        }
                    } else {
                        // If no product ID found, use the card itself as identifier
                        uniqueCards.push(card);
                    }
                } else {
                    // If no link found, use the card itself
                    uniqueCards.push(card);
                }
            });
            
            // Store original card HTML (deduplicated)
            allCards = uniqueCards.map(card => card.cloneNode(true));
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
            
            // Always reorganize if items per slide changed, no carousel items exist, or on mobile/tablet
            // Also reorganize on desktop to ensure wrapping works
            const shouldReorganize = newItemsPerSlide !== currentItemsPerSlide || 
                                    !carouselInner.querySelector('.carousel-item') ||
                                    window.innerWidth < 768 ||
                                    (newItemsPerSlide === 4 && window.innerWidth >= 1200); // Force reorganize on desktop
            
            if (!shouldReorganize && carouselInner.querySelector('.carousel-item')) {
                return;
            }
            
            currentItemsPerSlide = newItemsPerSlide;
            
            // Use the original cards array (stored at initialization)
            const cardsToUse = Array.from(allCards);
            
            if (cardsToUse.length === 0) {
                return;
            }
            
            // Clear existing carousel items
            carouselInner.innerHTML = '';
            
            // Group cards into slides with circular wrapping
            // If we have fewer cards than items per slide, we'll create multiple slides by looping
            let totalSlides;
            if (cardsToUse.length < currentItemsPerSlide) {
                // If fewer cards than items per slide, create at least one slide and loop cards to fill it
                totalSlides = 1;
            } else {
                totalSlides = Math.ceil(cardsToUse.length / currentItemsPerSlide);
            }
            
            for (let slideIndex = 0; slideIndex < totalSlides; slideIndex++) {
                let slideCards = [];
                
                // Get cards for this slide using circular wrapping
                for (let i = 0; i < currentItemsPerSlide; i++) {
                    const cardIndex = (slideIndex * currentItemsPerSlide + i) % cardsToUse.length;
                    slideCards.push(cardsToUse[cardIndex]);
                }
                
                // Always ensure we have exactly the right number of items (especially important when cards < itemsPerSlide)
                while (slideCards.length < currentItemsPerSlide && cardsToUse.length > 0) {
                    // Loop through available cards to fill remaining slots
                    const currentLength = slideCards.length;
                    for (let i = 0; i < currentItemsPerSlide - currentLength && i < cardsToUse.length; i++) {
                        slideCards.push(cardsToUse[i % cardsToUse.length]);
                    }
                }
                
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
                // Dispose old instance
                bsCarousel.dispose();
            }
            
            // Initialize carousel with auto-rotation
            // For category carousel, disable Bootstrap's touch to allow vertical scrolling
            const isCategoryCarousel = config.carouselId === 'categoryCarousel';
            const carouselInstance = new bootstrap.Carousel(carousel, {
                interval: 8000,
                ride: 'carousel',
                pause: 'hover',
                wrap: true,
                touch: !isCategoryCarousel // Disable Bootstrap touch for category carousel to allow vertical scrolling
            });
            
            // Add custom touch handling for category carousel that allows vertical scrolling
            if (isCategoryCarousel) {
                let touchStartX = 0;
                let touchStartY = 0;
                let touchEndX = 0;
                let touchEndY = 0;
                let isVerticalScroll = false;
                let touchMoved = false;
                
                // Prevent Bootstrap from interfering with vertical scrolling
                const carouselInner = carousel.querySelector('.carousel-inner');
                if (carouselInner) {
                    // Set CSS to allow vertical scrolling
                    carouselInner.style.touchAction = 'pan-y pan-x';
                }
                
                carousel.addEventListener('touchstart', function(e) {
                    touchStartX = e.touches[0].clientX;
                    touchStartY = e.touches[0].clientY;
                    isVerticalScroll = false;
                    touchMoved = false;
                }, { passive: true });
                
                carousel.addEventListener('touchmove', function(e) {
                    if (!touchStartX || !touchStartY) return;
                    
                    touchMoved = true;
                    touchEndX = e.touches[0].clientX;
                    touchEndY = e.touches[0].clientY;
                    
                    const diffX = Math.abs(touchStartX - touchEndX);
                    const diffY = Math.abs(touchStartY - touchEndY);
                    
                    // If vertical movement is greater than horizontal, it's a scroll - don't interfere
                    // Use a threshold to determine if it's primarily vertical
                    if (diffY > diffX && diffY > 15) {
                        isVerticalScroll = true;
                        // Don't prevent default - allow scrolling
                        return;
                    }
                    
                    // If it's clearly horizontal, allow carousel navigation
                    if (diffX > diffY && diffX > 15) {
                        isVerticalScroll = false;
                    }
                }, { passive: true });
                
                carousel.addEventListener('touchend', function(e) {
                    if (!touchMoved) {
                        // Reset if touch didn't move
                        touchStartX = 0;
                        touchStartY = 0;
                        return;
                    }
                    
                    if (isVerticalScroll) {
                        // Reset for next touch - don't handle as carousel swipe
                        touchStartX = 0;
                        touchStartY = 0;
                        touchEndX = 0;
                        touchEndY = 0;
                        isVerticalScroll = false;
                        touchMoved = false;
                        return;
                    }
                    
                    const diffX = touchStartX - touchEndX;
                    const swipeThreshold = 50;
                    
                    // Only handle horizontal swipes (not vertical scrolls)
                    if (Math.abs(diffX) > swipeThreshold && !isVerticalScroll) {
                        e.preventDefault();
                        e.stopPropagation();
                        
                        if (diffX > 0) {
                            // Swipe left - go to next
                            carouselInstance.next();
                        } else {
                            // Swipe right - go to prev
                            carouselInstance.prev();
                        }
                    }
                    
                    // Reset
                    touchStartX = 0;
                    touchStartY = 0;
                    touchEndX = 0;
                    touchEndY = 0;
                    isVerticalScroll = false;
                    touchMoved = false;
                }, { passive: false });
            }
        }
        
        function updateIndicators(totalSlides) {
            let indicatorsContainer = carousel.querySelector(`.${config.indicatorsClass}`);
            
            if (!indicatorsContainer && totalSlides > 1) {
                // Create indicators container if it doesn't exist
                indicatorsContainer = document.createElement('div');
                indicatorsContainer.className = `carousel-indicators ${config.indicatorsClass}`;
                carousel.appendChild(indicatorsContainer);
            }
            
            if (!indicatorsContainer) return;
            
            // Clear existing indicators
            indicatorsContainer.innerHTML = '';
            
            // Create new indicators
            for (let i = 0; i < totalSlides; i++) {
                const button = document.createElement('button');
                button.type = 'button';
                button.setAttribute('data-bs-target', `#${config.carouselId}`);
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
        const resizeHandler = () => {
            clearTimeout(resizeTimeout);
            resizeTimeout = setTimeout(() => {
                const newItemsPerSlide = getItemsPerSlide();
                if (newItemsPerSlide !== currentItemsPerSlide) {
                    reorganizeSlides();
                }
            }, 250);
        };
        
        // Store resize handler for cleanup if needed
        window.addEventListener('resize', resizeHandler);
    }
    
    // Initialize all carousels
    function initAllCarousels() {
        carouselConfigs.forEach(config => {
            initResponsiveCarousel(config);
        });
    }
    
    // Initialize when DOM is ready
    function initWhenReady() {
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', () => {
                setTimeout(initAllCarousels, 300);
            });
        } else {
            // DOM already loaded, wait a bit more for Bootstrap to initialize
            setTimeout(initAllCarousels, 300);
        }
    }
    
    // Also try on window load as fallback
    window.addEventListener('load', () => {
        setTimeout(initAllCarousels, 500);
    });
    
    initWhenReady();
})();
