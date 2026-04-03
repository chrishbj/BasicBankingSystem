# Banking.PlatformOps

React + TypeScript + Vite frontend for the platform operations console.

## Purpose

This app is the platform-facing control-plane shell for:

- service health by environment
- workflow monitoring
- correlation diagnostics
- platform maintenance actions
- platform audit visibility

It is intentionally separate from the business operations console.

## Local Development

```powershell
npm install
npm run dev
```

The Vite dev server proxies platform API calls to the local Gateway:

- `http://localhost:5000`

## Build

```powershell
npm run build
```
