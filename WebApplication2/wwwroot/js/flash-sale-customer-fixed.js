// ============================================
// FLASH SALE CUSTOMER - FIXED VERSION
// ============================================

class FlashSaleTimer {
    constructor(endDate, elementId, onExpire = null) {
        this.endDate = new Date(endDate).getTime();
        this.elementId = elementId;
        this.onExpire = onExpire;
        this.interval = null;
    }

    start() {
        this.update();
        this.interval = setInterval(() => this.update(), 1000);
    }

    stop() {
        if (this.interval) {
            clearInterval(this.interval);
        }
    }

    update() {
        const now = new Date().getTime();
        const distance = this.endDate - now;

        if (distance < 0) {
            this.stop();
            if (this.onExpire) {
                this.onExpire();
            }
            return;
        }

        const days = Math.floor(distance / (1000 * 60 * 60 * 24));
        const hours = Math.floor((distance % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
        const minutes = Math.floor((distance % (1000 * 60 * 60)) / (1000 * 60));
        const seconds = Math.floor((distance % (1000 * 60)) / 1000);

        const element = document.getElementById(this.elementId);
        if (element) {
            element.innerHTML = this.renderTimer(days, hours, minutes, seconds);
        }
    }

    renderTimer(days, hours, minutes, seconds) {
        return `
            <div class="timer-segment">
                <span class="timer-number">${this.padZero(days)}</span>
                <span class="timer-label">Days</span>
            </div>
            <div class="timer-segment">
                <span class="timer-number">${this.padZero(hours)}</span>
                <span class="timer-label">Hours</span>
            </div>
            <div class="timer-segment">
                <span class="timer-number">${this.padZero(minutes)}</span>
                <span class="timer-label">Min</span>
            </div>
            <div class="timer-segment">
                <span class="timer-number">${this.padZero(seconds)}</span>
                <span class="timer-label">Sec</span>
            </div>
        `;
    }

    padZero(num) {
        return num < 10 ? '0' + num : num;
    }
}

// Product Timer (compact version)
class ProductFlashTimer {
    constructor(endDate, elementId) {
        this.endDate = new Date(endDate).getTime();
        this.elementId = elementId;
        this.interval = null;
    }

    start() {
        this.update();
        this.interval = setInterval(() => this.update(), 1000);
    }

    stop() {
        if (this.interval) {
            clearInterval(this.interval);
        }
    }

    update() {
        const now = new Date().getTime();
        const distance = this.endDate - now;

        if (distance < 0) {
            this.stop();
            const element = document.getElementById(this.elementId);
            if (element) {
                element.innerHTML = '<span style="color: #e74c3c; font-weight: 900;">EXPIRED</span>';
            }
            return;
        }

        const hours = Math.floor(distance / (1000 * 60 * 60));
        const minutes = Math.floor((distance % (1000 * 60 * 60)) / (1000 * 60));
        const seconds = Math.floor((distance % (1000 * 60)) / 1000);

        const element = document.getElementById(this.elementId);
        if (element) {
            element.innerHTML = `
                <span class="product-timer-numbers">
                    ${this.padZero(hours)}:${this.padZero(minutes)}:${this.padZero(seconds)}
                </span>
            `;
        }
    }

    padZero(num) {
        return num < 10 ? '0' + num : num;
    }
}

// Add flash sale item to cart - FIXED VERSION
function addFlashSaleToCart(flashSaleItemId, productId, flashSalePrice) {
    console.log('Adding flash sale item:', { flashSaleItemId, productId, flashSalePrice });

    // Get button element
    const btn = event.target.closest('button');
    if (!btn) {
        console.error('Button not found');
        return;
    }

    // Save original state
    const originalText = btn.innerHTML;
    const originalClass = btn.className;

    // Show loading state
    btn.disabled = true;
    btn.classList.add('loading');
    btn.innerHTML = '<i class="bi bi-hourglass-split"></i> Adding...';

    // Get anti-forgery token
    const token = document.querySelector('input[name="__RequestVerificationToken"]');
    if (!token) {
        console.error('Anti-forgery token not found!');
        toastr.error('Security token missing. Please refresh the page.');
        btn.disabled = false;
        btn.className = originalClass;
        btn.innerHTML = originalText;
        return;
    }

    // Prepare form data
    const formData = new URLSearchParams();
    formData.append('productId', productId);
    formData.append('flashSaleItemId', flashSaleItemId);
    formData.append('flashSalePrice', flashSalePrice);
    formData.append('count', 1);

    console.log('Sending request with data:', Object.fromEntries(formData));

    // Send AJAX request
    fetch('/Customer/Cart/AddFlashSaleToCart', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': token.value
        },
        body: formData.toString()
    })
    .then(response => {
        console.log('Response status:', response.status);
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        return response.json();
    })
    .then(data => {
        console.log('Response data:', data);
        
        if (data.success) {
            // Success!
            toastr.success(data.message || 'Flash sale item added to cart!');
            
            // Update cart count
            if (data.cartCount !== undefined) {
                const cartCountElement = document.getElementById('cartCount');
                if (cartCountElement) {
                    cartCountElement.textContent = data.cartCount;
                    // Pulse animation
                    cartCountElement.style.animation = 'none';
                    setTimeout(() => {
                        cartCountElement.style.animation = 'cartPulse 0.6s ease';
                    }, 10);
                }
            }

            // Update button to success state
            btn.classList.remove('loading');
            btn.classList.add('success');
            btn.innerHTML = '<i class="bi bi-check-circle-fill"></i> Added!';
            
            // Reset button after 2 seconds
            setTimeout(() => {
                btn.disabled = false;
                btn.className = originalClass;
                btn.innerHTML = originalText;
            }, 2000);

        } else {
            // Error from server
            toastr.error(data.message || 'Could not add item to cart');
            btn.disabled = false;
            btn.className = originalClass;
            btn.innerHTML = originalText;
        }
    })
    .catch(error => {
        console.error('Error adding to cart:', error);
        toastr.error('An error occurred. Please try again.');
        btn.disabled = false;
        btn.className = originalClass;
        btn.innerHTML = originalText;
        openCartSidebar();

    });
}

