import { Pipe, PipeTransform } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { inject } from '@angular/core';
import DOMPurify from 'dompurify';
import { marked } from 'marked';

/** `[innerHTML]="body | vesperaMarkdown"` — renders trusted-author markdown (announcements) as
 * sanitized HTML. DOMPurify runs on marked's *output*, not the raw markdown source, so this is
 * safe even though announcement bodies are free text written by an HR admin, not the current
 * user: `marked` and `DOMPurify` are two independent passes, and only the sanitized string
 * ever reaches Angular's [innerHTML] binding (bypassSecurityTrustHtml is only ever called on
 * already-sanitized output — never move that call before the DOMPurify.sanitize step). */
@Pipe({ name: 'vesperaMarkdown', standalone: true, pure: true })
export class MarkdownPipe implements PipeTransform {
  private readonly sanitizer = inject(DomSanitizer);

  transform(value: string | null | undefined): SafeHtml {
    if (!value) {
      return '';
    }

    const rawHtml = marked.parse(value, { async: false, breaks: true, gfm: true });
    const cleanHtml = DOMPurify.sanitize(rawHtml, { ALLOWED_ATTR: ['href', 'target', 'rel'] });
    return this.sanitizer.bypassSecurityTrustHtml(cleanHtml);
  }
}
