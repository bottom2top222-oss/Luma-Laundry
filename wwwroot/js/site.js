// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

(function initLumaScroll() {
	if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
		return;
	}

	if (typeof window.Lenis !== 'function') {
		return;
	}

	if (window.__lumaLenisInitialized) {
		return;
	}

	window.__lumaLenisInitialized = true;

	const lenis = new window.Lenis({
		duration: 1.2,
		smoothWheel: true,
		anchors: {
			offset: -100,
		},
	});

	const raf = (time) => {
		lenis.raf(time);
		window.requestAnimationFrame(raf);
	};

	window.requestAnimationFrame(raf);
})();
