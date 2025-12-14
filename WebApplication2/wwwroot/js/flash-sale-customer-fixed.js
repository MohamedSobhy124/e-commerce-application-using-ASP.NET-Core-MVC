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
        const errorMsg = window.localizations?.securityTokenMissing || window.localizations?.errorOccurred || 'Security token missing. Please refresh the page.';
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
            
            // Update cart count everywhere (works on all screens)
            if (data.cartCount !== undefined) {
                // Update navigation cart count
                const cartCountElement = document.getElementById('cartCount');
                if (cartCountElement) {
                    cartCountElement.textContent = data.cartCount;
                    // Pulse animation
                    cartCountElement.style.animation = 'none';
                    setTimeout(() => {
                        cartCountElement.style.animation = 'cartPulse 0.6s ease';
                    }, 10);
                }
                
                // Update header cart badge
                const headerCartBadge = document.getElementById('headerCartBadge');
                if (headerCartBadge) {
                    if (data.cartCount > 0) {
                        headerCartBadge.textContent = data.cartCount;
                        headerCartBadge.style.display = 'flex';
                    } else {
                        headerCartBadge.style.display = 'none';
                    }
                }
                
                // Update mobile bottom nav cart badge
                if (typeof window.updateFloatingCartBadge === 'function') {
                    window.updateFloatingCartBadge(data.cartCount);
                } else if (typeof updateFloatingCartBadge === 'function') {
                    updateFloatingCartBadge(data.cartCount);
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
            const errorMsg = data.message || window.localizations?.couldNotAddItemToCart || window.localizations?.failedToAddToCart || 'Could not add item to cart';
            toastr.error(errorMsg);
            btn.disabled = false;
            btn.className = originalClass;
            btn.innerHTML = originalText;
        }
    })
    .catch(error => {
        console.error('Error adding to cart:', error);
        const errorMsg = window.localizations?.errorOccurred || window.localizations?.anErrorOccurredPleaseTryAgain || 'An error occurred. Please try again.';
        toastr.error(errorMsg);
        btn.disabled = false;
        btn.className = originalClass;
        btn.innerHTML = originalText;
        openCartSidebar();

    });
}

function openCartSidebar(forceOpen = false) {
    // Check if on mobile and home page - don't open cart sidebar automatically
    // But allow if forceOpen is true (user clicked the cart icon)
    if (!forceOpen) {
        const isMobile = window.innerWidth <= 768;
        const currentPath = window.location.pathname.toLowerCase();
        const isHomePage = currentPath === '/' || 
                          currentPath === '/customer/home' || 
                          currentPath === '/customer/home/index' ||
                          currentPath.startsWith('/customer/home/index');
        
        if (isMobile && isHomePage) {
            console.log('Cart sidebar disabled on mobile home screen');
            return;
        }
    }
    
    console.log('Opening cart sidebar...');
    const sidebar = document.querySelector('.cart-sidebar-enhanced') || document.getElementById('cartSidebar');
    const overlay = document.querySelector('.cart-overlay-enhanced') || document.getElementById('cartOverlay');

    if (sidebar && overlay) {
        sidebar.classList.add('active');
        overlay.classList.add('active');
        document.body.style.overflow = 'hidden'; // Prevent background scrolling
        console.log('Cart sidebar opened successfully');
        
        // Always load cart items when sidebar opens (on all pages)
        // Use a small delay to ensure sidebar is fully visible
        setTimeout(function() {
            console.log('Loading cart items...');
            // Load cart items if function is available (check global first, then local)
            if (typeof window.loadCartItems === 'function') {
                console.log('Calling window.loadCartItems');
                window.loadCartItems();
            } else if (typeof loadCartItems === 'function') {
                console.log('Calling local loadCartItems');
                loadCartItems();
            } else {
                console.error('loadCartItems function not available! Cart items will not load.');
                // Try to fetch cart items directly as last resort
                fetch('/Customer/Cart/GetCartItems')
                    .then(response => {
                        if (response.ok) {
                            return response.json();
                        }
                        throw new Error('Failed to fetch cart items');
                    })
                    .then(data => {
                        console.log('Cart items fetched directly:', data);
                        const container = document.getElementById('cartItemsContainer');
                        if (container && data.items) {
                            if (data.items.length > 0) {
                                container.innerHTML = '<p>Cart items loaded. Please refresh the page.</p>';
                            } else {
                                const emptyState = document.getElementById('cartEmptyState');
                                if (emptyState) {
                                    emptyState.classList.remove('hidden');
                                }
                            }
                        }
                    })
                    .catch(error => {
                        console.error('Failed to fetch cart items directly:', error);
                    });
            }
        }, 100);
    } else {
        console.error('Cart sidebar or overlay not found!', {
            sidebar: !!sidebar,
            overlay: !!overlay
        });
    }
}

