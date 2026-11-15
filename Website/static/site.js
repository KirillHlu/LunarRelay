(() => {
  const reduced = window.matchMedia('(prefers-reduced-motion: reduce)');
  if ('IntersectionObserver' in window && !reduced.matches) {
    document.documentElement.classList.add('js');
    const observer = new IntersectionObserver(entries => {
      entries.forEach(entry => { if (entry.isIntersecting) { entry.target.classList.add('in-view'); observer.unobserve(entry.target); } });
    }, { threshold: 0.08 });
    document.querySelectorAll('.reveal').forEach(element => observer.observe(element));
  }
  const hero = document.querySelector('.hero');
  const stage = document.querySelector('.moon-stage');
  let ticking = false;
  function paint() {
    if (hero && stage && !reduced.matches && window.scrollY < hero.offsetHeight) {
      stage.style.setProperty('--moon-scroll', `${Math.min(window.scrollY * 0.15, 130)}px`);
    }
    ticking = false;
  }
  window.addEventListener('scroll', () => { if (!ticking) { ticking = true; requestAnimationFrame(paint); } }, { passive: true });
  reduced.addEventListener('change', () => { if (reduced.matches) { stage?.style.removeProperty('--moon-scroll'); document.querySelectorAll('.reveal').forEach(e => e.classList.add('in-view')); } });
})();
