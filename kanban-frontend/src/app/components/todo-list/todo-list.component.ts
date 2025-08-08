import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TodoService } from '../../services/todo.service';
import { Todo } from '../../models/todo.interface';
import { TodoItemComponent } from '../todo-item/todo-item.component';
import { ThemeToggleComponent } from '../theme-toggle/theme-toggle.component';

@Component({
  selector: 'app-todo-list',
  standalone: true,
  imports: [CommonModule, FormsModule, TodoItemComponent, ThemeToggleComponent],
  templateUrl: './todo-list.component.html',
  styleUrls: ['./todo-list.component.css']
})
export class TodoListComponent implements OnInit {
  todos: Todo[] = [];
  newTodoTitle: string = '';
  draggedItemId: number | null = null;
  dragOverIndex: number | null = null;

  constructor(private todoService: TodoService) {}

  ngOnInit(): void {
    this.todoService.getTodos().subscribe(todos => {
      this.todos = todos;
    });
  }

  addTodo(): void {
    if (this.newTodoTitle.trim()) {
      this.todoService.addTodo(this.newTodoTitle);
      this.newTodoTitle = '';
    }
  }

  onToggleTodo(id: number): void {
    this.todoService.toggleTodo(id);
  }

  onDeleteTodo(id: number): void {
    this.todoService.deleteTodo(id);
  }

  onEditTodo(event: {id: number, title: string}): void {
    this.todoService.editTodo(event.id, event.title);
  }

  onDragStart(id: number): void {
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
    
    const draggedIndex = this.todos.findIndex(todo => todo.id === this.draggedItemId);
    
    if (draggedIndex !== -1 && draggedIndex !== targetIndex) {
      this.todoService.reorderTodos(draggedIndex, targetIndex);
    }
    
    this.draggedItemId = null;
    this.dragOverIndex = null;
  }

  getDragPlaceholderStyle(index: number): any {
    if (this.dragOverIndex === index && this.draggedItemId !== null) {
      return {
        'border-top': '3px solid #3498db',
        'margin-top': '8px'
      };
    }
    return {};
  }

  get completedCount(): number {
    return this.todos.filter(todo => todo.completed).length;
  }

  get totalCount(): number {
    return this.todos.length;
  }

  trackByFn(index: number, todo: Todo): number {
    return todo.id;
  }
}