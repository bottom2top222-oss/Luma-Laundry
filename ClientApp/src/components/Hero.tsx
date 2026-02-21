import { motion } from 'framer-motion';
import { ArrowRightIcon, LeafIcon, SparklesIcon, TruckIcon } from 'lucide-react';
import { GhostButton, PrimaryButton } from './Buttons';

const featureChips = [
    {
        icon: <SparklesIcon className="size-4 text-cyan-400" aria-hidden="true" />,
        label: 'Same-day service'
    },
    {
        icon: <LeafIcon className="size-4 text-emerald-400" aria-hidden="true" />,
        label: 'Eco-friendly cleaning'
    },
    {
        icon: <TruckIcon className="size-4 text-teal-300" aria-hidden="true" />,
        label: 'Free pickup & delivery'
    }
];

export default function Hero() {

    return (
        <section id="home" className="relative z-10 overflow-hidden">
            <div className="max-w-6xl mx-auto px-4 min-h-screen flex flex-col items-center justify-center text-center pt-28 pb-24">
                <motion.div
                    initial={{ y: 60, opacity: 0 }}
                    whileInView={{ y: 0, opacity: 1 }}
                    viewport={{ once: true }}
                    transition={{ type: 'spring', stiffness: 250, damping: 70, mass: 1 }}
                    className="text-[clamp(3rem,14vw,7rem)] font-semibold tracking-[0.2em] mb-6"
                >
                    <span className="bg-clip-text text-transparent bg-linear-to-r from-cyan-300 to-cyan-400">
                        LUMA
                    </span>
                </motion.div>

                <motion.p
                    className="text-gray-300 text-lg max-w-2xl mx-auto mb-8"
                    initial={{ y: 60, opacity: 0 }}
                    whileInView={{ y: 0, opacity: 1 }}
                    viewport={{ once: true }}
                    transition={{ type: 'spring', stiffness: 250, damping: 70, mass: 1, delay: 0.1 }}
                >
                    We know you're busy - our laundry service is designed for professionals like you. We pick up,
                    clean, and deliver, so you can focus on what matters most.
                </motion.p>

                <motion.div
                    className="flex flex-col sm:flex-row items-center gap-4 mb-12 justify-center w-full max-w-2xl"
                    initial={{ y: 60, opacity: 0 }}
                    whileInView={{ y: 0, opacity: 1 }}
                    viewport={{ once: true }}
                    transition={{ type: 'spring', stiffness: 250, damping: 70, mass: 1, delay: 0.2 }}
                >
                    <a href="/Orders/Schedule" className="w-full sm:w-auto">
                        <PrimaryButton className="max-sm:w-full py-3 px-7">
                            Schedule pickup <ArrowRightIcon className="size-4" />
                        </PrimaryButton>
                    </a>
                    <a href="#pricing" className="w-full sm:w-auto">
                        <GhostButton className="max-sm:w-full max-sm:justify-center py-3 px-6">
                            View pricing
                        </GhostButton>
                    </a>
                </motion.div>

                <motion.div
                    className="flex flex-wrap items-center justify-center gap-4 text-sm text-gray-200"
                    initial={{ y: 60, opacity: 0 }}
                    whileInView={{ y: 0, opacity: 1 }}
                    viewport={{ once: true }}
                    transition={{ type: 'spring', stiffness: 250, damping: 70, mass: 1, delay: 0.3 }}
                >
                    {featureChips.map((chip) => (
                        <div
                            key={chip.label}
                            className="flex items-center gap-2 px-4 py-2 rounded-full bg-white/5 border border-white/10"
                        >
                            {chip.icon}
                            <span>{chip.label}</span>
                        </div>
                    ))}
                </motion.div>
            </div>
        </section>
    );
};