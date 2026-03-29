# Banking.Web

React + TypeScript + Vite frontend for the Basic Banking System.

## Purpose

This app acts as a lightweight operations console for the current backend:

- customer creation and activation
- account opening and balance refresh
- deposit submission and status refresh
- pending-review queue inspection and recovery actions

## Local Development

```powershell
npm install
npm run dev
```

The Vite dev server proxies requests to the local backend services:

- `http://localhost:5101`
- `http://localhost:5102`
- `http://localhost:5103`
- `http://localhost:5104`

## Build

```powershell
npm run build
```
