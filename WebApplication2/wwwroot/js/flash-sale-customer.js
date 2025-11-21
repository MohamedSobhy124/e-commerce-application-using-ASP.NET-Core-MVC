// ============================================
// FLASH SALE CUSTOMER JAVASCRIPT - MEGA COOL!
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
                <span class="timer-label">Minutes</span>
            </div>
            <div class="timer-segment">
                <span class="timer-number">${this.padZero(seconds)}</span>
                <span class="timer-label">Seconds</span>
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

// Initialize all flash sale timers
function initFlashSaleTimers() {
    // Main hero timer
    const heroTimer = document.querySelector('[data-flash-sale-end]');
    if (heroTimer) {
        const endDate = heroTimer.getAttribute('data-flash-sale-end');
        const timerId = heroTimer.id;
        const timer = new FlashSaleTimer(endDate, timerId, () => {
            location.reload(); // Reload page when timer expires
        });
        timer.start();
    }

    // Product timers
    document.querySelectorAll('[data-product-timer-end]').forEach(timerElement => {
        const endDate = timerElement.getAttribute('data-product-timer-end');
        const timerId = timerElement.id;
        const timer = new ProductFlashTimer(endDate, timerId);
        timer.start();
    });
}

// Add flash sale item to cart
function addFlashSaleToCart(flashSaleItemId, productId, flashSalePrice) {
    // Show loading state
    const btn = event.target;
    const originalText = btn.innerHTML;
    btn.disabled = true;
    btn.innerHTML = '<i class="bi bi-hourglass-split spinning"></i> Adding...';

    // Prepare data
    const formData = new URLSearchParams();
    formData.append('productId', productId);
    formData.append('flashSaleItemId', flashSaleItemId);
    formData.append('flashSalePrice', flashSalePrice);
    formData.append('count', 1);

    // Add to cart
    fetch('/Customer/Cart/AddFlashSaleToCart', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
        },
        body: formData
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            // Success!
            triggerSuccessAnimation(btn);
            toastr.success(data.message || 'Flash sale item added to cart!');
            
            // Update cart count if exists
            if (data.cartCount !== undefined) {
                const cartCountElement = document.getElementById('cartCount');
                if (cartCountElement) {
                    cartCountElement.textContent = data.cartCount;
                    cartCountElement.classList.add('cart-count-pulse');
                    setTimeout(() => {
                        cartCountElement.classList.remove('cart-count-pulse');
                    }, 600);
                }
            }

            // Update button
            btn.innerHTML = '<i class="bi bi-check-circle-fill"></i> Added!';
            btn.classList.add('btn-success');
            
            setTimeout(() => {
                btn.innerHTML = originalText;
                btn.classList.remove('btn-success');
                btn.disabled = false;
            }, 2000);

        } else {
            // Error
            toastr.error(data.message || 'Could not add item to cart');
            btn.innerHTML = originalText;
            btn.disabled = false;
        }
    })
    .catch(error => {
        console.error('Error:', error);
        toastr.error('An error occurred. Please try again.');
        btn.innerHTML = originalText;
        btn.disabled = false;
    });
}

// Success animation
function triggerSuccessAnimation(button) {
    const card = button.closest('.flash-sale-product-card');
    if (card) {
        card.style.animation = 'none';
        setTimeout(() => {
            card.style.animation = 'successShake 0.5s ease';
        }, 10);
    }
}

// Update stock progress bars
function updateStockProgressBars() {
    document.querySelectorAll('[data-stock-percentage]').forEach(bar => {
        const percentage = parseFloat(bar.getAttribute('data-stock-percentage'));
        const fillElement = bar.querySelector('.stock-progress-bar-fill');
        if (fillElement) {
            fillElement.style.width = percentage + '%';
            
            // Change color based on stock level
            if (percentage < 20) {
                fillElement.style.background = 'linear-gradient(90deg, #e74c3c, #c0392b)';
            } else if (percentage < 50) {
                fillElement.style.background = 'linear-gradient(90deg, #f39c12, #e67e22)';
            }
        }
    });
}

// Lightning effect
function createLightningEffect() {
    const lightningElements = document.querySelectorAll('.lightning-icon');
    lightningElements.forEach(element => {
        setInterval(() => {
            element.style.opacity = Math.random() * 0.3 + 0.7;
        }, 150);
    });
}

// Fire effect
function createFireEffect() {
    const fireElements = document.querySelectorAll('.fire-effect');
    fireElements.forEach(element => {
        setInterval(() => {
            const randomScale = 0.9 + Math.random() * 0.2;
            element.style.transform = `scale(${randomScale})`;
        }, 100);
    });
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    initFlashSaleTimers();
    updateStockProgressBars();
    createLightningEffect();
    createFireEffect();
    
    console.log('🔥 Flash Sale System Initialized!');
});

// CSS animation for success shake
const style = document.createElement('style');
style.textContent = `
    @keyframes successShake {
        0%, 100% { transform: translateX(0) translateY(0); }
        10%, 30%, 50%, 70%, 90% { transform: translateX(-5px) translateY(-5px); }
        20%, 40%, 60%, 80% { transform: translateX(5px) translateY(5px); }
    }
    
    .cart-count-pulse {
        animation: cartPulse 0.6s ease;
    }
    
    @keyframes cartPulse {
        0%, 100% { transform: scale(1); }
        50% { transform: scale(1.3); }
    }
    
    .spinning {
        animation: spin 1s linear infinite;
    }
    
    @keyframes spin {
        from { transform: rotate(0deg); }
        to { transform: rotate(360deg); }
    }
`;
document.head.appendChild(style);



