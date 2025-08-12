(function(){
  const toggle = document.querySelector('[data-nav-toggle]');
  const menu = document.querySelector('[data-nav-menu]');
  if(!toggle || !menu) return;

  toggle.addEventListener('click', () => {
    const open = menu.classList.toggle('navbar__menu--open');
    toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
  });

  // Close menu on navigation (mobile)
  menu.addEventListener('click', e => {
    if(e.target.closest('a')) {
      if(window.innerWidth < 900) {
        menu.classList.remove('navbar__menu--open');
        toggle.setAttribute('aria-expanded', 'false');
      }
    }
  });

  // Ensure correct state on resize
  window.addEventListener('resize', () => {
    if(window.innerWidth >= 900) {
      menu.classList.add('navbar__menu--open');
      toggle.setAttribute('aria-expanded', 'true');
    } else {
      menu.classList.remove('navbar__menu--open');
      toggle.setAttribute('aria-expanded', 'false');
    }
  });
})();
