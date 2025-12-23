import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Project, CreateProjectDto, UpdateProjectDto, ProjectMember, AddMemberDto } from '../../core/models/project.model';

@Injectable({
    providedIn: 'root'
})
export class ProjectsService {
    private http = inject(HttpClient);
    private apiUrl = `${environment.apiUrl}/projects`;

    getAll(): Observable<Project[]> {
        return this.http.get<Project[]>(this.apiUrl);
    }

    getById(id: string): Observable<Project> {
        return this.http.get<Project>(`${this.apiUrl}/${id}`);
    }

    create(project: CreateProjectDto): Observable<Project> {
        return this.http.post<Project>(this.apiUrl, project);
    }

    update(id: string, data: UpdateProjectDto): Observable<Project> {
        return this.http.put<Project>(`${this.apiUrl}/${id}`, data);
    }

    delete(id: string): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/${id}`);
    }

    getMembers(projectId: string): Observable<ProjectMember[]> {
        return this.http.get<ProjectMember[]>(`${this.apiUrl}/${projectId}/members`);
    }

    addMember(projectId: string, data: AddMemberDto): Observable<ProjectMember> {
        return this.http.post<ProjectMember>(`${this.apiUrl}/${projectId}/members`, data);
    }

    removeMember(projectId: string, userId: string): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/${projectId}/members/${userId}`);
    }
}
