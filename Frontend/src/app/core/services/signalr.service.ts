import { Injectable, inject } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject, BehaviorSubject } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';
import { Task, TaskMovedEvent, Comment } from '../models/task.model';

@Injectable({
    providedIn: 'root'
})
export class SignalRService {
    private hubConnection?: signalR.HubConnection;
    private authService = inject(AuthService);

    public connectionStatus$ = new BehaviorSubject<'Disconnected' | 'Connecting' | 'Connected'>('Disconnected');

    public taskCreated$ = new Subject<Task>();
    public taskUpdated$ = new Subject<Task>();
    public taskMoved$ = new Subject<TaskMovedEvent>();
    public taskDeleted$ = new Subject<string>();
    public commentAdded$ = new Subject<Comment>();

    constructor() {
        // Reconnect on auth change (optional, but good practice)
    }

    public async startConnection(): Promise<void> {
        if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
            return;
        }

        this.connectionStatus$.next('Connecting');

        const token = this.authService.getToken();

        this.hubConnection = new signalR.HubConnectionBuilder()
            .withUrl(environment.signalRUrl, {
                accessTokenFactory: () => token || ''
            })
            .withAutomaticReconnect()
            .build();

        this.hubConnection.on('TaskCreated', (task: Task) => this.taskCreated$.next(task));
        this.hubConnection.on('TaskUpdated', (task: Task) => this.taskUpdated$.next(task));
        this.hubConnection.on('TaskMoved', (event: TaskMovedEvent) => this.taskMoved$.next(event));
        this.hubConnection.on('TaskDeleted', (taskId: string) => this.taskDeleted$.next(taskId));
        this.hubConnection.on('CommentAdded', (comment: Comment) => this.commentAdded$.next(comment));

        try {
            await this.hubConnection.start();
            this.connectionStatus$.next('Connected');
            console.log('SignalR Connected');
        } catch (err) {
            this.connectionStatus$.next('Disconnected');
            console.error('Error while starting connection: ' + err);
        }
    }

    public async stopConnection(): Promise<void> {
        if (this.hubConnection) {
            await this.hubConnection.stop();
            this.connectionStatus$.next('Disconnected');
        }
    }

    public async joinProjectGroup(projectId: string): Promise<void> {
        if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
            await this.hubConnection.invoke('JoinProject', projectId);
        }
    }

    public async leaveProjectGroup(projectId: string): Promise<void> {
        if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
            await this.hubConnection.invoke('LeaveProject', projectId);
        }
    }
}
