// NexusGear Dedicated Shopping Cart Module (A3, A11)

function updateCartSummaryDom(data) {
    if (!data) return;

    const summarySubtotal = document.getElementById('summarySubtotal');
    if (summarySubtotal) summarySubtotal.textContent = `${data.subtotal.toFixed(2)} RON`;

    const summarySubtotalLabel = document.getElementById('summarySubtotalLabel');
    if (summarySubtotalLabel) summarySubtotalLabel.textContent = `Items Subtotal (${data.totalCartCount})`;

    const summaryShipping = document.getElementById('summaryShipping');
    if (summaryShipping) {
        summaryShipping.innerHTML = data.shippingFee === 0
            ? '<span class="badge badge-mint fw-bold">FREE</span>'
            : `${data.shippingFee.toFixed(2)} RON`;
    }

    const summaryVat = document.getElementById('summaryVat');
    if (summaryVat) {
        const vat = (data.subtotal - (data.subtotal / 1.19)).toFixed(2);
        summaryVat.textContent = `${vat} RON`;
    }

    const summaryGrandTotal = document.getElementById('summaryGrandTotal');
    if (summaryGrandTotal) {
        summaryGrandTotal.innerHTML = `${data.grandTotal.toFixed(2)} <span class="fs-6 text-white">RON</span>`;
    }

    // Update free shipping bar
    const threshold = 300.00;
    const freeShippingStatus = document.getElementById('freeShippingStatus');
    const freeShippingBar = document.getElementById('freeShippingProgressBar');
    if (freeShippingStatus && freeShippingBar) {
        if (data.subtotal >= threshold) {
            freeShippingStatus.innerHTML = '<span class="text-mint">🎉 Free Standard Shipping Unlocked!</span>';
            freeShippingBar.style.width = '100%';
        } else {
            const needed = (threshold - data.subtotal).toFixed(2);
            const pct = Math.min(100, Math.round((data.subtotal / threshold) * 100));
            freeShippingStatus.innerHTML = `<span>Add <strong class="text-mint">${needed} RON</strong> more for <strong>FREE Shipping</strong></span>`;
            freeShippingBar.style.width = `${pct}%`;
        }
    }

    // Update cart header count
    const cartHeaderCount = document.getElementById('cartHeaderCount');
    if (cartHeaderCount) {
        cartHeaderCount.textContent = `${data.totalCartCount} ${data.totalCartCount === 1 ? 'item' : 'items'}`;
    }

    // Update global navbar cart badge
    if (typeof updateCartBadge === 'function') {
        updateCartBadge(data.totalCartCount);
    }
}

document.addEventListener('DOMContentLoaded', () => {
    // Preserve and restore scroll position on /Cart pages if page reloads
    const savedScroll = sessionStorage.getItem('nexusgear_cart_scroll');
    if (savedScroll !== null) {
        window.scrollTo({ top: parseInt(savedScroll), behavior: 'instant' });
        sessionStorage.removeItem('nexusgear_cart_scroll');
    }

    window.addEventListener('beforeunload', () => {
        sessionStorage.setItem('nexusgear_cart_scroll', window.scrollY);
    });

    // In-place AJAX stepper for cart quantity (+ / -) to prevent page jumps
    document.addEventListener('click', async (e) => {
        const qtyBtn = e.target.closest('.quantity-form button[type="submit"]');
        if (!qtyBtn) return;

        e.preventDefault();
        const form = qtyBtn.closest('.quantity-form');
        const row = qtyBtn.closest('.cart-item-row');
        if (!form || !row) return;

        const productId = form.querySelector('input[name="productId"]')?.value;
        const newQty = parseInt(qtyBtn.value);
        const token = form.querySelector('input[name="__RequestVerificationToken"]')?.value;

        if (!productId || isNaN(newQty)) return;

        const formButtons = form.querySelectorAll('button');
        formButtons.forEach(b => b.disabled = true);

        try {
            const formData = new FormData();
            formData.append('productId', productId);
            formData.append('quantity', newQty);
            if (token) formData.append('__RequestVerificationToken', token);

            const response = await fetch(form.action || '/Cart/Update', {
                method: 'POST',
                body: formData,
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });

            if (response.ok) {
                const data = await response.json();
                if (data.success) {
                    if (data.isEmpty) {
                        window.location.reload();
                        return;
                    }

                    if (data.updatedQuantity <= 0) {
                        row.style.transition = 'opacity 0.3s ease, transform 0.3s ease';
                        row.style.opacity = '0';
                        row.style.transform = 'translateX(20px)';
                        setTimeout(() => row.remove(), 300);
                    } else {
                        const qtyDisplay = row.querySelector('.cart-qty-value');
                        if (qtyDisplay) qtyDisplay.textContent = data.updatedQuantity;

                        const incBtn = row.querySelector('.btn-qty-inc');
                        const decBtn = row.querySelector('.btn-qty-dec');
                        if (incBtn) {
                            incBtn.value = data.updatedQuantity + 1;
                            const maxStock = parseInt(row.dataset.maxStock) || 999999;
                            incBtn.disabled = data.updatedQuantity >= maxStock;
                        }
                        if (decBtn) {
                            decBtn.value = data.updatedQuantity - 1;
                            decBtn.disabled = false;
                        }

                        const lineTotalElem = row.querySelector('.cart-line-total');
                        if (lineTotalElem) {
                            lineTotalElem.innerHTML = `${data.lineTotal.toFixed(2)} <span class="small">RON</span>`;
                        }
                    }

                    updateCartSummaryDom(data);
                } else {
                    if (typeof showNexusToast === 'function') {
                        showNexusToast(data.message || 'Could not update quantity.', 'danger');
                    }
                    formButtons.forEach(b => b.disabled = false);
                }
            } else {
                form.submit();
            }
        } catch {
            form.submit();
        }
    });

    // In-place AJAX item removal from cart
    document.addEventListener('submit', async (e) => {
        const removeForm = e.target.closest('.cart-remove-form');
        if (!removeForm) return;

        e.preventDefault();
        const row = removeForm.closest('.cart-item-row');
        const formData = new FormData(removeForm);

        try {
            const response = await fetch(removeForm.action || '/Cart/Remove', {
                method: 'POST',
                body: formData,
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });

            if (response.ok) {
                const data = await response.json();
                if (data.success) {
                    if (data.isEmpty) {
                        window.location.reload();
                        return;
                    }

                    if (row) {
                        row.style.transition = 'opacity 0.3s ease, transform 0.3s ease';
                        row.style.opacity = '0';
                        row.style.transform = 'translateX(20px)';
                        setTimeout(() => row.remove(), 300);
                    }

                    updateCartSummaryDom(data);
                    if (typeof showNexusToast === 'function') {
                        showNexusToast(data.message, 'success');
                    }
                }
            } else {
                removeForm.submit();
            }
        } catch {
            removeForm.submit();
        }
    });
});

