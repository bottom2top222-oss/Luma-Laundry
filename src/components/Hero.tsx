import { ArrowRightIcon } from 'lucide-react';
import { PrimaryButton, GhostButton } from './Buttons';
import { motion } from 'framer-motion';

export default function Hero() {
    return (
        <section id="home" className="relative z-10 overflow-hidden">
            <div className="pointer-events-none absolute inset-0 bg-linear-to-b from-[#08093b] via-[#060741] to-[#030329]" />

            <div className="relative max-w-6xl mx-auto px-4 min-h-[90vh] pt-28 pb-16 flex items-center justify-center text-center">
                <div className="w-full max-w-5xl">
                    <motion.img
                        src="/luma-hero.png"
                        alt="LUMA"
                        className="mx-auto w-[112%] max-w-[1240px] -ml-[6%] sm:w-full sm:max-w-[1160px] sm:ml-0"
                        initial={{ opacity: 0, y: 18 }}
                        whileInView={{ opacity: 1, y: 0 }}
                        viewport={{ once: true }}
                        transition={{ duration: 0.6, ease: 'easeOut' }}
                    />

                    <motion.p
                        className="mx-auto mt-3 max-w-4xl text-xl md:text-[2rem] text-gray-100/95 leading-relaxed"
                        initial={{ opacity: 0, y: 18 }}
                        whileInView={{ opacity: 1, y: 0 }}
                        viewport={{ once: true }}
                        transition={{ duration: 0.6, ease: 'easeOut', delay: 0.1 }}
                    >
                        We know you&rsquo;re busy&mdash;our laundry service is designed for professionals like you. We pick up,
                        pick up, clean, and deliver, so you can focus on what matters most.
                    </motion.p>

                    <motion.div
                        className="mt-8 flex flex-col sm:flex-row gap-5 justify-center items-center"
                        initial={{ opacity: 0, y: 18 }}
                        whileInView={{ opacity: 1, y: 0 }}
                        viewport={{ once: true }}
                        transition={{ duration: 0.6, ease: 'easeOut', delay: 0.2 }}
                    >
                        <a href="/schedule" className="w-full sm:w-auto">
                            <PrimaryButton className="w-full sm:w-auto min-w-[17.5rem] py-3.5 px-9 text-2xl rounded-full shadow-[0_0_0_1px_rgba(255,255,255,0.22),0_10px_28px_rgba(76,29,149,0.45)]">
                                Schedule pickup
                                <ArrowRightIcon className="size-6" />
                            </PrimaryButton>
                        </a>

                        <a href="/pricing" className="w-full sm:w-auto">
                            <GhostButton className="w-full sm:w-auto min-w-[17.5rem] py-3.5 px-9 text-2xl border-white/25 bg-transparent hover:bg-white/8 justify-center">
                                View pricing
                            </GhostButton>
                        </a>
                    </motion.div>
                </div>
            </div>
        </section>
    );
}