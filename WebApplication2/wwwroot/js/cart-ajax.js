// Cart AJAX Operations
$(document).ready(function () {
    // Handle Plus button click
    $(document).on('click', '.cart-quantity-btn.plus-btn', function (e) {
        e.preventDefault();
        const btn = $(this);
        const cartId = btn.data('cart-id');
        const productId = btn.data('product-id');
        const quantityInput = btn.siblings('.cart-quantity-input');
        const currentQuantity = parseInt(quantityInput.val()) || 0;
        
        // Disable button during request
        btn.prop('disabled', true);
        btn.addClass('loading');
        
        // Show loading spinner
        const spinner = $('<i class="bi bi-arrow-repeat spin"></i>');
        btn.html(spinner);
        
        $.ajax({
            url: window.cartUrls?.plus || '/Customer/Cart/Pluse',
            type: 'POST',
            data: {
                CartId: cartId,
                ProductId: productId,
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
            },
            success: function (response) {
                if (response.success) {
                    // Update quantity input
                    quantityInput.val(response.count);
                    
                    // Update price if provided
                    if (response.unitPrice !== undefined) {
                        const priceWrapper = btn.closest('.cart-item-card').find('.cart-item-price-wrapper');
                        updatePriceDisplay(priceWrapper, response.unitPrice, response.count, response.originalPrice);
                    }
                    
                    // Update total if provided
                    if (response.orderTotal !== undefined) {
                        updateCartTotal(response.orderTotal);
                    }
                    
                    // Show success message
                    if (response.message) {
                        showToast('success', response.message);
                    }
                    
                    // Refresh cart items list if needed
                    if (response.shouldReload) {
                        location.reload();
                    }
                } else {
                    showToast('error', response.message || 'Failed to update quantity');
                    // Revert button
                    btn.html('<i class="bi bi-plus"></i>');
                }
            },
            error: function (xhr, status, error) {
                console.error('Error updating quantity:', error);
                showToast('error', 'An error occurred. Please try again.');
                btn.html('<i class="bi bi-plus"></i>');
            },
            complete: function () {
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
        
        if (currentQuantity <= 1) {
            // If quantity is 1, ask for confirmation to remove
            if (confirm('Remove this item from cart?')) {
                removeCartItem(cartId, productId, btn);
            }
            return;
        }
        
        // Disable button during request
        btn.prop('disabled', true);
        btn.addClass('loading');
        
        // Show loading spinner
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
                                if (response.orderTotal !== undefined) {
                                    updateCartTotal(response.orderTotal);
                                }
                            }
                        });
                    } else {
                        // Update quantity input
                        quantityInput.val(response.count);
                        
                        // Update price if provided
                        if (response.unitPrice !== undefined) {
                            const priceWrapper = btn.closest('.cart-item-card').find('.cart-item-price-wrapper');
                            updatePriceDisplay(priceWrapper, response.unitPrice, response.count, response.originalPrice);
                        }
                        
                        // Update total if provided
                        if (response.orderTotal !== undefined) {
                            updateCartTotal(response.orderTotal);
                        }
                    }
                    
                    // Show success message
                    if (response.message) {
                        showToast('success', response.message);
                    }
                } else {
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
        
        if (confirm('Are you sure you want to remove this item from your cart?')) {
            removeCartItem(cartId, productId, btn);
        }
    });
});

function removeCartItem(cartId, productId, btn) {
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
            if (response.success) {
                const cartItem = btn.closest('.cart-item-card');
                cartItem.fadeOut(300, function () {
                    $(this).remove();
                    // Check if cart is empty
                    if ($('.cart-item-card').length === 0) {
                        location.reload();
                    } else {
                        // Update total
                        if (response.orderTotal !== undefined) {
                            updateCartTotal(response.orderTotal);
                        }
                    }
                });
                
                if (response.message) {
                    showToast('success', response.message);
                }
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
            btn.prop('disabled', false);
        }
    });
}

function updatePriceDisplay(priceWrapper, unitPrice, quantity, originalPrice) {
    const currencySymbol = getCurrencySymbol();
    
    // Update unit price
    let priceHtml = '';
    if (originalPrice && originalPrice > 0 && originalPrice > unitPrice) {
        priceHtml += `<div class="cart-item-price original-price">${currencySymbol} ${originalPrice.toFixed(2)}</div>`;
    }
    priceHtml += `<div class="cart-item-price ${originalPrice && originalPrice > unitPrice ? 'sale-price' : ''}">${currencySymbol} ${unitPrice.toFixed(2)}</div>`;
    priceHtml += `<div class="cart-item-total">${currencySymbol} ${(unitPrice * quantity).toFixed(2)}</div>`;
    
    priceWrapper.html(priceHtml);
}

function updateCartTotal(orderTotal) {
    const currencySymbol = getCurrencySymbol();
    $('.cart-total-amount').text(`${currencySymbol} ${orderTotal.toFixed(2)}`);
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
    `;
    document.head.appendChild(style);
}

