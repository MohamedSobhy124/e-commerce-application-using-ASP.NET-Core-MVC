// Cart AJAX Operations
// Ensure SweetAlert is available globally
if (typeof window.Swal === 'undefined' && typeof Swal !== 'undefined') {
    window.Swal = Swal;
}

$(document).ready(function () {
    // Ensure SweetAlert is loaded before any operations
    if (typeof Swal === 'undefined' && typeof window.Swal === 'undefined') {
        console.warn('⚠️ SweetAlert2 not loaded. Waiting for it...');
        // Wait for SweetAlert to load (it should be loaded in the page)
        let waitAttempts = 0;
        const maxWaitAttempts = 100; // Wait up to 10 seconds
        
        function waitForSweetAlert() {
            if (typeof Swal !== 'undefined' || typeof window.Swal !== 'undefined') {
                if (typeof Swal !== 'undefined') {
                    window.Swal = Swal;
                }
                console.log('✅ SweetAlert2 loaded successfully');
            } else if (waitAttempts < maxWaitAttempts) {
                waitAttempts++;
                setTimeout(waitForSweetAlert, 100);
            } else {
                console.error('❌ SweetAlert2 failed to load after waiting');
            }
        }
        
        waitForSweetAlert();
    } else {
        // Make sure it's globally available
        if (typeof Swal !== 'undefined') {
            window.Swal = Swal;
        }
        console.log('✅ SweetAlert2 is available');
    }
    // Sync with localStorage after cart operations
    function syncCartToLocalStorage(response) {
        if (window.cartStorage && response) {
            // Update localStorage after successful server operation
            if (response.cartCount !== undefined) {
                // Trigger sync from server to get latest state
                setTimeout(() => {
                    window.cartStorage.syncCartFromServer();
                }, 500);
            }
        }
    }

    // Handle Plus button click
    $(document).on('click', '.cart-quantity-btn.plus-btn', function (e) {
        e.preventDefault();
        const btn = $(this);
        const cartId = btn.data('cart-id');
        const productId = btn.data('product-id');
        const quantityInput = btn.siblings('.cart-quantity-input');
        const currentQuantity = parseInt(quantityInput.val()) || 0;
        const cartCard = btn.closest('.cart-item-card');
        
        // Add loading overlay to card only
        showCardLoader(cartCard);
        
        // Disable button during request
        btn.prop('disabled', true);
        btn.addClass('loading');
        
        // Show loading spinner on button
        const spinner = $('<i class="bi bi-arrow-repeat spin"></i>');
        btn.html(spinner);
        
        // Explicitly hide global loader before cart operation
        if (typeof window.hideLoader === 'function') {
            window.hideLoader();
        }
   
        
        $.ajax({
            url: window.cartUrls?.plus || '/Customer/Cart/Pluse',
            type: 'POST',
            data: {
                CartId: cartId,
                ProductId: productId,
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
            },
            success: function (response) {
                console.log('✅ Plus response received:', response);
                
                if (response.success) {
                    // Update quantity input
                    quantityInput.val(response.count);
                    
                    // Update price if provided - Always update even if price is 0
                    // Note: We update even if price is 0 to show 0.00 instead of old price
                    if (response.unitPrice !== undefined) {
                        const cartCard = btn.closest('.cart-item-card');
                        const priceWrapper = cartCard.find('.cart-item-price-wrapper');
                        
                        console.log('🔍 Price update check:', {
                            unitPrice: response.unitPrice,
                            count: response.count,
                            originalPrice: response.originalPrice,
                            priceWrapperFound: priceWrapper.length > 0,
                            cartCardFound: cartCard.length > 0
                        });
                        
                        if (priceWrapper.length > 0) {
                            updatePriceDisplay(priceWrapper, response.unitPrice, response.count, response.originalPrice);
                        } else {
                            console.error('❌ Price wrapper not found in cart card. Cart card:', cartCard);
                            // Try alternative selector
                            const altPriceWrapper = cartCard.find('.cart-item-price-col .cart-item-price-wrapper');
                            if (altPriceWrapper.length > 0) {
                                console.log('✅ Found price wrapper with alternative selector');
                                updatePriceDisplay(altPriceWrapper, response.unitPrice, response.count, response.originalPrice);
                            } else {
                                // Reload page to get correct structure
                                console.error('❌ Price wrapper not found with any selector, reloading...');
                                setTimeout(() => location.reload(), 500);
                                return;
                            }
                        }
                        
                        // Only reload if unitPrice is missing (undefined/null), not if it's 0
                        // Price can legitimately be 0 for free products or variants
                        if (response.unitPrice === null || response.unitPrice === undefined) {
                            // Only log in development mode
                            if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
                                console.warn('⚠️ Unit price is missing (null/undefined), reloading page', {
                                    debugInfo: response.debugInfo,
                                    productPrice: response.debugInfo?.productPrice,
                                    productListPrice: response.debugInfo?.productListPrice,
                                    unitPrice: response.unitPrice,
                                    hasVariant: response.debugInfo?.hasVariant
                                });
                            }
                            
                            // Reload page only if price is missing, not if it's 0
                            setTimeout(() => {
                                location.reload();
                            }, 500);
                            return; // Exit early to prevent further processing
                        }
                        
                        // Price is 0 is valid (free products/variants), just update display
                        // No need to reload or show error
                    } else {
                        // If unitPrice is not provided, reload to get correct prices
                        console.warn('⚠️ Unit price not in response, reloading to get correct prices');
                        setTimeout(() => location.reload(), 500);
                        return;
                    }
                    
                    // Update total if provided - Always update
                    if (response.orderTotal !== undefined && response.orderTotal !== null) {
                        updateCartTotal(response.orderTotal);
                        
                        // Only reload if orderTotal is missing (undefined/null), not if it's 0
                        // Order total can legitimately be 0 if all items are free
                        if (response.orderTotal === null || response.orderTotal === undefined) {
                            // Only log in development mode
                            if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
                                console.warn('⚠️ Order total is missing (null/undefined), reloading page', {
                                    orderTotal: response.orderTotal,
                                    unitPrice: response.unitPrice
                                });
                            }
                            
                            // Reload page only if total is missing, not if it's 0
                            setTimeout(() => {
                                location.reload();
                            }, 500);
                            return; // Exit early to prevent further processing
                        }
                        
                        // Order total is 0 is valid (all free items), just update display
                        // No need to reload or show error
                    } else {
                        // Only reload if orderTotal is missing, not if it's 0
                        if (response.orderTotal === null || response.orderTotal === undefined) {
                            console.warn('⚠️ Order total not in response or is null');
                            // Only reload if unitPrice is also missing
                            if (response.unitPrice === null || response.unitPrice === undefined) {
                                setTimeout(() => {
                                    location.reload();
                                }, 1000);
                            }
                        }
                        // If orderTotal is 0, that's valid - don't reload
                    }
                    
                    // Show success message
                    if (response.message) {
                        showToast('success', response.message);
                    }
                    
                    // Sync with localStorage
                    syncCartToLocalStorage(response);
                    
                    // Refresh cart items list if needed
                    if (response.shouldReload) {
                        location.reload();
                    }
                } else {
                    console.error('❌ Plus failed:', response.message);
                    
                    // Show translated error message to user
                    let errorMessage = response.message || 'Failed to update quantity';
                    
                    // Check if it's a stock limit message (flash sale or regular stock)
                    if (response.message && (response.message.includes('units available') || 
                        response.message.includes('متوفر فقط') || 
                        response.message.includes('Only') ||
                        response.message.includes('in stock') ||
                        response.message.match(/\d+\s*(units|وحدة|available)/i))) {
                        // Extract number from message if available
                        const stockMatch = response.message.match(/(\d+)/);
                        const stockCount = stockMatch ? stockMatch[1] : '';
                        
                        // Use localized message if available
                        if (window.cartMessages?.flashSaleStockLimit) {
                            errorMessage = window.cartMessages.flashSaleStockLimit.replace('{0}', stockCount);
                        } else {
                            // Fallback translation
                            const isArabic = typeof getCurrentLanguage === 'function' && getCurrentLanguage() === 'ar';
                            errorMessage = isArabic 
                                ? `متوفر فقط ${stockCount} وحدة في المخزون`
                                : `Only ${stockCount} units available in stock`;
                        }
                        
                        // Show SweetAlert for stock limit errors
                        function showStockError() {
                            console.log('🔍 Attempting to show stock error. Swal type:', typeof Swal, 'Swal.fire type:', typeof (Swal && Swal.fire));
                            
                            if (typeof Swal !== 'undefined' && typeof Swal.fire === 'function') {
                                console.log('✅ Showing SweetAlert error for stock limit');
                                const isArabic = typeof getCurrentLanguage === 'function' && getCurrentLanguage() === 'ar';
                                try {
                                    SwalToUse.fire({
                                        icon: 'error',
                                        title: isArabic ? 'خطأ' : 'Error',
                                        text: errorMessage,
                                        confirmButtonText: isArabic ? 'حسناً' : 'OK',
                                        confirmButtonColor: '#dc3545',
                                        allowOutsideClick: true,
                                        allowEscapeKey: true,
                                        showConfirmButton: true,
                                        showCloseButton: true
                                    }).then((result) => {
                                        console.log('SweetAlert closed:', result);
                                    }).catch((error) => {
                                        console.error('SweetAlert error:', error);
                                        showToast('error', errorMessage);
                                    });
                                } catch (error) {
                                    console.error('Error showing SweetAlert:', error);
                                    showToast('error', errorMessage);
                                }
                            } else {
                                console.warn('SweetAlert not available, using toastr fallback. Swal type:', typeof Swal);
                                showToast('error', errorMessage);
                            }
                        }
                        
                        // Try to show immediately
                        if (typeof Swal !== 'undefined' && typeof Swal.fire === 'function') {
                            showStockError();
                        } else {
                            // Wait for SweetAlert to load
                            console.log('⏳ Waiting for SweetAlert to load for stock error...');
                            let attempts = 0;
                            const maxAttempts = 30; // Wait up to 3 seconds (30 * 100ms)
                            const checkInterval = setInterval(function() {
                                attempts++;
                                console.log(`Checking for SweetAlert (attempt ${attempts}/${maxAttempts})...`);
                                if (typeof Swal !== 'undefined' && typeof Swal.fire === 'function') {
                                    clearInterval(checkInterval);
                                    console.log('✅ SweetAlert loaded after', attempts * 100, 'ms');
                                    showStockError();
                                } else if (attempts >= maxAttempts) {
                                    clearInterval(checkInterval);
                                    console.warn('❌ SweetAlert not loaded after waiting, using toastr fallback');
                                    showToast('error', errorMessage);
                                }
                            }, 100);
                        }
                    } else {
                        // Regular error - use toastr
                        if (!response.message) {
                            // Use default translated message
                            errorMessage = window.cartMessages?.failedToUpdateQuantity || 
                                (typeof getCurrentLanguage === 'function' && getCurrentLanguage() === 'ar'
                                    ? 'فشل تحديث الكمية. يرجى المحاولة مرة أخرى.'
                                    : 'Failed to update quantity. Please try again.');
                        }
                        showToast('error', errorMessage);
                    }
                    
                    // Revert button
                    btn.html('<i class="bi bi-plus"></i>');
                }
            },
            error: function (xhr, status, error) {
                console.error('Error updating quantity:', error);
                const errorMsg = window.cartMessages?.errorOccurred || 
                    (typeof getCurrentLanguage === 'function' && getCurrentLanguage() === 'ar'
                        ? 'حدث خطأ. يرجى المحاولة مرة أخرى.'
                        : 'An error occurred. Please try again.');
                showToast('error', errorMsg);
                btn.html('<i class="bi bi-plus"></i>');
            },
            complete: function () {
                // Remove loading overlay from card
                hideCardLoader(cartCard);
                
                btn.prop('disabled', false);
                btn.removeClass('loading');
                if (!btn.html().includes('bi-plus')) {
                    btn.html('<i class="bi bi-plus"></i>');
                }
            }
        });
    });
    
    // Handle Minus button click
    $(document).on('click', '.cart-quantity-btn.minus-btn', function (e) {
        e.preventDefault();
        const btn = $(this);
        const cartId = btn.data('cart-id');
        const productId = btn.data('product-id');
        const quantityInput = btn.siblings('.cart-quantity-input');
        const currentQuantity = parseInt(quantityInput.val()) || 0;
        const cartCard = btn.closest('.cart-item-card');
        
        if (currentQuantity <= 1) {
            // If quantity is 1, ask for confirmation to remove using SweetAlert
            const isArabic = typeof getCurrentLanguage === 'function' && getCurrentLanguage() === 'ar';
            
            // Wait for SweetAlert to be available (with timeout)
            let attempts = 0;
            const maxAttempts = 50; // Wait up to 5 seconds (50 * 100ms)
            
            function showRemoveConfirmation() {
                const SwalToUse = window.Swal || Swal;
                if (typeof SwalToUse !== 'undefined' && typeof SwalToUse.fire === 'function') {
                    SwalToUse.fire({
                        title: isArabic ? 'تأكيد الحذف' : 'Confirm Removal',
                        text: isArabic ? 'هل أنت متأكد من إزالة هذا العنصر من السلة؟' : 'Are you sure you want to remove this item from your cart?',
                        icon: 'warning',
                        showCancelButton: true,
                        confirmButtonColor: '#d33',
                        cancelButtonColor: '#3085d6',
                        confirmButtonText: isArabic ? 'نعم، احذف' : 'Yes, remove it',
                        cancelButtonText: isArabic ? 'إلغاء' : 'Cancel',
                        reverseButtons: true,
                        allowOutsideClick: false,
                        allowEscapeKey: true
                    }).then((result) => {
                        if (result.isConfirmed) {
                            removeCartItemFromCartPage(cartId, productId, btn, cartCard);
                        }
                    });
                } else if (attempts < maxAttempts) {
                    attempts++;
                    setTimeout(showRemoveConfirmation, 100);
                } else {
                    // SweetAlert not available - show error and don't proceed
                    console.error('SweetAlert not available after waiting. Please refresh the page.');
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
            
            showRemoveConfirmation();
            return;
        }
        
        // Explicitly hide global loader before cart operation
        if (typeof window.hideLoader === 'function') {
            window.hideLoader();
        }
    
        
        // Add loading overlay to card only
        showCardLoader(cartCard);
        
        // Disable button during request
        btn.prop('disabled', true);
        btn.addClass('loading');
        
        // Show loading spinner on button
        const spinner = $('<i class="bi bi-arrow-repeat spin"></i>');
        btn.html(spinner);
        
        $.ajax({
            url: window.cartUrls?.minus || '/Customer/Cart/Minus',
            type: 'POST',
            data: {
                CartId: cartId,
                ProductId: productId,
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
            },
            success: function (response) {
                
                if (response.success) {
                    if (response.removed) {
                        // Item was removed, fade out and remove from DOM
                        const cartItem = btn.closest('.cart-item-card');
                        cartItem.fadeOut(300, function () {
                            $(this).remove();
                            // Check if cart is empty
                            if ($('.cart-item-card').length === 0) {
                                location.reload();
                            } else {
                                // Update total
                                if (response.orderTotal !== undefined && response.orderTotal !== null) {
                                    updateCartTotal(response.orderTotal);
                                }
                            }
                        });
                    } else {
                        // Update quantity input
                        quantityInput.val(response.count);
                        
                        // Update price if provided - Always update
                        if (response.unitPrice !== undefined && response.unitPrice !== null) {
                            const cartCard = btn.closest('.cart-item-card');
                            const priceWrapper = cartCard.find('.cart-item-price-wrapper');
                            
                            console.log('🔍 Minus price update check:', {
                                unitPrice: response.unitPrice,
                                count: response.count,
                                originalPrice: response.originalPrice,
                                priceWrapperFound: priceWrapper.length > 0
                            });
                            
                            if (priceWrapper.length > 0) {
                                updatePriceDisplay(priceWrapper, response.unitPrice, response.count, response.originalPrice);
                            } else {
                                // Try alternative selector
                                const altPriceWrapper = cartCard.find('.cart-item-price-col .cart-item-price-wrapper');
                                if (altPriceWrapper.length > 0) {
                                    updatePriceDisplay(altPriceWrapper, response.unitPrice, response.count, response.originalPrice);
                                } else {
                                    console.error('❌ Price wrapper not found in cart card');
                                }
                            }
                        }
                        
                        // Update total if provided - Always update
                        if (response.orderTotal !== undefined && response.orderTotal !== null) {
                            updateCartTotal(response.orderTotal);
                        } else {
                            console.warn('⚠️ Order total not in response or is null');
                            // Try to reload cart to get updated total
                            setTimeout(() => {
                                if (typeof window.loadCartItems === 'function') {
                                    window.loadCartItems();
                                }
                            }, 500);
                        }
                    }
                    
                    // Show success message
                    if (response.message) {
                        showToast('success', response.message);
                    }
                    
                    // Sync with localStorage
                    syncCartToLocalStorage(response);
                } else {
                    console.error('❌ Minus failed:', response.message);
                    showToast('error', response.message || 'Failed to update quantity');
                    btn.html('<i class="bi bi-dash"></i>');
                }
            },
            error: function (xhr, status, error) {
                console.error('Error updating quantity:', error);
                showToast('error', 'An error occurred. Please try again.');
                btn.html('<i class="bi bi-dash"></i>');
            },
            complete: function () {
                // Remove loading overlay from card
                hideCardLoader(cartCard);
                
                btn.prop('disabled', false);
                btn.removeClass('loading');
                if (!btn.html().includes('bi-dash')) {
                    btn.html('<i class="bi bi-dash"></i>');
                }
            }
        });
    });
    
    // Handle Remove button click
    $(document).on('click', '.cart-delete-btn', function (e) {
        e.preventDefault();
        const btn = $(this);
        const cartId = btn.data('cart-id');
        const productId = btn.data('product-id');
        const cartCard = btn.closest('.cart-item-card');
        
        // Validate productId
        if (!productId || productId === 0) {
            console.error('Cannot remove item: missing productId', { cartId, productId });
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
        
        // Show SweetAlert confirmation instead of browser confirm
        const isArabic = typeof getCurrentLanguage === 'function' && getCurrentLanguage() === 'ar';
        
        // Wait for SweetAlert to be available (with timeout)
        let attempts = 0;
        const maxAttempts = 50; // Wait up to 5 seconds (50 * 100ms)
        
        function showRemoveConfirmation() {
            const SwalToUse = window.Swal || Swal;
            if (typeof SwalToUse !== 'undefined' && typeof SwalToUse.fire === 'function') {
                SwalToUse.fire({
                    title: isArabic ? 'تأكيد الحذف' : 'Confirm Removal',
                    text: isArabic ? 'هل أنت متأكد من إزالة هذا العنصر من السلة؟' : 'Are you sure you want to remove this item from your cart?',
                    icon: 'warning',
                    showCancelButton: true,
                    confirmButtonColor: '#d33',
                    cancelButtonColor: '#3085d6',
                    confirmButtonText: isArabic ? 'نعم، احذف' : 'Yes, remove it',
                    cancelButtonText: isArabic ? 'إلغاء' : 'Cancel',
                    reverseButtons: true,
                    allowOutsideClick: false,
                    allowEscapeKey: true
                }).then((result) => {
                    if (result.isConfirmed) {
                        // Explicitly hide global loader before cart operation
                        if (typeof window.hideLoader === 'function') {
                            window.hideLoader();
                        }
                        removeCartItemFromCartPage(cartId, productId, btn, cartCard);
                    }
                });
                } else if (attempts < maxAttempts) {
                    attempts++;
                    setTimeout(showRemoveConfirmation, 100);
                } else {
                    // SweetAlert not available - show error and don't proceed
                    console.error('SweetAlert not available after waiting. Please refresh the page.');
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
        
        showRemoveConfirmation();
    });
});

function removeCartItemFromCartPage(cartId, productId, btn, cartCard) {
    // Add loading overlay to card
    showCardLoader(cartCard);
    
    btn.prop('disabled', true);
    btn.html('<i class="bi bi-arrow-repeat spin"></i>');
    
    $.ajax({
        url: window.cartUrls?.remove || '/Customer/Cart/Remove',
        type: 'POST',
        data: {
            CartId: cartId,
            ProductId: productId,
            __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
        },
        success: function (response) {
            console.log('Remove response:', response);
            if (response.success) {
                const cartItem = btn.closest('.cart-item-card');
                
                // Update cart count immediately (before fadeOut)
                if (response.cartCount !== undefined) {
                    const cartCountElement = document.getElementById('cartCount');
                    if (cartCountElement) {
                        cartCountElement.textContent = response.cartCount;
                    }
                    const headerCartBadge = document.getElementById('headerCartBadge');
                    if (headerCartBadge) {
                        if (response.cartCount > 0) {
                            headerCartBadge.textContent = response.cartCount;
                            headerCartBadge.style.display = 'flex';
                        } else {
                            headerCartBadge.style.display = 'none';
                        }
                    }
                }
                
                // Update total immediately
                if (response.orderTotal !== undefined) {
                    updateCartTotal(response.orderTotal);
                }
                
                // Remove item with animation
                cartItem.fadeOut(300, function () {
                    $(this).remove();
                    
                    // Check if cart is empty
                    if ($('.cart-item-card').length === 0) {
                        // Reload page if cart is empty
                        setTimeout(() => {
                            location.reload();
                        }, 500);
                    } else {
                        // Force update cart sidebar if open (reload items)
                        setTimeout(() => {
                            console.log('Updating cart sidebar...');
                            if (typeof window.loadCartItems === 'function') {
                                window.loadCartItems();
                            } else if (typeof loadCartItems === 'function') {
                                loadCartItems();
                            }
                            
                            // Update floating cart badge if available
                            if (typeof updateFloatingCartBadge === 'function') {
                                updateFloatingCartBadge(response.cartCount || 0);
                            } else if (typeof window.updateFloatingCartBadge === 'function') {
                                window.updateFloatingCartBadge(response.cartCount || 0);
                            }
                            
                            // Update cart widget if available
                            if (typeof updateCartWidget === 'function') {
                                updateCartWidget();
                            } else if (typeof window.updateCartWidget === 'function') {
                                window.updateCartWidget();
                            }
                        }, 300);
                    }
                });
                
                if (response.message) {
                    showToast('success', response.message);
                }
                
                // Sync with localStorage
                syncCartToLocalStorage(response);
            } else {
                showToast('error', response.message || 'Failed to remove item');
                btn.html('<i class="bi bi-trash-fill"></i>');
            }
        },
        error: function () {
            showToast('error', 'An error occurred. Please try again.');
            btn.html('<i class="bi bi-trash-fill"></i>');
        },
        complete: function () {
            // Remove loading overlay
            hideCardLoader(cartCard);
            btn.prop('disabled', false);
        }
    });
}

// Show loading overlay on specific card
function showCardLoader(card) {
    // Ensure we have a valid card element
    if (!card || card.length === 0) {
        console.warn('⚠️ showCardLoader: Invalid card element');
        return;
    }
    
    // Remove any existing loader
    card.find('.card-loader-overlay').remove();
    
    // Ensure card has relative positioning
    if (card.css('position') === 'static' || !card.css('position')) {
        card.css('position', 'relative');
    }
    
    // Ensure card has overflow hidden to contain the overlay
    const currentOverflow = card.css('overflow');
    if (!currentOverflow || currentOverflow === 'visible') {
        card.css('overflow', 'hidden');
    }
    
    // Create overlay
    const overlay = $(`
        <div class="card-loader-overlay">
            <div class="card-loader-spinner">
                <i class="bi bi-arrow-repeat"></i>
            </div>
        </div>
    `);
    
    card.append(overlay);
    
    // Fade in
    setTimeout(() => {
        overlay.addClass('active');
    }, 10);
}

// Hide loading overlay from card
function hideCardLoader(card) {
    const overlay = card.find('.card-loader-overlay');
    
    if (overlay.length > 0) {
        overlay.removeClass('active');
        setTimeout(() => {
            overlay.remove();
        }, 300);
    }
}

function updatePriceDisplay(priceWrapper, unitPrice, quantity, originalPrice) {
    if (!priceWrapper || priceWrapper.length === 0) {
        console.error('❌ Price wrapper not found!');
        return;
    }
    
    const currencySymbol = getCurrencySymbol();
    
    // Handle null/undefined prices - treat as 0
    const safeUnitPrice = (unitPrice === null || unitPrice === undefined || isNaN(unitPrice)) ? 0 : parseFloat(unitPrice);
    const safeQuantity = (quantity === null || quantity === undefined || isNaN(quantity)) ? 0 : parseInt(quantity);
    const safeOriginalPrice = (originalPrice === null || originalPrice === undefined || isNaN(originalPrice)) ? 0 : parseFloat(originalPrice);
    
    console.log('🔄 Updating price display:', {
        unitPrice: safeUnitPrice,
        quantity: safeQuantity,
        originalPrice: safeOriginalPrice,
        currencySymbol: currencySymbol,
        currentHTML: priceWrapper.html()
    });
    
    // Clear existing content
    priceWrapper.empty();
    
    // Build price HTML structure to match server-side rendering exactly
    let priceHtml = '';
    
    // Original price (if exists and greater than unit price) - strikethrough
    if (safeOriginalPrice > 0 && safeOriginalPrice > safeUnitPrice) {
        priceHtml += `<div class="cart-item-price original-price">${currencySymbol} ${safeOriginalPrice.toFixed(2)}</div>`;
    }
    
    // Unit price (current price) - may have sale-price class
    // Always show unit price, even if 0
    const salePriceClass = (safeOriginalPrice > 0 && safeOriginalPrice > safeUnitPrice) ? 'sale-price' : '';
    priceHtml += `<div class="cart-item-price ${salePriceClass}">${currencySymbol} ${safeUnitPrice.toFixed(2)}</div>`;
    
    // Total price (unit price * quantity) - always show, even if 0
    const totalPrice = safeUnitPrice * safeQuantity;
    priceHtml += `<div class="cart-item-total">${currencySymbol} ${totalPrice.toFixed(2)}</div>`;
    
    // Update the wrapper
    priceWrapper.html(priceHtml);
    
    console.log('✅ Price updated successfully:', {
        unitPrice: safeUnitPrice,
        quantity: safeQuantity,
        totalPrice: totalPrice,
        originalPrice: safeOriginalPrice,
        newHTML: priceWrapper.html()
    });
    
    // Force a visual update by triggering a reflow
    if (priceWrapper[0]) {
        priceWrapper[0].offsetHeight;
    }
}

function updateCartTotal(orderTotal) {
    const currencySymbol = getCurrencySymbol();
    // Handle null/undefined - treat as 0
    const safeOrderTotal = (orderTotal === null || orderTotal === undefined) ? 0 : parseFloat(orderTotal);
    const formattedTotal = `${currencySymbol} ${safeOrderTotal.toFixed(2)}`;
    
    console.log('🔄 Updating cart total:', {
        orderTotal: safeOrderTotal,
        formattedTotal: formattedTotal
    });
    
    const totalElement = $('.cart-total-amount');
    
    if (totalElement.length > 0) {
        totalElement.text(formattedTotal);
        
        // Add animation to show change
        totalElement.addClass('updating');
        setTimeout(() => {
            totalElement.removeClass('updating');
        }, 600);
        
    } else {
        console.error('❌ Cart total element not found!');
    }
}

function getCurrencySymbol() {
    // Check if there's a global getCurrencySymbol function (from language-switcher.js)
    if (typeof window.getCurrencySymbol === 'function' && window.getCurrencySymbol !== getCurrencySymbol) {
        return window.getCurrencySymbol();
    }
    
    // Try to get from cookie if language-switcher.js is loaded
    if (typeof getCurrentLanguage === 'function') {
        const lang = getCurrentLanguage();
        return lang === 'ar' ? 'د.إ' : 'AED';
    }
    
    // Default to AED if we can't determine
    return 'AED';
}

function showToast(type, message) {
    if (typeof toastr !== 'undefined') {
        toastr[type](message);
    } else {
        // Fallback alert
        alert(message);
    }
}

// Spinning animation - only add if not already added
if (!document.getElementById('cart-ajax-styles')) {
    const style = document.createElement('style');
    style.id = 'cart-ajax-styles';
    style.textContent = `
        .spin {
            animation: spin 1s linear infinite;
        }
        @keyframes spin {
            from { transform: rotate(0deg); }
            to { transform: rotate(360deg); }
        }
        .cart-quantity-btn.loading {
            opacity: 0.6;
            cursor: not-allowed;
        }
        .cart-total-amount.updating {
            animation: pulseScale 0.6s ease-in-out;
        }
        @keyframes pulseScale {
            0%, 100% { transform: scale(1); }
            50% { transform: scale(1.1); color: #fbbf24; }
        }
        
        /* Card Loading Overlay - Only on specific card */
        .cart-item-card {
            position: relative !important;
            overflow: hidden !important;
        }
        
        .card-loader-overlay {
            position: absolute !important;
            top: 0 !important;
            left: 0 !important;
            right: 0 !important;
            bottom: 0 !important;
            width: 100% !important;
            height: 100% !important;
            background: rgba(255, 255, 255, 0.95) !important;
            backdrop-filter: blur(4px);
            display: flex !important;
            align-items: center;
            justify-content: center;
            border-radius: 16px;
            z-index: 10 !important;
            opacity: 0;
            transition: opacity 0.3s ease;
            pointer-events: auto;
            margin: 0 !important;
            padding: 0 !important;
        }
        
        .card-loader-overlay.active {
            opacity: 1;
        }
        
        .card-loader-spinner {
            display: flex;
            flex-direction: column;
            align-items: center;
            gap: 0.5rem;
        }
        
        .card-loader-spinner i {
            font-size: 2.5rem;
            color: #667eea;
            animation: spin 1s linear infinite;
        }
    `;
    document.head.appendChild(style);
}

