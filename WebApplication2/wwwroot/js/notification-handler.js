// Notification Bell Handler
let notificationDropdownOpen = false;

// Initialize notification dropdown behavior
document.addEventListener('DOMContentLoaded', function() {
    const dropdown = document.getElementById('notificationDropdown');
    const toggle = document.getElementById('notificationDropdownToggle');
    const dot = document.getElementById('notificationDot');
    
    if (!dropdown || !toggle) return;
    
    // Function to check if mobile
    function isMobile() {
        return window.innerWidth <= 991;
    }
    
    // Listen for Bootstrap dropdown show/hide events
    toggle.addEventListener('show.bs.dropdown', function(e) {
        console.log('Notification dropdown showing... (show.bs.dropdown)');
        // Prevent event from bubbling up
        e.stopPropagation();
    });
    
    toggle.addEventListener('shown.bs.dropdown', function(e) {
        console.log('Notification dropdown shown (shown.bs.dropdown)');
        notificationDropdownOpen = true;
        console.log('Notification dropdown state:', notificationDropdownOpen);
        loadNotifications();
        if (dot) {
            dot.classList.remove('active');
        }
        e.stopPropagation();
    });
    
    toggle.addEventListener('hide.bs.dropdown', function(e) {
        console.log('Notification dropdown hiding... (hide.bs.dropdown)');
        console.trace('Hide triggered from:');
    });
    
    toggle.addEventListener('hidden.bs.dropdown', function(e) {
        console.log('Notification dropdown hidden (hidden.bs.dropdown)');
        notificationDropdownOpen = false;
        console.log('Notification dropdown state:', notificationDropdownOpen);
    });
    
    // Desktop - override default behavior
    if (!isMobile()) {
        toggle.addEventListener('click', function(e) {
            e.preventDefault();
            e.stopPropagation();
            toggleNotificationsDesktop();
        });
        
        // Remove Bootstrap toggle on desktop
        toggle.removeAttribute('data-bs-toggle');
    } else {
        // On mobile, let Bootstrap auto-initialize via data-bs-toggle
        // Don't create a new instance if one already exists
        // Bootstrap will handle it automatically
    }
    
    // Handle window resize
    let resizeTimer;
    window.addEventListener('resize', function() {
        clearTimeout(resizeTimer);
        resizeTimer = setTimeout(function() {
            const nowMobile = isMobile();
            if (nowMobile && !toggle.hasAttribute('data-bs-toggle')) {
                toggle.setAttribute('data-bs-toggle', 'dropdown');
            } else if (!nowMobile && toggle.hasAttribute('data-bs-toggle')) {
                toggle.removeAttribute('data-bs-toggle');
            }
        }, 250);
    });
});

// Desktop toggle function
function toggleNotificationsDesktop() {
    const dropdown = document.getElementById('notificationDropdown');
    const dot = document.getElementById('notificationDot');
    
    notificationDropdownOpen = !notificationDropdownOpen;
    
    if (notificationDropdownOpen) {
        dropdown.classList.add('show', 'active');
        loadNotifications();
    } else {
        dropdown.classList.remove('show', 'active');
    }
    
    // Hide dot indicator when opening
    if (dot && notificationDropdownOpen) {
        dot.classList.remove('active');
    }
}

// Legacy toggle function for backward compatibility
function toggleNotifications() {
    const isMobile = window.innerWidth <= 991;
    if (isMobile) {
        // On mobile, Bootstrap handles dropdown via data-bs-toggle
        // Don't interfere - return immediately to prevent double-toggling
        console.log('toggleNotifications() called on mobile - Bootstrap handles this');
        return;
    } else {
        toggleNotificationsDesktop();
    }
}

// Close dropdown when clicking outside (desktop)
document.addEventListener('click', function(event) {
    const dropdown = document.getElementById('notificationDropdown');
    const toggle = document.getElementById('notificationDropdownToggle');
    const isMobile = window.innerWidth <= 991;
    
    if (isMobile) {
        // Bootstrap handles this on mobile
        return;
    }
    
    if (toggle && !toggle.contains(event.target) && 
        dropdown && !dropdown.contains(event.target) && 
        notificationDropdownOpen) {
        toggleNotifications();
    }
});

