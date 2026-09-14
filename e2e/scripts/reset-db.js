// Deletes the e2e SQLite database (and its WAL/SHM sidecar files) before
// KevTest.Api starts, so each test run begins from a clean, empty database
// regardless of what a previous run left behind.
const { rmSync } = require('fs');
const { join } = require('path');

const dbDir = join(__dirname, '..', '..', 'src', 'KevTest.Api');

for (const suffix of ['', '-shm', '-wal']) {
  rmSync(join(dbDir, `kevtest.e2e.db${suffix}`), { force: true });
}
