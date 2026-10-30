// NexusGear Client Interaction & AJAX Core (A3, B7, A11)

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
        } else {
            badge.classList.add('d-none');
        }
    });
}

// Global AJAX addToCart handler
async function addToCart(productId, quantity = 1) {
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

        const data = await response.json();
        if (data.success) {
            updateCartBadge(data.cartCount);
            showNexusToast(data.message || 'Item added to your cart!', 'success');
        } else {
            showNexusToast(data.message || 'Could not add item to cart.', 'danger');
        }
    } catch (err) {
        showNexusToast('An error occurred while adding to cart.', 'danger');
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

        const data = await response.json();
        if (data.success) {
            updateWishlistBadge(data.wishlistCount);
            showNexusToast(data.message, 'success');
            const heartBtn = document.querySelector(`[data-wishlist-id="${productId}"]`);
            if (heartBtn) {
                heartBtn.classList.toggle('text-danger', data.isAdded);
            }
        } else {
            showNexusToast(data.message || 'Could not update wishlist.', 'danger');
        }
    } catch (err) {
        showNexusToast('An error occurred updating your wishlist.', 'danger');
    }
}

document.addEventListener('DOMContentLoaded', () => {
    // Auto-dismiss standard alerts
    setTimeout(() => {
        document.querySelectorAll('.nexus-alert').forEach(alert => {
            alert.style.transition = 'opacity 0.5s ease';
            alert.style.opacity = '0';
            setTimeout(() => alert.remove(), 500);
        });
    }, 4500);
});