// Load notifications
async function loadNotifications() {
    try {
        const response = await fetch('/api/notifications/unread');
        if (response.ok) {
            const notifications = await response.json();
            displayNotifications(notifications);
        }
    } catch (error) {
         
    }
}

// Display notifications in dropdown
function displayNotifications(notifications) {
    const listContainer = document.getElementById('notificationList');
    const badge = document.getElementById('notificationBadge');
    
    if (!notifications || notifications.length === 0) {
        listContainer.innerHTML = `
            <div class="notification-empty">
                <i class="bi bi-bell-slash"></i>
                <p>No new notifications</p>
            </div>
        `;
        badge.textContent = '0';
        badge.style.display = 'none';
        return;
    }
    
    badge.textContent = notifications.length;
    badge.classList.add('active');
    
    let html = '';
    notifications.forEach(notification => {
        html += createNotificationHTML(notification);
    });
    
    listContainer.innerHTML = html;
}

// Create notification HTML
function createNotificationHTML(notification) {
    const timeAgo = getTimeAgo(new Date(notification.createdAt));
    const unreadClass = notification.isRead ? '' : 'unread';
    
    return `
        <div class="notification-item ${unreadClass}" onclick="handleNotificationClick(${notification.id}, '${notification.link || '#'}')">
            <div class="notification-icon">
                <i class="bi ${notification.icon || 'bi-bell'}"></i>
            </div>
            <div class="notification-content">
                <h6 class="notification-item-title">${notification.title}</h6>
                <p class="notification-item-message">${notification.message}</p>
                <span class="notification-item-time">
                    <i class="bi bi-clock me-1"></i>${timeAgo}
                </span>
            </div>
        </div>
    `;
}

// Handle notification click
async function handleNotificationClick(notificationId, link) {
    try {
        // Mark as read
        await fetch(`/api/notifications/mark-read/${notificationId}`, {
            method: 'POST'
        });
        
        // Redirect if link exists
        if (link && link !== '#') {
            window.location.href = link;
        }
        
        // Reload notifications
        loadNotifications();
    } catch (error) {
        console.error('Error marking notification as read:', error);
    }
}

// Mark all as read
async function markAllAsRead() {
    try {
        const response = await fetch('/api/notifications/mark-all-read', {
            method: 'POST'
        });
        
        if (response.ok) {
            loadNotifications();
            
            const badge = document.getElementById('notificationBadge');
            if (badge) {
                badge.textContent = '0';
                badge.style.display = 'none';
            }
            
            if (typeof toastr !== 'undefined') {
                toastr.success('All notifications marked as read');
            }
        }
    } catch (error) {
        console.error('Error marking all as read:', error);
    }
}

// Get time ago string
function getTimeAgo(date) {
    const seconds = Math.floor((new Date() - date) / 1000);
    
    let interval = seconds / 31536000;
    if (interval > 1) return Math.floor(interval) + " years ago";
    
    interval = seconds / 2592000;
    if (interval > 1) return Math.floor(interval) + " months ago";
    
    interval = seconds / 86400;
    if (interval > 1) return Math.floor(interval) + " days ago";
    
    interval = seconds / 3600;
    if (interval > 1) return Math.floor(interval) + " hours ago";
    
    interval = seconds / 60;
    if (interval > 1) return Math.floor(interval) + " minutes ago";
    
    return Math.floor(seconds) + " seconds ago";
}

// Add meta tag for user roles (call this from your layout)
function setUserRoles(roles) {
    const existingMeta = document.querySelector('meta[name="user-roles"]');
    if (existingMeta) {
        existingMeta.content = roles;
    } else {
        const meta = document.createElement('meta');
        meta.name = 'user-roles';
        meta.content = roles;
        document.head.appendChild(meta);
    }
}

