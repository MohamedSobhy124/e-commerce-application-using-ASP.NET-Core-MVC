(function () {
    if (!document.body.classList.contains('iw-admin')) {
        return;
    }

    function firstValidationError() {
        return document.querySelector('.input-validation-error, .field-validation-error:not(:empty), .validation-summary-errors, .alert-danger:not(:empty)');
    }

    function scrollToFirstError() {
        var error = firstValidationError();
        if (!error) {
            return;
        }
        var target = error.closest('.admin-form-group') || error;
        target.scrollIntoView({ behavior: 'smooth', block: 'center' });
        var field = error.matches('input, select, textarea') ? error : target.querySelector('input, select, textarea');
        if (field && typeof field.focus === 'function') {
            field.focus({ preventScroll: true });
        }
    }

    function bindImagePreviews() {
        document.querySelectorAll('input[type="file"][accept*="image"]').forEach(function (input) {
            var preview = document.getElementById('imagePreview');
            var previewImg = document.getElementById('previewImg');
            if (!preview || !previewImg) {
                return;
            }
            input.addEventListener('change', function () {
                var file = input.files && input.files[0];
                if (!file) {
                    preview.hidden = true;
                    return;
                }
                var reader = new FileReader();
                reader.onload = function (event) {
                    previewImg.src = event.target.result;
                    preview.hidden = false;
                };
                reader.readAsDataURL(file);
            });
        });
    }

    function bindFormLoading() {
        document.querySelectorAll('.iw-admin form').forEach(function (form) {
            form.addEventListener('submit', function (event) {
                if (typeof form.checkValidity === 'function' && !form.checkValidity()) {
                    return;
                }
                if (window.jQuery && window.jQuery.fn && window.jQuery.fn.valid && window.jQuery(form).valid && !window.jQuery(form).valid()) {
                    event.preventDefault();
                    scrollToFirstError();
                    return;
                }
                var submit = form.querySelector('[type="submit"]');
                if (!submit || submit.dataset.loading === '1') {
                    return;
                }
                submit.dataset.loading = '1';
                submit.disabled = true;
                submit.setAttribute('aria-busy', 'true');
                if (!submit.querySelector('.spinner-border')) {
                    var spinner = document.createElement('span');
                    spinner.className = 'spinner-border spinner-border-sm me-2';
                    spinner.setAttribute('aria-hidden', 'true');
                    submit.prepend(spinner);
                }
            });
        });
    }

    var scheduleBadgeTimer = null;

    function stateForScheduleBadge(el, now) {
            var enabled = el.getAttribute('data-enabled') !== 'false';
            var startRaw = el.getAttribute('data-start');
            var endRaw = el.getAttribute('data-end');
            if (!startRaw || !endRaw) {
                return 'inactive';
            }
            var start = new Date(startRaw);
            var end = new Date(endRaw);
            if (Number.isNaN(start.getTime()) || Number.isNaN(end.getTime())) {
                return 'inactive';
            }
            if (!enabled) {
                return 'inactive';
            }
            if (now > end) {
                return 'ended';
            }
            if (now >= start && now <= end) {
                return 'active';
            }
            if (now < start) {
                return 'scheduled';
            }
            return 'inactive';
    }

    function applyScheduleBadgeState(el, state) {
            var labels = {
                active: el.getAttribute('data-label-active'),
                scheduled: el.getAttribute('data-label-scheduled'),
                ended: el.getAttribute('data-label-ended'),
                inactive: el.getAttribute('data-label-inactive')
            };
            var classes = {
                active: 'bg-success',
                scheduled: 'bg-warning text-dark',
                ended: 'bg-secondary',
                inactive: 'bg-secondary'
            };
            el.className = el.getAttribute('data-badge-class') || 'badge admin-schedule-live';
            el.classList.add(classes[state] || 'bg-secondary');
            el.textContent = labels[state] || labels.inactive || '';
    }

    function refreshScheduleBadges() {
        var now = new Date();
        document.querySelectorAll('[data-schedule-badge]').forEach(function (el) {
            applyScheduleBadgeState(el, stateForScheduleBadge(el, now));
        });
    }

    window.refreshScheduleBadges = refreshScheduleBadges;

    function bindScheduleBadges() {
        refreshScheduleBadges();
        if (!scheduleBadgeTimer) {
            scheduleBadgeTimer = window.setInterval(refreshScheduleBadges, 60000);
        }
    }

    function bindScheduleBadgeFromInputs() {
        var root = document.querySelector('[data-schedule-from-inputs]');
        if (!root) {
            return;
        }
        var badge = root.querySelector('[data-schedule-badge]');
        var startInput = root.querySelector('input[name="StartDate"]');
        var endInput = root.querySelector('input[name="EndDate"]');
        var enabledInput = root.querySelector('input[name="IsActive"]');
        if (!badge || !startInput || !endInput) {
            return;
        }
        function sync() {
            badge.setAttribute('data-start', startInput.value || '');
            badge.setAttribute('data-end', endInput.value || '');
            badge.setAttribute('data-enabled', !enabledInput || enabledInput.checked ? 'true' : 'false');
            refreshScheduleBadges();
        }
        startInput.addEventListener('change', sync);
        endInput.addEventListener('change', sync);
        if (enabledInput) {
            enabledInput.addEventListener('change', sync);
        }
        sync();
    }

    function bindPickerRemoveConfirm() {
        document.querySelectorAll('form[data-picker-remove]').forEach(function (form) {
            form.addEventListener('submit', function (event) {
                var message = form.getAttribute('data-confirm-message');
                if (message && !window.confirm(message)) {
                    event.preventDefault();
                    return;
                }
                var submit = form.querySelector('[type="submit"]');
                if (submit && submit.dataset.loading !== '1') {
                    submit.dataset.loading = '1';
                    submit.disabled = true;
                    submit.setAttribute('aria-busy', 'true');
                }
            });
        });
    }

    function bindSidebarCollapse() {
        var key = 'iw-admin-sidebar-collapsed';
        var toggle = document.getElementById('adminSidebarCollapse');
        var collapseLabel = toggle ? toggle.getAttribute('aria-label') : '';
        var expandLabel = toggle ? (toggle.getAttribute('data-expand-label') || collapseLabel) : '';

        document.querySelectorAll('.iw-admin-sidebar nav a').forEach(function (link) {
            if (!link.getAttribute('title')) {
                var label = link.querySelector('span:not(.iw-admin-nav-badge)');
                if (label) {
                    link.setAttribute('title', label.textContent.trim());
                }
            }
        });

        function apply(collapsed) {
            document.body.classList.toggle('iw-admin-sidebar-collapsed', collapsed);
            if (toggle) {
                toggle.setAttribute('aria-pressed', collapsed ? 'true' : 'false');
                if (expandLabel) {
                    toggle.setAttribute('aria-label', collapsed ? expandLabel : collapseLabel);
                }
            }
            window.dispatchEvent(new Event('resize'));
        }

        apply(window.localStorage.getItem(key) === '1');
        if (toggle) {
            toggle.addEventListener('click', function () {
                var collapsed = !document.body.classList.contains('iw-admin-sidebar-collapsed');
                window.localStorage.setItem(key, collapsed ? '1' : '0');
                apply(collapsed);
            });
        }
    }

    function bindPendingOrderBadge() {
        var badge = document.getElementById('adminNavPendingBadge');
        if (!badge) {
            return;
        }
        fetch('/Admin/Order/GetOrderStatistics')
            .then(function (response) { return response.ok ? response.json() : null; })
            .then(function (data) {
                var pending = data && data.stats ? data.stats.pending : 0;
                if (pending > 0) {
                    badge.hidden = false;
                    badge.textContent = pending > 99 ? '99+' : String(pending);
                } else {
                    badge.hidden = true;
                    badge.textContent = '';
                }
            })
            .catch(function () { /* keep existing list pages usable if stats fail */ });
    }

    function bindDashboard() {
        var root = document.querySelector('[data-admin-dashboard]');
        if (!root) {
            return;
        }
        var ordersUrl = root.getAttribute('data-orders-url');
        var productsUrl = root.getAttribute('data-products-url');
        var errorMessage = root.getAttribute('data-error') || '';

        function setValue(key, value) {
            root.querySelectorAll('[data-kpi="' + key + '"]').forEach(function (el) {
                el.textContent = value;
            });
        }

        function loaded() {
            root.querySelectorAll('.admin-kpi-card.is-skeleton').forEach(function (card) {
                card.classList.remove('is-skeleton');
            });
        }

        Promise.all([
            fetch(ordersUrl).then(function (r) { return r.ok ? r.json() : null; }),
            fetch(productsUrl).then(function (r) { return r.ok ? r.json() : null; })
        ]).then(function (results) {
            var orders = results[0] && results[0].stats ? results[0].stats : {};
            var products = results[1] && results[1].stats ? results[1].stats : {};
            setValue('orders-pending', orders.pending || 0);
            setValue('orders-processing', orders.processing || 0);
            setValue('products-lowstock', products.lowStock || 0);
            setValue('products-outofstock', products.outOfStock || 0);
            loaded();
        }).catch(function () {
            loaded();
            if (window.toastr && errorMessage) {
                window.toastr.error(errorMessage);
            }
        });
    }

    function bindMarkAllRead() {
        var button = document.getElementById('adminMarkAllReadBtn');
        if (!button) {
            return;
        }
        button.addEventListener('click', function (event) {
            event.stopPropagation();
            if (typeof window.markAllAsRead === 'function') {
                window.markAllAsRead();
            }
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        scrollToFirstError();
        bindImagePreviews();
        bindFormLoading();
        bindScheduleBadges();
        bindScheduleBadgeFromInputs();
        bindPickerRemoveConfirm();
        bindSidebarCollapse();
        bindPendingOrderBadge();
        bindMarkAllRead();
        bindDashboard();
    });
})();
