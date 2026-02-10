// Product Ratings Loader
// This script loads product ratings for all product cards on the page

(function() {
    'use strict';
    
    // Load ratings for all products on the page
    function loadAllProductRatings() {
        const ratingElements = document.querySelectorAll('.product-card-rating');
        if (ratingElements.length === 0) {
            console.warn('No rating elements found on page');
            return;
        }
        
        // Get all unique product IDs
        const productIds = Array.from(ratingElements)
            .map(el => {
                const productId = el.getAttribute('data-product-id');
                return productId ? parseInt(productId) : null;
            })
            .filter((id, index, self) => id !== null && self.indexOf(id) === index); // Remove duplicates and nulls
        
        if (productIds.length === 0) {
            console.warn('No valid product IDs found');
            return;
        }
        
        // Fetch ratings for all products in a single batch request
        fetch('/Customer/Review/GetBatchProductRatings', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(productIds)
        })
            .then(response => {
                if (!response.ok) {
                    console.error('Rating API error:', response.status, response.statusText);
                    return null;
                }
                return response.json();
            })
            .then(data => {
                if (data && data.ratings) {
                    // Update each product card with its rating
                    ratingElements.forEach(element => {
                        const productId = element.getAttribute('data-product-id');
                        if (productId && data.ratings[productId]) {
                            const rating = data.ratings[productId];
                            if (rating.reviewCount > 0) {
                                const starsHtml = getStarsHTML(rating.averageRating);
                                const starsElement = element.querySelector('.product-card-stars');
                                const numberElement = element.querySelector('.product-card-rating-number');
                                const textElement = element.querySelector('.product-card-rating-text');
                                
                                if (starsElement) starsElement.innerHTML = starsHtml;
                                if (numberElement) {
                                    numberElement.textContent = rating.averageRating.toFixed(1);
                                    numberElement.style.display = 'inline'; // Show rating number
                                }
                                if (textElement) textElement.textContent = `(${rating.reviewCount})`;
                            }
                        }
                    });
                } else {
                    console.warn('No ratings data in response');
                }
            })
            .catch(error => console.error('Error loading ratings:', error));
    }
    
    // Generate HTML for star rating display
    function getStarsHTML(rating) {
        const fullStars = Math.floor(rating);
        const hasHalf = (rating % 1) >= 0.5;
        let starsHtml = '';
        
        // Full stars
        for (let i = 0; i < fullStars; i++) {
            starsHtml += '<i class="bi bi-star-fill"></i>';
        }
        
        // Half star
        if (hasHalf) {
            starsHtml += '<i class="bi bi-star-half"></i>';
        }
        
        // Empty stars
        for (let i = fullStars + (hasHalf ? 1 : 0); i < 5; i++) {
            starsHtml += '<i class="bi bi-star"></i>';
        }
        
        return starsHtml;
    }
    
    // Expose functions globally
    window.loadAllProductRatings = loadAllProductRatings;
    window.getStarsHTML = getStarsHTML;
    
    // Auto-load ratings when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function() {
            setTimeout(loadAllProductRatings, 200);
        });
    } else {
        // DOM already loaded
        setTimeout(loadAllProductRatings, 200);
    }
    
})();
