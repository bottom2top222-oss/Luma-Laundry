# Mobile Rollout Guide (Responsive + PWA + Store Wrapper)

This project now includes:

- Responsive layout improvements for mobile spacing/nav/footer
- PWA manifest (`wwwroot/manifest.webmanifest`)
- Service worker (`wwwroot/service-worker.js`)
- PWA registration in both MVC layout and SPA entry HTML

## 1) Verify Responsive Foundation

Run the app and check common breakpoints in browser dev tools:

- 390x844 (iPhone 14)
- 430x932 (iPhone 15 Pro Max)
- 768x1024 (iPad)
- 1280x800 (desktop)

Focus checks:

- Nav collapse and dropdown usability
- Footer columns stacking cleanly
- No horizontal scrolling

## 2) Verify PWA Installability

In Chrome/Edge:

1. Open app over HTTPS.
2. Open DevTools -> Application -> Manifest.
3. Confirm manifest is detected.
4. Confirm Service Worker is activated.
5. Use Install prompt or browser "Install app".

In iOS Safari:

1. Open app over HTTPS.
2. Share -> Add to Home Screen.

## 3) Optional App Store Wrapper (Capacitor)

Capacitor wraps your web app in a native shell for App Store / Play Store submission.

### Prerequisites

- Node.js LTS
- Xcode (for iOS)
- Android Studio (for Android)

### Recommended flow

1. Host your app at a stable HTTPS URL (production).
2. In a separate wrapper folder, initialize Capacitor:

```bash
npm init -y
npm install @capacitor/core @capacitor/cli
npx cap init LUMA com.luma.laundry --web-dir=www
```

3. Point wrapper web view to hosted app URL in capacitor config (`server.url`).
4. Add native platforms:

```bash
npm install @capacitor/ios @capacitor/android
npx cap add ios
npx cap add android
```

5. Open native projects:

```bash
npx cap open ios
npx cap open android
```

6. Configure icons/splash screens, permissions, deep links, and privacy metadata.
7. Build/sign in Xcode and Android Studio, then submit.

## Notes

- Keep responsive and PWA as source-of-truth.
- Treat wrapper as deployment shell, not where UI is built.
- Re-run mobile QA after each significant web release.
