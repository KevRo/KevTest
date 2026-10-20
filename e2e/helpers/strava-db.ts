// Seeds/clears the isolated e2e Strava SQLite database (strava.e2e.db) directly via the
// system sqlite3 CLI - there's no Node SQLite client in this project's devDependencies, and
// no HTTP endpoint exists to create a fake Strava athlete, so writing straight to the file
// is the simplest option. This only runs from inside test files (in beforeAll/afterAll),
// never from the reset script, because the schema doesn't exist until MyMVC.NetApp's own
// `stravaDb.Database.Migrate()` has run at startup.
import { execSync } from 'child_process';
import { join } from 'path';

const DB_PATH = join(__dirname, '..', '..', 'MyMVC.NetApp', 'strava.e2e.db');

function run(sql: string) {
  execSync(`sqlite3 "${DB_PATH}"`, { input: sql });
}

function esc(value: string) {
  return value.replace(/'/g, "''");
}

export function clearStravaData() {
  run('DELETE FROM StravaActivities; DELETE FROM StravaAthletes; DELETE FROM StravaTokens;');
}

export interface SeedAthlete {
  id: number;
  firstname: string;
  lastname: string;
  city?: string;
  country?: string;
  allTimeDistanceMeters: number;
  fetchedAtUtc: string; // 'YYYY-MM-DD HH:MM:SS'
}

export function seedAthlete(a: SeedAthlete) {
  // Premium/Summit are non-nullable bool columns with no DB-level default, so they must be
  // listed explicitly (as 0) even though the app itself never seeds them directly. Built as
  // column->value pairs so the column list and VALUES tuple can never drift out of sync.
  const row = {
    Id: String(a.id),
    Firstname: `'${esc(a.firstname)}'`,
    Lastname: `'${esc(a.lastname)}'`,
    City: a.city ? `'${esc(a.city)}'` : 'NULL',
    Country: a.country ? `'${esc(a.country)}'` : 'NULL',
    Premium: '0',
    Summit: '0',
    AllTimeDistanceMeters: String(a.allTimeDistanceMeters),
    ProfileRawJson: "''",
    FetchedAtUtc: `'${a.fetchedAtUtc}'`,
  };

  run(`INSERT INTO StravaAthletes (${Object.keys(row).join(', ')}) VALUES (${Object.values(row).join(', ')});`);
}

export interface SeedActivity {
  id: number;
  athleteId: number;
  name: string;
  type: string;
  distance: number; // meters
  movingTime: number; // seconds
  startDateUtc: string; // 'YYYY-MM-DD HH:MM:SS' - plain TEXT, not DateTimeOffset-ISO (see StravaActivity.cs)
  startDateLocal: string; // ISO8601 with offset is fine here
}

export function seedActivities(activities: SeedActivity[]) {
  if (activities.length === 0) {
    return;
  }

  // Built as column->value pairs (rather than two hand-aligned parallel lists) specifically
  // so the column list and each row's VALUES tuple can never drift out of sync - every column
  // below is NOT NULL with no DB-level default, so all of them are required on every insert
  // even though these tests only care about a handful of the values.
  const rows = activities.map((a) => ({
    Id: String(a.id),
    AthleteId: String(a.athleteId),
    Name: `'${esc(a.name)}'`,
    Type: `'${esc(a.type)}'`,
    Distance: String(a.distance),
    MovingTime: String(a.movingTime),
    ElapsedTime: '0',
    TotalElevationGain: '0',
    StartDateUtc: `'${a.startDateUtc}'`,
    StartDateLocal: `'${a.startDateLocal}'`,
    KudosCount: '0',
    AchievementCount: '0',
    Trainer: '0',
    Commute: '0',
    Manual: '0',
    Private: '0',
    FetchedAtUtc: `'${a.startDateUtc}'`,
    RawJson: "''",
  }));

  const columns = Object.keys(rows[0]).join(', ');
  const values = rows.map((row) => `(${Object.values(row).join(', ')})`).join(',\n');

  run(`INSERT INTO StravaActivities (${columns}) VALUES ${values};`);
}
