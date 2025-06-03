let notificationCount = 0;
const maxNotifications = 5;

function showNotification(title, description, duration = 5000, onClick = null) {
    return;
    const notification = document.createElement('div');
    notification.className = 'notification';
    notification.innerHTML = `
        <div class="notification-content">
            <h4>${title}</h4>
            <p>${description}</p>
        </div>
        <div class="notification-progress"></div>
    `;
    document.body.appendChild(notification);

    // Set up click handler
    if (onClick) {
        notification.addEventListener('click', () => {
            onClick();
            removeNotification(notification);
        });
        notification.style.cursor = 'pointer';
    }

    // Position the notification
    notificationCount++;
    notification.style.bottom = `${(notificationCount - 1) * 110 + 20}px`;

    // Trigger animation
    requestAnimationFrame(() => {
        notification.classList.add('show');
        // Animate progress bar
        const progress = notification.querySelector('.notification-progress');
        progress.style.transition = `width ${duration}ms linear`;
        progress.style.width = '0%';
    });

    // Schedule removal
    notification.timeout = setTimeout(() => {
        removeNotification(notification);
    }, duration);

    // Remove oldest notification if we exceed the maximum
    if (notificationCount > maxNotifications) {
        const oldestNotification = document.querySelector('.notification:not(.removing)');
        if (oldestNotification) {
            removeNotification(oldestNotification);
        }
    }
}

function removeNotification(notification) {
    if (notification.classList.contains('removing')) return;
    notification.classList.add('removing');
    clearTimeout(notification.timeout);
    
    const notificationsToMove = Array.from(document.querySelectorAll('.notification:not(.removing)'))
        .filter(n => parseInt(n.style.bottom) > parseInt(notification.style.bottom));
    
    notification.style.transform = 'translateX(400px)';
    notification.style.opacity = '0';

    notificationsToMove.forEach(n => {
        const currentBottom = parseInt(n.style.bottom);
        n.style.transition = 'bottom 0.3s ease';
        n.style.bottom = `${currentBottom - 110}px`;
    });

    setTimeout(() => {
        notification.remove();
        notificationCount--;
    }, 300);
}

// Usage example:
// showNotification('Welcome!', 'Thanks for visiting Onix Client.', 5000, () => console.log('Notification clicked!'));






