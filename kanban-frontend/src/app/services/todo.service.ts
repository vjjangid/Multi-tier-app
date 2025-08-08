import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { BehaviorSubject, Observable, throwError } from 'rxjs';
import { catchError, map, tap } from 'rxjs/operators';
import { Todo, TodoStatus } from '../models/todo.interface';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class TodoService {
  private todos: Todo[] = [];
  private todosSubject = new BehaviorSubject<Todo[]>(this.todos);
  public todos$ = this.todosSubject.asObservable();
  private readonly apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {
    this.loadTodos();
  }

  getTodos(): Observable<Todo[]> {
    return this.http.get<any>(`${this.apiUrl}/kanban`).pipe(
      map(response => {
        if (response.success && response.data) {
          const todos = response.data.map((task: any) => this.mapTaskToTodo(task));
          this.todos = todos;
          this.todosSubject.next([...this.todos]);
          return todos;
        }
        return [];
      }),
      catchError(this.handleError.bind(this))
    );
  }

  addTodo(title: string, description: string = ''): Observable<Todo> {
    const createData = {
      title: title.trim(),
      description: description.trim(),
      status: 0 // TodoStatus.Todo
    };

    return this.http.post<any>(`${this.apiUrl}/kanban`, createData).pipe(
      map(response => {
        if (response.success && response.data) {
          const todo = this.mapTaskToTodo(response.data);
          this.todos.push(todo);
          this.todosSubject.next([...this.todos]);
          return todo;
        }
        throw new Error('Failed to create task');
      }),
      catchError(this.handleError.bind(this))
    );
  }

  updateTodoStatus(id: string, status: TodoStatus): Observable<Todo> {
    const updateData = {
      status: this.mapStatusToNumber(status)
    };

    return this.http.put<any>(`${this.apiUrl}/kanban/${id}`, updateData).pipe(
      map(response => {
        if (response.success && response.data) {
          const updatedTodo = this.mapTaskToTodo(response.data);
          const index = this.todos.findIndex(t => t.id === id);
          if (index !== -1) {
            this.todos[index] = updatedTodo;
            this.todosSubject.next([...this.todos]);
          }
          return updatedTodo;
        }
        throw new Error('Failed to update task');
      }),
      catchError(this.handleError.bind(this))
    );
  }

  deleteTodo(id: string): Observable<boolean> {
    return this.http.delete<any>(`${this.apiUrl}/kanban/${id}`).pipe(
      map(response => {
        if (response.success) {
          this.todos = this.todos.filter(t => t.id !== id);
          this.todosSubject.next([...this.todos]);
          return true;
        }
        return false;
      }),
      catchError(this.handleError.bind(this))
    );
  }

  editTodo(id: string, newTitle: string, newDescription: string = ''): Observable<Todo> {
    const updateData = {
      title: newTitle.trim(),
      description: newDescription.trim()
    };

    return this.http.put<any>(`${this.apiUrl}/kanban/${id}`, updateData).pipe(
      map(response => {
        if (response.success && response.data) {
          const updatedTodo = this.mapTaskToTodo(response.data);
          const index = this.todos.findIndex(t => t.id === id);
          if (index !== -1) {
            this.todos[index] = updatedTodo;
            this.todosSubject.next([...this.todos]);
          }
          return updatedTodo;
        }
        throw new Error('Failed to update task');
      }),
      catchError(this.handleError.bind(this))
    );
  }

  moveTask(taskId: string, newStatus: TodoStatus, newOrder: number): Observable<Todo> {
    const moveData = {
      newStatus: this.mapStatusToNumber(newStatus),
      newOrder: newOrder
    };

    return this.http.post<any>(`${this.apiUrl}/kanban/${taskId}/move`, moveData).pipe(
      map(response => {
        if (response.success && response.data) {
          const updatedTodo = this.mapTaskToTodo(response.data);
          const index = this.todos.findIndex(t => t.id === taskId);
          if (index !== -1) {
            this.todos[index] = updatedTodo;
            this.todosSubject.next([...this.todos]);
          }
          return updatedTodo;
        }
        throw new Error('Failed to move task');
      }),
      catchError(this.handleError.bind(this))
    );
  }

  reorderTasks(status: TodoStatus, taskIds: string[]): Observable<Todo[]> {
    const reorderData = {
      status: this.mapStatusToNumber(status),
      taskIds: taskIds
    };

    return this.http.post<any>(`${this.apiUrl}/kanban/reorder`, reorderData).pipe(
      map(response => {
        if (response.success && response.data) {
          const reorderedTodos = response.data.map((task: any) => this.mapTaskToTodo(task));
          
          // Update local todos with reordered ones
          reorderedTodos.forEach(updatedTodo => {
            const index = this.todos.findIndex(t => t.id === updatedTodo.id);
            if (index !== -1) {
              this.todos[index] = updatedTodo;
            }
          });
          
          this.todosSubject.next([...this.todos]);
          return reorderedTodos;
        }
        throw new Error('Failed to reorder tasks');
      }),
      catchError(this.handleError.bind(this))
    );
  }

  private loadTodos(): void {
    this.getTodos().subscribe({
      error: (error) => {
        console.error('Failed to load todos:', error);
        // Fallback to empty array
        this.todos = [];
        this.todosSubject.next([]);
      }
    });
  }

  private mapTaskToTodo(task: any): Todo {
    return {
      id: task.id,
      title: task.title,
      description: task.description || '',
      status: this.mapNumberToStatus(task.status),
      createdAt: new Date(task.createdAt),
      updatedAt: task.updatedAt ? new Date(task.updatedAt) : undefined,
      order: task.order || 0
    };
  }

  private mapStatusToNumber(status: TodoStatus): number {
    const statusMap = {
      'todo': 0,
      'inprogress': 1,
      'done': 2
    };
    return statusMap[status] || 0;
  }

  private mapNumberToStatus(statusNumber: number): TodoStatus {
    const statusMap = {
      0: 'todo' as TodoStatus,
      1: 'inprogress' as TodoStatus,
      2: 'done' as TodoStatus
    };
    return statusMap[statusNumber] || 'todo';
  }

  private handleError(error: HttpErrorResponse): Observable<never> {
    let errorMessage = 'An unknown error occurred';
    
    if (error.error) {
      if (typeof error.error === 'string') {
        errorMessage = error.error;
      } else if (error.error.message) {
        errorMessage = error.error.message;
      } else if (error.error.errors && error.error.errors.length > 0) {
        errorMessage = error.error.errors[0];
      }
    } else if (error.message) {
      errorMessage = error.message;
    }
    
    console.error('TodoService error:', errorMessage);
    return throwError(() => ({ message: errorMessage }));
  }
}