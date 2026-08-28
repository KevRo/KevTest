(function () {
    var btn = document.getElementById('themeToggle');
    if (!btn) {
        return;
    }

    function currentTheme() {
        return document.documentElement.getAttribute('data-bs-theme') === 'dark' ? 'dark' : 'light';
    }

    function updateButton() {
        var isDark = currentTheme() === 'dark';
        btn.textContent = isDark ? '☀️' : '🌙';
        btn.setAttribute('aria-pressed', String(isDark));
    }

    updateButton();

    btn.addEventListener('click', function () {
        var next = currentTheme() === 'dark' ? 'light' : 'dark';
        document.documentElement.setAttribute('data-bs-theme', next);
        try {
            localStorage.setItem('theme', next);
        } catch (e) {}
        updateButton();
    });
})();
