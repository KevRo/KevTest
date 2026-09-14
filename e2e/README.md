# End-to-end tests

Playwright tests that drive KevTest.Api and MyMVC.NetApp together in a real browser.

## Setup (once)

```sh
cd e2e
npm install
npx playwright install --with-deps chromium
```

## Running

```sh
cd e2e
npx playwright test
```

The Playwright config always starts fresh instances of KevTest.Api (port 5132)
and MyMVC.NetApp (port 5090) — even if you already have your own dev instances
running on those ports — and stops them afterwards. KevTest.Api uses its own
`kevtest.e2e.db` SQLite file, separate from the `kevtest.db` used by local
development, and that file is wiped before every run, so running the suite
never touches your local data or leaves stale rows behind for the next run.

Note: MyMVC.NetApp runs on port 5090 here instead of 5000 — on macOS, port 5000
is claimed by the AirPlay Receiver (ControlCenter), which silently returns 403
to anything that hits it.
