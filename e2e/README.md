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

The Playwright config starts KevTest.Api (port 5132) and MyMVC.NetApp (port 5090)
itself before the run and stops them afterwards, so no apps need to be running
first. KevTest.Api uses its own `kevtest.e2e.db` SQLite file, separate from the
`kevtest.db` used by local development, so running the suite won't touch your
local data.

Note: MyMVC.NetApp runs on port 5090 here instead of 5000 — on macOS, port 5000
is claimed by the AirPlay Receiver (ControlCenter), which silently returns 403
to anything that hits it.