document.addEventListener('DOMContentLoaded', (event) => {
    const scrollContent = document.querySelector('.features-scroll-content');
    if (scrollContent) {
        scrollContent.innerHTML += scrollContent.innerHTML;
        
        function resetScroll() {
            if (scrollContent.scrollLeft >= scrollContent.scrollWidth / 2) {
                scrollContent.scrollLeft = 0;
            }
        }
        
        setInterval(resetScroll, 100);
    }

    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            e.preventDefault();
            document.querySelector(this.getAttribute('href')).scrollIntoView({
                behavior: 'smooth'
            });
        });
    });

    const animateOnScroll = (entries, observer) => {
        entries.forEach((entry, index) => {
            if (entry.isIntersecting) {
                setTimeout(() => {
                    entry.target.style.transition = 'opacity 0.5s ease, transform 0.5s ease, filter 0.5s ease';
                    entry.target.style.opacity = '1';
                    entry.target.style.transform = 'translateY(0)';
                    entry.target.style.filter = 'blur(0)';
                }, index * 200);
            } else {
                entry.target.style.opacity = '0';
                entry.target.style.transform = 'translateY(50px)';
                entry.target.style.filter = 'blur(5px)';
            }
        });
    };

    const observerOptions = {
        root: null,
        rootMargin: '0px',
        threshold: 0.1
    };

    const observer = new IntersectionObserver(animateOnScroll, observerOptions);

    document.querySelectorAll('.animate-on-scroll').forEach(element => {
        element.style.opacity = '0';
        element.style.transform = 'translateY(50px)';
        element.style.filter = 'blur(5px)';
        observer.observe(element);
    });

    const faqItems = document.querySelectorAll('.faq-item');
    faqItems.forEach(item => {
        const question = item.querySelector('.faq-question');
        question.addEventListener('click', () => {
            item.classList.toggle('active');
        });
    });

    document.querySelectorAll('.scroll-btn').forEach(button => {
        button.addEventListener('click', function(e) {
            e.preventDefault();
            const targetId = this.getAttribute('data-target');
            const targetElement = document.querySelector(targetId);
            
            if (targetElement) {
                targetElement.scrollIntoView({
                    behavior: 'smooth',
                    block: 'start'
                });
            }
        });
    });

    const navItems = document.querySelectorAll('.nav-links a');

    navItems.forEach(item => {
        item.addEventListener('click', (e) => {
            e.preventDefault();
            const targetId = item.getAttribute('href');
            const targetElement = document.querySelector(targetId);

            if (targetElement) {
                targetElement.scrollIntoView({
                    behavior: 'smooth',
                    block: 'start'
                });

                // Show notification with click handler
                const sectionName = item.textContent;
                showNotification(
                    'Navigation', 
                    `You've navigated to the ${sectionName} section`, 
                    5000, 
                    () => console.log(`Notification for ${sectionName} clicked!`)
                );
            }
        });
    });

    const navbar = document.querySelector('.nav-links');
    let targetRotationX = 0;
    let targetRotationY = 0;
    let currentRotationX = 0;
    let currentRotationY = 0;
    
    function updateNavbarRotation() {
        // Interpolate current rotation towards target rotation
        currentRotationX += (targetRotationX - currentRotationX) * 0.1;
        currentRotationY += (targetRotationY - currentRotationY) * 0.1;
    
        navbar.style.transform = `
            translateX(-50%)
            perspective(1000px)
            rotateX(${currentRotationX}deg)
            rotateY(${currentRotationY}deg)
            scale3d(1.03, 1.03, 1.03)
        `;
    
        requestAnimationFrame(updateNavbarRotation);
    }
    
    document.addEventListener('mousemove', (e) => {
        const { clientX, clientY } = e;
        const { left, top, width, height } = navbar.getBoundingClientRect();
        
        const centerX = left + width / 2;
        const centerY = top + height / 2;
        
        // Calculate the angle, but limit it to a maximum rotation
        const maxRotation = 5; // Maximum rotation in degrees
        targetRotationY = Math.max(-maxRotation, Math.min(maxRotation, (clientX - centerX) / width * maxRotation * 2));
        targetRotationX = Math.max(-maxRotation, Math.min(maxRotation, (clientY - centerY) / height * maxRotation * 2));
    });
    
    document.addEventListener('mouseleave', () => {
        targetRotationX = 0;
        targetRotationY = 0;
    });
    
    updateNavbarRotation();

    const featureCards = document.querySelectorAll('.feature-card');

    featureCards.forEach(card => {
        card.addEventListener('mousemove', handleFeatureCardHover);
        card.addEventListener('mouseleave', resetFeatureCard);
    });

    function handleFeatureCardHover(e) {
        const card = this;
        const { clientX, clientY } = e;
        const { left, top, width, height } = card.getBoundingClientRect();
        
        const centerX = left + width / 2;
        const centerY = top + height / 2;
        
        const deltaX = clientX - centerX;
        const deltaY = clientY - centerY;
        const angle = Math.atan2(deltaY, deltaX);
        
        const degrees = angle * (180 / Math.PI);
        const rotateX = -Math.sin(angle) * 5;
        const rotateY = Math.cos(angle) * 5;

        card.style.transform = `
            perspective(1000px)
            rotateX(${rotateX}deg)
            rotateY(${rotateY}deg)
            scale3d(1.05, 1.05, 1.05)
        `;
    }

    function resetFeatureCard() {
        this.style.transform = 'perspective(1000px) rotateX(0) rotateY(0) scale3d(1, 1, 1)';
    }
});