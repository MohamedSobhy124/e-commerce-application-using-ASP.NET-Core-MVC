/**
 * Cart Storage Manager
 * Manages cart state in localStorage for performance optimization
 * Syncs with server-side cart (database for authenticated users, session for guests)
 */

class CartStorageManager {
    constructor() {
        this.storageKey = 'ecommerce_cart';
        this.syncKey = 'ecommerce_cart_sync';
        this.lastSyncKey = 'ecommerce_cart_last_sync';
        this.syncInterval = 30000; // 30 seconds - sync with server periodically
        this.init();
    }

    init() {
        // Update cart count from localStorage immediately (for instant display)
        this.updateCartCountFromLocalStorage();
        
        // Sync cart on page load (after a short delay to allow page to render)
        setTimeout(() => {
            this.syncCartFromServer();
        }, 500);
        
        // Set up periodic sync
        setInterval(() => {
            this.syncCartFromServer();
        }, this.syncInterval);

        // Sync before page unload
        window.addEventListener('beforeunload', () => {
            this.syncCartToServer();
        });

        // Listen for authentication state changes
        this.setupAuthListeners();
        
        // Listen for cart changes to update UI
        document.addEventListener('cartChanged', (e) => {
            this.updateCartCountFromLocalStorage();
        });
    }
    
    /**
     * Update cart count in UI from localStorage (for instant display)
     */
    updateCartCountFromLocalStorage() {
        const count = this.getCartCount();
        
        // Update navigation cart count
        const cartCountElement = document.getElementById('cartCount');
        if (cartCountElement) {
            cartCountElement.textContent = count;
            if (count > 0) {
                cartCountElement.style.display = 'inline-block';
            } else {
                cartCountElement.style.display = 'none';
            }
        }
        
        // Update header cart badge
        const headerCartBadge = document.getElementById('headerCartBadge');
        if (headerCartBadge) {
            if (count > 0) {
                headerCartBadge.textContent = count;
                headerCartBadge.style.display = 'flex';
            } else {
                headerCartBadge.style.display = 'none';
            }
        }
        
        // Update floating cart badge (if function exists)
        if (typeof window.updateFloatingCartBadge === 'function') {
            window.updateFloatingCartBadge(count);
        } else if (typeof updateFloatingCartBadge === 'function') {
            updateFloatingCartBadge(count);
        }
    }

    /**
     * Get cart from localStorage
     */
    getCart() {
        try {
            const cartJson = localStorage.getItem(this.storageKey);
            if (!cartJson) {
                return [];
            }
            return JSON.parse(cartJson);
        } catch (error) {
            console.error('Error reading cart from localStorage:', error);
            return [];
        }
    }

    /**
     * Save cart to localStorage
     */
    saveCart(cart) {
        try {
            localStorage.setItem(this.storageKey, JSON.stringify(cart));
            localStorage.setItem(this.lastSyncKey, Date.now().toString());
        } catch (error) {
            console.error('Error saving cart to localStorage:', error);
            // If storage is full, try to clear old data
            if (error.name === 'QuotaExceededError') {
                this.clearOldData();
                try {
                    localStorage.setItem(this.storageKey, JSON.stringify(cart));
                } catch (e) {
                    console.error('Failed to save cart after clearing old data:', e);
                }
            }
        }
    }

    /**
     * Add item to cart (localStorage)
     */
    addItem(item) {
        const cart = this.getCart();
        
        // Check if item already exists
        const existingIndex = cart.findIndex(c => 
            c.productId === item.productId &&
            (c.productVariantId || null) === (item.productVariantId || null) &&
            (c.flashSaleItemId || null) === (item.flashSaleItemId || null) &&
            (c.comboOfferId || null) === (item.comboOfferId || null)
        );

        if (existingIndex >= 0) {
            // Update quantity
            cart[existingIndex].count += (item.count || 1);
        } else {
            // Add new item
            cart.push({
                productId: item.productId,
                count: item.count || 1,
                productVariantId: item.productVariantId || null,
                flashSaleItemId: item.flashSaleItemId || null,
                flashSalePrice: item.flashSalePrice || null,
                comboOfferId: item.comboOfferId || null,
                addedAt: new Date().toISOString()
            });
        }

        this.saveCart(cart);
        this.notifyCartChanged();
        return cart;
    }

    /**
     * Update item quantity in cart
     */
    updateQuantity(productId, count, productVariantId = null, flashSaleItemId = null, comboOfferId = null) {
        const cart = this.getCart();
        const item = cart.find(c => 
            c.productId === productId &&
            (c.productVariantId || null) === (productVariantId || null) &&
            (c.flashSaleItemId || null) === (flashSaleItemId || null) &&
            (c.comboOfferId || null) === (comboOfferId || null)
        );

        if (item) {
            if (count <= 0) {
                // Remove item
                const index = cart.indexOf(item);
                cart.splice(index, 1);
            } else {
                item.count = count;
            }
            this.saveCart(cart);
            this.notifyCartChanged();
        }

        return cart;
    }

