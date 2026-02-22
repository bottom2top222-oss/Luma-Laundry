import React from 'react'

export const PrimaryButton: React.FC<React.ButtonHTMLAttributes<HTMLButtonElement>> = ({ children, className, ...props }) => (
    <button className={`inline-flex items-center justify-center gap-2 rounded-full px-5 py-2 text-sm font-semibold text-white bg-linear-to-br from-indigo-500/90 via-indigo-500 to-violet-500/90 border border-indigo-300/25 shadow-[0_8px_24px_rgba(99,102,241,0.22)] hover:-translate-y-0.5 hover:shadow-[0_12px_30px_rgba(99,102,241,0.28)] active:scale-95 transition-all ${className}`} {...props} >
        {children}
    </button>
);

export const GhostButton: React.FC<React.ButtonHTMLAttributes<HTMLButtonElement>> = ({ children, className, ...props }) => (
    <button className={`inline-flex items-center gap-2 rounded-full px-4 py-2 text-sm font-medium border border-white/20 bg-white/5 hover:bg-white/10 backdrop-blur-md active:scale-95 transition ${className}`} {...props} >
        {children}
    </button>
);