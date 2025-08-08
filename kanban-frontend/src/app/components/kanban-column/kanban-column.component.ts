import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { KanbanColumn, Todo, TodoStatus } from '../../models/todo.interface';
import { TodoItemComponent } from '../todo-item/todo-item.component';

@Component({
  selector: 'app-kanban-column',
  standalone: true,
  imports: [CommonModule, TodoItemComponent],
  templateUrl: './kanban-column.component.html',
  styleUrls: ['./kanban-column.component.css']
})
export class KanbanColumnComponent {
  @Input() column!: KanbanColumn;
  @Output() editTodo = new EventEmitter<{id: string, title: string}>();
  @Output() deleteTodo = new EventEmitter<string>();
  @Output() statusChange = new EventEmitter<{todoId: string, newStatus: TodoStatus}>();
  @Output() reorderInColumn = new EventEmitter<{fromIndex: number, toIndex: number}>();

  draggedItemId: string | null = null;
  dragOverIndex: number | null = null;

  onEditTodo(event: {id: string, title: string}): void {
    this.editTodo.emit(event);
  }

  onDeleteTodo(id: string): void {
    this.deleteTodo.emit(id);
  }

  onToggleTodo(id: string): void {
    const todo = this.column.todos.find(t => t.id === id);
    if (todo) {
      let newStatus: TodoStatus;
      switch (todo.status) {
        case 'todo':
          newStatus = 'inprogress';
          break;
        case 'inprogress':
          newStatus = 'done';
          break;
        case 'done':
          newStatus = 'todo';
          break;
      }
      this.statusChange.emit({todoId: id, newStatus});
    }
  }

  onDragStart(id: string): void {
    this.draggedItemId = id;
  }

  onDragEnd(): void {
    this.draggedItemId = null;
    this.dragOverIndex = null;
  }

  onDragOver(event: DragEvent, index: number): void {
    event.preventDefault();
    this.dragOverIndex = index;
  }

  onDrop(event: DragEvent, targetIndex: number): void {
    event.preventDefault();
    
    if (this.draggedItemId === null) return;
    
    const draggedIndex = this.column.todos.findIndex(todo => todo.id === this.draggedItemId);
    
    if (draggedIndex !== -1 && draggedIndex !== targetIndex) {
      this.reorderInColumn.emit({fromIndex: draggedIndex, toIndex: targetIndex});
    }
    
    this.draggedItemId = null;
    this.dragOverIndex = null;
  }

  onColumnDragOver(event: DragEvent): void {
    event.preventDefault();
  }

  onColumnDrop(event: DragEvent): void {
    event.preventDefault();
    
    const todoId = event.dataTransfer?.getData('text/plain');
    if (todoId) {
      this.statusChange.emit({todoId: todoId, newStatus: this.column.id});
    }
  }

  getDragPlaceholderStyle(index: number): any {
    if (this.dragOverIndex === index && this.draggedItemId !== null) {
      return {
        'border-top': '3px solid var(--primary-color)',
        'margin-top': '8px'
      };
    }
    return {};
  }

  trackByTodo(index: number, todo: Todo): string {
    return todo.id;
  }

  get columnIcon(): string {
    switch (this.column.id) {
      case 'todo':
        return '📋';
      case 'inprogress':
        return '⚡';
      case 'done':
        return '✅';
      default:
        return '📝';
    }
  }

  get toggleButtonText(): string {
    switch (this.column.id) {
      case 'todo':
        return 'Start';
      case 'inprogress':
        return 'Complete';
      case 'done':
        return 'Reset';
      default:
        return 'Toggle';
    }
  }
}