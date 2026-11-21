// NexusGear Checkout Flow Client Module (A5, B5)

document.addEventListener('DOMContentLoaded', function () {
    const useNewAddressToggle = document.getElementById('useNewAddressToggle');
    const newAddressFields = document.getElementById('newAddressFields');
    const addressRadios = document.querySelectorAll('.address-radio');
    const shippingRadios = document.querySelectorAll('.shipping-radio');

    const discountInput = document.getElementById('discountCodeInput');
    const btnApplyDiscount = document.getElementById('btnApplyDiscount');
    const btnRemoveDiscount = document.getElementById('btnRemoveDiscount');
    const hiddenDiscountCode = document.getElementById('hiddenDiscountCode');
    const discountFeedbackContainer = document.getElementById('discountFeedbackContainer');
    const discountFeedbackText = document.getElementById('discountFeedbackText');

    const summarySubtotal = document.getElementById('summarySubtotal');
    const summaryShippingFee = document.getElementById('summaryShippingFee');
    const summaryDiscountRow = document.getElementById('summaryDiscountRow');
    const summaryDiscount = document.getElementById('summaryDiscount');
    const summaryGrandTotal = document.getElementById('summaryGrandTotal');

    let currentDiscountAmount = summaryDiscount && summaryDiscount.dataset.discount
        ? (parseFloat(summaryDiscount.dataset.discount) || 0)
        : 0;

    // Helper to get CSRF token
    function getCsrfToken() {
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        return tokenInput ? tokenInput.value : '';
    }

    // Parse numeric value from string (e.g. "1,249.97 RON")
    function parseRonAmount(text) {
        if (!text) return 0;
        // Strip thousands commas and any non-numeric except decimal dot
        const cleaned = text.replace(/,/g, '').replace(/[^0-9.]/g, '');
        return parseFloat(cleaned) || 0;
    }

    // 1. Address Selection Handling
    if (useNewAddressToggle && newAddressFields) {
        useNewAddressToggle.addEventListener('change', function () {
            if (this.checked) {
                newAddressFields.style.display = 'block';
                addressRadios.forEach(radio => {
                    radio.checked = false;
                    radio.closest('.nexus-card')?.classList.remove('border-primary');
                });
            } else {
                newAddressFields.style.display = 'none';
                if (addressRadios.length > 0) {
                    addressRadios[0].checked = true;
                    addressRadios[0].closest('.nexus-card')?.classList.add('border-primary');
                }
            }
        });
    }

    addressRadios.forEach(radio => {
        radio.addEventListener('change', function () {
            if (this.checked) {
                if (useNewAddressToggle) {
                    useNewAddressToggle.checked = false;
                    if (newAddressFields) newAddressFields.style.display = 'none';
                }
                addressRadios.forEach(r => r.closest('.nexus-card')?.classList.remove('border-primary'));
                this.closest('.nexus-card')?.classList.add('border-primary');
            }
        });
    });

    // 2. Shipping Option Handling
    function updateTotals() {
        const selectedShipping = document.querySelector('.shipping-radio:checked');
        const shippingPrice = selectedShipping ? parseFloat(selectedShipping.dataset.price || 0) : 0;
        
        // Prefer raw numeric data attribute, fallback to sanitized parser
        const subtotal = summarySubtotal && summarySubtotal.dataset.subtotal
            ? (parseFloat(summarySubtotal.dataset.subtotal) || 0)
            : parseRonAmount(summarySubtotal ? summarySubtotal.textContent : '0');

        const summaryBundleDiscount = document.getElementById('summaryBundleDiscount');
        const bundleDiscount = summaryBundleDiscount && summaryBundleDiscount.dataset.bundleDiscount
            ? (parseFloat(summaryBundleDiscount.dataset.bundleDiscount) || 0)
            : 0;

        if (summaryShippingFee) {
            if (shippingPrice === 0) {
                summaryShippingFee.innerHTML = '<span class="badge badge-mint fw-bold">FREE</span>';
            } else {
                summaryShippingFee.innerHTML = `<span class="text-white fw-bold">${shippingPrice.toFixed(2)} RON</span>`;
            }
        }

        const grandTotal = Math.max(0, subtotal + shippingPrice - bundleDiscount - currentDiscountAmount);
        if (summaryGrandTotal) {
            const formattedTotal = grandTotal.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
            summaryGrandTotal.innerHTML = `${formattedTotal} <span class="fs-6 text-white">RON</span>`;
        }
    }

    shippingRadios.forEach(radio => {
        radio.addEventListener('change', function () {
            shippingRadios.forEach(r => r.closest('.nexus-card')?.classList.remove('border-primary'));
            if (this.checked) {
                this.closest('.nexus-card')?.classList.add('border-primary');
                updateTotals();
            }
        });
    });

    // 3. Discount Voucher AJAX Engine (B5)
    async function applyCoupon() {
        const code = discountInput ? discountInput.value.trim() : '';
        if (!code) {
            if (typeof showNexusToast === 'function') {
                showNexusToast('Please enter a promo code first.', 'warning');
            }
            return;
        }

        const selectedShipping = document.querySelector('.shipping-radio:checked');
        const shippingMethod = selectedShipping ? selectedShipping.value : 'Standard Courier';

        btnApplyDiscount.disabled = true;
        btnApplyDiscount.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span> Checking...';

        try {
            const response = await fetch('/Checkout/ApplyDiscount', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': getCsrfToken()
                },
                body: JSON.stringify({
                    code: code,
                    shippingMethod: shippingMethod
                })
            });

            const data = await response.json();

            if (data.isValid) {
                currentDiscountAmount = parseFloat(data.discountAmount) || 0;
                hiddenDiscountCode.value = data.code;

                if (discountFeedbackContainer) discountFeedbackContainer.style.display = 'block';
                if (discountFeedbackText) {
                    discountFeedbackText.innerHTML = `Voucher applied: <strong class="text-mint font-monospace">${data.code}</strong> <span class="text-white">(-${currentDiscountAmount.toFixed(2)} RON)</span>`;
                }

                if (summaryDiscountRow) {
                    summaryDiscountRow.style.setProperty('display', 'flex', 'important');
                }
                if (summaryDiscount) {
                    summaryDiscount.dataset.discount = currentDiscountAmount.toFixed(2);
                    summaryDiscount.textContent = `-${currentDiscountAmount.toFixed(2)} RON`;
                }

                if (typeof showNexusToast === 'function') {
                    showNexusToast(data.message, 'success');
                }

                updateTotals();
            } else {
                if (typeof showNexusToast === 'function') {
                    showNexusToast(data.message, 'error');
                } else {
                    alert(data.message);
                }
            }
        } catch (error) {
            console.error('Error applying coupon:', error);
            if (typeof showNexusToast === 'function') {
                showNexusToast('Could not validate promo code. Please try again.', 'error');
            }
        } finally {
            btnApplyDiscount.disabled = false;
            btnApplyDiscount.innerHTML = 'Apply';
        }
    }

    if (btnApplyDiscount) {
        btnApplyDiscount.addEventListener('click', applyCoupon);
    }

    if (discountInput) {
        discountInput.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                applyCoupon();
            }
        });
    }

    if (btnRemoveDiscount) {
        btnRemoveDiscount.addEventListener('click', function () {
            currentDiscountAmount = 0;
            if (discountInput) discountInput.value = '';
            if (hiddenDiscountCode) hiddenDiscountCode.value = '';
            if (discountFeedbackContainer) discountFeedbackContainer.style.display = 'none';
            if (summaryDiscount) summaryDiscount.dataset.discount = '0';
            if (summaryDiscountRow) summaryDiscountRow.style.setProperty('display', 'none', 'important');

            updateTotals();

            if (typeof showNexusToast === 'function') {
                showNexusToast('Coupon removed.', 'info');
            }
        });
    }
});
