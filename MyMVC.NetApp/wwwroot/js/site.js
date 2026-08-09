// Site scripts go here.

(function () {
  var root = document.documentElement;
  var toggleBtn = document.getElementById('themeToggle');
  var icon = document.getElementById('themeIcon');

  function updateIcon(theme) {
    if (icon) {
      icon.textContent = theme === 'dark' ? '☀️' : '🌙';
    }
  }

  updateIcon(root.getAttribute('data-bs-theme') || 'light');

  if (toggleBtn) {
    toggleBtn.addEventListener('click', function () {
      var current = root.getAttribute('data-bs-theme') === 'dark' ? 'dark' : 'light';
      var next = current === 'dark' ? 'light' : 'dark';
      root.setAttribute('data-bs-theme', next);
      localStorage.setItem('theme', next);
      updateIcon(next);
    });
  }
})();
