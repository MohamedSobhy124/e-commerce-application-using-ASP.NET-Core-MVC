/**
 * Blog Category Selector - Dropdown with existing categories + add new
 */
(function () {
    function init() {
        document.querySelectorAll('.category-selector-wrapper').forEach(function (wrapper) {
            const input = wrapper.querySelector('.category-selector-input');
            const dropdownBtn = wrapper.querySelector('.category-dropdown-btn');
            const panel = wrapper.querySelector('.category-suggestions-panel');
            const suggestions = wrapper.querySelectorAll('.category-suggestion-item');
            const addNew = wrapper.querySelector('.category-add-new');

            function showPanel() {
                panel.style.display = 'block';
            }

            function hidePanel() {
                panel.style.display = 'none';
            }

            function togglePanel() {
                panel.style.display = panel.style.display === 'none' ? 'block' : 'none';
            }

            dropdownBtn.addEventListener('click', function (e) {
                e.preventDefault();
                togglePanel();
            });

            suggestions.forEach(function (btn) {
                btn.addEventListener('click', function () {
                    input.value = this.dataset.value || this.textContent;
                    hidePanel();
                });
            });

            addNew.addEventListener('click', function () {
                input.value = '';
                input.focus();
                hidePanel();
            });

            document.addEventListener('click', function (e) {
                if (!wrapper.contains(e.target)) {
                    hidePanel();
                }
            });
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
