// Real-time Notification System using SignalR
let notificationConnection = null;

// Initialize SignalR connection
function initializeNotifications() {
    if (typeof signalR === 'undefined') {
        console.error('SignalR library not loaded');
        return;
    }

    notificationConnection = new signalR.HubConnectionBuilder()
        .withUrl("/notificationHub")
        .withAutomaticReconnect()
        .build();

    // Handle incoming notifications
    notificationConnection.on("ReceiveOrderNotification", function (data) {
        
        // Show toastr notification
        if (typeof toastr !== 'undefined') {
            toastr.success(
                `Order #${data.orderId} - ${data.message}<br>Total: ${(typeof getCurrencySymbol === 'function' ? getCurrencySymbol() : (typeof getCurrentLanguage === 'function' && getCurrentLanguage() === 'ar' ? 'د.إ' : 'AED'))} ${data.total.toFixed(2)}`,
                data.title,
                {
                    timeOut: 10000,
                    extendedTimeOut: 5000,
                    closeButton: true,
                    progressBar: true,
                    onclick: function() {
                        window.location.href = `/Admin/Order/Details/${data.orderId}`;
                    }
                }
            );
        }

        // Play notification sound
        playNotificationSound();

        // Update notification bell
        updateNotificationBell();

        // Show browser notification if permitted
        showBrowserNotification(data.title, data.message);
    });

    notificationConnection.on("ReceiveOrderConfirmation", function (data) {
        
        if (typeof toastr !== 'undefined') {
            toastr.success(
                data.message,
                data.title,
                {
                    timeOut: 8000,
                    closeButton: true,
                    progressBar: true
                }
            );
        }
    });

    // Handle general notifications (including ReturnRequest)
    notificationConnection.on("ReceiveNotification", function (data) {
        // Determine redirect URL based on notification type
        let redirectUrl = null;
        
        if (data.type === "ReturnRequest" && data.returnRequestId) {
            redirectUrl = `/Admin/ReturnRequest/Details/${data.returnRequestId}`;
        } else if (data.type === "Order" && data.orderId) {
            redirectUrl = `/Admin/Order/Details/${data.orderId}`;
        } else if (data.orderId) {
            // Fallback: if orderId exists, redirect to order details
            redirectUrl = `/Admin/Order/Details/${data.orderId}`;
        }
        
        // Show toastr notification
        if (typeof toastr !== 'undefined') {
            const toastrType = data.type === "ReturnRequest" ? 'info' : 'success';
            toastr[toastrType](
                data.message,
                data.title,
                {
                    timeOut: 10000,
                    extendedTimeOut: 5000,
                    closeButton: true,
                    progressBar: true,
                    onclick: function() {
                        if (redirectUrl) {
                            window.location.href = redirectUrl;
                        }
                    }
                }
            );
        }

        // Play notification sound
        playNotificationSound();

        // Update notification bell
        updateNotificationBell();

        // Show browser notification if permitted
        showBrowserNotification(data.title, data.message);
    });

    // Handle stock alerts (for admins)
    notificationConnection.on("ReceiveStockAlert", function (data) {
        
        // Determine alert type
        const toastrType = data.isOutOfStock ? 'error' : 'warning';
        const icon = data.isOutOfStock ? '❌' : '⚠️';
        
        // Show toastr notification
        if (typeof toastr !== 'undefined') {
            toastr[toastrType](
                `${icon} ${data.message}<br><strong>Product:</strong> ${data.productName}<br><strong>Stock:</strong> ${data.stockQuantity} units`,
                data.title,
                {
                    timeOut: data.isOutOfStock ? 0 : 15000, // Don't auto-close out of stock alerts
                    extendedTimeOut: 10000,
                    closeButton: true,
                    progressBar: true,
                    onclick: function() {
                        window.location.href = `/Admin/Product/UpSert/${data.productId}`;
                    },
                    escapeHtml: false
                }
            );
        }

        // Play urgent sound for out of stock
        if (data.isOutOfStock) {
            playNotificationSound();
            playNotificationSound(); // Play twice for urgency
        } else {
            playNotificationSound();
        }

        // Update notification bell
        updateNotificationBell();

        // Show browser notification
        showBrowserNotification(
            `${data.urgency}: ${data.title}`,
            `${data.productName} - ${data.message}`
        );
    });

    // Start connection
    notificationConnection.start()
        .then(function () {
            
            // Join admin group if user is admin
            if (isUserAdmin()) {
                notificationConnection.invoke("JoinAdminGroup")
                    .then(() => console.log(' '))
                    .catch(err => console.error('', err));
            }
        })
        .catch(function (err) {
            console.error('SignalR connection error:', err);
        });

    // Reconnection handling
    notificationConnection.onreconnecting(function() {
    });

    notificationConnection.onreconnected(function() {
        if (isUserAdmin()) {
            notificationConnection.invoke("JoinAdminGroup");
        }
    });

    notificationConnection.onclose(function() {
    });
}

// Check if user is admin (you can customize this based on your role check)
function isUserAdmin() {
    // Check if user has admin role - you might need to pass this from the server
    const userRoles = document.querySelector('meta[name="user-roles"]')?.content || '';
    return userRoles.includes('Admin');
}

// Update notification bell count
function updateNotificationBell() {
    const badge = document.getElementById('notificationBadge');
    if (badge) {
        let currentCount = parseInt(badge.textContent) || 0;
        currentCount++;
        badge.textContent = currentCount;
        badge.style.display = 'inline-block';
        
        // Animate bell
        const bell = document.getElementById('notificationBell');
        if (bell) {
            bell.classList.add('ring');
            setTimeout(() => bell.classList.remove('ring'), 1000);
        }
    }
}

// Play notification sound
function playNotificationSound() {
    try {
        const audio = new Audio('data:audio/wav;base64,UklGRnoGAABXQVZFZm10IBAAAAABAAEAQB8AAEAfAAABAAgAZGF0YQoGAACBhYqFbF1fdJivrJBhNjVgodDbq2EcBj+a2/LDciUFLIHO8tiJNwgZaLvt559NEAxQp+PwtmMcBjiR1/LMeSwFJHfH8N2QQAoUXrTp66hVFApGn+DyvmwhBSuBzvLZiTYIG2m98OScTgwOUKfk77RgGgU7k9nzzn0pBSh+zPLaizsKGGS56+mnUhQKQ5zd8sFuJAUthM/z2Yk3CBppu+zn');
        audio.play().catch(e => console.log( '', e));
    } catch (e) {
    }
}

// Show browser notification
function showBrowserNotification(title, message) {
    if (!("Notification" in window)) {
        return;
    }

    if (Notification.permission === "granted") {
        const notification = new Notification(title, {
            body: message,
            icon: '/favicon.ico',
            badge: '/favicon.ico',
            tag: 'order-notification',
            requireInteraction: true
        });

        notification.onclick = function() {
            window.focus();
            notification.close();
        };
    } else if (Notification.permission !== "denied") {
        Notification.requestPermission().then(function (permission) {
            if (permission === "granted") {
                showBrowserNotification(title, message);
            }
        });
    }
}

// Request notification permission
function requestNotificationPermission() {
    if ("Notification" in window && Notification.permission === "default") {
        Notification.requestPermission();
    }
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    // Initialize SignalR
    initializeNotifications();
    
    // Request notification permission for admins
    if (isUserAdmin()) {
        requestNotificationPermission();
    }
});

// Cleanup on page unload
window.addEventListener('beforeunload', function() {
    if (notificationConnection) {
        notificationConnection.stop();
    }
});