// Make it globally available
window.openCartSidebar = openCartSidebar;
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
// Helper function to check if we're on home page
function isHomePage() {
    const currentPath = window.location.pathname.toLowerCase();
    return currentPath === '/' || 
           currentPath === '/customer/home' || 
           currentPath === '/customer/home/index' ||
           currentPath.startsWith('/customer/home/index') ||
           currentPath === '/home' ||
           currentPath === '/home/index';
}

function createCartItemHTML(item) {
    const imageUrl = getProductImageUrl(item.imageUrl);
    const price = formatCurrency(item.price);
    const total = formatCurrency(item.price * item.count);
    
    // Get available stock (flash sale quantity, variant stock, or product stock)
    const availableStock = item.availableStock || 0;
    const currentCount = item.count || 1;
    const canIncrease = currentCount < availableStock;
    const canDecrease = currentCount > 1;
    
    // Determine stock message
    let stockMessage = '';
    if (item.isFlashSale && item.flashSaleQuantity !== null && item.flashSaleQuantity !== undefined) {
        if (item.flashSaleQuantity === 0) {
            stockMessage = '<small class="text-danger d-block mt-1" style="font-size: 0.7rem;">⚡ Flash sale sold out</small>';
        } else if (currentCount >= item.flashSaleQuantity) {
            stockMessage = `<small class="text-warning d-block mt-1" style="font-size: 0.7rem;">⚡ Only ${item.flashSaleQuantity} available in flash sale</small>`;
        }
    } else if (availableStock > 0 && currentCount >= availableStock) {
        stockMessage = `<small class="text-warning d-block mt-1" style="font-size: 0.7rem;">Only ${availableStock} available in stock</small>`;
    } else if (availableStock === 0) {
        stockMessage = '<small class="text-danger d-block mt-1" style="font-size: 0.7rem;">Out of stock</small>';
    }
    
    // Quantity controls with validation
    const quantityControls = `
        <div class="cart-item-quantity-controls">
            <button class="btn-quantity btn-quantity-minus ${!canDecrease ? 'disabled' : ''}" 
                    onclick="${canDecrease ? `updateCartQuantityFromSidebar(${item.productId}, ${item.count - 1}, ${item.cartId || 'null'})` : 'return false;'}" 
                    title="${canDecrease ? 'Decrease' : 'Minimum quantity reached'}"
                    ${!canDecrease ? 'disabled style="opacity: 0.5; cursor: not-allowed;"' : ''}>
                <i class="bi bi-dash"></i>
            </button>
            <span class="cart-item-quantity-value">${item.count}</span>
            <button class="btn-quantity btn-quantity-plus ${!canIncrease ? 'disabled' : ''}" 
                    onclick="${canIncrease ? `updateCartQuantityFromSidebar(${item.productId}, ${item.count + 1}, ${item.cartId || 'null'})` : 'return false;'}" 
                    title="${canIncrease ? (window.localizations?.increase || 'Increase') : (window.localizations?.maximumStockReached || 'Maximum stock reached')}"
                    ${!canIncrease ? 'disabled style="opacity: 0.5; cursor: not-allowed;"' : ''}>
                <i class="bi bi-plus"></i>
            </button>
        </div>
        ${stockMessage}
    `;
    
    return `
        <div class="cart-sidebar-item" data-cart-id="${item.cartId || ''}" data-product-id="${item.productId}">
            <div class="cart-item-image-wrapper">
                <img src="${imageUrl}" alt="${item.title}" class="cart-sidebar-item-image" 
                     onerror="this.src='/images/no-image.png'">
            </div>
            <div class="cart-item-details">
                <h6 class="cart-item-title">${item.title}</h6>
                ${item.variantName ? `<p class="cart-item-variant">${item.variantName}</p>` : ''}
                <div class="cart-item-meta">
                    ${quantityControls}
                    <span class="cart-item-price">${total}</span>
                </div>
                ${(item.isFlashSale || item.isComboOffer) ? `
                <div style="margin-top: 0.5rem; display: flex; gap: 0.5rem; flex-wrap: wrap;">
                    ${item.isFlashSale ? `<span class="badge bg-danger" style="font-size: 0.7rem; padding: 0.25rem 0.5rem;">⚡ ${window.localizations?.flashSale || 'Flash Sale'}</span>` : ''}
                    ${item.isComboOffer ? `<span class="badge bg-warning text-dark" style="font-size: 0.7rem; padding: 0.25rem 0.5rem;">📦 ${window.localizations?.combo || 'Combo'}</span>` : ''}
                </div>
                ` : ''}
            </div>
            <button class="cart-item-remove" onclick="removeCartItem(${item.productId}, ${item.cartId || 'null'})" title="${window.localizations?.remove || 'Remove'}" aria-label="${window.localizations?.remove || 'Remove'} item">
                <i class="bi bi-trash"></i>
            </button>
        </div>
    `;
}

