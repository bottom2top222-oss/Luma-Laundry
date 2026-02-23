import { ChevronDownIcon, MenuIcon, XIcon } from 'lucide-react';
import { PrimaryButton } from './Buttons';
import { useEffect, useState } from 'react';
import { motion } from 'framer-motion';

export default function Navbar() {
    const [isOpen, setIsOpen] = useState(false);
    const [activeHref, setActiveHref] = useState('#home');

    const navLinks = [
        { name: 'Home', href: '#home' },
        { name: 'Features', href: '#features' },
        { name: 'How It Works', href: '/Home/HowItWorks' },
        { name: 'Pricing', href: '/Home/Pricing' },
        { name: 'FAQ', href: '#faq' },
    ];

    const locationLinks = [
        { name: 'Florida Service Area', href: '/locations/florida' },
        { name: 'Brandon, FL', href: '/locations/florida?city=Brandon#service' },
        { name: 'Riverview, FL', href: '/locations/florida?city=Riverview#service' },
        { name: 'Valrico, FL', href: '/locations/florida?city=Valrico#service' },
        { name: 'Lithia, FL', href: '/locations/florida?city=Lithia#service' },
    ];

    useEffect(() => {
        const sectionIds = ['home', 'features', 'faq'];
        const visibleRatios = new Map<string, number>();

        const syncActiveFromLocation = () => {
            const currentPath = window.location.pathname.toLowerCase();
            const currentHash = window.location.hash.toLowerCase();

            if (currentPath === '/home/howitworks') {
                setActiveHref('/Home/HowItWorks');
                return;
            }

            if (currentPath === '/home/pricing') {
                setActiveHref('/Home/Pricing');
                return;
            }

            if (currentPath === '/home/locations' || currentPath === '/locations/florida') {
                setActiveHref('/Home/Locations');
                return;
            }

            if (currentPath === '/' || currentPath === '') {
                if (currentHash === '#features' || currentHash === '#faq' || currentHash === '#home') {
                    setActiveHref(currentHash);
                } else {
                    setActiveHref('#home');
                }
            }
        };

        const updateActiveFromObservedSections = () => {
            if (visibleRatios.size === 0) {
                return;
            }

            let bestSectionId = 'home';
            let bestRatio = 0;

            for (const [sectionId, ratio] of visibleRatios.entries()) {
                if (ratio > bestRatio) {
                    bestRatio = ratio;
                    bestSectionId = sectionId;
                }
            }

            setActiveHref(`#${bestSectionId}`);
        };

        syncActiveFromLocation();

        const currentPath = window.location.pathname.toLowerCase();
        const isLandingPath = currentPath === '/' || currentPath === '';
        const sectionElements = sectionIds
            .map((sectionId) => document.getElementById(sectionId))
            .filter((element): element is HTMLElement => element !== null);

        const observer = isLandingPath
            ? new IntersectionObserver(
                (entries) => {
                    for (const entry of entries) {
                        const sectionId = entry.target.id;
                        if (entry.isIntersecting) {
                            visibleRatios.set(sectionId, entry.intersectionRatio);
                        } else {
                            visibleRatios.delete(sectionId);
                        }
                    }

                    updateActiveFromObservedSections();
                },
                {
                    root: null,
                    rootMargin: '-28% 0px -52% 0px',
                    threshold: [0.15, 0.3, 0.5, 0.7],
                }
            )
            : null;

        if (observer) {
            for (const sectionElement of sectionElements) {
                observer.observe(sectionElement);
            }
        }

        window.addEventListener('hashchange', syncActiveFromLocation);

        return () => {
            window.removeEventListener('hashchange', syncActiveFromLocation);
            if (observer) {
                observer.disconnect();
            }
        };
    }, []);

    return (
        <motion.nav className='fixed top-5 left-0 right-0 z-50 px-4'
            initial={{ y: -100, opacity: 0 }}
            animate={{ y: 0, opacity: 1 }}
            viewport={{ once: true }}
            transition={{ type: "spring", stiffness: 250, damping: 70, mass: 1 }}
        >
            <div className='max-w-6xl mx-auto flex items-center justify-between bg-black/50 backdrop-blur-md border border-white/4 rounded-2xl p-3'>
                <a href='/'>
                    <img src='/luma-logo.svg' alt="Luma Laundry" className="h-8" />
                </a>

                <div className='hidden md:flex items-center gap-8 text-sm font-medium text-gray-300'>
                    {navLinks.map((link) => (
                        <a
                            href={link.href}
                            key={link.name}
                            onClick={() => setActiveHref(link.href)}
                            className={`${activeHref === link.href
                                ? 'text-white [text-shadow:0_0_14px_rgba(56,217,255,0.45)] border-b-2 border-cyan-400 pb-0.5'
                                : 'text-gray-300 hover:text-white'} transition`}
                        >
                            {link.name}
                        </a>
                    ))}
                    <div className='relative group'>
                        <a
                            href='/Home/Locations'
                            onClick={() => setActiveHref('/Home/Locations')}
                            className={`${activeHref === '/Home/Locations'
                                ? 'text-white [text-shadow:0_0_14px_rgba(56,217,255,0.45)] border-b-2 border-cyan-400 pb-0.5'
                                : 'text-gray-300 hover:text-white'} inline-flex items-center gap-1 transition`}
                        >
                            Locations
                            <ChevronDownIcon className='size-4 opacity-80' />
                        </a>
                        <div className='invisible opacity-0 group-hover:visible group-hover:opacity-100 group-focus-within:visible group-focus-within:opacity-100 absolute left-0 mt-2 min-w-56 rounded-xl border border-white/10 bg-black/85 backdrop-blur-md p-2 transition'>
                            {locationLinks.map((locationLink) => (
                                <a
                                    key={locationLink.name}
                                    href={locationLink.href}
                                    className='block rounded-lg px-3 py-2 text-sm text-gray-200 hover:bg-white/10 hover:text-white transition'
                                >
                                    {locationLink.name}
                                </a>
                            ))}
                        </div>
                    </div>
                </div>

                <div className='hidden md:flex items-center gap-3'>
                    <a href='/Account/Login' className='text-sm font-medium text-gray-300 hover:text-white transition max-sm:hidden'>
                        Sign in
                    </a>
                    <a href='/Orders/Schedule'>
                        <PrimaryButton className='max-sm:text-xs hidden sm:inline-block'>Schedule Pickup</PrimaryButton>
                    </a>
                </div>

                <button onClick={() => setIsOpen(!isOpen)} className='md:hidden'>
                    <MenuIcon className='size-6' />
                </button>
            </div>
            <div className={`flex flex-col items-center justify-center gap-6 text-lg font-medium fixed inset-0 bg-black/40 backdrop-blur-md z-50 transition-all duration-300 ${isOpen ? "translate-x-0" : "translate-x-full"}`}>
                {navLinks.map((link) => (
                    <a
                        key={link.name}
                        href={link.href}
                        onClick={() => {
                            setActiveHref(link.href);
                            setIsOpen(false);
                        }}
                        className={activeHref === link.href ? 'text-white [text-shadow:0_0_14px_rgba(56,217,255,0.45)]' : 'text-gray-300 hover:text-white'}
                    >
                        {link.name}
                    </a>
                ))}

                <a
                    href='/Home/Locations'
                    onClick={() => {
                        setActiveHref('/Home/Locations');
                        setIsOpen(false);
                    }}
                    className={activeHref === '/Home/Locations' ? 'text-white [text-shadow:0_0_14px_rgba(56,217,255,0.45)]' : 'text-gray-300 hover:text-white'}
                >
                    Locations
                </a>
                {locationLinks.map((locationLink) => (
                    <a
                        key={locationLink.name}
                        href={locationLink.href}
                        onClick={() => setIsOpen(false)}
                        className='text-sm text-gray-400 hover:text-white transition'
                    >
                        {locationLink.name}
                    </a>
                ))}

                <a href='/Account/Login' onClick={() => setIsOpen(false)} className='font-medium text-gray-300 hover:text-white transition'>
                    Sign in
                </a>
                <a href='/Orders/Schedule' onClick={() => setIsOpen(false)}>
                    <PrimaryButton>Schedule Pickup</PrimaryButton>
                </a>

                <button
                    onClick={() => setIsOpen(false)}
                    className="rounded-md bg-white p-2 text-gray-800 ring-white active:ring-2"
                >
                    <XIcon />
                </button>
            </div>
        </motion.nav>
    );
};