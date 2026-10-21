(function () {
    var config = window.returnRequestDetailsConfig;
    if (!config) return;

    var strings = config.strings || {};

    function postForm(url, fields) {
        var form = document.createElement('form');
        form.method = 'POST';
        form.action = url;
        form.style.display = 'none';

        var tokenInput = document.createElement('input');
        tokenInput.type = 'hidden';
        tokenInput.name = '__RequestVerificationToken';
        tokenInput.value = config.requestToken;
        form.appendChild(tokenInput);

        var idInput = document.createElement('input');
        idInput.type = 'hidden';
        idInput.name = 'id';
        idInput.value = String(config.id);
        form.appendChild(idInput);

        Object.keys(fields || {}).forEach(function (key) {
            if (fields[key] !== undefined && fields[key] !== null && fields[key] !== '') {
                var input = document.createElement('input');
                input.type = 'hidden';
                input.name = key;
                input.value = fields[key];
                form.appendChild(input);
            }
        });

        document.body.appendChild(form);
        form.submit();
    }

    function bindActions() {
        var root = document.getElementById('returnRequestActions');
        if (!root) return;

        root.addEventListener('click', function (event) {
            var button = event.target.closest('[data-return-action]');
            if (!button) return;

            var action = button.getAttribute('data-return-action');

            if (action === 'approve') {
                Swal.fire({
                    title: strings.approveTitle,
                    html: strings.approveConfirm +
                        '<div class="text-start mt-3">' +
                        '<label class="form-label">' + strings.trackingNumber + ' (' + strings.optional + ')</label>' +
                        '<input id="swalReturnTracking" class="form-control mb-2" />' +
                        '<label class="form-label">' + strings.carrier + ' (' + strings.optional + ')</label>' +
                        '<input id="swalReturnCarrier" class="form-control mb-2" />' +
                        '<label class="form-label">' + strings.adminNotes + ' (' + strings.optional + ')</label>' +
                        '<textarea id="swalAdminNotes" class="form-control" rows="2"></textarea>' +
                        '</div>',
                    icon: 'question',
                    showCancelButton: true,
                    confirmButtonText: strings.confirm,
                    cancelButtonText: strings.cancel,
                    focusConfirm: false,
                    preConfirm: function () {
                        return {
                            returnTrackingNumber: document.getElementById('swalReturnTracking').value,
                            returnCarrier: document.getElementById('swalReturnCarrier').value,
                            adminNotes: document.getElementById('swalAdminNotes').value
                        };
                    }
                }).then(function (result) {
                    if (result.isConfirmed) {
                        postForm(config.approveUrl, result.value || {});
                    }
                });
            }

            if (action === 'reject') {
                Swal.fire({
                    title: strings.rejectTitle,
                    text: strings.rejectConfirm,
                    input: 'textarea',
                    inputLabel: strings.rejectionReason,
                    inputValidator: function (value) {
                        if (!value || !value.trim()) {
                            return strings.rejectionReason;
                        }
                    },
                    icon: 'warning',
                    showCancelButton: true,
                    confirmButtonText: strings.confirm,
                    cancelButtonText: strings.cancel
                }).then(function (result) {
                    if (result.isConfirmed) {
                        postForm(config.rejectUrl, {
                            rejectionReason: result.value,
                            adminNotes: ''
                        });
                    }
                });
            }

            if (action === 'received') {
                Swal.fire({
                    title: strings.markReceivedTitle,
                    text: strings.receivedConfirm,
                    icon: 'question',
                    showCancelButton: true,
                    confirmButtonText: strings.confirm,
                    cancelButtonText: strings.cancel
                }).then(function (result) {
                    if (result.isConfirmed) {
                        document.getElementById('returnMarkReceivedForm').submit();
                    }
                });
            }

            if (action === 'complete') {
                Swal.fire({
                    title: strings.completeTitle,
                    html: strings.completeConfirm +
                        '<p class="text-muted small mt-2">' + strings.completeHint + '</p>' +
                        '<p><strong>' + strings.refundAmount + ':</strong> ' + config.refundAmount + '</p>' +
                        '<label class="form-label">' + strings.transactionId + ' (' + strings.optional + ')</label>' +
                        '<input id="swalRefundTxn" class="form-control" placeholder="' + strings.enterTransactionId + '" />',
                    icon: 'question',
                    showCancelButton: true,
                    confirmButtonText: strings.confirm,
                    cancelButtonText: strings.cancel,
                    focusConfirm: false,
                    preConfirm: function () {
                        return {
                            refundTransactionId: document.getElementById('swalRefundTxn').value
                        };
                    }
                }).then(function (result) {
                    if (result.isConfirmed) {
                        postForm(config.completeUrl, result.value || {});
                    }
                });
            }

            if (action === 'cancel') {
                Swal.fire({
                    title: strings.cancelTitle,
                    text: strings.confirmCancel,
                    icon: 'warning',
                    showCancelButton: true,
                    confirmButtonText: strings.confirm,
                    cancelButtonText: strings.cancel
                }).then(function (result) {
                    if (result.isConfirmed) {
                        document.getElementById('returnCancelForm').submit();
                    }
                });
            }
        });
    }

    document.addEventListener('DOMContentLoaded', bindActions);
})();
