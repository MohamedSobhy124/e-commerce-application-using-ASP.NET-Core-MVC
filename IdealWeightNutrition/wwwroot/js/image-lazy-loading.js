/**
 * Image Lazy Loading Implementation for SEO & Performance
 * Improves Core Web Vitals (LCP, CLS) and Google Page Speed Score
 */

(function() {
    'use strict';

    // Feature detection for native lazy loading
    const supportsNativeLazyLoading = 'loading' in HTMLImageElement.prototype;

    // Intersection Observer configuration
    const imageObserverConfig = {
        root: null,
        rootMargin: '50px 0px', // Start loading 50px before entering viewport
        threshold: 0.01
    };

    // Lazy load images using Intersection Observer
    function lazyLoadWithIntersectionObserver() {
        const lazyImages = document.querySelectorAll('img[data-src], img[data-srcset]');
        
        if (lazyImages.length === 0) return;

        const imageObserver = new IntersectionObserver((entries, observer) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const img = entry.target;
                    loadImage(img);
                    observer.unobserve(img);
                }
            });
        }, imageObserverConfig);

        lazyImages.forEach(img => imageObserver.observe(img));
    }

    // Load individual image
    function loadImage(img) {
        // Load srcset first (for responsive images)
        if (img.dataset.srcset) {
            img.srcset = img.dataset.srcset;
        }
        
        // Load src
        if (img.dataset.src) {
            img.src = img.dataset.src;
        }
        
        // Remove data attributes to prevent reloading
        delete img.dataset.src;
        delete img.dataset.srcset;
        
        // Add loaded class for CSS transitions
        img.classList.add('lazy-loaded');
        
        // Remove loading placeholder
        img.classList.remove('lazy-loading');
    }

    // Fallback for browsers that don't support Intersection Observer
    function lazyLoadFallback() {
        const lazyImages = document.querySelectorAll('img[data-src], img[data-srcset]');
        
        function loadVisibleImages() {
            lazyImages.forEach(img => {
                if (isInViewport(img)) {
                    loadImage(img);
                }
            });
        }
        
        function isInViewport(element) {
            const rect = element.getBoundingClientRect();
            return (
                rect.top >= 0 &&
                rect.left >= 0 &&
                rect.bottom <= (window.innerHeight || document.documentElement.clientHeight) + 200 &&
                rect.right <= (window.innerWidth || document.documentElement.clientWidth)
            );
        }
        
        // Load visible images on scroll, resize, and orientation change
        let scrollTimeout;
        function handleScroll() {
            if (scrollTimeout) {
                window.cancelAnimationFrame(scrollTimeout);
            }
            scrollTimeout = window.requestAnimationFrame(loadVisibleImages);
        }
        
        window.addEventListener('scroll', handleScroll, { passive: true });
        window.addEventListener('resize', handleScroll, { passive: true });
        window.addEventListener('orientationchange', handleScroll, { passive: true });
        
        // Initial load
        loadVisibleImages();
    }

    // Initialize lazy loading
    function initLazyLoading() {
        // For browsers with native lazy loading support
        if (supportsNativeLazyLoading) {
            const lazyImages = document.querySelectorAll('img[data-src]');
            lazyImages.forEach(img => {
                if (img.dataset.src) {
                    img.src = img.dataset.src;
                }
                if (img.dataset.srcset) {
                    img.srcset = img.dataset.srcset;
                }
                img.loading = 'lazy';
                img.classList.add('lazy-loaded');
                delete img.dataset.src;
                delete img.dataset.srcset;
            });
            return;
        }

        // Use Intersection Observer if available
        if ('IntersectionObserver' in window) {
            lazyLoadWithIntersectionObserver();
        } else {
            // Fallback for older browsers
            lazyLoadFallback();
        }
    }

    // Priority image loading for LCP (Largest Contentful Paint)
    function prioritizeLCPImages() {
        const lcpImages = document.querySelectorAll('img[data-lcp="true"], img.lcp-image');
        lcpImages.forEach(img => {
            if (img.dataset.src) {
                img.src = img.dataset.src;
                delete img.dataset.src;
            }
            if (img.dataset.srcset) {
                img.srcset = img.dataset.srcset;
                delete img.dataset.srcset;
            }
            img.loading = 'eager';
            img.fetchpriority = 'high';
            img.classList.add('lazy-loaded');
        });
    }

    // Handle background images with lazy loading
    function lazyLoadBackgrounds() {
        const bgElements = document.querySelectorAll('[data-bg], [data-bg-set]');
        
        if (bgElements.length === 0) return;

        const bgObserver = new IntersectionObserver((entries, observer) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const element = entry.target;
                    
                    if (element.dataset.bg) {
                        element.style.backgroundImage = `url('${element.dataset.bg}')`;
                        delete element.dataset.bg;
                    }
                    
                    if (element.dataset.bgSet) {
                        element.style.backgroundImage = `image-set(${element.dataset.bgSet})`;
                        delete element.dataset.bgSet;
                    }
                    
                    element.classList.add('bg-loaded');
                    observer.unobserve(element);
                }
            });
        }, imageObserverConfig);

        bgElements.forEach(element => bgObserver.observe(element));
    }

    // Handle iframe lazy loading (for embedded content)
    function lazyLoadIframes() {
        const lazyIframes = document.querySelectorAll('iframe[data-src]');
        
        if (lazyIframes.length === 0) return;

        const iframeObserver = new IntersectionObserver((entries, observer) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const iframe = entry.target;
                    iframe.src = iframe.dataset.src;
                    delete iframe.dataset.src;
                    observer.unobserve(iframe);
                }
            });
        }, imageObserverConfig);

        lazyIframes.forEach(iframe => iframeObserver.observe(iframe));
    }

    // Image error handling
    function handleImageErrors() {
        document.addEventListener('error', function(e) {
            if (e.target.tagName === 'IMG') {
                const img = e.target;
                
                // Try to load from fallback if available
                if (img.dataset.fallback && !img.dataset.fallbackAttempted) {
                    img.src = img.dataset.fallback;
                    img.dataset.fallbackAttempted = 'true';
                } else {
                    // Add error class for styling
                    img.classList.add('img-error');
                    
                    // Optional: Replace with placeholder
                    if (!img.dataset.noPlaceholder) {
                        img.src = '/images/placeholder.png';
                    }
                }
            }
        }, true);
    }

    // Monitor image loading performance
    function monitorImagePerformance() {
        if ('PerformanceObserver' in window) {
            try {
                const observer = new PerformanceObserver((list) => {
                    list.getEntries().forEach((entry) => {
                        if (entry.entryType === 'largest-contentful-paint') {
                            console.log('LCP element:', entry.element);
                            console.log('LCP time:', entry.renderTime || entry.loadTime);
                        }
                    });
                });
                observer.observe({ entryTypes: ['largest-contentful-paint'] });
            } catch (e) {
                console.warn('Performance monitoring not available');
            }
        }
    }

    // Initialize everything when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function() {
            prioritizeLCPImages();
            initLazyLoading();
            
            if ('IntersectionObserver' in window) {
                lazyLoadBackgrounds();
                lazyLoadIframes();
            }
            
            handleImageErrors();
            monitorImagePerformance();
        });
    } else {
        prioritizeLCPImages();
        initLazyLoading();
        
        if ('IntersectionObserver' in window) {
            lazyLoadBackgrounds();
            lazyLoadIframes();
        }
        
        handleImageErrors();
        monitorImagePerformance();
    }

    // Export for manual triggering if needed
    window.LazyLoader = {
        init: initLazyLoading,
        loadImage: loadImage,
        prioritizeLCP: prioritizeLCPImages
    };
})();
