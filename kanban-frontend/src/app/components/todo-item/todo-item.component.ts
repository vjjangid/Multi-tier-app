import { Component, Input, Output, EventEmitter, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Todo } from '../../models/todo.interface';

@Component({
  selector: 'app-todo-item',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './todo-item.component.html',
  styleUrls: ['./todo-item.component.css']
})
export class TodoItemComponent {
  @Input() todo!: Todo;
  @Input() toggleButtonText: string = 'Toggle';
  @Output() toggle = new EventEmitter<string>();
  @Output() delete = new EventEmitter<string>();
  @Output() edit = new EventEmitter<{id: string, title: string}>();
  @Output() dragStart = new EventEmitter<string>();
  @Output() dragEnd = new EventEmitter<void>();

  isEditing = false;
  editTitle = '';

  onToggle(): void {
    this.toggle.emit(this.todo.id);
  }

  onDelete(): void {
    this.delete.emit(this.todo.id);
  }

  onEdit(): void {
    this.isEditing = true;
    this.editTitle = this.todo.title;
  }

  onSaveEdit(): void {
    if (this.editTitle.trim()) {
      this.edit.emit({id: this.todo.id, title: this.editTitle.trim()});
    }
    this.isEditing = false;
  }

  onCancelEdit(): void {
    this.isEditing = false;
    this.editTitle = '';
  }

  onDragStart(event: DragEvent): void {
    if (event.dataTransfer) {
      event.dataTransfer.effectAllowed = 'move';
      event.dataTransfer.setData('text/plain', this.todo.id);
      // Create empty drag image to avoid default plus icon
      const emptyImg = new Image();
      emptyImg.src = 'data:image/gif;base64,R0lGODlhAQABAIAAAAUEBAAAACwAAAAAAQABAAACAkQBADs=';
      event.dataTransfer.setDragImage(emptyImg, 0, 0);
    }
    this.dragStart.emit(this.todo.id);
  }

  onDragEnd(): void {
    this.dragEnd.emit();
  }

  formatDate(date: Date): string {
    return new Intl.DateTimeFormat('en-US', {
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    }).format(date);
  }
}