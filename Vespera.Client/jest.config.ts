import type { Config } from 'jest';

const config: Config = {
  preset: 'jest-preset-angular',
  setupFilesAfterEnv: ['<rootDir>/setup-jest.ts'],
  testPathIgnorePatterns: ['<rootDir>/node_modules/', '<rootDir>/e2e/'],
  // jest-preset-angular's own default (node_modules/(?!(.*\.mjs$|@angular/common/locales/.*\.js$)))
  // plus `marked`, which ships ESM-only — overriding this key replaces the preset's array
  // entirely, so it has to be repeated here, not just extended.
  transformIgnorePatterns: ['node_modules/(?!(.*\\.mjs$|@angular/common/locales/.*\\.js$|marked))'],
  collectCoverageFrom: ['src/app/**/*.ts', '!src/app/**/*.spec.ts', '!src/app/core/api/generated/**'],
};

export default config;
