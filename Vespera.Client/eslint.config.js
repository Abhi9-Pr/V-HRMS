// @ts-check
const eslint = require("@eslint/js");
const { defineConfig } = require("eslint/config");
const tseslint = require("typescript-eslint");
const angular = require("angular-eslint");
const eslintConfigPrettier = require("eslint-config-prettier");

module.exports = defineConfig([
  {
    files: ["**/*.ts"],
    extends: [
      eslint.configs.recommended,
      tseslint.configs.recommended,
      tseslint.configs.stylistic,
      angular.configs.tsRecommended,
      eslintConfigPrettier,
    ],
    processor: angular.processInlineTemplates,
    rules: {
      // Angular 22 flipped the default change-detection strategy to OnPush and its `ng update`
      // migration explicitly opted every existing component out via `changeDetection:
      // ChangeDetectionStrategy.Eager` to preserve pre-v22 behavior (see the Angular 19->22
      // upgrade commits). angular-eslint's tsRecommended config now flags that explicit opt-out.
      // Actually adopting OnPush across ~80 components is a real behavioral migration requiring
      // per-component verification, not something to bundle into a dependency-upgrade pass -
      // tracked as separate follow-up work, so this rule is off until that happens.
      "@angular-eslint/prefer-on-push-component-change-detection": "off",
      "@angular-eslint/directive-selector": [
        "error",
        {
          type: "attribute",
          prefix: "vespera",
          style: "camelCase",
        },
      ],
      "@angular-eslint/component-selector": [
        "error",
        {
          type: "element",
          prefix: "vespera",
          style: "kebab-case",
        },
      ],
    },
  },
  {
    files: ["**/*.html"],
    extends: [
      angular.configs.templateRecommended,
      angular.configs.templateAccessibility,
    ],
    rules: {},
  }
]);