// Make createCartItemHTML globally available so it's used everywhere
// Only set if not already set (to prevent overwriting with a recursive version)
if (!window.createCartItemHTML) {
    window.createCartItemHTML = createCartItemHTML;
} else {
    // If already set, make sure it's not the same function (prevent recursion)
    const existingFunc = window.createCartItemHTML;
    if (existingFunc !== createCartItemHTML && typeof existingFunc === 'function') {
        // Keep the existing one if it's different, otherwise use ours
        // But in this case, we want to use ours from flash-sale-customer-fixed.js
        window.createCartItemHTML = createCartItemHTML;
    }
}

// Function to update cart quantity from sidebar (for home page only)
function updateCartQuantityFromSidebar(productId, newCount, cartId) {
    if (newCount < 1) {
        removeCartItem(productId, cartId);
        return;
    }

    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    const formData = new FormData();
    formData.append('productId', productId);
    formData.append('count', newCount);
    formData.append('__RequestVerificationToken', token);

    fetch('/Customer/Cart/UpdateQuantity', {
        method: 'POST',
        body: formData
    })
    .then(response => {
        return response.json().then(data => {
            if (!response.ok) {
                throw new Error(data.message || window.localizations?.failedToUpdateQuantity || 'Failed to update quantity');
            }
            return data;
        }).catch(err => {
            // If JSON parsing fails, throw a generic error
            if (!response.ok) {
                throw new Error(window.localizations?.failedToUpdateQuantity || 'Failed to update quantity. Please try again.');
            }
            throw err;
        });
    })
    .then(data => {
        if (data.success) {
            // Show success message
            if (typeof toastr !== 'undefined') {
                const successMsg = data.message || window.localizations?.quantityUpdated || 'Quantity updated successfully';
                toastr.success(successMsg, '', {
                    timeOut: 2000
                });
            }
            
            // Reload cart items
            if (typeof window.loadCartItems === 'function') {
                window.loadCartItems();
            } else if (typeof loadCartItems === 'function') {
                loadCartItems();
            }
            // Update cart widget if available
            if (typeof updateCartWidget === 'function') {
                updateCartWidget();
            }
            // Update floating badge
            if (typeof updateFloatingCartBadge === 'function') {
                updateFloatingCartBadge(data.cartCount || 0);
            } else if (typeof window.updateFloatingCartBadge === 'function') {
                window.updateFloatingCartBadge(data.cartCount || 0);
            }
        } else {
            // Show validation error message
            const errorMsg = data.message || window.localizations?.failedToUpdateQuantity || 'Failed to update quantity';
            throw new Error(errorMsg);
        }
    })
    .catch(error => {
        console.error('Error updating quantity:', error);
        // Show the actual error message from the server
        const errorMsg = error.message || window.localizations?.failedToUpdateQuantity || window.localizations?.failedToUpdateCart || 'Failed to update quantity. Please try again.';
        if (typeof toastr !== 'undefined') {
            toastr.error(errorMsg, '', {
                timeOut: 5000,
                extendedTimeOut: 3000,
                closeButton: true
            });
        } else {
            alert(errorMsg);
        }
    });
}

// Make it globally available
window.updateCartQuantityFromSidebar = updateCartQuantityFromSidebar;

// Remove item from cart
function removeCartItem(productId, cartId) {
    console.log('Removing cart item:', { productId, cartId });
    
    // Get anti-forgery token
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    
    // Build FormData with parameters
    const formData = new FormData();
    if (cartId && cartId !== 'null' && cartId !== null) {
        formData.append('CartId', cartId);
    }
    if (productId) {
        formData.append('ProductId', productId);
    } else {
        console.error('Cannot remove item: missing productId');
        if (typeof toastr !== 'undefined') {
            toastr.error('Cannot remove item: missing product information');
        }
        return;
    }
    
    if (token) {
        formData.append('__RequestVerificationToken', token);
    }

    fetch('/Customer/Cart/Remove', {
        method: 'POST',
        body: formData,
        headers: {
            'X-Requested-With': 'XMLHttpRequest'
        }
    })
    .then(response => {
        if (response.ok) {
            return response.json();
        } else {
            throw new Error(window.localizations?.failedToRemoveItem || 'Failed to remove item');
        }
    })
    .then(data => {
        // Reload cart items (use global if available)
        if (typeof window.loadCartItems === 'function') {
            window.loadCartItems();
        } else if (typeof loadCartItems === 'function') {
            loadCartItems();
        }
        // Update cart widget if available
        if (typeof updateCartWidget === 'function') {
            updateCartWidget();
        }
        // Update floating badge
        if (typeof updateFloatingCartBadge === 'function') {
            updateFloatingCartBadge(data.cartCount || 0);
        } else if (typeof window.updateFloatingCartBadge === 'function') {
            window.updateFloatingCartBadge(data.cartCount || 0);
        }
        // Show success message
        if (typeof toastr !== 'undefined') {
            const successMsg = window.localizations?.itemRemovedFromCart || data.message || 'Item removed from cart';
            toastr.success(successMsg);
        }
    })
    .catch(error => {
        console.error('Error removing cart item:', error);
        if (typeof toastr !== 'undefined') {
            const errorMsg = window.localizations?.failedToRemoveItem || window.localizations?.failedToUpdateCart || 'Failed to remove item. Please try again.';
            toastr.error(errorMsg);
        }
    });
}

