export type TodoStatus = 'todo' | 'inprogress' | 'done';

export interface Todo {
  id: string;
  title: string;
  description?: string;
  status: TodoStatus;
  createdAt: Date;
  updatedAt?: Date;
  order?: number;
}

export interface KanbanColumn {
  id: TodoStatus;
  title: string;
  todos: Todo[];
}