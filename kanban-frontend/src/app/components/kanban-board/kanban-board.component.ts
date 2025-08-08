import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TodoService } from '../../services/todo.service';
import { Todo, KanbanColumn, TodoStatus } from '../../models/todo.interface';
import { KanbanColumnComponent } from '../kanban-column/kanban-column.component';

@Component({
  selector: 'app-kanban-board',
  standalone: true,
  imports: [CommonModule, FormsModule, KanbanColumnComponent],
  templateUrl: './kanban-board.component.html',
  styleUrls: ['./kanban-board.component.css']
})
export class KanbanBoardComponent implements OnInit {
  columns: KanbanColumn[] = [
    { id: 'todo', title: 'To Do', todos: [] },
    { id: 'inprogress', title: 'In Progress', todos: [] },
    { id: 'done', title: 'Done', todos: [] }
  ];
  
  newTodoTitle: string = '';
  todos: Todo[] = [];

  constructor(private todoService: TodoService) {}

  ngOnInit(): void {
    this.todoService.todos$.subscribe(todos => {
      this.todos = todos;
      this.organizeColumns();
    });
    
    // Initial load
    this.todoService.getTodos().subscribe();
  }

  addTodo(): void {
    if (this.newTodoTitle.trim()) {
      this.todoService.addTodo(this.newTodoTitle).subscribe({
        next: () => {
          this.newTodoTitle = '';
        },
        error: (error) => {
          console.error('Failed to add todo:', error);
        }
      });
    }
  }

  onEditTodo(event: {id: string, title: string}): void {
    this.todoService.editTodo(event.id, event.title).subscribe({
      error: (error) => {
        console.error('Failed to edit todo:', error);
      }
    });
  }

  onDeleteTodo(id: string): void {
    this.todoService.deleteTodo(id).subscribe({
      error: (error) => {
        console.error('Failed to delete todo:', error);
      }
    });
  }

  onStatusChange(todoId: string, newStatus: TodoStatus): void {
    this.todoService.updateTodoStatus(todoId, newStatus).subscribe({
      error: (error) => {
        console.error('Failed to update todo status:', error);
      }
    });
  }

  onReorderWithinColumn(columnId: TodoStatus, fromIndex: number, toIndex: number): void {
    const columnTodos = this.columns.find(col => col.id === columnId)?.todos || [];
    if (fromIndex !== toIndex && columnTodos.length > 0) {
      // Create array of task IDs in new order
      const reorderedTodos = [...columnTodos];
      const [movedTodo] = reorderedTodos.splice(fromIndex, 1);
      reorderedTodos.splice(toIndex, 0, movedTodo);
      
      const taskIds = reorderedTodos.map(todo => todo.id);
      
      this.todoService.reorderTasks(columnId, taskIds).subscribe({
        error: (error) => {
          console.error('Failed to reorder todos:', error);
        }
      });
    }
  }

  private organizeColumns(): void {
    this.columns.forEach(column => {
      column.todos = this.todos.filter(todo => todo.status === column.id);
    });
  }

  get totalTodos(): number {
    return this.todos.length;
  }

  get completedTodos(): number {
    return this.todos.filter(todo => todo.status === 'done').length;
  }

  get inProgressTodos(): number {
    return this.todos.filter(todo => todo.status === 'inprogress').length;
  }

  trackByColumn(index: number, column: KanbanColumn): TodoStatus {
    return column.id;
  }
}