// Make removeCartItem globally available
window.removeCartItem = removeCartItem;

function loadCartItems() {
    // If global loadCartItems exists (from Home page), use it
    if (window.loadCartItems && window.loadCartItems !== loadCartItems) {
        console.log('Using global loadCartItems from Home page');
        window.loadCartItems();
        return;
    }
    
    // Get isAuthenticated - try multiple sources
    let isAuthenticated = false;
    if (typeof window.isAuthenticated !== 'undefined') {
        isAuthenticated = window.isAuthenticated;
    } else if (typeof isAuthenticated !== 'undefined') {
        isAuthenticated = isAuthenticated;
    } else {
        // Fallback: try to determine from DOM
        const logoutLink = document.querySelector('a[href*="Logout"], a[href*="logout"]');
        const loginLink = document.querySelector('a[href*="Login"], a[href*="login"]');
        isAuthenticated = !!logoutLink && !loginLink;
        // Cache it globally for future use
        window.isAuthenticated = isAuthenticated;
    }
    console.log('loadCartItems called, isAuthenticated:', isAuthenticated);

    console.log('Fetching cart items from server...');
    fetch('/Customer/Cart/GetCartItems')
        .then(response => {
            console.log('Cart items response received:', response.status);
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            return response.json();
        })
        .then(data => {
            console.log('Cart items data:', data);
            const container = document.getElementById('cartItemsContainer');
            const countElement = document.getElementById('sidebarCartCount');
            const subtotalElement = document.getElementById('cartSubtotal');
            const emptyState = document.getElementById('cartEmptyState');

            if (!container || !countElement || !subtotalElement) {
                console.error('Cart sidebar elements not found');
                return;
            }

            // Hide/show empty state
            if (emptyState) {
                if (data.items && data.items.length > 0) {
                    emptyState.classList.add('hidden');
                } else {
                    emptyState.classList.remove('hidden');
                }
            }
            
            if (data.items && data.items.length > 0) {
                // Use global createCartItemHTML if available, otherwise use local one
                const createItemHTML = window.createCartItemHTML || createCartItemHTML;
                if (!createItemHTML) {
                    console.error('createCartItemHTML function not found');
                    if (emptyState) emptyState.classList.remove('hidden');
                    return;
                }
                
                container.innerHTML = data.items.map(item => createItemHTML(item)).join('');
                countElement.textContent = data.items.length;
                subtotalElement.textContent = formatCurrency(data.subtotal);

                // Update floating cart badge
                if (typeof updateFloatingCartBadge === 'function') {
                    updateFloatingCartBadge(data.items.length);
                } else if (typeof window.updateFloatingCartBadge === 'function') {
                    window.updateFloatingCartBadge(data.items.length);
                }
                console.log('Cart items loaded successfully:', data.items.length, 'items');
            } else {
                container.innerHTML = '';
                countElement.textContent = '0';
                subtotalElement.textContent = formatCurrency(0);

                // Update floating cart badge
                if (typeof updateFloatingCartBadge === 'function') {
                    updateFloatingCartBadge(0);
                } else if (typeof window.updateFloatingCartBadge === 'function') {
                    window.updateFloatingCartBadge(0);
                }
                console.log('Cart is empty');
            }
        })
        .catch(error => {
            console.error('Error loading cart items:', error);
            const container = document.getElementById('cartItemsContainer');
            const emptyState = document.getElementById('cartEmptyState');
            if (container) {
                container.innerHTML = '';
            }
            if (emptyState) {
                emptyState.classList.remove('hidden');
            }
        });
}

// Make loadCartItems globally available (always use the one from flash-sale-customer-fixed.js as fallback)
// This ensures it's available on all pages, not just Home/Offer
if (!window.loadCartItems || window.loadCartItems === loadCartItems) {
    window.loadCartItems = loadCartItems;
} else {
    // If Home page has its own version, keep it but also keep this as fallback
    // Store the flash-sale version as backup
    window.loadCartItemsFallback = loadCartItems;
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




