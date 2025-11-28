// Category Carousel - Enhanced Smooth Scrolling
(function() {
    'use strict';
    
    let isScrolling = false;
    let scrollTimeout;
    
    // Enhanced scroll function - scroll 4 items at a time
    window.scrollCategoryCarousel = function(direction) {
        const carousel = document.getElementById('categoryCarousel');
        if (!carousel || isScrolling) return;
        
        isScrolling = true;
        // Get card width dynamically
        const firstCard = carousel.querySelector('.category-card');
        const cardWidth = firstCard ? firstCard.offsetWidth : 220;
        const gap = window.innerWidth <= 768 ? 16 : 20; // Adjust gap for mobile
        const itemsPerScroll = 4; // Scroll 4 items at a time
        const scrollAmount = (cardWidth + gap) * itemsPerScroll;
        
        // Calculate current scroll position
        const currentScroll = carousel.scrollLeft;
        const targetScroll = currentScroll + (direction * scrollAmount);
        
        // Smooth scroll with easing
        const startTime = performance.now();
        const duration = 500; // milliseconds
        
        function animateScroll(currentTime) {
            const elapsed = currentTime - startTime;
            const progress = Math.min(elapsed / duration, 1);
            
            // Easing function (ease-out cubic)
            const easeOut = 1 - Math.pow(1 - progress, 3);
            
            carousel.scrollLeft = currentScroll + (direction * scrollAmount * easeOut);
            
            if (progress < 1) {
                requestAnimationFrame(animateScroll);
            } else {
                isScrolling = false;
                updateCarouselScrollIndicators();
                updateCarouselDots();
            }
        }
        
        requestAnimationFrame(animateScroll);
        
        // Update indicators after animation
        setTimeout(() => {
            updateCarouselScrollIndicators();
            updateCarouselDots();
        }, duration + 50);
    };
    
    function updateCarouselScrollIndicators() {
        const carousel = document.getElementById('categoryCarousel');
        const container = document.getElementById('categoryCarouselContainer');
        const prevBtn = container?.querySelector('.carousel-nav.prev');
        const nextBtn = container?.querySelector('.carousel-nav.next');
        
        if (!carousel || !container) return;
        
        const isScrollable = carousel.scrollWidth > carousel.clientWidth;
        const isAtStart = carousel.scrollLeft <= 10;
        const isAtEnd = carousel.scrollLeft >= carousel.scrollWidth - carousel.clientWidth - 10;
        
        if (!isScrollable) {
            if (prevBtn) prevBtn.style.display = 'none';
            if (nextBtn) nextBtn.style.display = 'none';
            container.classList.add('scroll-start');
            container.classList.add('scroll-end');
            return;
        }
        
        if (prevBtn) {
            prevBtn.style.display = 'flex';
            prevBtn.disabled = isAtStart;
        }
        
        if (nextBtn) {
            nextBtn.style.display = 'flex';
            nextBtn.disabled = isAtEnd;
        }
        
        // Update container classes for gradient indicators
        container.classList.toggle('scroll-start', isAtStart);
        container.classList.toggle('scroll-end', isAtEnd);
    }
    
    // Update carousel dot indicators
    function updateCarouselDots() {
        const carousel = document.getElementById('categoryCarousel');
        const indicators = document.querySelectorAll('.carousel-indicator');
        
        if (!carousel || indicators.length === 0) return;
        
        const cardWidth = carousel.querySelector('.category-card')?.offsetWidth || 220;
        const gap = window.innerWidth <= 768 ? 16 : 20; // Adjust gap for mobile
        const itemsPerPage = 4; // Show 4 items per view
        const scrollAmount = (cardWidth + gap) * itemsPerPage;
        
        // Calculate which page we're on
        const currentIndex = Math.round(carousel.scrollLeft / scrollAmount);
        const maxIndex = indicators.length - 1;
        const activeIndex = Math.min(Math.max(0, currentIndex), maxIndex);
        
        indicators.forEach((indicator, index) => {
            if (index === activeIndex) {
                indicator.classList.add('active');
            } else {
                indicator.classList.remove('active');
            }
        });
    }
    
    // Create carousel indicators
    function createCarouselIndicators() {
        const container = document.getElementById('categoryCarouselContainer');
        const carousel = document.getElementById('categoryCarousel');
        
        if (!container || !carousel) return;
        
        const cards = carousel.querySelectorAll('.category-card');
        if (cards.length === 0) return;
        
        const cardWidth = cards[0].offsetWidth || 220;
        const gap = window.innerWidth <= 768 ? 16 : 20; // Adjust gap for mobile
        const itemsPerPage = 4;
        const totalPages = Math.max(1, Math.ceil(cards.length / itemsPerPage));
        
        // Remove existing indicators
        const existingIndicators = container.querySelector('.carousel-indicators');
        if (existingIndicators) {
            existingIndicators.remove();
        }
        
        // Create indicators container
        const indicatorsContainer = document.createElement('div');
        indicatorsContainer.className = 'carousel-indicators';
        
        for (let i = 0; i < totalPages; i++) {
            const indicator = document.createElement('button');
            indicator.className = 'carousel-indicator';
            indicator.setAttribute('aria-label', `Go to page ${i + 1}`);
            indicator.setAttribute('data-page', i);
            
            indicator.addEventListener('click', function() {
                const page = parseInt(this.getAttribute('data-page'));
                const scrollAmount = (cardWidth + gap) * itemsPerPage * page;
                carousel.scrollTo({
                    left: scrollAmount,
                    behavior: 'smooth'
                });
            });
            
            if (i === 0) {
                indicator.classList.add('active');
            }
            
            indicatorsContainer.appendChild(indicator);
        }
        
        container.appendChild(indicatorsContainer);
    }
    
    // Initialize carousel on page load
    function initCategoryCarousel() {
        const carousel = document.getElementById('categoryCarousel');
        const container = document.getElementById('categoryCarouselContainer');
        
        if (!carousel || !container) return;
        
        // Update indicators on scroll (with debounce)
        carousel.addEventListener('scroll', function() {
            clearTimeout(scrollTimeout);
            scrollTimeout = setTimeout(() => {
                updateCarouselScrollIndicators();
                updateCarouselDots();
            }, 100);
        }, { passive: true });
        
        // Create and initialize indicators
        createCarouselIndicators();
        
        // Initial update
        updateCarouselScrollIndicators();
        updateCarouselDots();
        
        // Update on window resize (with debounce)
        let resizeTimeout;
        window.addEventListener('resize', function() {
            clearTimeout(resizeTimeout);
            resizeTimeout = setTimeout(() => {
                createCarouselIndicators(); // Recreate indicators after resize
                updateCarouselScrollIndicators();
                updateCarouselDots();
            }, 250);
        });
        
        // Enhanced touch/swipe support for mobile
        let isDown = false;
        let startX;
        let scrollLeft;
        let velocity = 0;
        let lastX = 0;
        let lastTime = 0;
        
        carousel.addEventListener('mousedown', (e) => {
            isDown = true;
            carousel.style.cursor = 'grabbing';
            startX = e.pageX - carousel.offsetLeft;
            scrollLeft = carousel.scrollLeft;
            lastX = e.pageX;
            lastTime = Date.now();
            velocity = 0;
            e.preventDefault();
        });
        
        carousel.addEventListener('mouseleave', () => {
            if (isDown) {
                // Apply momentum scrolling
                if (Math.abs(velocity) > 0.5) {
                    carousel.scrollBy({
                        left: velocity * 10,
                        behavior: 'smooth'
                    });
                }
            }
            isDown = false;
            carousel.style.cursor = 'grab';
        });
        
        carousel.addEventListener('mouseup', () => {
            if (isDown) {
                // Apply momentum scrolling
                if (Math.abs(velocity) > 0.5) {
                    carousel.scrollBy({
                        left: velocity * 10,
                        behavior: 'smooth'
                    });
                }
            }
            isDown = false;
            carousel.style.cursor = 'grab';
        });
        
        carousel.addEventListener('mousemove', (e) => {
            if (!isDown) return;
            e.preventDefault();
            
            const x = e.pageX - carousel.offsetLeft;
            const walk = (x - startX) * 2; // Scroll speed multiplier
            carousel.scrollLeft = scrollLeft - walk;
            
            // Calculate velocity for momentum
            const currentTime = Date.now();
            const timeDelta = currentTime - lastTime;
            if (timeDelta > 0) {
                const xDelta = e.pageX - lastX;
                velocity = xDelta / timeDelta;
            }
            lastX = e.pageX;
            lastTime = currentTime;
        });
        
        // Touch events for mobile
        let touchStartX = 0;
        let touchScrollLeft = 0;
        
        carousel.addEventListener('touchstart', (e) => {
            touchStartX = e.touches[0].pageX - carousel.offsetLeft;
            touchScrollLeft = carousel.scrollLeft;
        }, { passive: true });
        
        carousel.addEventListener('touchmove', (e) => {
            if (!touchStartX) return;
            const x = e.touches[0].pageX - carousel.offsetLeft;
            const walk = (x - touchStartX) * 2;
            carousel.scrollLeft = touchScrollLeft - walk;
        }, { passive: true });
        
        carousel.addEventListener('touchend', () => {
            touchStartX = 0;
        }, { passive: true });
        
        // Keyboard navigation
        carousel.addEventListener('keydown', (e) => {
            if (document.activeElement !== carousel) return;
            
            if (e.key === 'ArrowLeft') {
                e.preventDefault();
                scrollCategoryCarousel(-1);
            } else if (e.key === 'ArrowRight') {
                e.preventDefault();
                scrollCategoryCarousel(1);
            }
        });
        
        // Make carousel focusable for keyboard navigation
        carousel.setAttribute('tabindex', '0');
        carousel.setAttribute('role', 'region');
        carousel.setAttribute('aria-label', 'Category carousel');
    }
    
    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initCategoryCarousel);
    } else {
        initCategoryCarousel();
    }
    
    // Re-initialize if carousel is added dynamically
    const observer = new MutationObserver(function(mutations) {
        mutations.forEach(function(mutation) {
            if (mutation.addedNodes.length) {
                const carousel = document.getElementById('categoryCarousel');
                if (carousel && !carousel.hasAttribute('data-initialized')) {
                    carousel.setAttribute('data-initialized', 'true');
                    initCategoryCarousel();
                }
            }
        });
    });
    
    observer.observe(document.body, {
        childList: true,
        subtree: true
    });
})();
