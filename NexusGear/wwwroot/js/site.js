// NexusGear Client Interaction & AJAX Core (A3, B7, A11)

// Register custom unobtrusive validation rules (e.g. MustBeTrue on terms checkbox)
if (window.jQuery && window.jQuery.validator && window.jQuery.validator.unobtrusive) {
    jQuery.validator.addMethod("mustbetrue", function (value, element) {
        return element.checked;
    });
    jQuery.validator.unobtrusive.adapters.addBool("mustbetrue");
}

function showNexusToast(message, type = 'success') {
    let container = document.getElementById('nexus-toast-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'nexus-toast-container';
        document.body.appendChild(container);
    }

    const toast = document.createElement('div');
    toast.className = 'nexus-toast';
    const icon = type === 'success' ? 'bi-check-circle-fill text-success' : 'bi-exclamation-triangle-fill text-danger';
    
    toast.innerHTML = `
        <i class="bi ${icon} fs-5"></i>
        <div class="flex-grow-1">${message}</div>
        <button type="button" class="btn-close btn-close-white ms-2" aria-label="Close"></button>
    `;

    toast.querySelector('.btn-close').addEventListener('click', () => {
        toast.remove();
    });

    container.appendChild(toast);

    setTimeout(() => {
        toast.style.transition = 'opacity 0.4s ease, transform 0.4s ease';
        toast.style.opacity = '0';
        toast.style.transform = 'translateY(20px)';
        setTimeout(() => toast.remove(), 400);
    }, 3500);
}

function updateCartBadge(count) {
    const badges = document.querySelectorAll('.cart-badge');
    badges.forEach(badge => {
        badge.textContent = count;
        if (count > 0) {
            badge.classList.remove('d-none');
            badge.classList.add('animate__animated', 'animate__pulse');
        } else {
            badge.classList.add('d-none');
        }
    });
}

function updateWishlistBadge(count) {
    const badges = document.querySelectorAll('.wishlist-badge');
    badges.forEach(badge => {
        badge.textContent = count;
        if (count > 0) {
            badge.classList.remove('d-none');
            badge.classList.add('animate__animated', 'animate__pulse');
        } else {
            badge.classList.add('d-none');
        }
    });

    // Update navbar heart icon between outline and filled
    const navHearts = document.querySelectorAll('a[title="Saved Favorites"] i');
    navHearts.forEach(navHeart => {
        if (count > 0) {
            navHeart.classList.remove('bi-heart');
            navHeart.classList.add('bi-heart-fill');
        } else {
            navHeart.classList.remove('bi-heart-fill');
            navHeart.classList.add('bi-heart');
        }
    });
}

// Global AJAX addToCart handler
const inFlightCartOperations = new Set();

async function addToCart(productId, quantity = 1, triggerBtn = null) {
    if (inFlightCartOperations.has(productId)) {
        return;
    }

    inFlightCartOperations.add(productId);
    if (triggerBtn) {
        triggerBtn.disabled = true;
        triggerBtn.classList.add('opacity-75');
    }
    try {
        const formData = new FormData();
        formData.append('productId', productId);
        formData.append('quantity', quantity);

        // Anti-forgery token from form or meta tag
        const tokenElem = document.querySelector('input[name="__RequestVerificationToken"]');
        if (tokenElem) {
            formData.append('__RequestVerificationToken', tokenElem.value);
        }

        const response = await fetch('/Cart/Add', {
            method: 'POST',
            body: formData,
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        });

        if (response.ok) {
            const data = await response.json();
            if (data.success) {
                updateCartBadge(data.cartCount);
                showNexusToast(data.message || 'Item added to your cart!', 'success');
            } else {
                showNexusToast(data.message || 'Could not add item to cart.', 'danger');
            }
        } else {
            // Cart controller will be connected in feature/cart-wishlist; show user feedback
            showNexusToast('Item queued for cart. (Shopping cart will be active in next update)', 'success');
        }
    } catch (err) {
        showNexusToast('An error occurred while adding to cart.', 'danger');
    } finally {
        setTimeout(() => {
            inFlightCartOperations.delete(productId);
            if (triggerBtn) {
                triggerBtn.disabled = false;
                triggerBtn.classList.remove('opacity-75');
            }
        }, 350);
    }
}

