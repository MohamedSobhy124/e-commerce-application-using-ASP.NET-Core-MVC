// ===================================
// MEGA COOL EXTRAS JAVASCRIPT
// Maximum Coolness Features
// ===================================

(function() {
    'use strict';

    // ========================================
    // 1. CONFETTI CELEBRATION
    // ========================================
    function triggerConfetti() {
        let container = document.getElementById('confettiContainer');
        if (!container) {
            container = document.createElement('div');
            container.id = 'confettiContainer';
            container.className = 'confetti-container';
            document.body.appendChild(container);
        }
        
        const colors = ['#FF6B35', '#3B9DD5', '#7BC043', '#FFD700', '#FF1493', '#00CED1'];
        
        for (let i = 0; i < 100; i++) {
            const confetti = document.createElement('div');
            confetti.className = 'confetti-piece';
            confetti.style.left = Math.random() * 100 + '%';
            confetti.style.background = colors[Math.floor(Math.random() * colors.length)];
            confetti.style.animationDelay = Math.random() * 0.5 + 's';
            confetti.style.animationDuration = (Math.random() * 2 + 2) + 's';
            container.appendChild(confetti);
            
            setTimeout(() => confetti.remove(), 3500);
        }
    }

    // Make it global for use anywhere
    window.triggerConfetti = triggerConfetti;

    // ========================================
    // 2. SPINNING DISCOUNT WHEEL
    // ========================================
    function initSpinWheel() {
        // Show after 5 seconds
        setTimeout(() => {
            showSpinWheel();
        }, 5000);
    }

    function showSpinWheel() {
        let popup = document.getElementById('spinWheelPopup');
        if (!popup) {
            popup = document.createElement('div');
            popup.id = 'spinWheelPopup';
            popup.className = 'spin-wheel-popup';
            popup.innerHTML = `
                <h2 style="font-size: 2rem; font-weight: 900; color: #1976D2; margin-bottom: 1rem;">
                    🎉 SPIN TO WIN! 🎉
                </h2>
                <p style="color: #6b7280; margin-bottom: 2rem;">
                    Try your luck for an exclusive discount!
                </p>
                <div class="wheel-container">
                    <div class="wheel-pointer"></div>
                    <div class="wheel" id="discountWheel"></div>
                </div>
                <button class="spin-btn" id="spinButton">
                    SPIN NOW!
                </button>
                <button style="background: none; border: none; margin-top: 1rem; color: #9ca3af; cursor: pointer; font-weight: 600;" onclick="document.getElementById('spinWheelPopup').classList.remove('active')">
                    No thanks
                </button>
            `;
            document.body.appendChild(popup);
            
            const overlay = document.createElement('div');
            overlay.style.cssText = `
                position: fixed;
                top: 0;
                left: 0;
                width: 100%;
                height: 100%;
                background: rgba(0, 0, 0, 0.7);
                z-index: 9999;
                opacity: 0;
                transition: opacity 0.3s ease;
            `;
            overlay.id = 'spinWheelOverlay';
            document.body.insertBefore(overlay, popup);
            
            // Spin button click
            document.getElementById('spinButton').addEventListener('click', function() {
                spinWheel(this);
            });
        }
        
        popup.classList.add('active');
        document.getElementById('spinWheelOverlay').style.opacity = '1';
    }

    function spinWheel(button) {
        const wheel = document.getElementById('discountWheel');
        const prizes = ['10% OFF', '20% OFF', '30% OFF', '50% OFF', 'Free Shipping', 'Try Again'];
        
        button.disabled = true;
        wheel.classList.add('spinning');
        
        setTimeout(() => {
            wheel.classList.remove('spinning');
            const randomPrize = prizes[Math.floor(Math.random() * prizes.length)];
            
            showToast('success', 'Congratulations!', `You won: ${randomPrize}! 🎉`);
            triggerConfetti();
            
            setTimeout(() => {
                document.getElementById('spinWheelPopup').classList.remove('active');
                document.getElementById('spinWheelOverlay').style.opacity = '0';
            }, 2000);
        }, 3000);
    }

    // ========================================
    // 3. PRODUCT BADGES (New, Hot, Trending) & STOCK ALERTS
    // ========================================
    function initProductBadges() {
        document.querySelectorAll('.product-card').forEach((card, index) => {
            const imageWrapper = card.querySelector('.product-image-wrapper');
            if (!imageWrapper) return;
            
            // Get stock data from data attributes
            const stockQty = parseInt(card.getAttribute('data-stock-quantity')) || 0;
            const minStock = parseInt(card.getAttribute('data-minimum-stock')) || 5;
            const isOutOfStock = stockQty === 0;
            const isLowStock = stockQty > 0 && stockQty <= minStock;
            
            // Add status badge based on stock
            let badge;
            if (isOutOfStock) {
                badge = '<div class="badge-out-of-stock" style="background: linear-gradient(135deg, #ef4444, #dc2626); color: white; padding: 0.4rem 1rem; border-radius: 20px; font-size: 0.75rem; font-weight: 900; text-transform: uppercase; letter-spacing: 1px; position: absolute; top: 0.5rem; right: 0.5rem; z-index: 10;">❌ OUT OF STOCK</div>';
            } else if (isLowStock) {
                // Low stock - add HOT badge to create urgency
                badge = '<div class="badge-hot">🔥 LOW STOCK</div>';
            } 
            
            if (badge) {
                imageWrapper.insertAdjacentHTML('beforeend', badge);
            }
            
            // Add detailed stock alert in product content
            if (isOutOfStock) {
                const content = card.querySelector('.product-content');
                if (content) {
                    const stockAlert = document.createElement('div');
                    stockAlert.className = 'stock-alert-out';
                    stockAlert.style.cssText = `
                        background: linear-gradient(135deg, #fee2e2, #fecaca);
                        color: #dc2626;
                        padding: 0.75rem 1rem;
                        border-radius: 8px;
                        font-weight: 700;
                        font-size: 0.6rem;
                        display: flex;
                        align-items: center;
                        gap: 0.5rem;
                        margin-bottom: 0.75rem;
                        border-left: 4px solid #dc2626;
                    `;
                    stockAlert.innerHTML = `
                        <i class="bi bi-x-circle-fill" style="font-size: 1.2rem;"></i>
                        <span>Out of Stock - Currently Unavailable</span>
                    `;
                    const priceSection = content.querySelector('.price-section');
                    if (priceSection) {
                        content.insertBefore(stockAlert, priceSection);
                    }
                    
                    // Disable add to cart button with proper styling
                    const addButton = card.querySelector('.btn-add-cart-quick');
                    if (addButton) {
                        addButton.disabled = true;
                        addButton.classList.add('out-of-stock');
                        // Update button text and icon
                        addButton.innerHTML = '<i class="bi bi-x-circle"></i>';
                        addButton.title = 'Out of Stock';
                        // Make button wider to show it's disabled
                        addButton.style.width = '40px';
                        addButton.style.height = '40px';
                    }
                }
            } else if (isLowStock) {
                const content = card.querySelector('.product-content');
                if (content) {
                    const stockAlert = document.createElement('div');
                    stockAlert.className = 'stock-alert';
                    stockAlert.innerHTML = `
                        <i class="bi bi-exclamation-triangle"></i>
                        Only ${stockQty} left in stock - Order soon!
                    `;
                    const priceSection = content.querySelector('.price-section');
                    if (priceSection) {
                        content.insertBefore(stockAlert, priceSection);
                    }
                }
            }
        });
    }

    // ========================================
    // 4. FLOATING CHAT WIDGET
    // ========================================
    function initChatWidget() {
        const widget = document.createElement('div');
        widget.className = 'chat-widget';
        widget.innerHTML = `
            <div class="chat-bubble" id="chatBubble">
                <i class="bi bi-chat-dots"></i>
                <div class="chat-notification">3</div>
            </div>
            <div class="chat-window" id="chatWindow">
                <div style="background: linear-gradient(135deg, #667eea, #764ba2); padding: 1.5rem; color: white;">
                    <h3 style="margin: 0; font-size: 1.25rem; font-weight: 800;">Chat with us!</h3>
                    <p style="margin: 0.5rem 0 0; opacity: 0.9; font-size: 0.9rem;">We're online now</p>
                </div>
                <div style="padding: 1.5rem;">
                    <div style="background: #f3f4f6; padding: 1rem; border-radius: 12px; margin-bottom: 1rem;">
                        <p style="margin: 0; color: #374151; font-size: 0.9rem;">
                            👋 Hi! How can we help you today?
                        </p>
                    </div>
                    <textarea style="width: 100%; padding: 1rem; border: 2px solid #e5e7eb; border-radius: 12px; resize: none;" rows="3" placeholder="Type your message..."></textarea>
                    <button style="width: 100%; margin-top: 1rem; background: linear-gradient(135deg, #667eea, #764ba2); color: white; border: none; padding: 1rem; border-radius: 12px; font-weight: 700; cursor: pointer;">
                        Send Message
                    </button>
                </div>
            </div>
        `;
        document.body.appendChild(widget);
        
        document.getElementById('chatBubble').addEventListener('click', () => {
            document.getElementById('chatWindow').classList.toggle('active');
        });
    }

    // ========================================
    // 5. TOAST NOTIFICATIONS
    // ========================================
    function showToast(type, title, message) {
        let container = document.getElementById('toastContainer');
        if (!container) {
            container = document.createElement('div');
            container.id = 'toastContainer';
            container.className = 'toast-container';
            document.body.appendChild(container);
        }
        
        const toast = document.createElement('div');
        toast.className = `toast ${type}`;
        
        const icons = {
            success: 'bi-check-circle-fill',
            error: 'bi-x-circle-fill',
            info: 'bi-info-circle-fill'
        };
        
        toast.innerHTML = `
            <div class="toast-icon">
                <i class="bi ${icons[type]}"></i>
            </div>
            <div class="toast-content">
                <div class="toast-title">${title}</div>
                <div class="toast-message">${message}</div>
            </div>
        `;
        
        container.appendChild(toast);
        
        setTimeout(() => {
            toast.style.animation = 'toastSlideIn 0.4s reverse';
            setTimeout(() => toast.remove(), 400);
        }, 3000);
    }

    window.showToast = showToast;

    // ========================================
    // 6. CUSTOM CURSOR
    // ========================================
    function initCustomCursor() {
        const cursor = document.createElement('div');
        cursor.className = 'custom-cursor';
        document.body.appendChild(cursor);
        
        const trails = [];
        for (let i = 0; i < 5; i++) {
            const trail = document.createElement('div');
            trail.className = 'custom-cursor-trail';
            document.body.appendChild(trail);
            trails.push(trail);
        }
        
        let mouseX = 0, mouseY = 0;
        let cursorX = 0, cursorY = 0;
        
        document.addEventListener('mousemove', (e) => {
            mouseX = e.clientX;
            mouseY = e.clientY;
        });
        
        function animateCursor() {
            cursorX += (mouseX - cursorX) * 0.2;
            cursorY += (mouseY - cursorY) * 0.2;
            
            cursor.style.left = cursorX + 'px';
            cursor.style.top = cursorY + 'px';
            
            trails.forEach((trail, index) => {
                const delay = (index + 1) * 0.1;
                trail.style.left = (cursorX - (mouseX - cursorX) * delay) + 'px';
                trail.style.top = (cursorY - (mouseY - cursorY) * delay) + 'px';
            });
            
            requestAnimationFrame(animateCursor);
        }
        
        animateCursor();
        
        document.addEventListener('mousedown', () => cursor.classList.add('clicking'));
        document.addEventListener('mouseup', () => cursor.classList.remove('clicking'));
    }

    // ========================================
    // 7. SKELETON LOADERS
    // ========================================
    function initSkeletonLoaders() {
        // Add skeletons to product grid on page load
        const productContainer = document.querySelector('.products-grid-container');
        if (!productContainer) return;
        
        const originalContent = productContainer.innerHTML;
        
        // Show skeletons
        productContainer.innerHTML = '';
        for (let i = 0; i < 8; i++) {
            const skeleton = document.createElement('div');
            skeleton.className = 'skeleton-product';
            skeleton.innerHTML = `
                <div class="skeleton-box skeleton-image"></div>
                <div class="skeleton-box skeleton-text"></div>
                <div class="skeleton-box skeleton-text short"></div>
            `;
            productContainer.appendChild(skeleton);
        }
        
        // Restore content after 2 seconds
        setTimeout(() => {
            productContainer.innerHTML = originalContent;
        }, 2000);
    }

    // ========================================
    // 8. STAGGER SCROLL ANIMATIONS
    // ========================================
    function initStaggerAnimations() {
        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.classList.add('visible');
                }
            });
        }, { threshold: 0.1 });
        
        document.querySelectorAll('.stagger-item').forEach(item => {
            observer.observe(item);
        });
    }

    // ========================================
    // 9. SNOW/CONFETTI FALLING
    // ========================================
    function initSnowEffect() {
        const snowflakes = ['❄️', '❅', '❆', '✨', '⭐', '🌟'];
        
        setInterval(() => {
            if (Math.random() < 0.3) return; // Don't create every time
            
            const flake = document.createElement('div');
            flake.className = 'snow-flake';
            flake.textContent = snowflakes[Math.floor(Math.random() * snowflakes.length)];
            flake.style.left = Math.random() * 100 + '%';
            flake.style.animationDuration = (Math.random() * 3 + 2) + 's';
            flake.style.fontSize = (Math.random() * 1 + 0.5) + 'rem';
            
            document.body.appendChild(flake);
            
            setTimeout(() => flake.remove(), 5000);
        }, 500);
    }

    // ========================================
    // 10. PRODUCT SUCCESS ANIMATION
    // ========================================
    function showSuccessCheckmark(element) {
        const checkmark = document.createElement('div');
        checkmark.style.cssText = `
            position: fixed;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%);
            z-index: 10001;
        `;
        checkmark.innerHTML = `
            <div class="checkmark-circle">
                <i class="bi bi-check-lg checkmark-icon"></i>
            </div>
        `;
        
        document.body.appendChild(checkmark);
        
        setTimeout(() => {
            checkmark.style.animation = 'fadeOut 0.3s ease';
            setTimeout(() => checkmark.remove(), 300);
        }, 1500);
    }

    window.showSuccessCheckmark = showSuccessCheckmark;

    // ========================================
    // 11. AUTO-TRIGGER COOL EFFECTS
    // ========================================
    function initAutoEffects() {
        // Trigger confetti on first add to cart
        let firstAddToCart = true;
        document.addEventListener('click', (e) => {
            if (e.target.closest('.btn-add-cart-quick') || e.target.closest('.magnetic-btn')) {
                if (firstAddToCart) {
                    setTimeout(() => {
                        triggerConfetti();
                        showSuccessCheckmark();
                    }, 500);
                    firstAddToCart = false;
                }
            }
        });
    }

    // ========================================
    // 12. PRICE DROP ANIMATION
    // ========================================
    function initPriceDropAlerts() {
        document.querySelectorAll('.product-card').forEach(card => {
            if (Math.random() < 0.2) { // 20% of products have price drop
                const imageWrapper = card.querySelector('.product-image-wrapper');
                if (imageWrapper) {
                    const alert = document.createElement('div');
                    alert.className = 'price-drop-alert';
                    alert.innerHTML = `📉 Price Dropped ${Math.floor(Math.random() * 30) + 10}%!`;
                    alert.style.display = 'none';
                    imageWrapper.appendChild(alert);
                    
                    card.addEventListener('mouseenter', () => {
                        alert.style.display = 'block';
                    });
                    
                    card.addEventListener('mouseleave', () => {
                        alert.style.display = 'none';
                    });
                }
            }
        });
    }

    // ========================================
    // 13. FLOATING ELEMENTS
    // ========================================
    function createFloatingElements() {
        const emojis = ['💎', '✨', '⭐', '🌟', '💫', '🎯', '🔥', '⚡'];
        
        setInterval(() => {
            if (Math.random() < 0.5) return;
            
            const emoji = document.createElement('div');
            emoji.style.cssText = `
                position: fixed;
                font-size: 2rem;
                pointer-events: none;
                z-index: 9997;
                animation: floatUp 3s ease-out forwards;
            `;
            emoji.textContent = emojis[Math.floor(Math.random() * emojis.length)];
            emoji.style.left = Math.random() * 100 + '%';
            emoji.style.bottom = '-50px';
            
            document.body.appendChild(emoji);
            
            setTimeout(() => emoji.remove(), 3000);
        }, 3000);
    }

    // Add animation
    const style = document.createElement('style');
    style.textContent = `
        @keyframes floatUp {
            0% {
                transform: translateY(0) rotate(0deg);
                opacity: 0.8;
            }
            100% {
                transform: translateY(-100vh) rotate(360deg);
                opacity: 0;
            }
        }
        @keyframes fadeOut {
            to { opacity: 0; }
        }
    `;
    document.head.appendChild(style);

    // ========================================
    // INITIALIZE ALL FEATURES
    // ========================================
    document.addEventListener('DOMContentLoaded', () => {
        console.log('🎉 MEGA COOL EXTRAS Initialized!');
        
        // Core features (always active)
        initProductBadges();
        //initChatWidget();
        initStaggerAnimations();
        initAutoEffects();
        //initPriceDropAlerts();
        
        // Optional features (uncomment to activate)
        //initSpinWheel();           // Spin wheel popup
          //initCustomCursor();        // Custom cursor (can be heavy)
          //initSnowEffect();          // Falling snowflakes/stars
          //createFloatingElements();  // Floating emojis
         //initSkeletonLoaders();     // Skeleton loading state
        
        console.log('✨ All MEGA COOL features ready!');
        
        // Show welcome toast
        setTimeout(() => {
            showToast('info', 'Welcome! 🎉', 'Discover amazing products with exclusive offers!');
        }, 1000);
    });

})();

