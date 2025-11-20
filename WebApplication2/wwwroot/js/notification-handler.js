// Notification Bell Handler
let notificationDropdownOpen = false;

// Toggle notification dropdown
function toggleNotifications() {
    const dropdown = document.getElementById('notificationDropdown');
    const bell = document.getElementById('notificationBell');
    const dot = document.getElementById('notificationDot');
    
    notificationDropdownOpen = !notificationDropdownOpen;
    
    if (notificationDropdownOpen) {
        dropdown.classList.add('active');
        loadNotifications();
        
        // Hide dot indicator when opening
        if (dot) {
            dot.classList.remove('active');
        }
    } else {
        dropdown.classList.remove('active');
    }
}

// Close dropdown when clicking outside
document.addEventListener('click', function(event) {
    const container = document.querySelector('.notification-bell-container');
    if (container && !container.contains(event.target) && notificationDropdownOpen) {
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

