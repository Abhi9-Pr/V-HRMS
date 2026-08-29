import type { Config } from 'jest';

const config: Config = {
  preset: 'jest-preset-angular',
  setupFilesAfterEnv: ['<rootDir>/setup-jest.ts'],
  testPathIgnorePatterns: ['<rootDir>/node_modules/'],
  // dist/ carries this app's own build output plus, transitively, whatever ng build copied in;
  // excluding it avoids jest-haste-map module-name collisions the same way Vespera.Client's
  // jest.config.ts does for vespera-shared's dist output.
  modulePathIgnorePatterns: ['<rootDir>/dist/'],
  // vespera-shared has no build step in the test loop — map straight to its TS source so
  // ts-jest compiles it like any other app source instead of requiring `ng build vespera-shared`
  // in Vespera.Client before every test run here.
  moduleNameMapper: {
    '^vespera-shared$': '<rootDir>/../Vespera.Client/projects/vespera-shared/src/public-api.ts',
  },
  transformIgnorePatterns: ['node_modules/(?!(.*\\.mjs$|@angular/common/locales/.*\\.js$))'],
  collectCoverageFrom: ['src/app/**/*.ts', '!src/app/**/*.spec.ts'],
};

export default config;
