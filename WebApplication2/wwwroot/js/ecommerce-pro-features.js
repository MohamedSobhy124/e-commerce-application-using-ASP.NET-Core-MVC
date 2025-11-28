// ===================================
// PRO E-COMMERCE FEATURES JAVASCRIPT
// Advanced Shopping Experience
// ===================================

(function() {
    'use strict';

    // ========================================
    // 1. QUICK VIEW MODAL
    // ========================================
    function initQuickView() {
        // Add quick view buttons to products
        document.querySelectorAll('.product-card').forEach(card => {
            const quickViewBtn = document.createElement('button');
            quickViewBtn.className = 'quick-view-btn';
            quickViewBtn.innerHTML = '<i class="bi bi-eye"></i> Quick View';
            quickViewBtn.style.cssText = `
                position: absolute;
                top: 50%;
                left: 50%;
                transform: translate(-50%, -50%);
                background: white;
                border: none;
                padding: 0.75rem 1.5rem;
                border-radius: 50px;
                font-weight: 700;
                opacity: 0;
                pointer-events: none;
                transition: all 0.3s ease;
                z-index: 10;
                box-shadow: 0 4px 16px rgba(0, 0, 0, 0.2);
            `;
            
            const imageWrapper = card.querySelector('.product-image-wrapper');
            if (imageWrapper) {
                imageWrapper.style.position = 'relative';
                imageWrapper.appendChild(quickViewBtn);
                
                card.addEventListener('mouseenter', () => {
                    quickViewBtn.style.opacity = '1';
                    quickViewBtn.style.pointerEvents = 'all';
                });
                
                card.addEventListener('mouseleave', () => {
                    quickViewBtn.style.opacity = '0';
                    quickViewBtn.style.pointerEvents = 'none';
                });
                
                quickViewBtn.addEventListener('click', (e) => {
                    e.preventDefault();
                    e.stopPropagation();
                    showQuickView(card);
                });
            }
        });
    }

    function showQuickView(productCard) {
        // Create modal if it doesn't exist
        let modal = document.getElementById('quickViewModal');
        if (!modal) {
            modal = document.createElement('div');
            modal.id = 'quickViewModal';
            modal.className = 'quick-view-modal';
            modal.innerHTML = `
                <button class="quick-view-close"><i class="bi bi-x-lg"></i></button>
                <div class="quick-view-content">
                    <div class="quick-view-images"></div>
                    <div class="quick-view-details"></div>
                </div>
            `;
            document.body.appendChild(modal);
            
            // Close button
            modal.querySelector('.quick-view-close').addEventListener('click', closeQuickView);
            
            // Close on overlay click
            modal.addEventListener('click', (e) => {
                if (e.target === modal) closeQuickView();
            });
        }
        
        // Populate modal with product data
        const title = productCard.querySelector('.product-title')?.textContent || 'Product';
        const price = productCard.querySelector('.current-price')?.textContent || '$0.00';
        const image = productCard.querySelector('.product-image')?.src || '';
        
        modal.querySelector('.quick-view-images').innerHTML = `
            <img src="${image}" alt="${title}" style="width: 100%; border-radius: 16px;">
        `;
        
        modal.querySelector('.quick-view-details').innerHTML = `
            <h2 style="font-size: 2rem; font-weight: 800; color: #1976D2; margin-bottom: 1rem;">${title}</h2>
            <div style="font-size: 2.5rem; font-weight: 900; background: linear-gradient(135deg, #7BC043, #558B2F); -webkit-background-clip: text; -webkit-text-fill-color: transparent; margin-bottom: 2rem;">${price}</div>
            <p style="color: #6b7280; line-height: 1.8; margin-bottom: 2rem;">Premium quality supplement designed to help you achieve your fitness goals. Made with natural ingredients.</p>
            <button class="btn-primary" style="width: 100%; padding: 1rem; font-size: 1.1rem;" onclick="Swal.fire({icon: 'success', title: 'Success', text: 'Added to cart!', timer: 1500, showConfirmButton: false})">
                <i class="bi bi-cart-plus me-2"></i> Add to Cart
            </button>
        `;
        
        modal.classList.add('active');
        document.body.style.overflow = 'hidden';
    }

    function closeQuickView() {
        const modal = document.getElementById('quickViewModal');
        if (modal) {
            modal.classList.remove('active');
            document.body.style.overflow = '';
        }
    }

    // ========================================
    // 2. WISHLIST FUNCTIONALITY (Logged-in users only)
    // ========================================
    function initWishlist() {
        // Wishlist buttons are now rendered server-side for logged-in users
        // Just initialize any additional functionality here
        
        // Load wishlist items on page load if user is authenticated
        const isAuthenticated = document.body.getAttribute('data-is-authenticated') === 'true' || 
                                typeof window.isAuthenticated !== 'undefined' && window.isAuthenticated;
        
        if (isAuthenticated) {
            loadWishlistItems();
            loadWishlistCount();
        }
    }
    
    // Load wishlist items
    function loadWishlistItems() {
        fetch('/Customer/Home/GetWishlistItems')
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    updateFloatingWishlistBadge(data.count);
                    // Update wishlist buttons on page
                    data.items.forEach(item => {
                        const btn = document.querySelector(`.wishlist-btn[data-product-id="${item.productId}"]`);
                        if (btn) {
                            btn.classList.add('active');
                            btn.querySelector('i').className = 'bi bi-heart-fill';
                            btn.title = 'Remove from wishlist';
                        }
                    });
                }
            })
            .catch(error => console.error('Error loading wishlist:', error));
    }
    
    // Load wishlist count
    function loadWishlistCount() {
        fetch('/Customer/Home/GetWishlistProductIds')
            .then(response => response.json())
            .then(data => {
                if (data.productIds) {
                    updateFloatingWishlistBadge(data.productIds.length);
                }
            })
            .catch(error => console.error('Error loading wishlist count:', error));
    }

    // Global function for wishlist toggle (called from onclick in view)
    window.toggleWishlist = function(productId, btn) {
        // Check if user is authenticated
        const isAuthenticated = document.body.getAttribute('data-is-authenticated') === 'true' || 
                                typeof window.isAuthenticated !== 'undefined' && window.isAuthenticated;
        
        if (!isAuthenticated) {
            if (typeof toastr !== 'undefined') {
                toastr.warning('Please login to use wishlist');
            } else {
                Swal.fire({
                    icon: 'warning',
                    title: 'Login Required',
                    text: 'Please login to use wishlist'
                });
            }
            return;
        }

        const isCurrentlyActive = btn.classList.contains('active');
        const originalIcon = btn.querySelector('i').className;
        
        // Show loading state
        btn.disabled = true;
        btn.querySelector('i').className = 'bi bi-arrow-repeat';
        btn.querySelector('i').style.animation = 'spin 1s linear infinite';

        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        const formData = new FormData();
        formData.append('productId', productId);
        formData.append('__RequestVerificationToken', token);

        fetch('/Customer/Home/ToggleWishlist', {
            method: 'POST',
            body: formData
        })
        .then(response => response.json())
        .then(data => {
            btn.disabled = false;
            btn.querySelector('i').style.animation = '';
            
            if (data.success) {
                if (data.isAdded) {
                    // Product was added to wishlist
                    btn.classList.add('active');
                    btn.querySelector('i').className = 'bi bi-heart-fill';
                    btn.title = 'Remove from wishlist';
                    
                    // Create particles
                    for (let i = 0; i < 6; i++) {
                        const particle = document.createElement('div');
                        particle.className = 'wishlist-particle';
                        const angle = (Math.PI * 2 * i) / 6;
                        const distance = 30;
                        particle.style.setProperty('--tx', `${Math.cos(angle) * distance}px`);
                        particle.style.setProperty('--ty', `${Math.sin(angle) * distance}px`);
                        btn.appendChild(particle);
                        setTimeout(() => particle.remove(), 1000);
                    }
                    
                    // Update floating wishlist badge
                    updateFloatingWishlistBadge(data.wishlistCount);
                    
                    if (typeof toastr !== 'undefined') {
                        toastr.success(data.message);
                    }
                } else {
                    // Product was removed from wishlist
                    btn.classList.remove('active');
                    btn.querySelector('i').className = 'bi bi-heart';
                    btn.title = 'Add to wishlist';
                    
                    // Update floating wishlist badge
                    updateFloatingWishlistBadge(data.wishlistCount);
                    
                    if (typeof toastr !== 'undefined') {
                        toastr.info(data.message);
                    }
                }
            } else {
                // Error handling
                if (data.requiresLogin) {
                    if (typeof toastr !== 'undefined') {
                        toastr.warning(data.message);
                    } else {
                        Swal.fire({
                            icon: 'warning',
                            title: 'Login Required',
                            text: data.message
                        });
                    }
                } else {
                    btn.querySelector('i').className = originalIcon;
                    if (typeof toastr !== 'undefined') {
                        toastr.error(data.message || 'Failed to update wishlist');
                    } else {
                        Swal.fire({
                            icon: 'error',
                            title: 'Error',
                            text: data.message || 'Failed to update wishlist'
                        });
                    }
                }
            }
        })
        .catch(error => {
            console.error('Error:', error);
            btn.disabled = false;
            btn.querySelector('i').className = originalIcon;
            btn.querySelector('i').style.animation = '';
            if (typeof toastr !== 'undefined') {
                toastr.error('Failed to update wishlist. Please try again.');
            } else {
                Swal.fire({
                    icon: 'error',
                    title: 'Error',
                    text: 'Failed to update wishlist. Please try again.'
                });
            }
        });
    };

    // Function to update floating wishlist badge
    function updateFloatingWishlistBadge(count) {
        const floatingBadge = document.getElementById('floatingWishlistBadge');
        const floatingBtn = document.querySelector('.floating-wishlist-btn');
        if (floatingBadge) {
            floatingBadge.textContent = count;
            if (count > 0) {
                floatingBadge.style.display = 'flex';
            } else {
                floatingBadge.style.display = 'none';
            }
            // Add pulse animation
            if (floatingBtn) {
                floatingBtn.classList.add('pulse');
                setTimeout(() => {
                    floatingBtn.classList.remove('pulse');
                }, 600);
            }
        }
    }

    // ========================================
    // 3. SOCIAL PROOF NOTIFICATIONS
    // ========================================
    function initSocialProof() {
        const names = ['Ahmed', 'Fatima', 'Mohammed', 'Sara', 'Ali', 'Layla'];
        const actions = ['just purchased', 'is viewing', 'added to cart'];
        const products = ['Protein Powder', 'Vitamins', 'Supplements', 'Pre-Workout'];
        
        function showSocialProof() {
            const name = names[Math.floor(Math.random() * names.length)];
            const action = actions[Math.floor(Math.random() * actions.length)];
            const product = products[Math.floor(Math.random() * products.length)];
            
            let popup = document.getElementById('socialProofPopup');
            if (!popup) {
                popup = document.createElement('div');
                popup.id = 'socialProofPopup';
                popup.className = 'social-proof-popup';
                document.body.appendChild(popup);
            }
            
            popup.innerHTML = `
                <div class="social-proof-avatar" style="background: linear-gradient(135deg, #3B9DD5, #7BC043); display: flex; align-items: center; justify-content: center; color: white; font-weight: bold; font-size: 1.2rem;">
                    ${name.charAt(0)}
                </div>
                <div class="social-proof-text">
                    <div class="social-proof-name">${name}</div>
                    <div class="social-proof-action">${action} ${product}</div>
                    <div class="social-proof-time">Just now</div>
                </div>
                <button class="social-proof-close" onclick="this.parentElement.classList.remove('show')">
                    <i class="bi bi-x"></i>
                </button>
            `;
            
            setTimeout(() => popup.classList.add('show'), 100);
            
            setTimeout(() => popup.classList.remove('show'), 5000);
        }
        
        // Show first one after 3 seconds
        setTimeout(showSocialProof, 3000);
        
        // Then show randomly every 15-30 seconds
        setInterval(showSocialProof, Math.random() * 15000 + 15000);
    }

    // ========================================
    // 4. FLASH SALE COUNTDOWN
    // ========================================
    function initFlashSale() {
        // Create flash sale banner
        const banner = document.createElement('div');
        banner.className = 'flash-sale-banner';
        banner.innerHTML = `
            <div class="container">
                <div style="font-size: 1.5rem; font-weight: 900; text-transform: uppercase; letter-spacing: 2px;">
                    ⚡ FLASH SALE - UP TO 50% OFF! ⚡
                </div>
                <div class="countdown-timer">
                    <div class="countdown-item">
                        <span class="countdown-number" id="hours">00</span>
                        <span class="countdown-label">Hours</span>
                    </div>
                    <div class="countdown-item">
                        <span class="countdown-number" id="minutes">00</span>
                        <span class="countdown-label">Minutes</span>
                    </div>
                    <div class="countdown-item">
                        <span class="countdown-number" id="seconds">00</span>
                        <span class="countdown-label">Seconds</span>
                    </div>
                </div>
            </div>
        `;
        
        // Insert at top of page
        const heroSection = document.querySelector('.hero-section');
        if (heroSection) {
            heroSection.parentNode.insertBefore(banner, heroSection);
        }
        
        // Countdown logic
        const endTime = new Date().getTime() + (2 * 60 * 60 * 1000); // 2 hours from now
        
        function updateCountdown() {
            const now = new Date().getTime();
            const distance = endTime - now;
            
            const hours = Math.floor((distance % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
            const minutes = Math.floor((distance % (1000 * 60 * 60)) / (1000 * 60));
            const seconds = Math.floor((distance % (1000 * 60)) / 1000);
            
            // Check if elements exist before updating
            const hoursEl = document.getElementById('hours');
            const minutesEl = document.getElementById('minutes');
            const secondsEl = document.getElementById('seconds');
            
            if (hoursEl) hoursEl.textContent = hours.toString().padStart(2, '0');
            if (minutesEl) minutesEl.textContent = minutes.toString().padStart(2, '0');
            if (secondsEl) secondsEl.textContent = seconds.toString().padStart(2, '0');
            
            if (distance < 0 && banner) {
                banner.remove();
            }
        }
        
        updateCountdown();
        setInterval(updateCountdown, 1000);
    }

    // ========================================
    // 5. LIVE VIEWERS BADGE
    // ========================================
    function initLiveViewers() {
        document.querySelectorAll('.product-card').forEach(card => {
            const viewers = Math.floor(Math.random() * 20) + 5;
            const badge = document.createElement('div');
            badge.className = 'live-viewers';
            badge.innerHTML = `
                <div class="live-dot"></div>
                ${viewers} people viewing
            `;
            badge.style.cssText = `
                position: absolute;
                top: 1rem;
                left: 1rem;
                z-index: 10;
            `;
            
            const imageWrapper = card.querySelector('.product-image-wrapper');
            if (imageWrapper) {
                imageWrapper.appendChild(badge);
                
                // Update viewers count every 10 seconds
                setInterval(() => {
                    const newViewers = Math.floor(Math.random() * 20) + 5;
                    badge.innerHTML = `
                        <div class="live-dot"></div>
                        ${newViewers} people viewing
                    `;
                }, 10000);
            }
        });
    }

    // ========================================
    // 6. STICKY ADD TO CART BAR
    // ========================================
    function initStickyCart() {
        const stickyBar = document.createElement('div');
        stickyBar.className = 'sticky-cart-bar';
        stickyBar.innerHTML = `
         
        `;
        document.body.appendChild(stickyBar);
        
        // Show/hide on scroll
        let lastScroll = 0;
        window.addEventListener('scroll', () => {
            const currentScroll = window.pageYOffset;
            if (currentScroll > 500 && currentScroll > lastScroll) {
                stickyBar.classList.add('active');
            } else if (currentScroll < lastScroll - 50) {
                stickyBar.classList.remove('active');
            }
            lastScroll = currentScroll;
        });
    }

    // Quantity change function (global)
    window.changeQty = function(delta) {
        const qtyElement = document.getElementById('stickyQty');
        let qty = parseInt(qtyElement.textContent);
        qty = Math.max(1, qty + delta);
        qtyElement.textContent = qty;
        
        // Animate
        qtyElement.style.transform = 'scale(1.3)';
        setTimeout(() => {
            qtyElement.style.transform = 'scale(1)';
        }, 200);
    };

    // ========================================
    // 7. TRUST BADGES CAROUSEL
    // ========================================
    function initTrustBadges() {
        const badges = [
            { icon: 'shield-check', text: 'Secure Payments' },
            { icon: 'truck', text: 'Free Shipping' },
            { icon: 'arrow-repeat', text: 'Easy Returns' },
            { icon: 'award', text: 'Quality Guaranteed' },
            { icon: 'chat-dots', text: '24/7 Support' },
            { icon: 'star-fill', text: 'Top Rated' }
        ];
        
        const section = document.createElement('div');
        section.className = 'trust-badges-section';
        section.innerHTML = `
            <div class="trust-badges-slider">
                ${badges.map(badge => `
                    <div class="trust-badge">
                        <i class="bi bi-${badge.icon} trust-badge-icon"></i>
                        <span class="trust-badge-text">${badge.text}</span>
                    </div>
                `).join('')}
                ${badges.map(badge => `
                    <div class="trust-badge">
                        <i class="bi bi-${badge.icon} trust-badge-icon"></i>
                        <span class="trust-badge-text">${badge.text}</span>
                    </div>
                `).join('')}
            </div>
        `;
        
        // Insert before footer
        const footer = document.querySelector('.enhanced-footer');
        if (footer) {
            footer.parentNode.insertBefore(section, footer);
        }
    }

    // ========================================
    // 8. RECENTLY VIEWED PRODUCTS
    // ========================================
    function initRecentlyViewed() {
        // Store viewed products in localStorage
        const productCards = document.querySelectorAll('.product-card');
        productCards.forEach(card => {
            card.addEventListener('click', () => {
                const productId = card.querySelector('[data-product-id]')?.dataset.productId;
                if (productId) {
                    let viewed = JSON.parse(localStorage.getItem('recentlyViewed') || '[]');
                    viewed = viewed.filter(id => id !== productId);
                    viewed.unshift(productId);
                    viewed = viewed.slice(0, 10);
                    localStorage.setItem('recentlyViewed', JSON.stringify(viewed));
                }
            });
        });
    }

    // ========================================
    // 9. NOTIFICATION SYSTEM
    // ========================================
    function showNotification(message) {
        const notification = document.createElement('div');
        notification.style.cssText = `
            position: fixed;
            top: 100px;
            right: 20px;
            background: white;
            padding: 1rem 1.5rem;
            border-radius: 12px;
            box-shadow: 0 10px 40px rgba(0, 0, 0, 0.2);
            z-index: 10001;
            font-weight: 600;
            color: #1976D2;
            animation: slideInRight 0.4s ease;
        `;
        notification.textContent = message;
        document.body.appendChild(notification);
        
        setTimeout(() => {
            notification.style.animation = 'slideOutRight 0.4s ease';
            setTimeout(() => notification.remove(), 400);
        }, 3000);
    }

    // Add animation keyframes
    const style = document.createElement('style');
    style.textContent = `
        @keyframes slideInRight {
            from { transform: translateX(400px); opacity: 0; }
            to { transform: translateX(0); opacity: 1; }
        }
        @keyframes slideOutRight {
            from { transform: translateX(0); opacity: 1; }
            to { transform: translateX(400px); opacity: 0; }
        }
    `;
    document.head.appendChild(style);

    // ========================================
    // 10. PRODUCT IMAGE ZOOM
    // ========================================
    function initImageZoom() { //product-image-wrapper product-image-zoom
        document.querySelectorAll('.product-image-wrapper').forEach(wrapper => {
            wrapper.classList.add('product-image-zoom');
            
            const lens = document.createElement('div');
            lens.className = 'zoom-lens';
            wrapper.appendChild(lens);
            
            wrapper.addEventListener('mousemove', (e) => {
                const rect = wrapper.getBoundingClientRect();
                const x = e.clientX - rect.left;
                const y = e.clientY - rect.top;
                
                lens.style.left = (x - 50) + 'px';
                lens.style.top = (y - 50) + 'px';
            });
        });
    }

    // ========================================
    // INITIALIZE ALL FEATURES
    // ========================================
    document.addEventListener('DOMContentLoaded', () => {
        console.log('🛍️ E-Commerce Pro Features Initialized!');
        
        //initQuickView();
        initWishlist();
        //initSocialProof();
        //initFlashSale(); // Disabled - using new flash sale system
        //initLiveViewers();
         //initStickyCart(); 
        initTrustBadges();
        //initRecentlyViewed();
        //initImageZoom();
        
        console.log('✨ All e-commerce features ready!');
    });

})();

