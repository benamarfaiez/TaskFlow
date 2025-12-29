import { User } from './auth.models';

export interface Project {
    id: string;
    name: string;
    description: string;
    ownerId: string;
    owner?: User;
    members: ProjectMember[];
    createdAt: Date;
    updatedAt: Date;
}

export interface ProjectMember {
    userId: string;
    projectId: string;
    role: 'OWNER' | 'ADMIN' | 'MEMBER';
    user?: User;
}

export interface CreateProjectDto {
    name: string;
    description: string;
}

export interface UpdateProjectDto {
    name?: string;
    description?: string;
}

export interface AddMemberDto {
    email: string;
    role: 'ADMIN' | 'MEMBER';
}
