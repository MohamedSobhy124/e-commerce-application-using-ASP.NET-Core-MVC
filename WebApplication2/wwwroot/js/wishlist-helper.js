/**
 * Wishlist Helper Functions
 * Global functions for wishlist operations including add to cart from wishlist
 */

// Add to cart from wishlist - Global function
window.addToCartFromWishlist = function(productId, wishlistId) {
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    if (!token) {
        console.error('Anti-forgery token not found');
        if (typeof toastr !== 'undefined') {
            toastr.error('Security token missing. Please refresh the page.');
        }
        return;
    }

    const formData = new FormData();
    formData.append('productId', productId);
    formData.append('__RequestVerificationToken', token);

    fetch('/Customer/Home/ToggleCart', {
        method: 'POST',
        body: formData
    })
    .then(response => response.json())
    .then(data => {
        if (data.success && data.isAdded) {
            // Sync with localStorage
            if (window.cartStorage) {
                window.cartStorage.addItem({
                    productId: productId,
                    count: 1
                });
            }

            // Show success message
            const successMsg = data.message || (window.localizations?.addedToCart || 'Product added to cart successfully');
            if (typeof toastr !== 'undefined') {
                toastr.success(successMsg);
            }

            // Update cart count everywhere
            if (data.cartCount !== undefined) {
                // Update floating cart badge
                if (typeof updateFloatingCartBadge === 'function') {
                    updateFloatingCartBadge(data.cartCount);
                } else if (typeof window.updateFloatingCartBadge === 'function') {
                    window.updateFloatingCartBadge(data.cartCount);
                }

                // Update navigation cart count
                if (typeof updateNavigationCartCount === 'function') {
                    updateNavigationCartCount(data.cartCount);
                }

                // Update cart widget
                if (typeof updateCartWidget === 'function') {
                    setTimeout(() => {
                        updateCartWidget();
                    }, 150);
                }

                // Update localStorage cart count
                if (window.cartStorage) {
                    window.cartStorage.updateCartCountFromLocalStorage();
                }
            }

            // Update cart sidebar if open
            if (typeof loadCartItems === 'function') {
                loadCartItems();
            } else if (typeof window.loadCartItems === 'function') {
                window.loadCartItems();
            }
        } else if (data.success && !data.isAdded) {
            // Product was removed from cart
            if (window.cartStorage) {
                window.cartStorage.removeItem(productId);
            }

            const removedMsg = data.message || (window.localizations?.removedFromCart || 'Product removed from cart');
            if (typeof toastr !== 'undefined') {
                toastr.info(removedMsg);
            }

            // Update cart count
            if (data.cartCount !== undefined) {
                if (typeof updateFloatingCartBadge === 'function') {
                    updateFloatingCartBadge(data.cartCount);
                } else if (typeof window.updateFloatingCartBadge === 'function') {
                    window.updateFloatingCartBadge(data.cartCount);
                }

                if (window.cartStorage) {
                    window.cartStorage.updateCartCountFromLocalStorage();
                }
            }
        } else {
            // Error
            const errorMsg = data.message || (window.localizations?.failedToAddToCart || 'Failed to add to cart');
            if (typeof toastr !== 'undefined') {
                toastr.error(errorMsg);
            }
        }
    })
    .catch(error => {
        console.error('Error adding to cart from wishlist:', error);
        const errorMsg = window.localizations?.failedToAddToCart || 'Failed to add to cart';
        if (typeof toastr !== 'undefined') {
            toastr.error(errorMsg);
        }
    });
};

// Remove from wishlist - Global function (if not already defined)
if (typeof window.removeFromWishlist === 'undefined') {
    window.removeFromWishlist = function(wishlistId, productId) {
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        if (!token) {
            console.error('Anti-forgery token not found');
            return;
        }

        const formData = new FormData();
        formData.append('wishlistId', wishlistId);
        formData.append('__RequestVerificationToken', token);

        fetch('/Customer/Home/RemoveFromWishlist', {
            method: 'DELETE',
            body: formData
        })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                // Remove item from DOM
                const item = document.querySelector(`.wishlist-item[data-product-id="${productId}"]`);
                if (item) item.remove();

                // Update wishlist button
                const btn = document.querySelector(`.wishlist-btn[data-product-id="${productId}"]`);
                if (btn) {
                    btn.classList.remove('active');
                    btn.querySelector('i').className = 'bi bi-heart';
                    btn.title = 'Add to wishlist';
                }

                // Update wishlist count
                if (typeof updateFloatingWishlistBadge === 'function') {
                    updateFloatingWishlistBadge(data.wishlistCount);
                }

                const countElement = document.getElementById('sidebarWishlistCount');
                if (countElement) countElement.textContent = data.wishlistCount;

                if (data.wishlistCount === 0) {
                    if (typeof loadWishlistItemsToSidebar === 'function') {
                        loadWishlistItemsToSidebar();
                    }
                }

                const successMsg = data.message || (window.localizations?.removedFromWishlist || 'Removed from wishlist');
                if (typeof toastr !== 'undefined') {
                    toastr.info(successMsg);
                }
            } else {
                const errorMsg = data.message || (window.localizations?.failedToUpdateCart || 'Failed to remove from wishlist');
                if (typeof toastr !== 'undefined') {
                    toastr.error(errorMsg);
                }
            }
        })
        .catch(error => {
            console.error('Error removing from wishlist:', error);
            const errorMsg = window.localizations?.failedToUpdateCart || 'Failed to remove from wishlist';
            if (typeof toastr !== 'undefined') {
                toastr.error(errorMsg);
            }
        });
    };
}

