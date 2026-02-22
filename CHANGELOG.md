# Changelog

All notable changes to this project will be documented in this file.

## 2026-02-22

### Added
- New `How It Works` page at `/Home/HowItWorks` with Luma-themed content and calls to action.
- Global top navigation link for `How It Works` in the shared Razor layout.
- Landing frontend navigation link for `How It Works` in the React navbar.

### Changed
- Added active-link highlighting for `Home`, `How It Works`, and `Pricing` in the Razor nav.
- Added active-link highlighting for landing sections (`Home`, `Features`, `FAQ`) and page links in the React nav.
- Upgraded landing nav section tracking from scroll listener to `IntersectionObserver` and tuned transition thresholds for smoother active-state behavior.
- Expanded the `How It Works` page with richer visual sections for onboarding, preference setup, pickup preparation, and post-pickup process flow.
- Updated `How It Works` copy and step structure to explicitly cover sign-up, customizing laundry preferences, and leaving bags out for pickup.

### Notes
- Styling remains consistent with the existing Luma visual system and theme.
