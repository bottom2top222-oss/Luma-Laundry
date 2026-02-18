import { ArrowRightIcon, ZapIcon, CheckIcon } from 'lucide-react';
import { PrimaryButton, GhostButton } from './Buttons';
import { motion } from 'framer-motion';

export default function Hero() {

    return (
        <>
            <section id="home" className="relative z-10">
                <div className="max-w-6xl mx-auto px-4 min-h-screen max-md:w-screen max-md:overflow-hidden pt-32 md:pt-26 flex items-center justify-center">
                    <div className="grid grid-cols-1 gap-10 items-center">
                        <div className="text-center max-w-4xl mx-auto">
                            <motion.h1 className="text-6xl md:text-8xl font-bold leading-tight mb-6"
                                initial={{ y: 60, opacity: 0 }}
                                whileInView={{ y: 0, opacity: 1 }}
                                viewport={{ once: true }}
                                transition={{ type: "spring", stiffness: 250, damping: 70, mass: 1, delay: 0.1 }}
                            >
                                <span className="bg-clip-text text-transparent bg-linear-to-r from-cyan-300 to-cyan-400">
                                    LUMA
                                </span>
                            </motion.h1>

                            <motion.p className="text-gray-300 text-lg max-w-2xl mx-auto mb-8"
                                initial={{ y: 60, opacity: 0 }}
                                whileInView={{ y: 0, opacity: 1 }}
                                viewport={{ once: true }}
                                transition={{ type: "spring", stiffness: 250, damping: 70, mass: 1, delay: 0.2 }}
                            >
                                We know you're busy—our laundry service is designed for professionals like you. We pick up, clean, and deliver, so you can focus on what matters most.
                            </motion.p>

                            <motion.div className="flex flex-col sm:flex-row items-center gap-4 mb-12 justify-center"
                                initial={{ y: 60, opacity: 0 }}
                                whileInView={{ y: 0, opacity: 1 }}
                                viewport={{ once: true }}
                                transition={{ type: "spring", stiffness: 250, damping: 70, mass: 1, delay: 0.3 }}
                            >
                                <a href="/Orders/Schedule" className="w-full sm:w-auto">
                                    <PrimaryButton className="max-sm:w-full py-3 px-7">
                                        Schedule pickup
                                        <ArrowRightIcon className="size-4" />
                                    </PrimaryButton>
                                </a>

                                <a href="#pricing" className="w-full sm:w-auto">
                                    <GhostButton className="max-sm:w-full max-sm:justify-center py-3 px-5">
                                        View pricing
                                    </GhostButton>
                                </a>
                            </motion.div>

                            <motion.div className="flex flex-wrap items-center justify-center gap-6 text-sm text-gray-200"
                                initial={{ y: 60, opacity: 0 }}
                                whileInView={{ y: 0, opacity: 1 }}
                                viewport={{ once: true }}
                                transition={{ type: "spring", stiffness: 250, damping: 70, mass: 1, delay: 0.4 }}
                            >
                                <div className="flex items-center gap-2 px-4 py-2 rounded-lg bg-white/5 border border-white/10">
                                    <ZapIcon className="size-4 text-cyan-400" />
                                    <div>Same-day service</div>
                                </div>

                                <div className="flex items-center gap-2 px-4 py-2 rounded-lg bg-white/5 border border-white/10">
                                    <CheckIcon className="size-4 text-cyan-400" />
                                    <div>Eco-friendly cleaning</div>
                                </div>

                                <div className="flex items-center gap-2 px-4 py-2 rounded-lg bg-white/5 border border-white/10">
                                    <CheckIcon className="size-4 text-cyan-400" />
                                    <div>Free pickup & delivery</div>
                                </div>
                            </motion.div>
                        </div>
                    </div>
                </div>
            </section>
        </>
    );
};