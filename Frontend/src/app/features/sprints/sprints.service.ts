import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface Sprint {
    id: string;
    name: string;
    startDate: string;
    endDate: string;
    projectId: string;
    active: boolean;
}

export interface CreateSprintDto {
    name: string;
    startDate: string;
    endDate: string;
}

@Injectable({
    providedIn: 'root'
})
export class SprintsService {
    private http = inject(HttpClient);
    private apiUrl = `${environment.apiUrl}/projects`;

    getAll(projectId: string): Observable<Sprint[]> {
        return this.http.get<Sprint[]>(`${this.apiUrl}/${projectId}/sprints`);
    }

    create(projectId: string, sprint: CreateSprintDto): Observable<Sprint> {
        return this.http.post<Sprint>(`${this.apiUrl}/${projectId}/sprints`, sprint);
    }

    delete(projectId: string, sprintId: string): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/${projectId}/sprints/${sprintId}`);
    }
}
