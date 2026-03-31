// ===== Theme System =====
(function () {
    'use strict';

    const THEME_KEY = 'theme';

    function getPreferredTheme() {
        const stored = localStorage.getItem(THEME_KEY);
        if (stored) return stored;
        return 'light';
    }

    function applyTheme(theme) {
        document.documentElement.setAttribute('data-theme', theme);
        updateThemeIcon(theme);
        localStorage.setItem(THEME_KEY, theme);
    }

    function updateThemeIcon(theme) {
        const icon = document.getElementById('themeIcon');
        if (!icon) return;
        if (theme === 'dark') {
            icon.className = 'bi bi-moon-fill';
        } else {
            icon.className = 'bi bi-sun-fill';
        }
    }

    function toggleTheme() {
        const current = document.documentElement.getAttribute('data-theme') || 'light';
        const next = current === 'dark' ? 'light' : 'dark';
        applyTheme(next);
    }

    // Apply theme on DOM ready (backup — inline script in <head> handles FOUC)
    document.addEventListener('DOMContentLoaded', function () {
        const theme = getPreferredTheme();
        applyTheme(theme);

        // Bind toggle button
        const toggleBtn = document.getElementById('themeToggle');
        if (toggleBtn) {
            toggleBtn.addEventListener('click', toggleTheme);
        }
    });

    // Expose globally for inline usage
    window.toggleTheme = toggleTheme;
})();
