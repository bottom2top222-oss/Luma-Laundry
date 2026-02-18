import { UploadIcon, VideoIcon, ZapIcon } from 'lucide-react';

export const featuresData = [
    {
        icon: <UploadIcon className="w-6 h-6" />,
        title: 'Free Pickup & Delivery',
        desc: 'Schedule a pickup at your convenience. We\'ll collect your laundry and return it fresh and folded.'
    },
    {
        icon: <ZapIcon className="w-6 h-6" />,
        title: 'Same-Day Service',
        desc: 'Need it fast? Our same-day service ensures your clothes are cleaned and returned within hours.'
    },
    {
        icon: <VideoIcon className="w-6 h-6" />,
        title: 'Eco-Friendly Cleaning',
        desc: 'We use environmentally safe detergents and energy-efficient processes for a cleaner planet.'
    }
];

export const plansData = [
    {
        id: 'starter',
        name: 'Basic',
        price: '$1.50',
        desc: 'Perfect for individuals.',
        credits: 'per lb',
        features: [
            'Wash, dry & fold service',
            'Standard detergent',
            'Next-day turnaround',
            'Free pickup & delivery',
            'Email notifications'
        ]
    },
    {
        id: 'pro',
        name: 'Premium',
        price: '$2.00',
        desc: 'Great for families.',
        credits: 'per lb',
        features: [
            'Everything in Basic',
            'Premium eco-friendly detergent',
            'Same-day service available',
            'Gentle cycle for delicates',
            'Priority scheduling'
        ],
        popular: true
    },
    {
        id: 'ultra',
        name: 'Business',
        price: 'Custom',
        desc: 'For commercial needs.',
        credits: 'pricing',
        features: [
            'Everything in Premium',
            'Dedicated account manager',
            'Bulk discounts',
            'Commercial-grade cleaning',
            'Flexible scheduling'
        ]
    }
];

export const faqData = [
    {
        question: 'What laundry services do you provide?',
        answer: 'We offer wash & fold, dry cleaning, delicate care, and commercial laundry services with free pickup and delivery.'
    },
    {
        question: 'How does pickup and delivery work?',
        answer: 'Simply schedule a pickup through our website or app. We\'ll collect your laundry at your door and return it cleaned and folded within 24-48 hours (same-day available).'
    },
    {
        question: 'What are your turnaround times?',
        answer: 'Standard service is 24-48 hours. We also offer same-day service for orders placed before 10 AM. Rush services are available upon request.'
    },
    {
        question: 'Are your cleaning products safe?',
        answer: 'Yes! We use eco-friendly, hypoallergenic detergents that are safe for sensitive skin and the environment. Premium options are available.'
    }
];

export const footerLinks = [
    {
        title: "Company",
        links: [
            { name: "Home", url: "#" },
            { name: "Services", url: "#" },
            { name: "Work", url: "#" },
            { name: "Contact", url: "#" }
        ]
    },
    {
        title: "Legal",
        links: [
            { name: "Privacy Policy", url: "#" },
            { name: "Terms of Service", url: "#" }
        ]
    },
    {
        title: "Connect",
        links: [
            { name: "Twitter", url: "#" },
            { name: "LinkedIn", url: "#" },
            { name: "GitHub", url: "#" }
        ]
    }
];