import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { CdkDragDrop, moveItemInArray, transferArrayItem, DragDropModule } from '@angular/cdk/drag-drop';
import { TasksService } from '../tasks.service';
import { SignalRService } from '../../../core/services/signalr.service';
import { Task, TaskStatus } from '../../../core/models/task.model';
import { BehaviorSubject, Subscription } from 'rxjs';

@Component({
    selector: 'app-task-board',
    standalone: true,
    imports: [
        CommonModule,
        DragDropModule
    ],
    templateUrl: './task-board.component.html',
    styleUrl: './task-board.component.scss'
})
export class TaskBoardComponent implements OnInit, OnDestroy {
    private route = inject(ActivatedRoute);
    private tasksService = inject(TasksService);
    private signalRService = inject(SignalRService);

    private projectId = '';
    private subscriptions: Subscription[] = [];

    // Mock initial columns if API is slow or empty
    board$ = new BehaviorSubject<{ columns: { status: TaskStatus, tasks: Task[] }[] } | null>(null);

    ngOnInit() {
        this.projectId = this.route.parent?.snapshot.paramMap.get('id') || '';

        if (this.projectId) {
            this.loadBoard();
            this.setupSignalR();
        }
    }

    loadBoard() {
        this.tasksService.getBoard(this.projectId).subscribe(board => {
            this.board$.next(board);
        });
    }

    setupSignalR() {
        this.signalRService.joinProjectGroup(this.projectId);

        this.subscriptions.push(
            this.signalRService.taskMoved$.subscribe(() => {
                // Handle realtime move update
                // For simplicity, reload board or manually move item in local state
                this.loadBoard();
            }),
            this.signalRService.taskUpdated$.subscribe(() => this.loadBoard()),
            this.signalRService.taskCreated$.subscribe(() => this.loadBoard())
        );
    }

    ngOnDestroy() {
        this.subscriptions.forEach(s => s.unsubscribe());
        if (this.projectId) {
            this.signalRService.leaveProjectGroup(this.projectId);
        }
    }

    drop(event: CdkDragDrop<Task[]>, newStatus: TaskStatus) {
        if (event.previousContainer === event.container) {
            moveItemInArray(event.container.data, event.previousIndex, event.currentIndex);
            // Optional: Update position in backend
        } else {
            transferArrayItem(
                event.previousContainer.data,
                event.container.data,
                event.previousIndex,
                event.currentIndex,
            );

            const task = event.container.data[event.currentIndex];
            this.tasksService.update(this.projectId, task.id, {
                status: newStatus
            }).subscribe();
        }
    }
}