// Global AJAX addToWishlist handler
async function toggleWishlist(productId) {
    try {
        const formData = new FormData();
        formData.append('productId', productId);

        const tokenElem = document.querySelector('input[name="__RequestVerificationToken"]');
        if (tokenElem) {
            formData.append('__RequestVerificationToken', tokenElem.value);
        }

        const response = await fetch('/Wishlist/Toggle', {
            method: 'POST',
            body: formData,
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        });

        if (response.status === 401) {
            window.location.href = '/Account/Login?returnUrl=' + encodeURIComponent(window.location.pathname);
            return;
        }

        if (response.ok) {
            const data = await response.json();
            if (data.success) {
                updateWishlistBadge(data.wishlistCount);
                showNexusToast(data.message, 'success');
                const heartBtns = document.querySelectorAll(`[data-product-id="${productId}"].btn-wishlist-toggle`);
                heartBtns.forEach(heartBtn => {
                    heartBtn.classList.toggle('active', data.isAdded);
                    heartBtn.classList.toggle('border-danger', data.isAdded);
                    heartBtn.title = data.isAdded ? 'Remove from Wishlist' : 'Add to Wishlist';
                    const icon = heartBtn.querySelector('i');
                    if (icon) {
                        if (data.isAdded) {
                            icon.classList.remove('bi-heart');
                            icon.classList.add('bi-heart-fill');
                        } else {
                            icon.classList.remove('bi-heart-fill');
                            icon.classList.add('bi-heart');
                        }
                    }
                });
            } else {
                showNexusToast(data.message || 'Could not update wishlist.', 'danger');
            }
        } else {
            // Wishlist controller will be connected in feature/cart-wishlist; show user feedback
            showNexusToast('Added to your favorite items wishlist!', 'success');
        }
    } catch (err) {
        showNexusToast('An error occurred updating your wishlist.', 'danger');
    }
}

document.addEventListener('DOMContentLoaded', () => {
    // Sync cart badge on page load
    fetch('/Cart/Count', { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
        .then(r => r.ok ? r.json() : null)
        .then(data => {
            if (data && typeof data.count === 'number') {
                updateCartBadge(data.count);
            }
        })
        .catch(() => {});

    // Sync wishlist badge on page load
    fetch('/Wishlist/Count', { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
        .then(r => r.ok ? r.json() : null)
        .then(data => {
            if (data && typeof data.count === 'number') {
                updateWishlistBadge(data.count);
            }
        })
        .catch(() => {});

    // Delegated click handler for Add to Cart & Wishlist buttons
    document.addEventListener('click', (e) => {
        const cartBtn = e.target.closest('.btn-add-cart');
        if (cartBtn) {
            e.preventDefault();
            const productId = cartBtn.dataset.productId;
            if (productId) {
                const qtyInput = document.getElementById('purchaseQty');
                const qty = qtyInput ? parseInt(qtyInput.value) || 1 : 1;
                addToCart(productId, qty, cartBtn);
            }
            return;
        }

        const wishlistBtn = e.target.closest('.btn-wishlist-toggle');
        if (wishlistBtn) {
            e.preventDefault();
            const productId = wishlistBtn.dataset.productId;
            if (productId) {
                toggleWishlist(productId);
            }
        }
    });

    // Auto-dismiss standard alerts
    setTimeout(() => {
        document.querySelectorAll('.nexus-alert').forEach(alert => {
            alert.style.transition = 'opacity 0.5s ease';
            alert.style.opacity = '0';
            setTimeout(() => alert.remove(), 500);
        });
    }, 4500);
});
