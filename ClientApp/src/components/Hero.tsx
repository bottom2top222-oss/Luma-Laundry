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

const sparkleDots = [
    { left: '6%', top: '16%', size: 3, delay: 0.15 },
    { left: '14%', top: '30%', size: 2, delay: 0.4 },
    { left: '21%', top: '10%', size: 4, delay: 0.9 },
    { left: '30%', top: '24%', size: 2, delay: 0.55 },
    { left: '39%', top: '12%', size: 3, delay: 0.75 },
    { left: '49%', top: '30%', size: 2, delay: 0.25 },
    { left: '58%', top: '14%', size: 4, delay: 0.65 },
    { left: '69%', top: '26%', size: 2, delay: 0.5 },
    { left: '77%', top: '11%', size: 3, delay: 0.95 },
    { left: '86%', top: '22%', size: 2, delay: 0.35 },
    { left: '93%', top: '15%', size: 3, delay: 0.8 }
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
                    className="relative text-[clamp(3rem,14vw,7rem)] font-semibold tracking-[0.2em] mb-12"
                >
                    <span className="pointer-events-none absolute -inset-x-16 -top-8 -bottom-14">
                        {sparkleDots.map((sparkle, index) => (
                            <motion.span
                                key={`${sparkle.left}-${sparkle.top}-${index}`}
                                className="absolute rounded-full bg-cyan-100"
                                style={{
                                    left: sparkle.left,
                                    top: sparkle.top,
                                    width: `${sparkle.size}px`,
                                    height: `${sparkle.size}px`,
                                    boxShadow: '0 0 12px rgba(103, 232, 249, 0.95), 0 0 26px rgba(59, 130, 246, 0.55)'
                                }}
                                initial={{ opacity: 0.45, scale: 0.9 }}
                                animate={{ opacity: [0.25, 1, 0.35], scale: [0.9, 1.35, 1] }}
                                transition={{ duration: 2.8, repeat: Infinity, repeatType: 'mirror', delay: sparkle.delay }}
                            />
                        ))}
                    </span>

                    <span className="relative inline-block pb-14">
                        <span
                            aria-hidden="true"
                            className="absolute inset-0 blur-2xl opacity-70 text-cyan-300"
                        >
                            LUMA
                        </span>
                        <span
                            aria-hidden="true"
                            className="absolute inset-0 blur-md opacity-90 bg-clip-text text-transparent bg-linear-to-b from-cyan-100 via-cyan-300 to-blue-400"
                        >
                            LUMA
                        </span>
                        <span className="relative bg-clip-text text-transparent bg-linear-to-b from-cyan-100 via-cyan-300 to-blue-400 drop-shadow-[0_0_16px] drop-shadow-cyan-300">
                            LUMA
                        </span>

                        <span
                            aria-hidden="true"
                            className="pointer-events-none absolute left-1/2 top-full -translate-x-1/2 blur-[1.5px]"
                            style={{
                                transform: 'translateX(-50%) scaleY(-0.55)',
                                opacity: 0.34,
                                WebkitMaskImage: 'linear-gradient(to bottom, rgba(255,255,255,0.95), rgba(255,255,255,0))',
                                maskImage: 'linear-gradient(to bottom, rgba(255,255,255,0.95), rgba(255,255,255,0))'
                            }}
                        >
                            <span className="bg-clip-text text-transparent bg-linear-to-b from-cyan-100 via-cyan-300 to-blue-500 drop-shadow-[0_0_16px] drop-shadow-cyan-300">
                                LUMA
                            </span>
                        </span>
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