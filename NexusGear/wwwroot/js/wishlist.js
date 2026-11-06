// NexusGear Dedicated Wishlist Module (B7, A11)

document.addEventListener('DOMContentLoaded', () => {
    // In-place AJAX Move To Cart
    document.addEventListener('submit', async (e) => {
        const moveForm = e.target.closest('.wishlist-move-form');
        if (!moveForm) return;

        e.preventDefault();
        const col = moveForm.closest('.wishlist-item-col');
        const submitBtn = moveForm.querySelector('button[type="submit"]');
        if (submitBtn) {
            submitBtn.disabled = true;
            submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span> Moving...';
        }

        try {
            const formData = new FormData(moveForm);
            const response = await fetch(moveForm.action || '/Wishlist/MoveToCart', {
                method: 'POST',
                body: formData,
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });

            if (response.status === 401) {
                window.location.href = '/Account/Login?returnUrl=' + encodeURIComponent(window.location.pathname);
                return;
            }

            if (response.ok) {
                const data = await response.json();
                if (data.success) {
                    if (typeof updateCartBadge === 'function') {
                        updateCartBadge(data.cartCount);
                    }
                    if (typeof updateWishlistBadge === 'function') {
                        updateWishlistBadge(data.wishlistCount);
                    }

                    if (typeof showNexusToast === 'function') {
                        showNexusToast(data.message, 'success');
                    }

                    if (col) {
                        col.style.transition = 'opacity 0.3s ease, transform 0.3s ease';
                        col.style.opacity = '0';
                        col.style.transform = 'scale(0.9)';
                        setTimeout(() => {
                            col.remove();
                            if (data.wishlistCount === 0) {
                                window.location.reload();
                            }
                        }, 300);
                    }

                    const headerCount = document.getElementById('wishlistHeaderCount');
                    if (headerCount) {
                        headerCount.textContent = `${data.wishlistCount} ${data.wishlistCount === 1 ? 'item' : 'items'}`;
                    }
                } else {
                    if (typeof showNexusToast === 'function') {
                        showNexusToast(data.message || 'Could not move item to cart.', 'danger');
                    }
                    if (submitBtn) {
                        submitBtn.disabled = false;
                        submitBtn.innerHTML = '<i class="bi bi-cart-plus me-1"></i> Move to Cart';
                    }
                }
            } else {
                moveForm.submit();
            }
        } catch {
            moveForm.submit();
        }
    });

    // In-place AJAX Remove Item
    document.addEventListener('submit', async (e) => {
        const removeForm = e.target.closest('.wishlist-remove-form');
        if (!removeForm) return;

        e.preventDefault();
        const col = removeForm.closest('.wishlist-item-col');
        const submitBtn = removeForm.querySelector('button[type="submit"]');
        if (submitBtn) submitBtn.disabled = true;

        try {
            const formData = new FormData(removeForm);
            const response = await fetch(removeForm.action || '/Wishlist/Remove', {
                method: 'POST',
                body: formData,
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });

            if (response.status === 401) {
                window.location.href = '/Account/Login?returnUrl=' + encodeURIComponent(window.location.pathname);
                return;
            }

            if (response.ok) {
                const data = await response.json();
                if (data.success) {
                    if (typeof updateWishlistBadge === 'function') {
                        updateWishlistBadge(data.wishlistCount);
                    }

                    if (typeof showNexusToast === 'function') {
                        showNexusToast(data.message, 'success');
                    }

                    if (col) {
                        col.style.transition = 'opacity 0.3s ease, transform 0.3s ease';
                        col.style.opacity = '0';
                        col.style.transform = 'scale(0.9)';
                        setTimeout(() => {
                            col.remove();
                            if (data.wishlistCount === 0) {
                                window.location.reload();
                            }
                        }, 300);
                    }

                    const headerCount = document.getElementById('wishlistHeaderCount');
                    if (headerCount) {
                        headerCount.textContent = `${data.wishlistCount} ${data.wishlistCount === 1 ? 'item' : 'items'}`;
                    }
                } else {
                    if (typeof showNexusToast === 'function') {
                        showNexusToast(data.message || 'Could not remove item from wishlist.', 'danger');
                    }
                    if (submitBtn) submitBtn.disabled = false;
                }
            } else {
                removeForm.submit();
            }
        } catch {
            removeForm.submit();
        }
    });
});
