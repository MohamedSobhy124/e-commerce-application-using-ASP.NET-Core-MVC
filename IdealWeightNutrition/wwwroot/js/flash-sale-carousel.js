// Flash Sale Carousel - Auto-swapping functionality
(function() {
    'use strict';
    
    let currentSlide = 0;
    let autoSwapInterval = null;
    let isAutoSwapping = true;
    const swapInterval = 8000; // 8 seconds - slower for better UX
    
    // Helper functions
    function updateIndicators() {
        const carousel = document.getElementById('flashSaleCarousel');
        const indicators = document.querySelectorAll('.flash-sale-indicator');
        
        if (!carousel || indicators.length === 0) return;
        
        const slides = carousel.querySelectorAll('.flash-sale-slide');
        if (slides.length === 0) return;
        
        // Calculate current slide index based on scroll position
        const containerWidth = carousel.offsetWidth;
        if (containerWidth === 0) return;
        const currentIndex = Math.round(carousel.scrollLeft / containerWidth);
        
        indicators.forEach((indicator, index) => {
            if (index === currentIndex) {
                indicator.classList.add('active');
            } else {
                indicator.classList.remove('active');
            }
        });
        
        currentSlide = Math.max(0, Math.min(currentIndex, slides.length - 1));
    }
    
    function pauseAutoSwap() {
        isAutoSwapping = false;
    }
    
    function resumeAutoSwap() {
        isAutoSwapping = true;
    }
    
    function resetAutoSwap() {
        if (autoSwapInterval) {
            clearInterval(autoSwapInterval);
            autoSwapInterval = null;
        }
        isAutoSwapping = true;
        startAutoSwap();
    }
    
    // Auto-swap functionality
    function startAutoSwap() {
        if (autoSwapInterval) {
            clearInterval(autoSwapInterval);
            autoSwapInterval = null;
        }
        
        const carousel = document.getElementById('flashSaleCarousel');
        if (!carousel) return;
        
        const slides = carousel.querySelectorAll('.flash-sale-slide');
        if (slides.length <= 1) {
            return;
        }
        
        autoSwapInterval = setInterval(() => {
            if (!isAutoSwapping) return;
            
            const carousel = document.getElementById('flashSaleCarousel');
            if (!carousel) {
                clearInterval(autoSwapInterval);
                autoSwapInterval = null;
                return;
            }
            
            const slides = carousel.querySelectorAll('.flash-sale-slide');
            if (slides.length <= 1) {
                clearInterval(autoSwapInterval);
                autoSwapInterval = null;
                return;
            }
            
            const containerWidth = carousel.offsetWidth;
            if (containerWidth === 0) return;
            
            // Get current slide from scroll position
            const currentIndex = Math.round(carousel.scrollLeft / containerWidth);
            
            // Move to next slide
            const nextSlide = (currentIndex + 1) % slides.length;
            currentSlide = nextSlide;
            
            
            // Use the actual slide element's offsetLeft for reliable scrolling
            const targetSlide = slides[nextSlide];
            if (targetSlide) {
                // offsetLeft gives us the position relative to the carousel container
                const scrollPosition = targetSlide.offsetLeft;
                
                
                carousel.scrollTo({
                    left: scrollPosition,
                    behavior: 'smooth'
                });
                
                // Verify scroll happened
                setTimeout(() => {
                    updateIndicators();
                }, 600);
            } else {
                // Fallback to simple calculation
                const scrollAmount = containerWidth * nextSlide;
                carousel.scrollTo({
                    left: scrollAmount,
                    behavior: 'smooth'
                });
                setTimeout(() => {
                    updateIndicators();
                }, 600);
            }
        }, swapInterval);
        
    }
    
    // Expose functions globally
    window.scrollFlashSaleCarousel = function(direction) {
        const carousel = document.getElementById('flashSaleCarousel');
        if (!carousel) {
            return;
        }
        
        const slides = carousel.querySelectorAll('.flash-sale-slide');
        if (slides.length === 0) {
            return;
        }
        
        // Get current slide from actual scroll position
        const containerWidth = carousel.offsetWidth;
        const currentIndex = Math.round(carousel.scrollLeft / containerWidth);
        
        
        // Calculate next slide index
        let nextIndex = currentIndex + direction;
        
        if (nextIndex < 0) {
            nextIndex = slides.length - 1;
        } else if (nextIndex >= slides.length) {
            nextIndex = 0;
        }
        
        currentSlide = nextIndex;
        
        // Use the slide's actual offsetLeft for reliable scrolling
        const targetSlide = slides[nextIndex];
        if (targetSlide) {
            const scrollPosition = targetSlide.offsetLeft;
            
            
            carousel.scrollTo({
                left: scrollPosition,
                behavior: 'smooth'
            });
            
            setTimeout(() => {
                updateIndicators();
            }, 300);
            
            resetAutoSwap();
        } else {
        }
    };
    
    window.goToFlashSaleSlide = function(index) {
        const carousel = document.getElementById('flashSaleCarousel');
        if (!carousel) {
            return;
        }
        
        const slides = carousel.querySelectorAll('.flash-sale-slide');
        if (!slides[index]) {
            return;
        }
        
        currentSlide = index;
        
        
        // Use the slide's actual offsetLeft for reliable scrolling
        const targetSlide = slides[index];
        if (targetSlide) {
            const scrollPosition = targetSlide.offsetLeft;
            
            
            carousel.scrollTo({
                left: scrollPosition,
                behavior: 'smooth'
            });
            
            setTimeout(() => {
                updateIndicators();
            }, 300);
            
            resetAutoSwap();
        }
    };
    
    window.openFlashSaleProducts = function(flashSaleId) {
        const modal = document.getElementById('flashSaleProductsModal');
        if (!modal) {
            return;
        }
        
        modal.classList.add('active');
        document.body.style.overflow = 'hidden';
        
        loadFlashSaleProducts(flashSaleId);
    };
    
    window.closeFlashSaleProducts = function() {
        const modal = document.getElementById('flashSaleProductsModal');
        if (!modal) return;
        
        modal.classList.remove('active');
        document.body.style.overflow = '';
    };
    
    function loadFlashSaleProducts(flashSaleId) {
        const container = document.getElementById('flashSaleProductsContainer');
        const titleEl = document.getElementById('modalFlashSaleTitle');
        const timerEl = document.getElementById('modalFlashSaleTimer');
        
        if (!container) return;
        
        // Show loading
        container.innerHTML = `
            <div class="text-center py-5">
                <div class="spinner-border text-primary" role="status">
                    <span class="visually-hidden">Loading...</span>
                </div>
            </div>
        `;
        
        // Get flash sale info from clicked slide
        const slide = document.querySelector(`[data-flash-sale-id="${flashSaleId}"]`);
        const endDate = slide ? slide.getAttribute('data-flash-sale-end') : '';
        
        // Get localized flash sale name - prioritize data attribute, then fallback to title text
        let flashSaleName = 'Flash Sale';
        if (slide) {
            // First try to get from data-flash-sale-name attribute (already localized in view)
            flashSaleName = slide.getAttribute('data-flash-sale-name');
            
            // Fallback to title element text if data attribute is missing
            if (!flashSaleName || flashSaleName.trim() === '') {
                const titleElement = slide.querySelector('.flash-sale-slide-title');
                flashSaleName = titleElement ? titleElement.textContent.trim() : 'Flash Sale';
            }
        }
        
        // Set title and timer
        if (titleEl) titleEl.textContent = flashSaleName || 'Flash Sale';
        if (timerEl && endDate) {
            timerEl.setAttribute('data-flash-sale-end', endDate);
            updateModalTimer(timerEl);
        }
        
        // Fetch flash sale products partial view
        fetch(`/Customer/FlashSale/GetFlashSaleProducts/${flashSaleId}`)
            .then(response => {
                if (!response.ok) {
                    return response.text().then(text => {
                        throw new Error(text || 'Failed to load flash sale products');
                    });
                }
                return response.text();
            })
            .then(html => {
                container.innerHTML = html;
                
                // Update stock progress bars
                setTimeout(() => {
                    document.querySelectorAll('[data-stock-percentage]').forEach(bar => {
                        const percentage = parseFloat(bar.getAttribute('data-stock-percentage'));
                        const fillElement = bar.querySelector('.stock-progress-bar-fill');
                        if (fillElement) {
                            fillElement.style.width = percentage + '%';
                            
                            if (percentage < 20) {
                                fillElement.style.background = 'linear-gradient(90deg, #e74c3c, #c0392b)';
                            } else if (percentage < 50) {
                                fillElement.style.background = 'linear-gradient(90deg, #f39c12, #e67e22)';
                            }
                        }
                    });
                }, 100);
            })
            .catch(error => {
                container.innerHTML = `
                    <div class="alert alert-danger">
                        <i class="bi bi-exclamation-triangle me-2"></i>
                        ${error.message || 'Failed to load flash sale products. Please try again.'}
                    </div>
                    <div class="text-center mt-3">
                        <a href="/Customer/FlashSale/Details/${flashSaleId}" class="btn btn-primary">
                            View Flash Sale Details
                        </a>
                    </div>
                `;
            });
    }
    
    function updateModalTimer(timerEl) {
        const endTimeStr = timerEl.getAttribute('data-flash-sale-end');
        if (!endTimeStr) return;
        
        const endTime = new Date(endTimeStr);
        
        function update() {
            const now = new Date();
            const diff = endTime - now;
            
            if (diff <= 0) {
                timerEl.innerHTML = '<div class="timer-segment"><span class="timer-number">00</span><span class="timer-label">Ended</span></div>';
                return;
            }
            
            const days = Math.floor(diff / (1000 * 60 * 60 * 24));
            const hours = Math.floor((diff % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
            const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));
            const seconds = Math.floor((diff % (1000 * 60)) / 1000);
            
            timerEl.innerHTML = `
                ${days > 0 ? `<div class="timer-segment"><span class="timer-number">${String(days).padStart(2, '0')}</span><span class="timer-label">Days</span></div>` : ''}
                <div class="timer-segment"><span class="timer-number">${String(hours).padStart(2, '0')}</span><span class="timer-label">Hours</span></div>
                <div class="timer-segment"><span class="timer-number">${String(minutes).padStart(2, '0')}</span><span class="timer-label">Mins</span></div>
                <div class="timer-segment"><span class="timer-number">${String(seconds).padStart(2, '0')}</span><span class="timer-label">Secs</span></div>
            `;
        }
        
        update();
        setInterval(update, 1000);
    }
    
    // Initialize timers for all slides
    function initializeTimers() {
        const timerElements = document.querySelectorAll('.flash-sale-slide-timer');
        
        timerElements.forEach(timerEl => {
            const endTimeStr = timerEl.getAttribute('data-flash-sale-end');
            if (!endTimeStr) return;
            
            const endTime = new Date(endTimeStr);
            
            function updateTimer() {
                const now = new Date();
                const diff = endTime - now;
                
                if (diff <= 0) {
                    timerEl.innerHTML = '<span class="timer-number">00</span><span class="timer-label">Ended</span>';
                    return;
                }
                
                const days = Math.floor(diff / (1000 * 60 * 60 * 24));
                const hours = Math.floor((diff % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
                const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));
                const seconds = Math.floor((diff % (1000 * 60)) / 1000);
                
                timerEl.innerHTML = `
                    ${days > 0 ? `<div class="timer-segment"><span class="timer-number">${String(days).padStart(2, '0')}</span><span class="timer-label">Days</span></div>` : ''}
                    <div class="timer-segment"><span class="timer-number">${String(hours).padStart(2, '0')}</span><span class="timer-label">Hours</span></div>
                    <div class="timer-segment"><span class="timer-number">${String(minutes).padStart(2, '0')}</span><span class="timer-label">Mins</span></div>
                    <div class="timer-segment"><span class="timer-number">${String(seconds).padStart(2, '0')}</span><span class="timer-label">Secs</span></div>
                `;
            }
            
            updateTimer();
            setInterval(updateTimer, 1000);
        });
    }
    
    // Initialize carousel
    function initFlashSaleCarousel() {
        const carousel = document.getElementById('flashSaleCarousel');
        if (!carousel) {
            return;
        }
        
        // Prevent double initialization
        if (carousel.hasAttribute('data-initialized')) {
            return;
        }
        carousel.setAttribute('data-initialized', 'true');
        
        const slides = carousel.querySelectorAll('.flash-sale-slide');
        if (slides.length === 0) {
            return;
        }
        
        // Stop any existing auto-swap
        if (autoSwapInterval) {
            clearInterval(autoSwapInterval);
            autoSwapInterval = null;
        }
        
        // Set initial slide position
        currentSlide = 0;
        carousel.scrollLeft = 0;
        isAutoSwapping = true;
        
        
        // Initialize timers for all slides
        initializeTimers();
        
        // Start auto-swap
        setTimeout(() => {
            startAutoSwap();
        }, 1000);
        
        // Pause auto-swap on hover
        carousel.addEventListener('mouseenter', pauseAutoSwap);
        carousel.addEventListener('mouseleave', resumeAutoSwap);
        
        // Update indicators on scroll
        let scrollTimeout;
        carousel.addEventListener('scroll', () => {
            clearTimeout(scrollTimeout);
            scrollTimeout = setTimeout(() => {
                updateIndicators();
            }, 100);
        }, { passive: true });
        
        // Simple touch handling - let CSS handle scrolling, just pause auto-swap on interaction
        carousel.addEventListener('touchstart', () => {
            pauseAutoSwap(); // Pause auto-swap when user starts touching
        }, { passive: true });
        
        carousel.addEventListener('touchend', () => {
            // Resume auto-swap after a delay when user finishes touching
            setTimeout(() => {
                resumeAutoSwap();
            }, 2000);
        }, { passive: true });
        
        // Initial indicator update
        updateIndicators();
        
    }
    
    // Global event delegation for carousel (set up once, works for dynamically added content)
    document.addEventListener('click', function(e) {
        const target = e.target.closest('[data-action]');
        if (!target) return;
        
        const action = target.getAttribute('data-action');
        
        if (action === 'prev-slide') {
            e.preventDefault();
            e.stopPropagation();
            if (typeof window.scrollFlashSaleCarousel === 'function') {
                window.scrollFlashSaleCarousel(-1);
            }
        } else if (action === 'next-slide') {
            e.preventDefault();
            e.stopPropagation();
            if (typeof window.scrollFlashSaleCarousel === 'function') {
                window.scrollFlashSaleCarousel(1);
            }
        } else if (action === 'go-to-slide') {
            e.preventDefault();
            e.stopPropagation();
            const index = parseInt(target.getAttribute('data-slide-index'));
            if (!isNaN(index) && typeof window.goToFlashSaleSlide === 'function') {
                window.goToFlashSaleSlide(index);
            }
        } else if (action === 'open-flash-sale') {
            e.preventDefault();
            e.stopPropagation();
            const flashSaleId = parseInt(target.getAttribute('data-flash-sale-id'));
            if (!isNaN(flashSaleId) && typeof window.openFlashSaleProducts === 'function') {
                window.openFlashSaleProducts(flashSaleId);
            }
        }
    });
    
    // Initialize on page load
    function initializeCarouselWhenReady() {
        const carousel = document.getElementById('flashSaleCarousel');
        if (carousel) {
            initFlashSaleCarousel();
        } else {
            setTimeout(initializeCarouselWhenReady, 500);
        }
    }
    
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initializeCarouselWhenReady);
    } else {
        setTimeout(initializeCarouselWhenReady, 100);
    }
    
    // Close modal on escape key
    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape') {
            if (typeof window.closeFlashSaleProducts === 'function') {
                window.closeFlashSaleProducts();
            }
        }
    });
    
})();