    /**
     * Remove item from cart
     */
    removeItem(productId, productVariantId = null, flashSaleItemId = null, comboOfferId = null) {
        const cart = this.getCart();
        const filtered = cart.filter(c => 
            !(c.productId === productId &&
              (c.productVariantId || null) === (productVariantId || null) &&
              (c.flashSaleItemId || null) === (flashSaleItemId || null) &&
              (c.comboOfferId || null) === (comboOfferId || null))
        );

        this.saveCart(filtered);
        this.notifyCartChanged();
        return filtered;
    }

    /**
     * Clear cart
     */
    clearCart() {
        localStorage.removeItem(this.storageKey);
        localStorage.removeItem(this.lastSyncKey);
        localStorage.removeItem(this.syncKey);
        this.notifyCartChanged();
    }

    /**
     * Get cart count (number of unique items)
     */
    getCartCount() {
        return this.getCart().length;
    }

    /**
     * Get total quantity (sum of all item counts)
     */
    getTotalQuantity() {
        return this.getCart().reduce((sum, item) => sum + (item.count || 1), 0);
    }

    /**
     * Sync cart from server (fetch latest cart state)
     */
    async syncCartFromServer() {
        try {
            // Check if we need to sync (avoid too frequent requests)
            const lastSync = localStorage.getItem(this.lastSyncKey);
            if (lastSync) {
                const timeSinceSync = Date.now() - parseInt(lastSync);
                if (timeSinceSync < 5000) { // Don't sync if synced less than 5 seconds ago
                    return;
                }
            }

            const response = await fetch('/Customer/Cart/GetCartItems', {
                method: 'GET',
                headers: {
                    'Accept': 'application/json'
                }
            });

            if (response.ok) {
                const data = await response.json();
                if (data.items && Array.isArray(data.items)) {
                    // Convert server cart items to localStorage format
                    const localCart = data.items.map(item => ({
                        productId: item.productId,
                        count: item.count,
                        productVariantId: item.productVariantId || null,
                        flashSaleItemId: item.flashSaleItemId || null,
                        flashSalePrice: item.isFlashSale ? item.price : null,
                        comboOfferId: item.isComboOffer ? item.productId : null, // Note: ComboOfferId might need adjustment
                        addedAt: new Date().toISOString()
                    }));

                    this.saveCart(localCart);
                    localStorage.setItem(this.syncKey, 'synced');
                    this.notifyCartChanged();
                }
            }
        } catch (error) {
            console.error('Error syncing cart from server:', error);
        }
    }

    /**
     * Sync cart to server (send localStorage cart to server)
     * This is called when user makes changes locally
     */
    async syncCartToServer() {
        const cart = this.getCart();
        if (cart.length === 0) {
            return;
        }

        try {
            // For authenticated users, server already has the cart
            // For guest users, we need to ensure session is updated
            // This is mainly for ensuring consistency
            await this.syncCartFromServer();
        } catch (error) {
            console.error('Error syncing cart to server:', error);
        }
    }

    /**
     * Merge localStorage cart with server cart after login
     */
    async mergeCartAfterLogin() {
        const localCart = this.getCart();
        if (localCart.length === 0) {
            // No local cart to merge, just sync from server
            await this.syncCartFromServer();
            return;
        }

        try {
            // First, sync from server to get existing cart
            await this.syncCartFromServer();
            
            // Get server cart to check what's already there
            const serverCart = this.getCart(); // This will be updated by syncCartFromServer
            
            // Send local cart items to server for merging
            // The server will handle the merge logic (it checks for duplicates)
            for (const item of localCart) {
                // Check if item already exists in server cart
                const exists = serverCart.some(sc => 
                    sc.productId === item.productId &&
                    (sc.productVariantId || null) === (item.productVariantId || null) &&
                    (sc.flashSaleItemId || null) === (item.flashSaleItemId || null) &&
                    (sc.comboOfferId || null) === (item.comboOfferId || null)
                );
                
                if (exists) {
                    continue; // Skip if already in server cart
                }
                
                if (item.flashSaleItemId) {
                    // Add flash sale item
                    try {
                        await fetch('/Customer/Cart/AddFlashSaleToCart', {
                            method: 'POST',
                            headers: {
                                'Content-Type': 'application/x-www-form-urlencoded',
                                'RequestVerificationToken': this.getAntiForgeryToken()
                            },
                            body: new URLSearchParams({
                                productId: item.productId,
                                flashSaleItemId: item.flashSaleItemId,
                                flashSalePrice: item.flashSalePrice || 0,
                                count: item.count,
                                productVariantId: item.productVariantId || ''
                            })
                        });
                    } catch (e) {
                        console.warn('Failed to add flash sale item to cart:', e);
                    }
                } else if (item.comboOfferId) {
                    // Add combo offer
                    try {
                        await fetch('/Customer/ComboOffer/AddToCart', {
                            method: 'POST',
                            headers: {
                                'Content-Type': 'application/x-www-form-urlencoded',
                                'RequestVerificationToken': this.getAntiForgeryToken()
                            },
                            body: new URLSearchParams({
                                comboOfferId: item.comboOfferId,
                                __RequestVerificationToken: this.getAntiForgeryToken()
                            })
                        });
                    } catch (e) {
                        console.warn('Failed to add combo offer to cart:', e);
                    }
                } else {
                    // Add regular product - use ToggleCart which handles add/remove
                    try {
                        await fetch('/Customer/Home/ToggleCart', {
                            method: 'POST',
                            headers: {
                                'Content-Type': 'application/x-www-form-urlencoded',
                                'RequestVerificationToken': this.getAntiForgeryToken()
                            },
                            body: new URLSearchParams({
                                productId: item.productId,
                                __RequestVerificationToken: this.getAntiForgeryToken()
                            })
                        });
                    } catch (e) {
                        console.warn('Failed to add product to cart:', e);
                    }
                }
                
                // Small delay between requests to avoid overwhelming server
                await new Promise(resolve => setTimeout(resolve, 200));
            }

            // After merging, sync from server to get the final state
            setTimeout(() => {
                this.syncCartFromServer();
            }, 2000);
        } catch (error) {
            console.error('Error merging cart after login:', error);
            // Still try to sync from server
            this.syncCartFromServer();
        }
    }

