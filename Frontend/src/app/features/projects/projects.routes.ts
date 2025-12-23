import { Routes } from '@angular/router';
import { ProjectDetailComponent } from './project-detail/project-detail.component';
import { TaskBoardComponent } from '../tasks/task-board/task-board.component';

export default [
    {
        path: ':id',
        component: ProjectDetailComponent,
        children: [
            { path: '', redirectTo: 'board', pathMatch: 'full' },
            { path: 'board', component: TaskBoardComponent },
            {
                path: 'sprints',
                loadComponent: () => import('../sprints/sprint-list/sprint-list.component').then(m => m.SprintListComponent)
            }
        ]
    },
    {
        path: 'new',
        loadComponent: () => import('./project-form/project-form.component').then(m => m.ProjectFormComponent)
    }
] as Routes;
