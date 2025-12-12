/* ===================================
   Modern Home Screen - Professional UX/UI
   AJAX Functionality & Smooth Interactions
   =================================== */

(function() {
    'use strict';

    // ===================================
    // 1. STICKY HEADER & SCROLL BEHAVIOR
    // ===================================
    
    const modernHeader = document.getElementById('modernHomeHeader') || document.getElementById('heroSearchBoxSticky');
    const heroSearchBoxSticky = document.getElementById('heroSearchBoxSticky');
    let lastScrollTop = 0;
    
    if (modernHeader || heroSearchBoxSticky) {
        const headerElement = modernHeader || heroSearchBoxSticky;
        let ticking = false;
        window.addEventListener('scroll', function() {
            if (!ticking) {
                window.requestAnimationFrame(() => {
                    const scrollTop = window.pageYOffset || document.documentElement.scrollTop;
                    
                    if (scrollTop > 50) {
                        headerElement.classList.add('scrolled');
                    } else {
                        headerElement.classList.remove('scrolled');
                    }
                    
                    lastScrollTop = scrollTop;
                    ticking = false;
                });
                ticking = true;
            }
        }, { passive: true });
    }

    // ===================================
    // 2. SEARCH FUNCTIONALITY
    // ===================================
    
    const searchInput = document.getElementById('modernSearchInput') || document.getElementById('heroSearchInput');
    const searchForm = document.getElementById('modernSearchForm') || document.getElementById('heroSearchForm');
    const searchSuggestions = document.getElementById('modernSearchSuggestions');
    let searchTimeout;
    let currentSearchTerm = '';
    
    // Debounced search suggestions
    if (searchInput) {
        searchInput.addEventListener('input', function() {
            const term = this.value.trim();
            currentSearchTerm = term;
            
            clearTimeout(searchTimeout);
            
            if (term.length >= 2) {
                searchTimeout = setTimeout(() => {
                    fetchSearchSuggestions(term);
                }, 300);
            } else {
                hideSearchSuggestions();
            }
        });
        
        searchInput.addEventListener('focus', function() {
            if (this.value.trim().length >= 2) {
                showSearchSuggestions();
            }
        });
        
        // Hide suggestions when clicking outside
        document.addEventListener('click', function(e) {
            if (searchInput && searchSuggestions && 
                !searchInput.contains(e.target) && !searchSuggestions.contains(e.target)) {
                hideSearchSuggestions();
            }
        });
    }
    
    // AJAX search form submission
    if (searchForm) {
        searchForm.addEventListener('submit', function(e) {
            e.preventDefault();
            const searchTerm = searchInput.value.trim();
            performSearch(searchTerm);
        });
    }
    
    // Also handle regular form submission (fallback) - scroll after page load
    if (searchForm && window.location.search.includes('searchTerm=')) {
        // Page loaded with search term, scroll to products
        window.addEventListener('load', function() {
            setTimeout(() => {
                scrollToProductsSection();
            }, 500);
        });
    }
    
    function fetchSearchSuggestions(term) {
        // TODO: Implement search suggestions API
        // For now, just show/hide the suggestions container
        if (term.length >= 2) {
            showSearchSuggestions();
            // Populate suggestions would go here
        }
    }
    
    function showSearchSuggestions() {
        if (searchSuggestions) {
            searchSuggestions.classList.add('active');
        }
    }
    
    function hideSearchSuggestions() {
        if (searchSuggestions) {
            searchSuggestions.classList.remove('active');
        }
    }
    
    function performSearch(searchTerm) {
        hideSearchSuggestions();
        // Clear current filters and set only search term
        currentFilters = { searchTerm: searchTerm };
        // Use AJAX without updating URL
        if (typeof loadProductsWithFilters === 'function') {
            loadProductsWithFilters({ searchTerm: searchTerm }, true, false);
        }
    }
    
    // Function to smoothly scroll to products section
    function scrollToProductsSection() {
        const productsSection = document.getElementById('productsSection');
        if (productsSection) {
            const headerHeight = document.getElementById('heroSearchBoxSticky')?.offsetHeight || 0;
            const filterChipsHeight = document.querySelector('.modern-filter-chips-bar')?.offsetHeight || 0;
            const totalOffset = headerHeight + filterChipsHeight + 20;
            const targetPosition = productsSection.offsetTop - totalOffset;
            
            window.scrollTo({
                top: Math.max(0, targetPosition),
                behavior: 'smooth'
            });
        }
    }

    // ===================================
    // 3. EXPANDABLE FILTER SYSTEM
    // ===================================
    
    const filterToggleBtn = document.getElementById('modernFilterToggleBtn');
    const filterOverlay = document.getElementById('modernFilterOverlay');
    const filterBottomSheet = document.getElementById('modernFilterBottomSheet');
    const filterSidePanel = document.getElementById('modernFilterSidePanel');
    const filterBottomSheetClose = document.getElementById('modernFilterBottomSheetClose');
    const filterSidePanelClose = document.getElementById('modernFilterSidePanelClose');
    const originalFilterSection = document.getElementById('originalFilterSection');
    
    // Move filter content to appropriate container on load
    if (originalFilterSection && filterBottomSheet && filterSidePanel) {
        const filterContent = originalFilterSection.querySelector('.filter-groups');
        if (filterContent) {
            // Clone for mobile
            const mobileFilterContent = filterContent.cloneNode(true);
            // Remove any submit buttons from cloned content
            const mobileSubmitBtns = mobileFilterContent.querySelectorAll('button[type="submit"]');
            mobileSubmitBtns.forEach(btn => btn.remove());
            filterBottomSheet.querySelector('.modern-filter-bottom-sheet-content').appendChild(mobileFilterContent);
            
            // Clone for desktop
            const desktopFilterContent = filterContent.cloneNode(true);
            // Remove any submit buttons from cloned content
            const desktopSubmitBtns = desktopFilterContent.querySelectorAll('button[type="submit"]');
            desktopSubmitBtns.forEach(btn => btn.remove());
            filterSidePanel.querySelector('.modern-filter-side-panel-content').appendChild(desktopFilterContent);
        }
    }
    
    // Prevent form submission on the original form
    const originalFilterForm = document.getElementById('filterForm');
    if (originalFilterForm) {
        originalFilterForm.addEventListener('submit', function(e) {
            e.preventDefault();
            return false;
        });
        
        // Prevent category select from auto-submitting
        const categorySelects = originalFilterForm.querySelectorAll('select[name="categoryId"]');
    
    }
    
    // Prevent form submission in cloned forms (mobile and desktop panels)
    function preventFormSubmission() {
        const clonedForms = document.querySelectorAll('.modern-filter-bottom-sheet form, .modern-filter-side-panel form');
        clonedForms.forEach(form => {
            form.addEventListener('submit', function(e) {
                e.preventDefault();
                return false;
            });
            
            // Prevent category select from auto-submitting
            const categorySelects = form.querySelectorAll('select[name="categoryId"]');
            categorySelects.forEach(select => {
                select.addEventListener('change', function(e) {
                    e.preventDefault();
                    // Don't auto-apply, wait for user to click Apply button
                });
            });
        });
    }
    
    // Call after cloning
    setTimeout(preventFormSubmission, 100);
    
    // Mobile: Open bottom sheet
    if (filterToggleBtn) {
        filterToggleBtn.addEventListener('click', function() {
            if (window.innerWidth <= 768) {
                openFilterBottomSheet();
            } else {
                openFilterSidePanel();
            }
        });
    }
    
    // Mobile: Close bottom sheet
    if (filterBottomSheetClose) {
        filterBottomSheetClose.addEventListener('click', closeFilterBottomSheet);
    }
    
    // Desktop: Close side panel
    if (filterSidePanelClose) {
        filterSidePanelClose.addEventListener('click', closeFilterSidePanel);
    }
    
    // Close on overlay click
    if (filterOverlay) {
        filterOverlay.addEventListener('click', function() {
            closeFilterBottomSheet();
            closeFilterSidePanel();
        });
    }
    
    // Filter chip clicks (mobile)
    const filterChips = document.querySelectorAll('.modern-filter-chip');
    filterChips.forEach(chip => {
        chip.addEventListener('click', function() {
            if (window.innerWidth <= 768) {
                openFilterBottomSheet();
            } else {
                openFilterSidePanel();
            }
        });
    });
    
    function openFilterBottomSheet() {
        if (filterBottomSheet && filterOverlay) {
            filterBottomSheet.classList.add('active');
            filterOverlay.classList.add('active');
            document.body.style.overflow = 'hidden';
        }
    }
    
    function closeFilterBottomSheet() {
        if (filterBottomSheet && filterOverlay) {
            filterBottomSheet.classList.remove('active');
            filterOverlay.classList.remove('active');
            document.body.style.overflow = '';
        }
    }
    
    function openFilterSidePanel() {
        if (filterSidePanel && filterOverlay) {
            filterSidePanel.classList.add('active');
            filterOverlay.classList.add('active');
        }
    }
    
    function closeFilterSidePanel() {
        if (filterSidePanel && filterOverlay) {
            filterSidePanel.classList.remove('active');
            filterOverlay.classList.remove('active');
        }
    }
    
    // Apply filters (AJAX)
    const applyFilterBtns = document.querySelectorAll('.modern-filter-apply-btn');
    applyFilterBtns.forEach(btn => {
        btn.addEventListener('click', function() {
            // Find the filter container (bottom sheet or side panel)
            const filterContainer = this.closest('.modern-filter-bottom-sheet, .modern-filter-side-panel');
            
            // Try to find form in the container, or fallback to original form
            let filterForm = null;
            if (filterContainer) {
                filterForm = filterContainer.querySelector('form');
                // If no form found, look for filter-groups div (cloned content)
                if (!filterForm) {
                    const filterGroups = filterContainer.querySelector('.filter-groups');
                    if (filterGroups) {
                        // Create a temporary form-like object to collect data
                        const filters = collectFiltersFromGroups(filterGroups);
                        console.log('Collected filters from groups:', filters);
                        closeFilterBottomSheet();
                        closeFilterSidePanel();
                        loadProductsWithFilters(filters, true);
                        return;
                    }
                }
            }
            
            // Fallback to original form
            if (!filterForm) {
                filterForm = document.getElementById('filterForm');
            }
            
            if (filterForm) {
                const formData = new FormData(filterForm);
                const filters = {};
                
                for (let [key, value] of formData.entries()) {
                    if (value) {
                        filters[key] = value;
                    }
                }
                
                console.log('Collected filters from form:', filters);
                closeFilterBottomSheet();
                closeFilterSidePanel();
                loadProductsWithFilters(filters, true);
            } else {
                console.error('Filter form not found!');
            }
        });
    });
    
    // Helper function to collect filter values from filter-groups div
    function collectFiltersFromGroups(filterGroups) {
        const filters = {};
        
        // Get search term from the search input (if exists)
        const searchInput = document.getElementById('modernSearchInput') || document.getElementById('heroSearchInput');
        if (searchInput && searchInput.value && searchInput.value.trim()) {
            filters.searchTerm = searchInput.value.trim();
        }
        
        // Get category select
        const categorySelect = filterGroups.querySelector('select[name="categoryId"]');
        if (categorySelect) {
            const categoryValue = categorySelect.value;
            if (categoryValue && categoryValue !== '') {
                filters.categoryId = parseInt(categoryValue);
            }
        }
        
        // Get brand select
        const brandSelect = filterGroups.querySelector('select[name="brandId"]');
        if (brandSelect) {
            const brandValue = brandSelect.value;
            if (brandValue && brandValue !== '') {
                filters.brandId = parseInt(brandValue);
            }
        }
        
        // Get availability select
        const availabilitySelect = filterGroups.querySelector('select[name="availability"]');
        if (availabilitySelect && availabilitySelect.value) {
            filters.availability = availabilitySelect.value;
        }
        
        // Get sort by select
        const sortBySelect = filterGroups.querySelector('select[name="sortBy"]');
        if (sortBySelect && sortBySelect.value) {
            filters.sortBy = sortBySelect.value;
        }
        
        // Get price inputs (manual entry)
        const minPriceInput = filterGroups.querySelector('input[name="minPrice"]');
        if (minPriceInput && minPriceInput.value) {
            filters.minPrice = parseFloat(minPriceInput.value);
        }
        
        const maxPriceInput = filterGroups.querySelector('input[name="maxPrice"]');
        if (maxPriceInput && maxPriceInput.value) {
            filters.maxPrice = parseFloat(maxPriceInput.value);
        }
        
        // Get price range sliders (if manual inputs are empty, use sliders)
        const priceRangeMin = filterGroups.querySelector('#priceRangeMin') || document.getElementById('priceRangeMin');
        const priceRangeMax = filterGroups.querySelector('#priceRangeMax') || document.getElementById('priceRangeMax');
        
        // Only use slider values if manual inputs are empty
        if (priceRangeMin && priceRangeMin.value) {
            const sliderMin = parseFloat(priceRangeMin.value);
            if (!minPriceInput || !minPriceInput.value || parseFloat(minPriceInput.value) === 0) {
                // Check if slider value is different from min (meaning user actually moved it)
                const sliderMinValue = parseFloat(priceRangeMin.min);
                if (sliderMin > sliderMinValue) {
                    filters.minPrice = sliderMin;
                }
            }
        }
        
        if (priceRangeMax && priceRangeMax.value) {
            const sliderMax = parseFloat(priceRangeMax.value);
            if (!maxPriceInput || !maxPriceInput.value) {
                // Check if slider value is different from max (meaning user actually moved it)
                const sliderMaxValue = parseFloat(priceRangeMax.max);
                if (sliderMax < sliderMaxValue) {
                    filters.maxPrice = sliderMax;
                }
            }
        }
        
        return filters;
    }
    
    // Reset filters
    const resetFilterBtns = document.querySelectorAll('.modern-filter-reset-btn');
    resetFilterBtns.forEach(btn => {
        btn.addEventListener('click', function() {
            const filterForm = this.closest('.modern-filter-bottom-sheet, .modern-filter-side-panel')
                .querySelector('form') || document.getElementById('filterForm');
            
            if (filterForm) {
                filterForm.reset();
                // Reset price sliders if they exist
                const priceMin = document.getElementById('priceRangeMin');
                const priceMax = document.getElementById('priceRangeMax');
                if (priceMin && priceMax) {
                    priceMin.value = priceMin.min;
                    priceMax.value = priceMax.max;
                }
            }
        });
    });
    
    // Active filter removal
    const activeFilterRemoveBtns = document.querySelectorAll('.modern-active-filter-tag .remove-btn');
    activeFilterRemoveBtns.forEach(btn => {
        btn.addEventListener('click', function() {
            const filterName = this.getAttribute('data-filter');
            removeFilter(filterName);
        });
    });

    // ===================================
    // 4. AJAX PRODUCT LOADING
    // ===================================
    
    let isLoading = false;
    let currentPage = 0; // Start at 0 (first page)
    let hasMoreProducts = false;
    let currentFilters = {};
    
    function getFilterParams(filters = {}) {
        const params = new URLSearchParams();
        
        Object.keys(filters).forEach(key => {
            if (filters[key] !== null && filters[key] !== undefined && filters[key] !== '') {
                params.append(key, filters[key]);
            }
        });
        
        return params.toString();
    }
    
    function loadProductsWithFilters(filters, resetPage = false) {
        if (isLoading) {
            console.log('Already loading, skipping...');
            return;
        }
        
        isLoading = true;
        
        // Merge filters - if resetPage, replace all filters, otherwise merge
        if (resetPage) {
            currentFilters = { ...filters };
        } else {
            currentFilters = { ...currentFilters, ...filters };
        }
        
        if (resetPage) {
            currentPage = 0; // Reset to first page (page 0)
            const productContainer = document.getElementById('productContainer');
            if (productContainer) {
                productContainer.innerHTML = '';
            }
        }
        
        // Convert categoryId and brandId to integer if they exist, or remove if null/empty
        const cleanedFilters = { ...currentFilters };
        if (cleanedFilters.categoryId === null || cleanedFilters.categoryId === '' || cleanedFilters.categoryId === undefined) {
            delete cleanedFilters.categoryId;
        } else if (cleanedFilters.categoryId) {
            cleanedFilters.categoryId = parseInt(cleanedFilters.categoryId);
        }
        
        if (cleanedFilters.brandId === null || cleanedFilters.brandId === '' || cleanedFilters.brandId === undefined) {
            delete cleanedFilters.brandId;
        } else if (cleanedFilters.brandId) {
            cleanedFilters.brandId = parseInt(cleanedFilters.brandId);
        }
        
        const filterParams = getFilterParams(cleanedFilters);
        const url = `/Customer/Home/LoadMoreProducts?page=${currentPage}&pageSize=20${filterParams ? '&' + filterParams : ''}`;
        
        console.log('Loading products with URL:', url);
        console.log('Current filters:', currentFilters);
        console.log('Current page:', currentPage);
        
        // Show loading indicator
        showLoadingIndicator();
        
        fetch(url)
            .then(response => {
                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }
                return response.json();
            })
            .then(data => {
                console.log('Received data:', data);
                if (data.products && data.products.length > 0) {
                    console.log(`Rendering ${data.products.length} products`);
                    renderProducts(data.products, resetPage);
                    hasMoreProducts = data.hasMore || false;
                    updateLoadMoreButton();
                    // Increment page for next load
                    currentPage++;
                    // Scroll to products section if this is a reset (new search/filter)
                    if (resetPage) {
                        setTimeout(() => {
                            scrollToProductsSection();
                        }, 400);
                    }
                } else {
                    console.log('No products found');
                    showNoProductsMessage();
                    hideLoadMoreButton();
                    // Still scroll even if no products found
                    if (resetPage) {
                        setTimeout(() => {
                            scrollToProductsSection();
                        }, 400);
                    }
                }
            })
            .catch(error => {
                console.error('Error loading products:', error);
                showErrorMessage();
            })
            .finally(() => {
                isLoading = false;
                hideLoadingIndicator();
            });
    }
    
    function renderProducts(products, isFirstPage) {
        const container = document.getElementById('productContainer');
        if (!container) {
            console.error('Product container not found');
            return;
        }
        
        console.log('Rendering products:', products.length);
        
        // Check if we have the existing createProductCard function from the page
        const existingCards = container.querySelectorAll('.product-card').length;
        const isAuthenticated = document.body.getAttribute('data-is-authenticated') === 'true' || 
                               window.location.pathname.includes('/Customer/');
        
        // Try to find createProductCard function - check multiple possible locations
        let createProductCardFn = null;
        if (typeof window.createProductCard === 'function') {
            createProductCardFn = window.createProductCard;
        } else if (typeof createProductCard === 'function') {
            createProductCardFn = createProductCard;
        }
        
        products.forEach((product, index) => {
            let productHtml = '';
            
            // Try to use existing createProductCard function if available
            if (createProductCardFn) {
                try {
                    productHtml = createProductCardFn(product, isAuthenticated);
                } catch (error) {
                    console.error('Error creating product card with createProductCard:', error);
                    productHtml = createSimpleProductCard(product);
                }
            } else {
                // Fallback: Simple product card rendering
                console.warn('createProductCard function not found, using fallback');
                productHtml = createSimpleProductCard(product);
            }
            
            if (productHtml) {
                container.insertAdjacentHTML('beforeend', productHtml);
            }
        });
        
        // Add stagger animation to new cards
        const allCards = container.querySelectorAll('.product-card');
        const newCards = Array.from(allCards).slice(existingCards);
        
        newCards.forEach((card, index) => {
            if (!card.classList.contains('stagger-item')) {
                card.classList.add('stagger-item', 'modern-stagger-item');
            }
            if (!card.classList.contains('visible')) {
                card.classList.add('visible');
            }
            card.style.animationDelay = `${index * 0.05}s`;
        });
        
        // Don't update URL - use pure AJAX without URL changes
        // updateURL(currentFilters); // Disabled to prevent URL updates
        
        // Re-initialize any carousels or other interactive elements
        setTimeout(() => {
            initializeProductCards();
        }, 100);
    }
    
    function initializeProductCards() {
        // Re-initialize Bootstrap carousels if any
        if (typeof bootstrap !== 'undefined' && bootstrap.Carousel) {
            const carousels = document.querySelectorAll('.carousel:not([data-bs-initialized])');
            carousels.forEach(carousel => {
                if (!bootstrap.Carousel.getInstance(carousel)) {
                    new bootstrap.Carousel(carousel, {
                        interval: false,
                        ride: false
                    });
                    carousel.setAttribute('data-bs-initialized', 'true');
                }
            });
        }
        
        // Re-initialize wishlist buttons if function exists
        if (typeof loadProductRatings === 'function') {
            const newCards = document.querySelectorAll('.product-card[data-product-id]:not([data-rating-loaded])');
            newCards.forEach(card => {
                const productId = card.getAttribute('data-product-id');
                if (productId) {
                    loadProductRatings(parseInt(productId));
                    card.setAttribute('data-rating-loaded', 'true');
                }
            });
        }
    }
    
    function createSimpleProductCard(product) {
        // Simple fallback product card HTML
        // Get current culture from document or default to 'en'
        const currentCulture = document.documentElement.lang || 'en';
        const productSlug = product.slugEn || product.id;
        return `
            <div class="product-card modern-stagger-item visible" onclick="window.location.href='/Customer/Home/Details/${productSlug}'">
                <div class="product-image-wrapper">
                    <img src="${product.imageUrl || '/images/placeholder.png'}" class="product-image" alt="${product.title}" />
                </div>
                <div class="product-content">
                    <h3 class="product-title">${product.title}</h3>
                    <div class="price-section">
                        <span class="product-price">${product.price}</span>
                    </div>
                </div>
            </div>
        `;
    }
    
    function showLoadingIndicator() {
        const container = document.getElementById('productContainer');
        if (container && container.children.length === 0) {
            container.innerHTML = `
                <div class="modern-loading-indicator">
                    <div class="modern-loading-spinner"></div>
                    <div class="modern-loading-text">Loading products...</div>
                </div>
            `;
        }
    }
    
    function hideLoadingIndicator() {
        const loadingIndicator = document.querySelector('.modern-loading-indicator');
        if (loadingIndicator) {
            loadingIndicator.remove();
        }
    }
    
    function showNoProductsMessage() {
        const container = document.getElementById('productContainer');
        if (container) {
            container.innerHTML = `
                <div class="text-center py-5">
                    <i class="bi bi-inbox" style="font-size: 3rem; color: var(--home-gray-400);"></i>
                    <p class="mt-3" style="color: var(--home-gray-600);">No products found</p>
                </div>
            `;
        }
    }
    
    function showErrorMessage() {
        const container = document.getElementById('productContainer');
        if (container) {
            container.innerHTML = `
                <div class="text-center py-5">
                    <i class="bi bi-exclamation-triangle" style="font-size: 3rem; color: var(--home-gray-400);"></i>
                    <p class="mt-3" style="color: var(--home-gray-600);">Error loading products. Please try again.</p>
                </div>
            `;
        }
    }
    
    function updateLoadMoreButton() {
        const loadMoreContainer = document.getElementById('loadMoreContainer');
        const loadMoreBtn = document.getElementById('loadMoreBtn');
        
        if (hasMoreProducts) {
            if (loadMoreContainer) loadMoreContainer.style.display = 'block';
            if (loadMoreBtn) loadMoreBtn.disabled = false;
        } else {
            if (loadMoreContainer) loadMoreContainer.style.display = 'none';
        }
    }
    
    function hideLoadMoreButton() {
        const loadMoreContainer = document.getElementById('loadMoreContainer');
        if (loadMoreContainer) loadMoreContainer.style.display = 'none';
    }
    
    // Enhanced loadMoreProducts function
    window.loadMoreProducts = function() {
        if (isLoading || !hasMoreProducts) return;
        
        // currentPage is already incremented in loadProductsWithFilters
        loadProductsWithFilters(currentFilters, false);
    };
    
    // Disabled - don't update URL, use pure AJAX
    // function updateURL(filters) {
    //     const params = new URLSearchParams();
    //     Object.keys(filters).forEach(key => {
    //         if (filters[key]) {
    //             params.append(key, filters[key]);
    //         }
    //     });
    //     
    //     const newURL = window.location.pathname + (params.toString() ? '?' + params.toString() : '');
    //     window.history.pushState({ path: newURL }, '', newURL);
    // }
    
    function removeFilter(filterName) {
        delete currentFilters[filterName];
        loadProductsWithFilters({}, true);
    }
    
    // ===================================
    // 5. INFINITE SCROLL (Optional)
    // ===================================
    
    let infiniteScrollEnabled = false;
    
    function enableInfiniteScroll() {
        if (infiniteScrollEnabled) return;
        infiniteScrollEnabled = true;
        
        let scrollTicking = false;
        window.addEventListener('scroll', function() {
            if (isLoading || !hasMoreProducts || scrollTicking) return;
            
            scrollTicking = true;
            window.requestAnimationFrame(() => {
                const scrollPosition = window.innerHeight + window.scrollY;
                const pageHeight = document.documentElement.scrollHeight;
                const threshold = 200; // Load when 200px from bottom
                
                if (scrollPosition >= pageHeight - threshold) {
                    loadMoreProducts();
                }
                scrollTicking = false;
            });
        }, { passive: true });
    }
    
    // Enable infinite scroll on mobile by default
    if (window.innerWidth <= 768) {
        enableInfiniteScroll();
    }
    
    // ===================================
    // 6. INITIALIZATION
    // ===================================
    
    document.addEventListener('DOMContentLoaded', function() {
        // Initialize filter chips
        updateFilterChips();
        
        // Initialize filters from URL parameters
        initializeFiltersFromURL();
        
        // Initialize price range sliders
        initializePriceSliders();
        
        // Convert category badge links to AJAX
        const categoryBadges = document.querySelectorAll('.hero-category-badge');
        categoryBadges.forEach(badge => {
            badge.addEventListener('click', function(e) {
                e.preventDefault();
                const href = this.getAttribute('href');
                if (href) {
                    const url = new URL(href, window.location.origin);
                    const categoryId = url.searchParams.get('categoryId');
                    if (categoryId) {
                        loadProductsWithFilters({ categoryId: categoryId }, true);
                    } else {
                        // All categories - clear category filter
                        loadProductsWithFilters({ categoryId: null }, true);
                    }
                    // Update active state
                    categoryBadges.forEach(b => b.classList.remove('active'));
                    this.classList.add('active');
                }
            });
        });
        
        // Handle window resize
        let resizeTimeout;
        window.addEventListener('resize', function() {
            clearTimeout(resizeTimeout);
            resizeTimeout = setTimeout(function() {
                if (window.innerWidth > 768) {
                    closeFilterBottomSheet();
                } else {
                    closeFilterSidePanel();
                }
            }, 250);
        });
    });
    
    function initializeFiltersFromURL() {
        const urlParams = new URLSearchParams(window.location.search);
        const categoryId = urlParams.get('categoryId');
        const brandId = urlParams.get('brandId');
        const searchTerm = urlParams.get('searchTerm');
        const sortBy = urlParams.get('sortBy');
        const minPrice = urlParams.get('minPrice');
        const maxPrice = urlParams.get('maxPrice');
        const availability = urlParams.get('availability');
        
        // Update all filter selects (original and cloned)
        const allSelects = document.querySelectorAll('select[name="categoryId"], select[name="brandId"], select[name="sortBy"], select[name="availability"]');
        allSelects.forEach(select => {
            const name = select.name;
            let value = null;
            if (name === 'categoryId') value = categoryId;
            else if (name === 'brandId') value = brandId;
            else if (name === 'sortBy') value = sortBy;
            else if (name === 'availability') value = availability;
            
            if (value && value.trim() !== '') {
                select.value = value;
            } else {
                select.value = '';
            }
        });
        
        // Update price inputs
        const allMinPriceInputs = document.querySelectorAll('input[name="minPrice"]');
        const allMaxPriceInputs = document.querySelectorAll('input[name="maxPrice"]');
        allMinPriceInputs.forEach(input => {
            if (minPrice && minPrice.trim() !== '') {
                input.value = minPrice;
            }
        });
        allMaxPriceInputs.forEach(input => {
            if (maxPrice && maxPrice.trim() !== '') {
                input.value = maxPrice;
            }
        });
        
        // Update price sliders
        const priceRangeMin = document.getElementById('priceRangeMin');
        const priceRangeMax = document.getElementById('priceRangeMax');
        if (priceRangeMin && minPrice) {
            priceRangeMin.value = minPrice;
        }
        if (priceRangeMax && maxPrice) {
            priceRangeMax.value = maxPrice;
        }
    }
    
    function initializePriceSliders() {
        const priceRangeMin = document.getElementById('priceRangeMin');
        const priceRangeMax = document.getElementById('priceRangeMax');
        const priceMinDisplay = document.getElementById('priceMinDisplay');
        const priceMaxDisplay = document.getElementById('priceMaxDisplay');
        
        if (priceRangeMin && priceRangeMax) {
            // Update displays when sliders change
            function updatePriceDisplays() {
                if (priceMinDisplay) {
                    const minValue = parseFloat(priceRangeMin.value) || 0;
                    priceMinDisplay.textContent = formatPrice(minValue);
                }
                if (priceMaxDisplay) {
                    const maxValue = parseFloat(priceRangeMax.value) || 0;
                    priceMaxDisplay.textContent = formatPrice(maxValue);
                }
            }
            
            priceRangeMin.addEventListener('input', updatePriceDisplays);
            priceRangeMax.addEventListener('input', updatePriceDisplays);
            
            // Initial update
            updatePriceDisplays();
        }
    }
    
    function formatPrice(price) {
        // Get currency symbol from page if available
        const currencySymbol = document.querySelector('[data-currency-symbol]')?.getAttribute('data-currency-symbol') || 'AED';
        return `${currencySymbol} ${parseFloat(price).toFixed(2)}`;
    }
    
    function updateFilterChips() {
        // Update filter chip active states based on current filters
        const chips = document.querySelectorAll('.modern-filter-chip');
        chips.forEach(chip => {
            const filterType = chip.getAttribute('data-filter');
            // Logic to determine if chip should be active
        });
    }
    
})();

