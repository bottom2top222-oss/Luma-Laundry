import { footerLinks } from '../assets/dummy-data';
import { motion } from 'framer-motion';

export default function Footer() {

    return (
        <motion.footer className="bg-white/6 border-t border-white/6 pt-10 text-gray-300"
            initial={{ opacity: 0 }}
            whileInView={{ opacity: 1 }}
            viewport={{ once: true }}
            transition={{ type: "spring", duration: 0.5 }}
        >
            <div className="max-w-6xl mx-auto px-6">
                <div className="flex flex-col md:flex-row items-start justify-between gap-10 py-10 border-b border-white/10">
                    <div>
                        <img src='/luma-logo.svg' alt="LUMA" className="h-8" />
                        <p className="max-w-[410px] mt-6 text-sm leading-relaxed text-white">
                            Email <a href="mailto:support@luma-laundry.app" className="font-semibold hover:text-white/90 transition">support@luma-laundry.app</a>
                        </p>
                        <p className="max-w-[410px] mt-2 text-sm leading-relaxed text-white">
                            Call <a href="tel:+19107534859" className="font-semibold hover:text-white/90 transition">(910) 753-4859</a>
                        </p>
                    </div>

                    <div className="flex flex-wrap justify-between w-full md:w-[45%] gap-5">
                        {footerLinks.map((section, index) => (
                            <div key={index}>
                                <h3 className="font-semibold text-base text-white md:mb-5 mb-2">
                                    {section.title}
                                </h3>
                                <ul className="text-sm space-y-1">
                                    {section.links.map(
                                        (link: { name: string; url: string }, i) => (
                                            <li key={i}>
                                                <a
                                                    href={link.url}
                                                    target={link.url.startsWith('http://') || link.url.startsWith('https://') ? '_blank' : undefined}
                                                    rel={link.url.startsWith('http://') || link.url.startsWith('https://') ? 'noopener noreferrer' : undefined}
                                                    className="hover:text-white transition"
                                                >
                                                    {link.name}
                                                </a>
                                            </li>
                                        )
                                    )}
                                </ul>
                            </div>
                        ))}
                    </div>
                </div>

                <p className="py-4 text-center text-sm text-gray-400">
                    © {new Date().getFullYear()} LUMA. All rights reserved.
                </p>
            </div>
        </motion.footer>
    );
};