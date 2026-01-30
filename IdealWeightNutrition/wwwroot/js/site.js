// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

/**
 * iOS/Mobile Touch Fix
 * Fixes the double-tap issue on iOS Safari for buttons with hover states
 */
(function() {
    'use strict';
    
    // Only run on touch devices
    const isTouchDevice = ('ontouchstart' in window) || 
                          (navigator.maxTouchPoints > 0) || 
                          (navigator.msMaxTouchPoints > 0);
    
    if (!isTouchDevice) {
        return;
    }
    
    // Add touch-device class to body for CSS targeting
    document.addEventListener('DOMContentLoaded', function() {
        document.body.classList.add('touch-device');
    });
    
    // Button selectors that need the fix
    const buttonSelectors = [
        '.btn-add-cart',
        '.btn-add-cart-quick',
        '.btn-add-cart-mini',
        '.add-to-cart-button',
        '.add-to-cart-btn',
        '.flash-sale-add-to-cart-btn',
        '.btn-add-cart-from-wishlist',
        '.discounted-cart-btn',
        '.bestseller-cart-btn',
        '.newarrival-cart-btn',
        '.best-seller-cart-btn',
        '.new-arrival-cart-btn',
        '.offer-btn-add',
        '.wishlist-btn',
        '.quick-action-btn'
    ].join(', ');
    
    // Fix for iOS Safari: Clear hover state on touchend
    // This ensures the button is ready for the next tap
    function setupTouchFix() {
        document.addEventListener('touchend', function(e) {
            const button = e.target.closest(buttonSelectors);
            if (button) {
                // Brief delay to allow click event to fire first
                setTimeout(function() {
                    // Remove hover effects by briefly unfocusing
                    if (document.activeElement === button) {
                        button.blur();
                    }
                }, 100);
            }
        }, { passive: true });
        
        // Ensure touch-action is set for immediate response
        document.querySelectorAll(buttonSelectors).forEach(function(button) {
            button.style.touchAction = 'manipulation';
        });
    }
    
    // Run on DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', setupTouchFix);
    } else {
        setupTouchFix();
    }
    
    // Re-run when new content is added dynamically (lazy loading, AJAX, etc.)
    const observer = new MutationObserver(function(mutations) {
        mutations.forEach(function(mutation) {
            if (mutation.addedNodes.length > 0) {
                mutation.addedNodes.forEach(function(node) {
                    if (node.nodeType === 1) { // Element node
                        // Apply touch-action to any new buttons
                        const buttons = node.querySelectorAll ? node.querySelectorAll(buttonSelectors) : [];
                        buttons.forEach(function(button) {
                            button.style.touchAction = 'manipulation';
                        });
                        // Also check if the node itself is a button
                        if (node.matches && node.matches(buttonSelectors)) {
                            node.style.touchAction = 'manipulation';
                        }
                    }
                });
            }
        });
    });
    
    // Start observing once DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function() {
            observer.observe(document.body, { childList: true, subtree: true });
        });
    } else {
        observer.observe(document.body, { childList: true, subtree: true });
    }
})();

/**
 * iOS Zoom Prevention Fix
 * Prevents unwanted zoom on iPhone/iPad when focusing on input fields
 */
(function() {
    'use strict';
    
    // Detect iOS devices
    const isIOS = /iPad|iPhone|iPod/.test(navigator.userAgent) && !window.MSStream;
    
    if (!isIOS) {
        return; // Only run on iOS devices
    }
    
    // Prevent zoom on input focus
    function preventZoomOnFocus() {
        const inputs = document.querySelectorAll('input, select, textarea');
        
        inputs.forEach(function(input) {
            // Ensure minimum font-size of 16px to prevent zoom
            const computedStyle = window.getComputedStyle(input);
            const fontSize = parseFloat(computedStyle.fontSize);
            
            if (fontSize < 16) {
                input.style.fontSize = '16px';
            }
            
            // Prevent zoom on focus
            input.addEventListener('focus', function(e) {
                // Set viewport to prevent zoom
                const viewport = document.querySelector('meta[name="viewport"]');
                if (viewport) {
                    viewport.setAttribute('content', 'width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no');
                }
            }, { passive: true });
            
            // Restore viewport after blur (optional - allows pinch zoom when not typing)
            input.addEventListener('blur', function(e) {
                // Small delay to ensure zoom doesn't happen
                setTimeout(function() {
                    const viewport = document.querySelector('meta[name="viewport"]');
                    if (viewport) {
                        viewport.setAttribute('content', 'width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no');
                    }
                }, 100);
            }, { passive: true });
        });
    }
    
    // Run on DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', preventZoomOnFocus);
    } else {
        preventZoomOnFocus();
    }
    
    // Watch for dynamically added inputs
    const inputObserver = new MutationObserver(function(mutations) {
        mutations.forEach(function(mutation) {
            if (mutation.addedNodes.length > 0) {
                mutation.addedNodes.forEach(function(node) {
                    if (node.nodeType === 1) { // Element node
                        if (node.tagName === 'INPUT' || node.tagName === 'SELECT' || node.tagName === 'TEXTAREA') {
                            const computedStyle = window.getComputedStyle(node);
                            const fontSize = parseFloat(computedStyle.fontSize);
                            if (fontSize < 16) {
                                node.style.fontSize = '16px';
                            }
                        }
                        // Check for inputs within the node
                        const inputs = node.querySelectorAll ? node.querySelectorAll('input, select, textarea') : [];
                        inputs.forEach(function(input) {
                            const computedStyle = window.getComputedStyle(input);
                            const fontSize = parseFloat(computedStyle.fontSize);
                            if (fontSize < 16) {
                                input.style.fontSize = '16px';
                            }
                        });
                    }
                });
            }
        });
    });
    
    // Start observing
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function() {
            inputObserver.observe(document.body, { childList: true, subtree: true });
        });
    } else {
        inputObserver.observe(document.body, { childList: true, subtree: true });
    }
})();

