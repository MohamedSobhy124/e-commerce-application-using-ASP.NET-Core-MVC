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
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        return response.json();
    })
    .then(data => {
        
        if (data.success) {
            // Success!
            const successMsg = data.message || (window.localizations?.flashSaleAddedToCart || 'Flash sale item added to cart!');
            toastr.success(successMsg);
            
            // Sync with localStorage
            if (window.cartStorage) {
                window.cartStorage.addItem({
                    productId: productId,
                    count: 1,
                    flashSaleItemId: flashSaleItemId,
                    flashSalePrice: flashSalePrice
                });
            }
            
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
    // Always allow cart sidebar to open when user clicks (forceOpen = true)
    // Only prevent automatic opens if needed (but allow manual opens)
    const sidebar = document.querySelector('.cart-sidebar-new') || document.getElementById('cartSidebar');
    const overlay = document.querySelector('.cart-sidebar-overlay') || document.getElementById('cartOverlay');

    if (sidebar && overlay) {
        sidebar.classList.add('active');
        overlay.classList.add('active');
        document.body.style.overflow = 'hidden'; // Prevent background scrolling
        // Always load cart items when sidebar opens (on all pages)
        // Use a small delay to ensure sidebar is fully visible
        setTimeout(function() {
            // Load cart items if function is available (check global first, then local)
            if (typeof window.loadCartItems === 'function') {
                window.loadCartItems();
            } else if (typeof loadCartItems === 'function') {
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
    
    // Build product details URL
    const productDetailsUrl = item.productSlug 
        ? `/Customer/Home/Details/${item.productSlug}` 
        : `/Customer/Home/Details/${item.productId}`;
    
    // Ensure productId is always valid (never 0 or empty)
    const validProductId = item.productId && item.productId > 0 ? item.productId : null;
    const validCartId = item.cartId && item.cartId > 0 ? item.cartId : null;
    
    if (!validProductId) {
        console.error('Invalid productId in createCartItemHTML:', { item, productId: item.productId });
        return ''; // Return empty string if productId is invalid
    }
    
    return `
        <div class="cart-sidebar-item" data-cart-id="${validCartId || ''}" data-product-id="${validProductId}">
            <a href="${productDetailsUrl}" class="cart-item-image-wrapper" style="text-decoration: none; cursor: pointer;">
                <img src="${imageUrl}" alt="${item.title}" class="cart-sidebar-item-image" 
                     onerror="this.src='/images/no-image.png'">
            </a>
            <div class="cart-item-details">
                <a href="${productDetailsUrl}" class="cart-item-title-link" style="text-decoration: none; color: inherit; display: block;">
                    <h6 class="cart-item-title">${item.title}</h6>
                </a>
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
            <button class="cart-item-remove" title="${window.localizations?.remove || 'Remove'}" aria-label="${window.localizations?.remove || 'Remove'} item" data-product-id="${validProductId}" data-cart-id="${validCartId || ''}">
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
        removeCartItemFromSidebar(productId, cartId);
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
function removeCartItemFromSidebar(productId, cartId) {
    // Validate productId
    if (!productId || productId === 0 || productId === '0' || productId === 'null' || productId === null || productId === undefined) {
        console.error('Cannot remove item: missing productId', { productId, cartId });
        const isArabic = typeof getCurrentLanguage === 'function' && getCurrentLanguage() === 'ar';
        const errorMsg = isArabic ? 'لا يمكن إزالة العنصر: معلومات المنتج مفقودة' : 'Cannot remove item: missing product information';
        if (typeof toastr !== 'undefined') {
            toastr.error(errorMsg);
        } else if (typeof Swal !== 'undefined' && typeof Swal.fire === 'function') {
            Swal.fire({
                icon: 'error',
                title: isArabic ? 'خطأ' : 'Error',
                text: errorMsg
            });
        }
        return;
    }
    
    // Convert to number if string
    productId = parseInt(productId);
    
    // Final validation after parsing
    if (isNaN(productId) || productId <= 0) {
        console.error('Cannot remove item: invalid productId', { productId, cartId, originalProductId: arguments[0] });
        const isArabic = typeof getCurrentLanguage === 'function' && getCurrentLanguage() === 'ar';
        const errorMsg = isArabic ? 'لا يمكن إزالة العنصر: معرف المنتج غير صحيح' : 'Cannot remove item: invalid product ID';
        if (typeof toastr !== 'undefined') {
            toastr.error(errorMsg);
        } else if (typeof Swal !== 'undefined' && typeof Swal.fire === 'function') {
            Swal.fire({
                icon: 'error',
                title: isArabic ? 'خطأ' : 'Error',
                text: errorMsg
            });
        }
        return;
    }
    
    // Show SweetAlert confirmation instead of browser confirm
    if (typeof Swal !== 'undefined' && typeof Swal.fire === 'function') {
        const isArabic = typeof getCurrentLanguage === 'function' && getCurrentLanguage() === 'ar';
        Swal.fire({
            title: isArabic ? 'تأكيد الحذف' : 'Confirm Removal',
            text: isArabic ? 'هل أنت متأكد من إزالة هذا العنصر من السلة؟' : 'Are you sure you want to remove this item from your cart?',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#d33',
            cancelButtonColor: '#3085d6',
            confirmButtonText: isArabic ? 'نعم، احذف' : 'Yes, remove it',
            cancelButtonText: isArabic ? 'إلغاء' : 'Cancel',
            reverseButtons: true
        }).then((result) => {
            if (result.isConfirmed) {
                performRemoveCartItem(productId, cartId);
            }
        });
    } else {
        // SweetAlert not available - show error and don't proceed
        console.error('SweetAlert not available. Please refresh the page.');
        const isArabic = typeof getCurrentLanguage === 'function' && getCurrentLanguage() === 'ar';
        const errorMsg = isArabic 
            ? 'خطأ في تحميل المكتبة. يرجى تحديث الصفحة والمحاولة مرة أخرى.' 
            : 'Error loading library. Please refresh the page and try again.';
        if (typeof toastr !== 'undefined') {
            toastr.error(errorMsg);
        } else {
            alert(errorMsg);
        }
    }
}

function performRemoveCartItem(productId, cartId) {
    // Get anti-forgery token
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    
    // Build FormData with parameters
    const formData = new FormData();
    if (cartId && cartId !== 'null' && cartId !== null && cartId !== 'undefined' && cartId !== undefined) {
        formData.append('CartId', cartId);
    }
    formData.append('ProductId', productId);
    
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

// Make removeCartItemFromSidebar globally available
window.removeCartItemFromSidebar = removeCartItemFromSidebar;
// Keep removeCartItem as alias for backward compatibility (but use correct function)
window.removeCartItem = removeCartItemFromSidebar;

// Add event delegation for cart-item-remove buttons (better than onclick)
// This ensures productId is always read from data attributes
(function() {
    function initCartRemoveHandler() {
        // Remove any existing handlers to avoid duplicates
        document.removeEventListener('click', handleCartRemoveClick);
        // Add event delegation for dynamically added cart items
        document.addEventListener('click', handleCartRemoveClick);
    }
    
    function handleCartRemoveClick(e) {
        const removeBtn = e.target.closest('.cart-item-remove');
        if (!removeBtn) return;
        
        e.preventDefault();
        e.stopPropagation();
        
        // Always read from data attributes (most reliable)
        const productId = removeBtn.getAttribute('data-product-id') || removeBtn.dataset?.productId;
        const cartId = removeBtn.getAttribute('data-cart-id') || removeBtn.dataset?.cartId;
        
        console.log('Remove button clicked:', { productId, cartId, btn: removeBtn });
        
        // Validate productId
        if (!productId || productId === '' || productId === 'null' || productId === '0') {
            console.error('Cannot remove item: missing productId in data attribute', { productId, cartId, btn: removeBtn, allAttributes: Array.from(removeBtn.attributes).map(a => `${a.name}=${a.value}`) });
            const isArabic = typeof getCurrentLanguage === 'function' && getCurrentLanguage() === 'ar';
            const errorMsg = isArabic ? 'لا يمكن إزالة العنصر: معلومات المنتج مفقودة' : 'Cannot remove item: missing product information';
            if (typeof toastr !== 'undefined') {
                toastr.error(errorMsg);
            } else if (typeof Swal !== 'undefined' && typeof Swal.fire === 'function') {
                Swal.fire({
                    icon: 'error',
                    title: isArabic ? 'خطأ' : 'Error',
                    text: errorMsg
                });
            }
            return;
        }
        
        // Parse productId and cartId
        const parsedProductId = parseInt(productId);
        const parsedCartId = cartId && cartId !== 'null' && cartId !== '' && cartId !== 'undefined' ? parseInt(cartId) : null;
        
        if (isNaN(parsedProductId) || parsedProductId <= 0) {
            console.error('Cannot remove item: invalid productId', { productId, parsedProductId, cartId });
            const isArabic = typeof getCurrentLanguage === 'function' && getCurrentLanguage() === 'ar';
            const errorMsg = isArabic ? 'لا يمكن إزالة العنصر: معرف المنتج غير صحيح' : 'Cannot remove item: invalid product ID';
            if (typeof toastr !== 'undefined') {
                toastr.error(errorMsg);
            }
            return;
        }
        
        console.log('Calling removeCartItemFromSidebar with:', { parsedProductId, parsedCartId });
        // Call removeCartItemFromSidebar with correct parameters
        removeCartItemFromSidebar(parsedProductId, parsedCartId);
    }
    
    // Initialize on DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initCartRemoveHandler);
    } else {
        initCartRemoveHandler();
    }
})();

function loadCartItems() {
    // If global loadCartItems exists (from Home page), use it
    if (window.loadCartItems && window.loadCartItems !== loadCartItems) {
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

    fetch('/Customer/Cart/GetCartItems')
        .then(response => {
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            return response.json();
        })
        .then(data => {
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
    
    initFlashSaleTimers();
    updateStockProgressBars();
    
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

// ============================================
// TOGGLE CART FUNCTION - Global availability
// ============================================
// Make toggleCart globally available for partial views
if (typeof window.toggleCart === 'undefined') {
    window.toggleCart = function(productId, button) {
        // Prevent multiple clicks
        if (!button || button.disabled || button.classList.contains('loading')) {
            return;
        }
        
        const isCurrentlyInCart = button && button.classList.contains('in-cart');
        const originalIcon = button && button.querySelector('i') ? button.querySelector('i').className : '';
        const originalText = button.innerHTML;
        
        // Save scroll position if function exists
        if (typeof saveScrollPosition === 'function') {
            saveScrollPosition();
        }
        
        if (button) {
            // Disable button and show loading state ONLY in button
            button.disabled = true;
            button.classList.add('loading');
            button.style.opacity = '0.7';
            button.style.cursor = 'wait';
            
            const icon = button.querySelector('i');
            if (icon) {
                icon.className = 'bi bi-arrow-repeat';
                icon.style.animation = 'spin 1s linear infinite';
            }
        }

        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        const formData = new FormData();
        formData.append('productId', productId);
        if (token) {
            formData.append('__RequestVerificationToken', token);
        }

        fetch('/Customer/Home/ToggleCart', {
            method: 'POST',
            body: formData
        })
        .then(response => response.json())
        .then(data => {
            if (data.success && button) {
                // Remove loading state from button only
                button.disabled = false;
                button.classList.remove('loading');
                button.style.opacity = '';
                button.style.cursor = '';
                const icon = button.querySelector('i');
                if (icon) {
                    icon.style.animation = '';
                }
                
                if (data.isAdded) {
                    // Product was added
                    button.classList.add('in-cart');
                    if (icon) {
                        icon.className = 'bi bi-check-lg';
                    }
                    
                    // Get localizations from window or use defaults
                    const localizations = window.localizations || {};
                    button.title = localizations.removeFromCart || 'Remove from Cart';
                    
                    // Update cart product IDs if available
                    if (typeof window.cartProductIds !== 'undefined' && Array.isArray(window.cartProductIds)) {
                        if (!window.cartProductIds.includes(productId)) {
                            window.cartProductIds.push(productId);
                        }
                    }
                    
                    // Show notification
                    if (typeof toastr !== 'undefined') {
                        toastr.success(data.message);
                    }
                    
                    // Update cart count everywhere
                    if (typeof window.updateFloatingCartBadge === 'function') {
                        window.updateFloatingCartBadge(data.cartCount);
                    } else if (typeof updateFloatingCartBadge === 'function') {
                        updateFloatingCartBadge(data.cartCount);
                    }
                    
                    // Update navigation cart count
                    if (typeof window.updateNavigationCartCount === 'function') {
                        window.updateNavigationCartCount(data.cartCount);
                    } else if (typeof updateNavigationCartCount === 'function') {
                        updateNavigationCartCount(data.cartCount);
                    }
                    
                    // Update cart widget
                    if (typeof window.updateCartWidget === 'function') {
                        setTimeout(() => {
                            window.updateCartWidget();
                        }, 150);
                    } else if (typeof updateCartWidget === 'function') {
                        setTimeout(() => {
                            updateCartWidget();
                        }, 150);
                    }
                    
                    // Open cart sidebar if available - but NOT on mobile screens
                    if (typeof window.openCartSidebar === 'function') {
                        const sidebar = document.querySelector('.cart-sidebar-new') || document.getElementById('cartSidebar');
                        const isMobile = window.innerWidth <= 768;
                        
                        // Only open sidebar on desktop, not on mobile
                        if (sidebar && !isMobile) {
                            window.openCartSidebar(true);
                        }
                    }
                } else {
                    // Product was removed
                    button.classList.remove('in-cart');
                    if (icon) {
                        icon.className = 'bi bi-plus-lg';
                    }
                    
                    const localizations = window.localizations || {};
                    button.title = localizations.addToCart || 'Add to Cart';
                    
                    // Update cart product IDs if available
                    if (typeof window.cartProductIds !== 'undefined' && Array.isArray(window.cartProductIds)) {
                        window.cartProductIds = window.cartProductIds.filter(id => id !== productId);
                    }
                    
                    // Show notification
                    if (typeof toastr !== 'undefined') {
                        toastr.info(data.message);
                    }
                    
                    // Update cart count everywhere
                    if (typeof window.updateFloatingCartBadge === 'function') {
                        window.updateFloatingCartBadge(data.cartCount);
                    } else if (typeof updateFloatingCartBadge === 'function') {
                        updateFloatingCartBadge(data.cartCount);
                    }
                    
                    // Update navigation cart count
                    if (typeof window.updateNavigationCartCount === 'function') {
                        window.updateNavigationCartCount(data.cartCount);
                    } else if (typeof updateNavigationCartCount === 'function') {
                        updateNavigationCartCount(data.cartCount);
                    }
                    
                    // Update cart widget
                    if (typeof window.updateCartWidget === 'function') {
                        setTimeout(() => {
                            window.updateCartWidget();
                        }, 150);
                    } else if (typeof updateCartWidget === 'function') {
                        setTimeout(() => {
                            updateCartWidget();
                        }, 150);
                    }
                }
            } else {
                throw new Error(data.message || 'Failed to update cart');
            }
        })
        .catch(error => {
            console.error('Error:', error);
            if (button) {
                // Remove loading state from button only
                button.disabled = false;
                button.classList.remove('loading');
                button.style.opacity = '';
                button.style.cursor = '';
                const icon = button.querySelector('i');
                if (icon) {
                    icon.className = originalIcon;
                    icon.style.animation = '';
                }
            }
            
            const localizations = window.localizations || {};
            const errorMsg = localizations.failedToUpdateCart || 'Failed to update cart';
            
            if (typeof toastr !== 'undefined') {
                toastr.error(errorMsg);
            } else if (typeof Swal !== 'undefined') {
                Swal.fire({
                    icon: 'error',
                    title: 'Error',
                    text: errorMsg
                });
            }
        });
    };
}

// ============================================
// WISHLIST SIDEBAR FUNCTIONS (Global)
// ============================================

// Make wishlist sidebar functions globally available
if (typeof window.openWishlistSidebar === 'undefined') {
    window.openWishlistSidebar = function() {
        const sidebar = document.getElementById('wishlistSidebar');
        const overlay = document.getElementById('wishlistOverlay');
        
        if (sidebar && overlay) {
            sidebar.classList.add('active');
            overlay.classList.add('active');
            document.body.style.overflow = 'hidden';
            
            // Always try to load wishlist items when sidebar opens
            // Check if user is authenticated first
            const isAuthenticated = document.body.getAttribute('data-is-authenticated') === 'true' || 
                                    (typeof window.isAuthenticated !== 'undefined' && window.isAuthenticated);
            
            if (isAuthenticated) {
                // Call loadWishlistItemsToSidebar if it exists (defined in Index.cshtml)
                if (typeof loadWishlistItemsToSidebar === 'function') {
                    loadWishlistItemsToSidebar();
                } else if (typeof window.loadWishlistItemsToSidebar === 'function') {
                    window.loadWishlistItemsToSidebar();
                } else {
                    // Fallback: load wishlist items directly
                    fetch('/Customer/Home/GetWishlistItems')
                        .then(response => {
                            return response.json();
                        })
                        .then(data => {
                            const container = document.getElementById('wishlistItemsContainer');
                            const countElement = document.getElementById('sidebarWishlistCount');
                            
                            if (!container) {
                                console.error('wishlistItemsContainer not found!');
                                return;
                            }
                            
                            if (data.success && data.items && data.items.length > 0) {
                                // Use global createWishlistItemHTML if available, otherwise use inline version
                                const createHTML = window.createWishlistItemHTML;
                                if (typeof createHTML === 'function') {
                                    container.innerHTML = data.items.map(item => createHTML(item)).join('');
                                } else {
                                    // Fallback: simple HTML rendering
                                    container.innerHTML = data.items.map(item => {
                                        const currentCulture = document.documentElement.lang || 'en';
                                        const displayTitle = (currentCulture === 'ar' && item.titleAr) ? item.titleAr : item.title;
                                        const productSlug = item.slugEn || item.productId;
                                        const isVariable = item.productType === 1;
                                        const discount = item.listPrice && item.listPrice > item.price 
                                            ? Math.floor((item.listPrice - item.price) / item.listPrice * 100) 
                                            : 0;
                                        
                                        return `
                                            <div class="wishlist-item" data-product-id="${item.productId}">
                                                <img src="${item.imageUrl || '/images/no-image.png'}" alt="${displayTitle}" class="wishlist-item-image" onerror="this.src='/images/no-image.png'">
                                                <div class="wishlist-item-details">
                                                    <h6 class="wishlist-item-title">${displayTitle}</h6>
                                                    <div class="wishlist-item-price">
                                                        <span class="current-price">AED ${item.price.toFixed(2)}</span>
                                                        ${item.listPrice && item.listPrice > item.price ? `<span class="list-price">AED ${item.listPrice.toFixed(2)}</span>` : ''}
                                                    </div>
                                                    <div class="wishlist-item-actions">
                                                        ${isVariable
                                                            ? `<a href="/Customer/Home/Details/${productSlug}" class="btn btn-select-options-from-wishlist" title="Select Options">
                                                                <i class="bi bi-list-check me-1"></i>Select Options
                                                               </a>`
                                                            : `<button class="btn btn-add-cart-from-wishlist" onclick="(function() { if(typeof window.addToCartFromWishlist === 'function') { window.addToCartFromWishlist(${item.productId}, ${item.id}); } else { console.error('addToCartFromWishlist function not available. Please ensure wishlist-helper.js is loaded.'); if(typeof toastr !== 'undefined') { toastr.error('Unable to add to cart. Please refresh the page.'); } } })();" title="Add to Cart">
                                                                <i class="bi bi-cart-plus me-1"></i>Add to Cart
                                                               </button>`
                                                        }
                                                        <button class="btn btn-remove-wishlist" onclick="if(typeof window.removeFromWishlist === 'function') { window.removeFromWishlist(${item.id}, ${item.productId}); } else { console.error('removeFromWishlist not available'); }" title="Remove">
                                                            <i class="bi bi-trash"></i>
                                                        </button>
                                                    </div>
                                                </div>
                                            </div>
                                        `;
                                    }).join('');
                                }
                                if (countElement) countElement.textContent = data.count;
                                
                                // Update floating badge
                                if (typeof window.updateFloatingWishlistBadge === 'function') {
                                    window.updateFloatingWishlistBadge(data.count);
                                }
                            } else {
                                container.innerHTML = '<div class="wishlist-empty"><i class="bi bi-heart"></i><p>Your wishlist is empty</p></div>';
                                if (countElement) countElement.textContent = '0';
                                if (typeof window.updateFloatingWishlistBadge === 'function') {
                                    window.updateFloatingWishlistBadge(0);
                                }
                            }
                        })
                        .catch(error => {
                            console.error('Error loading wishlist items:', error);
                            const container = document.getElementById('wishlistItemsContainer');
                            if (container) {
                                container.innerHTML = '<div class="wishlist-empty"><p>Error loading wishlist</p></div>';
                            }
                        });
                }
            }
        }
    };
}

if (typeof window.closeWishlistSidebar === 'undefined') {
    window.closeWishlistSidebar = function() {
        const sidebar = document.getElementById('wishlistSidebar');
        const overlay = document.getElementById('wishlistOverlay');
        
        if (sidebar) sidebar.classList.remove('active');
        if (overlay) overlay.classList.remove('active');
        document.body.style.overflow = '';
    };
}




