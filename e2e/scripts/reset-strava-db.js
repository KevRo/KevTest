// Deletes the e2e Strava SQLite database (and its WAL/SHM sidecar files) before
// MyMVC.NetApp starts, so each test run begins from a clean, freshly-migrated
// database regardless of what a previous run left behind. Never touches the
// developer's real strava.db (different filename, selected via ConnectionStrings__Strava).
const { rmSync } = require('fs');
const { join } = require('path');

const dbDir = join(__dirname, '..', '..', 'MyMVC.NetApp');

for (const suffix of ['', '-shm', '-wal']) {
  rmSync(join(dbDir, `strava.e2e.db${suffix}`), { force: true });
}