function openCartSidebar() {
    console.log('Opening cart sidebar...');
    const sidebar = document.getElementById('cartSidebar');
    const overlay = document.getElementById('cartOverlay');

    if (sidebar && overlay) {
        sidebar.classList.add('active');
        overlay.classList.add('active');
        document.body.style.overflow = 'hidden'; // Prevent background scrolling
        console.log('Cart sidebar opened successfully');
        loadCartItems();
    } else {
        console.error('Cart sidebar or overlay not found!');
    }
}
function loadCartItems() {
    console.log('loadCartItems called, isAuthenticated:', isAuthenticated);

    console.log('Fetching cart items from server...');
    fetch('/Customer/Cart/GetCartItems')
        .then(response => {
            console.log('Cart items response received:', response.status);
            return response.json();
        })
        .then(data => {
            console.log('Cart items data:', data);
            const container = document.getElementById('cartItemsContainer');
            const countElement = document.getElementById('sidebarCartCount');
            const subtotalElement = document.getElementById('cartSubtotal');

            if (data.items && data.items.length > 0) {
                container.innerHTML = data.items.map(item => createCartItemHTML(item)).join('');
                countElement.textContent = data.items.length;
                subtotalElement.textContent = formatCurrency(data.subtotal);

                // Update floating cart badge
                updateFloatingCartBadge(data.items.length);
                console.log('Cart items loaded successfully:', data.items.length, 'items');
            } else {
                container.innerHTML = '<div class="cart-empty"><i class="bi bi-cart-x"></i><p>Your cart is empty</p></div>';
                countElement.textContent = '0';
                subtotalElement.textContent = 'AED 0.00';

                // Update floating cart badge
                updateFloatingCartBadge(0);
                console.log('Cart is empty');
            }
        })
        .catch(error => {
            console.error('Error loading cart items:', error);
        });
}

// Initialize all flash sale timers
function initFlashSaleTimers() {
    // Main hero timer
    const heroTimer = document.querySelector('[data-flash-sale-end]');
    if (heroTimer && heroTimer.id) {
        const endDate = heroTimer.getAttribute('data-flash-sale-end');
        const timerId = heroTimer.id;
        const timer = new FlashSaleTimer(endDate, timerId, () => {
            location.reload();
        });
        timer.start();
        console.log('Main timer initialized');
    }

    // Product timers
    document.querySelectorAll('[data-product-timer-end]').forEach(timerElement => {
        if (timerElement.id) {
            const endDate = timerElement.getAttribute('data-product-timer-end');
            const timerId = timerElement.id;
            const timer = new ProductFlashTimer(endDate, timerId);
            timer.start();
        }
    });
    console.log('Product timers initialized');
}

// Update stock progress bars
function updateStockProgressBars() {
    document.querySelectorAll('[data-stock-percentage]').forEach(bar => {
        const percentage = parseFloat(bar.getAttribute('data-stock-percentage'));
        const fillElement = bar.querySelector('.stock-progress-bar-fill');
        if (fillElement) {
            // Animate to percentage
            setTimeout(() => {
                fillElement.style.width = percentage + '%';
            }, 100);
            
            // Change color based on stock level
            if (percentage < 20) {
                fillElement.style.background = 'linear-gradient(90deg, #e74c3c, #c0392b)';
            } else if (percentage < 50) {
                fillElement.style.background = 'linear-gradient(90deg, #f39c12, #e67e22)';
            }
        }
    });
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    console.log('Flash Sale System Initializing...');
    
    initFlashSaleTimers();
    updateStockProgressBars();
    
    console.log('🔥 Flash Sale System Ready!');
});

// Add cart pulse animation
const style = document.createElement('style');
style.textContent = `
    @keyframes cartPulse {
        0%, 100% { transform: scale(1); }
        50% { transform: scale(1.3); }
    }
`;
document.head.appendChild(style);




