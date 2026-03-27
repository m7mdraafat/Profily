// ===== 3D PURPLE TEMPLATE — MAIN.JS =====

(function () {
    'use strict';

    // ===== LOADER =====
    window.addEventListener('load', () => {
        const loader = document.getElementById('loader');
        if (loader) {
            setTimeout(() => loader.classList.add('hidden'), 800);
        }
    });

    // ===== CUSTOM CURSOR =====
    const cursor = document.getElementById('cursor');
    const follower = document.getElementById('cursor-follower');

    if (cursor && follower && window.matchMedia('(hover: hover)').matches) {
        let mouseX = 0, mouseY = 0;
        let cursorX = 0, cursorY = 0;
        let followerX = 0, followerY = 0;

        document.addEventListener('mousemove', (e) => {
            mouseX = e.clientX;
            mouseY = e.clientY;
        });

        function animateCursor() {
            cursorX += (mouseX - cursorX) * 0.2;
            cursorY += (mouseY - cursorY) * 0.2;
            followerX += (mouseX - followerX) * 0.08;
            followerY += (mouseY - followerY) * 0.08;

            cursor.style.left = cursorX + 'px';
            cursor.style.top = cursorY + 'px';
            follower.style.left = followerX + 'px';
            follower.style.top = followerY + 'px';

            requestAnimationFrame(animateCursor);
        }
        animateCursor();

        // Hover effects on interactive elements
        const interactiveEls = document.querySelectorAll('a, button, .project-card, .service-card, .skill-badge');
        interactiveEls.forEach((el) => {
            el.addEventListener('mouseenter', () => {
                cursor.classList.add('active');
                follower.classList.add('active');
            });
            el.addEventListener('mouseleave', () => {
                cursor.classList.remove('active');
                follower.classList.remove('active');
            });
        });
    }

    // ===== THREE.JS BACKGROUND =====
    const canvas = document.getElementById('bg-canvas');
    if (canvas && typeof THREE !== 'undefined') {
        const scene = new THREE.Scene();
        const camera = new THREE.PerspectiveCamera(75, window.innerWidth / window.innerHeight, 0.1, 1000);
        const renderer = new THREE.WebGLRenderer({ canvas, alpha: true, antialias: true });
        renderer.setSize(window.innerWidth, window.innerHeight);
        renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));

        // Particles
        const particleCount = 800;
        const positions = new Float32Array(particleCount * 3);
        const colors = new Float32Array(particleCount * 3);

        for (let i = 0; i < particleCount; i++) {
            positions[i * 3] = (Math.random() - 0.5) * 20;
            positions[i * 3 + 1] = (Math.random() - 0.5) * 20;
            positions[i * 3 + 2] = (Math.random() - 0.5) * 20;

            // Purple-ish colors
            colors[i * 3] = 0.5 + Math.random() * 0.3;     // R
            colors[i * 3 + 1] = 0.2 + Math.random() * 0.2; // G
            colors[i * 3 + 2] = 0.8 + Math.random() * 0.2; // B
        }

        const particleGeometry = new THREE.BufferGeometry();
        particleGeometry.setAttribute('position', new THREE.BufferAttribute(positions, 3));
        particleGeometry.setAttribute('color', new THREE.BufferAttribute(colors, 3));

        const particleMaterial = new THREE.PointsMaterial({
            size: 0.03,
            vertexColors: true,
            transparent: true,
            opacity: 0.7,
            blending: THREE.AdditiveBlending,
        });

        const particles = new THREE.Points(particleGeometry, particleMaterial);
        scene.add(particles);

        // Morphing geometry
        const torusGeometry = new THREE.TorusKnotGeometry(1.5, 0.4, 100, 16);
        const torusMaterial = new THREE.MeshBasicMaterial({
            color: 0xa855f7,
            wireframe: true,
            transparent: true,
            opacity: 0.08,
        });
        const torus = new THREE.Mesh(torusGeometry, torusMaterial);
        torus.position.set(4, 0, -5);
        scene.add(torus);

        camera.position.z = 6;

        let scrollY = 0;
        window.addEventListener('scroll', () => {
            scrollY = window.scrollY;
        });

        function animateScene() {
            requestAnimationFrame(animateScene);

            particles.rotation.y += 0.0003;
            particles.rotation.x += 0.0001;

            torus.rotation.x += 0.003;
            torus.rotation.y += 0.005;

            // Parallax on scroll
            camera.position.y = -scrollY * 0.001;

            renderer.render(scene, camera);
        }
        animateScene();

        window.addEventListener('resize', () => {
            camera.aspect = window.innerWidth / window.innerHeight;
            camera.updateProjectionMatrix();
            renderer.setSize(window.innerWidth, window.innerHeight);
        });
    }

    // ===== NAVIGATION =====
    const nav = document.getElementById('nav');
    const menuBtn = document.getElementById('menu-btn');
    const mobileMenu = document.getElementById('mobile-menu');

    // Scroll effect
    if (nav) {
        window.addEventListener('scroll', () => {
            nav.classList.toggle('scrolled', window.scrollY > 50);
        });
    }

    // Mobile menu toggle
    if (menuBtn && mobileMenu) {
        menuBtn.addEventListener('click', () => {
            menuBtn.classList.toggle('active');
            mobileMenu.classList.toggle('active');
            document.body.style.overflow = mobileMenu.classList.contains('active') ? 'hidden' : '';
        });

        // Close on link click
        mobileMenu.querySelectorAll('.mobile-link').forEach((link) => {
            link.addEventListener('click', () => {
                menuBtn.classList.remove('active');
                mobileMenu.classList.remove('active');
                document.body.style.overflow = '';
            });
        });
    }

    // Active nav link on scroll
    const sections = document.querySelectorAll('.section[id]');
    const navLinks = document.querySelectorAll('.nav-link');

    if (sections.length && navLinks.length) {
        window.addEventListener('scroll', () => {
            const scrollPos = window.scrollY + 200;

            sections.forEach((section) => {
                const top = section.offsetTop;
                const height = section.offsetHeight;
                const id = section.getAttribute('id');

                if (scrollPos >= top && scrollPos < top + height) {
                    navLinks.forEach((link) => {
                        link.classList.remove('active');
                        if (link.getAttribute('href') === '#' + id) {
                            link.classList.add('active');
                        }
                    });
                }
            });
        });
    }

    // ===== TYPED ROLE EFFECT =====
    const typedEl = document.getElementById('typed-role');
    if (typedEl) {
        const roles = ['Software Engineer', 'Backend Developer', 'AI Agent Builder', 'Problem Solver'];
        let roleIndex = 0;
        let charIndex = 0;
        let isDeleting = false;
        let typingSpeed = 100;

        function typeRole() {
            const current = roles[roleIndex];

            if (isDeleting) {
                typedEl.textContent = current.substring(0, charIndex - 1);
                charIndex--;
                typingSpeed = 50;
            } else {
                typedEl.textContent = current.substring(0, charIndex + 1);
                charIndex++;
                typingSpeed = 100;
            }

            if (!isDeleting && charIndex === current.length) {
                typingSpeed = 2000; // Pause at end
                isDeleting = true;
            } else if (isDeleting && charIndex === 0) {
                isDeleting = false;
                roleIndex = (roleIndex + 1) % roles.length;
                typingSpeed = 400; // Pause before new word
            }

            setTimeout(typeRole, typingSpeed);
        }
        setTimeout(typeRole, 1000);
    }

    // ===== SCROLL ANIMATIONS =====
    const animateElements = document.querySelectorAll('[data-animate]');

    if (animateElements.length) {
        const observer = new IntersectionObserver(
            (entries) => {
                entries.forEach((entry, index) => {
                    if (entry.isIntersecting) {
                        setTimeout(() => {
                            entry.target.classList.add('animated');
                        }, index * 100);
                        observer.unobserve(entry.target);
                    }
                });
            },
            { threshold: 0.1, rootMargin: '0px 0px -50px 0px' }
        );

        animateElements.forEach((el) => observer.observe(el));
    }

    // Skill categories animation
    const skillCategories = document.querySelectorAll('.skill-category');
    if (skillCategories.length) {
        const skillObserver = new IntersectionObserver(
            (entries) => {
                entries.forEach((entry, index) => {
                    if (entry.isIntersecting) {
                        setTimeout(() => {
                            entry.target.classList.add('animated');
                        }, index * 200);
                        skillObserver.unobserve(entry.target);
                    }
                });
            },
            { threshold: 0.1 }
        );

        skillCategories.forEach((el) => skillObserver.observe(el));
    }

    // ===== GSAP ANIMATIONS =====
    if (typeof gsap !== 'undefined' && typeof ScrollTrigger !== 'undefined') {
        gsap.registerPlugin(ScrollTrigger);

        // Hero entrance
        gsap.from('.hero-greeting', { opacity: 0, y: 30, duration: 0.8, delay: 1 });
        gsap.from('.hero-name', { opacity: 0, y: 30, duration: 0.8, delay: 1.2 });
        gsap.from('.hero-role', { opacity: 0, y: 30, duration: 0.8, delay: 1.4 });
        gsap.from('.hero-description', { opacity: 0, y: 30, duration: 0.8, delay: 1.6 });
        gsap.from('.hero-cta', { opacity: 0, y: 30, duration: 0.8, delay: 1.8 });
        gsap.from('.hero-socials', { opacity: 0, y: 30, duration: 0.8, delay: 2.0 });
        gsap.from('.hero-image-wrapper', { opacity: 0, scale: 0.8, duration: 1, delay: 1.2, ease: 'back.out(1.7)' });

        // Section headers
        gsap.utils.toArray('.section-header').forEach((header) => {
            gsap.from(header, {
                scrollTrigger: {
                    trigger: header,
                    start: 'top 85%',
                    toggleActions: 'play none none none',
                },
                opacity: 0,
                y: 40,
                duration: 0.8,
            });
        });
    }

    // ===== CONTACT FORM =====
    const contactForm = document.getElementById('contact-form');
    if (contactForm) {
        contactForm.addEventListener('submit', (e) => {
            e.preventDefault();
            // Placeholder — form submission would be handled by the portfolio owner
            const btn = contactForm.querySelector('button[type="submit"]');
            const originalText = btn.querySelector('span').textContent;
            btn.querySelector('span').textContent = 'Message Sent!';
            btn.disabled = true;
            setTimeout(() => {
                btn.querySelector('span').textContent = originalText;
                btn.disabled = false;
                contactForm.reset();
            }, 3000);
        });
    }
})();
