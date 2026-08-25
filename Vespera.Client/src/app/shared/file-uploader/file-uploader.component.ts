import { Component, ElementRef, ViewChild, output, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

/** Drag/drop + click-to-browse, emits the picked File[]. No upload endpoint exists yet (no
 * IFileStorage HTTP surface to call — same "plumbing only" treatment as Phase 4's mobile
 * affordances), so this stops at "here are the files the user picked." */
@Component({
  selector: 'vespera-file-uploader',
  standalone: true,
  imports: [MatButtonModule, MatIconModule],
  templateUrl: './file-uploader.component.html',
})
export class FileUploaderComponent {
  @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>;

  readonly filesSelected = output<File[]>();
  readonly dragOver = signal(false);

  browse(): void {
    this.fileInput.nativeElement.click();
  }

  onFileInputChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.emitFiles(input.files);
    input.value = '';
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.dragOver.set(false);
    this.emitFiles(event.dataTransfer?.files ?? null);
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.dragOver.set(true);
  }

  onDragLeave(): void {
    this.dragOver.set(false);
  }

  private emitFiles(fileList: FileList | null): void {
    if (fileList && fileList.length > 0) {
      this.filesSelected.emit(Array.from(fileList));
    }
  }
}
