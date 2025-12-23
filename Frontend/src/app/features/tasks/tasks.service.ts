import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Task, CreateTaskDto, UpdateTaskDto, TaskBoard } from '../../core/models/task.model';

@Injectable({
    providedIn: 'root'
})
export class TasksService {
    private http = inject(HttpClient);
    private apiUrl = `${environment.apiUrl}/projects`;

    // Tasks are scoped to projects usually: /api/projects/:projectId/tasks
    // Or global /api/tasks?projectId=...
    // Based on user prompt: create(projectId, ...), getBoard(projectId)

    create(projectId: string, task: CreateTaskDto): Observable<Task> {
        return this.http.post<Task>(`${this.apiUrl}/${projectId}/tasks`, task);
    }

    getAll(projectId: string): Observable<Task[]> {
        return this.http.get<Task[]>(`${this.apiUrl}/${projectId}/tasks`);
    }

    getById(projectId: string, taskId: string): Observable<Task> {
        return this.http.get<Task>(`${this.apiUrl}/${projectId}/tasks/${taskId}`);
    }

    update(projectId: string, taskId: string, data: UpdateTaskDto): Observable<Task> {
        return this.http.put<Task>(`${this.apiUrl}/${projectId}/tasks/${taskId}`, data);
    }

    delete(projectId: string, taskId: string): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/${projectId}/tasks/${taskId}`);
    }

    getBoard(projectId: string): Observable<TaskBoard> {
        return this.http.get<TaskBoard>(`${this.apiUrl}/${projectId}/tasks/board`);
    }
}
