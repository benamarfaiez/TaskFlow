import { User } from './auth.models';

export type TaskStatus = 'TODO' | 'IN_PROGRESS' | 'DONE';
export type TaskPriority = 'LOW' | 'MEDIUM' | 'HIGH';
export type TaskType = 'TASK' | 'BUG' | 'STORY';

export interface Task {
    id: string;
    projectId: string;
    title: string;
    description: string;
    status: TaskStatus;
    priority: TaskPriority;
    type: TaskType;
    assigneeId?: string;
    assignee?: User;
    sprintId?: string;
    createdAt: Date;
    updatedAt: Date;
    position: number; // For manual ordering
}

export interface CreateTaskDto {
    title: string;
    description?: string;
    priority?: TaskPriority;
    type?: TaskType;
    assigneeId?: string;
    sprintId?: string;
}

export interface UpdateTaskDto {
    title?: string;
    description?: string;
    status?: TaskStatus;
    priority?: TaskPriority;
    type?: TaskType;
    assigneeId?: string;
    sprintId?: string;
    position?: number;
}

export interface TaskMovedEvent {
    taskId: string;
    newStatus: TaskStatus;
    newPosition: number;
}

export interface Comment {
    id: string;
    taskId: string;
    userId: string;
    user?: User;
    content: string;
    createdAt: Date;
}

export interface TaskBoard {
    columns: {
        status: TaskStatus;
        tasks: Task[];
    }[];
}
