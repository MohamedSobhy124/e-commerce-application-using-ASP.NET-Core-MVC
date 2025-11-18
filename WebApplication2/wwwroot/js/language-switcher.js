// Language Switcher using JavaScript and Cookies
// Default: Arabic

// Function to set language cookie
function setLanguage(culture) {
    // Set cookie for 1 year
    const expires = new Date();
    expires.setFullYear(expires.getFullYear() + 1);
    
    // Create the culture cookie value
    const cookieValue = `c=${culture}|uic=${culture}`;
    
    // Set the cookie
    document.cookie = `.AspNetCore.Culture=${cookieValue}; expires=${expires.toUTCString()}; path=/; SameSite=Lax`;
    
    // Reload the page to apply the language
    window.location.reload();
}

// Function to get current language from cookie
function getCurrentLanguage() {
    const cookies = document.cookie.split(';');
    for (let cookie of cookies) {
        cookie = cookie.trim();
        if (cookie.startsWith('.AspNetCore.Culture=')) {
            const value = cookie.substring('.AspNetCore.Culture='.length);
            // Extract culture code (e.g., "c=ar|uic=ar" -> "ar")
            const match = value.match(/c=([^|]+)/);
            return match ? match[1] : 'ar'; // Default to Arabic
        }
    }
    return 'ar'; // Default to Arabic if no cookie
}

// Initialize default language on first visit
function initializeLanguage() {
    const currentLang = getCurrentLanguage();
    
    // If no cookie exists, set Arabic as default
    if (!document.cookie.includes('.AspNetCore.Culture=')) {
        setLanguage('ar');
    }
    
    // Update the language switcher display
    updateLanguageSwitcherDisplay(currentLang);
}

// Update the language switcher dropdown to show current language
function updateLanguageSwitcherDisplay(currentLang) {
    const langDisplay = document.getElementById('currentLanguageDisplay');
    if (langDisplay) {
        langDisplay.textContent = currentLang === 'ar' ? 'العربية' : 'English';
    }
    
    // Update active state in dropdown
    const arButton = document.getElementById('langButtonAr');
    const enButton = document.getElementById('langButtonEn');
    
    if (arButton && enButton) {
        if (currentLang === 'ar') {
            arButton.classList.add('active');
            enButton.classList.remove('active');
        } else {
            enButton.classList.add('active');
            arButton.classList.remove('active');
        }
    }
}

// Switch to Arabic
function switchToArabic() {
    setLanguage('ar');
}

// Switch to English
function switchToEnglish() {
    setLanguage('en');
}

// Run on page load
document.addEventListener('DOMContentLoaded', function() {
    // Check if cookie exists
    if (!document.cookie.includes('.AspNetCore.Culture=')) {
        // No cookie - set Arabic as default without reload
        const expires = new Date();
        expires.setFullYear(expires.getFullYear() + 1);
        document.cookie = `.AspNetCore.Culture=c=ar|uic=ar; expires=${expires.toUTCString()}; path=/; SameSite=Lax`;
        
        // Force reload to apply Arabic
        if (document.documentElement.lang !== 'ar') {
            window.location.reload();
            return;
        }
    }
    
    const currentLang = getCurrentLanguage();
    updateLanguageSwitcherDisplay(currentLang);
});

