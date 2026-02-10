// Service Ratings Loader
// This script loads service ratings for all service cards on the page

(function() {
    'use strict';
    
    // Load ratings for all services on the page
    function loadAllServiceRatings() {
        const ratingElements = document.querySelectorAll('.service-card-rating');
        
        if (ratingElements.length === 0) {
            return;
        }
        
        // Get all unique service IDs
        const serviceIds = Array.from(ratingElements)
            .map(el => {
                const serviceId = el.getAttribute('data-service-id');
                return serviceId ? parseInt(serviceId) : null;
            })
            .filter((id, index, self) => id !== null && self.indexOf(id) === index); // Remove duplicates and nulls
        
        console.log('📋 Service IDs to fetch:', serviceIds);
        
        if (serviceIds.length === 0) {
            console.warn('⚠️ No valid service IDs found');
            return;
        }
        
        // Fetch ratings for all services in a single batch request
        fetch('/Customer/Review/GetBatchServiceRatings', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(serviceIds)
        })
            .then(response => {
                if (!response.ok) {
                    return null;
                }
                return response.json();
            })
            .then(data => {
                
                if (data && data.ratings) {
                    
                    // Update each service card with its rating
                    let updatedCount = 0;
                    ratingElements.forEach(element => {
                        const serviceId = parseInt(element.getAttribute('data-service-id'));
                        
                        if (serviceId && data.ratings[serviceId]) {
                            const rating = data.ratings[serviceId];
                            
                            // Always update, even if reviewCount is 0
                            const starsHtml = getStarsHTML(rating.averageRating);
                            const starsElement = element.querySelector('.service-card-stars');
                            const numberElement = element.querySelector('.service-card-rating-number');
                            const textElement = element.querySelector('.service-card-rating-text');
                            
                            if (starsElement) {
                                starsElement.innerHTML = starsHtml;
                            }
                            if (numberElement) {
                                numberElement.textContent = rating.averageRating.toFixed(1);
                                if (rating.reviewCount > 0) {
                                    numberElement.style.display = 'inline'; // Show rating number
                                }
                            }
                            if (textElement) {
                                textElement.textContent = `(${rating.reviewCount})`;
                            }
                            
                            // Hide rating container if no reviews
                            if (rating.reviewCount === 0) {
                            } else {
                                element.style.display = 'flex';
                                updatedCount++;
                            }
                        } 
                    });
                } 
            })
            .catch(error => {
            });
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
    window.loadAllServiceRatings = loadAllServiceRatings;
    window.getServiceStarsHTML = getStarsHTML;
    
    // Auto-load ratings when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function() {
            setTimeout(loadAllServiceRatings, 200);
        });
    } else {
        // DOM already loaded
        setTimeout(loadAllServiceRatings, 200);
    }
    
})();
