export default function SoftBackdrop() {
    return (
        <div className="fixed inset-0 -z-1 pointer-events-none">
            <div className="absolute left-1/2 top-20 -translate-x-1/2 w-[980px] h-[460px] bg-linear-to-tr from-violet-800/34 to-transparent rounded-full blur-3xl luma-backdrop-drift" />
            <div className="absolute right-12 bottom-10 w-[420px] h-[220px] bg-linear-to-bl from-fuchsia-700/30 to-transparent rounded-full blur-2xl luma-backdrop-drift" style={{ animationDelay: '4s' }} />
            <div className="absolute -left-24 top-1/3 w-[760px] h-[420px] bg-radial from-cyan-500/10 to-transparent rounded-full blur-3xl luma-backdrop-drift" style={{ animationDelay: '8s' }} />
            <div className="absolute right-1/4 -bottom-16 w-[860px] h-[500px] bg-radial from-indigo-500/8 to-transparent rounded-full blur-3xl luma-backdrop-drift" style={{ animationDelay: '12s' }} />
        </div>
    )
}