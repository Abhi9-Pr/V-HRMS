/** Default validator-key → message map. A field with special wording passes its own `override`
 * input to vespera-form-error rather than this file growing per-field special cases. */
export const VALIDATION_MESSAGES: Record<string, string> = {
  required: 'This field is required.',
  email: 'Enter a valid email address.',
  minlength: 'This value is too short.',
  maxlength: 'This value is too long.',
  pattern: 'This value is not in the expected format.',
};
