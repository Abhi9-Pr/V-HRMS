import type { Config } from 'jest';

const config: Config = {
  preset: 'jest-preset-angular',
  setupFilesAfterEnv: ['<rootDir>/setup-jest.ts'],
  // `[\\/]` rather than a literal `/`: `<rootDir>` substitutes to a backslash path on Windows, so
  // a forward-slash-only pattern silently never matches there and these folders leak into the run.
  testPathIgnorePatterns: ['[\\\\/]node_modules[\\\\/]', '[\\\\/]e2e[\\\\/]'],
  // dist/ carries ng-packagr's own build output for vespera-shared, complete with a
  // package.json name that collides with projects/vespera-shared/package.json — jest-haste-map
  // scans every directory under <rootDir> by default and throws on the duplicate unless dist/
  // is excluded here.
  modulePathIgnorePatterns: ['[\\\\/]dist[\\\\/]'],
  // vespera-shared has no build step in the test loop — map straight to its TS source so
  // ts-jest compiles it like any other app source instead of requiring `ng build vespera-shared`
  // before every test run.
  moduleNameMapper: {
    '^vespera-shared$': '<rootDir>/projects/vespera-shared/src/public-api.ts',
  },
  // jest-preset-angular's own default (node_modules/(?!(.*\.mjs$|@angular/common/locales/.*\.js$)))
  // plus `marked`, which ships ESM-only — overriding this key replaces the preset's array
  // entirely, so it has to be repeated here, not just extended.
  transformIgnorePatterns: ['node_modules/(?!(.*\\.mjs$|@angular/common/locales/.*\\.js$|marked))'],
  collectCoverageFrom: ['src/app/**/*.ts', '!src/app/**/*.spec.ts'],
};

export default config;