    /**
     * Clear cart on logout
     */
    onLogout() {
        this.clearCart();
    }

    /**
     * Setup listeners for authentication state changes
     */
    setupAuthListeners() {
        // Check authentication state on page load
        this.checkAuthStateOnLoad();
        
        // Listen for login events (custom event)
        document.addEventListener('userLoggedIn', () => {
            this.mergeCartAfterLogin();
        });

        // Listen for logout events (custom event)
        document.addEventListener('userLoggedOut', () => {
            this.onLogout();
        });

        // Monitor for authentication state changes via DOM observation
        // This is a fallback if custom events aren't fired
        let lastAuthState = this.isAuthenticated();
        const observer = new MutationObserver(() => {
            const currentAuthState = this.isAuthenticated();
            if (currentAuthState !== lastAuthState) {
                lastAuthState = currentAuthState;
                if (currentAuthState) {
                    // User just logged in - merge cart
                    setTimeout(() => {
                        this.mergeCartAfterLogin();
                    }, 1000);
                } else {
                    // User just logged out - clear cart
                    this.onLogout();
                }
            }
        });

        // Observe body for changes
        if (document.body) {
            observer.observe(document.body, {
                childList: true,
                subtree: true,
                attributes: true,
                attributeFilter: ['data-is-authenticated', 'data-user-authenticated']
            });
        }
    }
    
    /**
     * Check authentication state on page load
     */
    checkAuthStateOnLoad() {
        // Check if user is authenticated by looking at body attribute or DOM elements
        const isAuth = this.isAuthenticated();
        const wasGuest = !localStorage.getItem('was_authenticated');
        
        if (isAuth && wasGuest) {
            // User just logged in - merge cart
            localStorage.setItem('was_authenticated', 'true');
            setTimeout(() => {
                this.mergeCartAfterLogin();
            }, 1500);
        } else if (!isAuth) {
            localStorage.removeItem('was_authenticated');
        } else {
            localStorage.setItem('was_authenticated', 'true');
        }
    }
    
    /**
     * Check if user is authenticated
     */
    isAuthenticated() {
        // Check body attribute
        const bodyAuth = document.body.getAttribute('data-is-authenticated');
        if (bodyAuth === 'true') {
            return true;
        }
        
        // Check window global
        if (window.isAuthenticated === true) {
            return true;
        }
        
        // Check for authenticated user elements in DOM
        const authElements = document.querySelectorAll('[data-user-authenticated="true"]');
        if (authElements.length > 0) {
            return true;
        }
        
        // Check for login/logout links
        const logoutLink = document.querySelector('form[action*="/Account/Logout"]');
        if (logoutLink) {
            return true;
        }
        
        return false;
    }

    /**
     * Notify that cart has changed (dispatch custom event)
     */
    notifyCartChanged() {
        const event = new CustomEvent('cartChanged', {
            detail: {
                cart: this.getCart(),
                count: this.getCartCount(),
                totalQuantity: this.getTotalQuantity()
            }
        });
        document.dispatchEvent(event);
    }

    /**
     * Get anti-forgery token from page
     */
    getAntiForgeryToken() {
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        return tokenInput ? tokenInput.value : '';
    }

    /**
     * Clear old data if storage is full
     */
    clearOldData() {
        // Remove old sync timestamps
        const keys = Object.keys(localStorage);
        keys.forEach(key => {
            if (key.startsWith('ecommerce_') && key !== this.storageKey) {
                localStorage.removeItem(key);
            }
        });
    }
}

// Create global instance
window.cartStorage = new CartStorageManager();

// Export for module systems
if (typeof module !== 'undefined' && module.exports) {
    module.exports = CartStorageManager;
}

