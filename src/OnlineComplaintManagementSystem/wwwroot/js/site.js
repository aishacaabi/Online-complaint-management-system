// Light/dark appearance toggle
document.addEventListener('DOMContentLoaded', function () {
    var themeToggle = document.getElementById('themeToggle');
    var themeIcon = document.getElementById('themeToggleIcon');

    function applyIcon(theme) {
        if (!themeIcon) return;
        themeIcon.classList.toggle('bi-moon-stars', theme === 'light');
        themeIcon.classList.toggle('bi-sun', theme === 'dark');
    }

    applyIcon(document.documentElement.getAttribute('data-bs-theme') || 'light');

    if (themeToggle) {
        themeToggle.addEventListener('click', function () {
            var current = document.documentElement.getAttribute('data-bs-theme') || 'light';
            var next = current === 'dark' ? 'light' : 'dark';
            document.documentElement.setAttribute('data-bs-theme', next);
            localStorage.setItem('cms-theme', next);
            applyIcon(next);
        });
    }
});

// Sidebar toggle for mobile
document.addEventListener('DOMContentLoaded', function () {
    var toggle = document.getElementById('sidebarToggle');
    var sidebar = document.getElementById('appSidebar');
    var backdrop = document.getElementById('sidebarBackdrop');

    function closeSidebar() {
        if (sidebar) sidebar.classList.remove('show');
        if (backdrop) backdrop.classList.remove('show');
    }

    if (toggle && sidebar) {
        toggle.addEventListener('click', function () {
            sidebar.classList.toggle('show');
            if (backdrop) backdrop.classList.toggle('show');
        });
    }
    if (backdrop) {
        backdrop.addEventListener('click', closeSidebar);
    }

    // Auto-dismiss alerts after a few seconds
    document.querySelectorAll('.alert.alert-dismissible').forEach(function (el) {
        setTimeout(function () {
            var alert = bootstrap.Alert.getOrCreateInstance(el);
            if (alert) alert.close();
        }, 6000);
    });

    // Star rating widget (feedback form)
    document.querySelectorAll('.star-rating').forEach(function (widget) {
        var input = widget.querySelector('input[type=hidden]');
        var stars = widget.querySelectorAll('.star');
        stars.forEach(function (star) {
            star.addEventListener('click', function () {
                var value = parseInt(star.getAttribute('data-value'), 10);
                input.value = value;
                stars.forEach(function (s) {
                    s.classList.toggle('bi-star-fill', parseInt(s.getAttribute('data-value'), 10) <= value);
                    s.classList.toggle('bi-star', parseInt(s.getAttribute('data-value'), 10) > value);
                });
            });
        });
    });
});
