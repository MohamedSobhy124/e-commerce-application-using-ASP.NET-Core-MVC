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
        const errorMsg = window.localizations?.errorOccurred || 'Security token missing. Please refresh the page.';
        toastr.error(errorMsg);
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
            const successMsg = data.message || (window.localizations?.flashSaleAddedToCart || 'Flash sale item added to cart!');
            toastr.success(successMsg);
            
            // Update cart count
            if (data.cartCount !== undefined) {
                const cartCountElement = document.getElementById('cartCount');
                const headerCartBadgeElement = document.getElementById('headerCartBadge');
                if (cartCountElement) {
                    cartCountElement.textContent = data.cartCount;
                    // Pulse animation
                    cartCountElement.style.animation = 'none';
                    setTimeout(() => {
                        cartCountElement.style.animation = 'cartPulse 0.6s ease';
                    }, 10);
                }
                if (headerCartBadgeElement) {
                    headerCartBadgeElement.textContent = data.cartCount;
                    // Pulse animation
                    headerCartBadgeElement.style.animation = 'none';
                    setTimeout(() => {
                        headerCartBadgeElement.style.animation = 'cartPulse 0.6s ease';
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
            const errorMsg = data.message || (window.localizations?.couldNotAddItemToCart || 'Could not add item to cart');
            toastr.error(errorMsg);
            btn.disabled = false;
            btn.className = originalClass;
            btn.innerHTML = originalText;
        }
    })
    .catch(error => {
        console.error('Error adding to cart:', error);
        const errorMsg = window.localizations?.errorOccurred || 'An error occurred. Please try again.';
        toastr.error(errorMsg);
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
// Helper function to get currency symbol based on language
function getCurrencySymbol() {
    // Try to get from cookie if language-switcher.js is loaded
    if (typeof getCurrentLanguage === 'function') {
        const lang = getCurrentLanguage();
        return lang === 'ar' ? 'د.إ' : 'AED';
    }
    // Default to Arabic if we can't determine
    return 'د.إ';
}

// Helper function to format currency
function formatCurrency(amount) {
    const symbol = getCurrencySymbol();
    return symbol + ' ' + amount.toFixed(2);
}

// Helper function to get product image URL (handles null/empty images)
function getProductImageUrl(imageUrl) {
    if (!imageUrl || imageUrl.trim() === '') {
        return '/images/no-image.png'; // Default placeholder image
    }
    return imageUrl;
}

// Create HTML for a cart item in the sidebar
function createCartItemHTML(item) {
    const imageUrl = getProductImageUrl(item.imageUrl);
    const price = formatCurrency(item.price);
    const total = formatCurrency(item.price * item.count);
    
    return `
        <div class="cart-sidebar-item" data-cart-id="${item.cartId || ''}" data-product-id="${item.productId}">
            <div class="cart-item-image-wrapper">
                <img src="${imageUrl}" alt="${item.title}" class="cart-sidebar-item-image" 
                     onerror="this.src='/images/no-image.png'">
            </div>
            <div class="cart-item-details">
                <h6 class="cart-item-title">${item.title}</h6>
                ${item.variantName ? `<p class="cart-item-variant" style="font-size: 0.85rem; color: #666; margin-top: 0.25rem; margin-bottom: 0.25rem;">${item.variantName}</p>` : ''}
                <div class="cart-item-meta">
                    <span class="cart-item-quantity">Qty: ${item.count}</span>
                    <span class="cart-item-price">${price}</span>
                </div>
                <div style="margin-top: 0.25rem;">
                    ${item.isFlashSale ? '<span class="badge bg-danger" style="font-size: 0.7rem; margin-right: 0.25rem;">Flash Sale</span>' : ''}
                    ${item.isComboOffer ? '<span class="badge bg-warning text-dark" style="font-size: 0.7rem;">Combo Offer</span>' : ''}
                </div>
            </div>
            <button class="cart-item-remove" onclick="removeCartItem(${item.productId}, ${item.cartId || 'null'})" title="Remove">
                <i class="bi bi-x-lg"></i>
            </button>
        </div>
    `;
}

// Remove item from cart
function removeCartItem(productId, cartId) {
    console.log('Removing cart item:', { productId, cartId });
    
    // Build URL based on whether we have cartId
    let url = '/Customer/Cart/Remove';
    if (cartId && cartId !== 'null') {
        url += `?CartId=${cartId}&ProductId=${productId}`;
    } else if (productId) {
        url += `?ProductId=${productId}`;
    } else {
        console.error('Cannot remove item: missing productId');
        return;
    }

    fetch(url, {
        method: 'GET',
        headers: {
            'X-Requested-With': 'XMLHttpRequest'
        }
    })
    .then(response => {
        if (response.ok) {
            // Reload cart items
            loadCartItems();
            // Show success message
            if (typeof toastr !== 'undefined') {
                const successMsg = window.localizations?.itemRemovedFromCart || 'Item removed from cart';
                toastr.success(successMsg);
            }
        } else {
            throw new Error('Failed to remove item');
        }
    })
    .catch(error => {
        console.error('Error removing cart item:', error);
        if (typeof toastr !== 'undefined') {
            const errorMsg = window.localizations?.failedToUpdateCart || 'Failed to remove item. Please try again.';
            toastr.error(errorMsg);
        }
    });
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




