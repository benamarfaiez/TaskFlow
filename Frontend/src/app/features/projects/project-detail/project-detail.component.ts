import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatTabsModule } from '@angular/material/tabs';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ProjectsService } from '../projects.service';
import { switchMap } from 'rxjs/operators';

@Component({
    selector: 'app-project-detail',
    standalone: true,
    imports: [
        CommonModule,
        RouterLink,
        RouterLinkActive,
        RouterOutlet,
        MatTabsModule,
        MatProgressSpinnerModule
    ],
    templateUrl: './project-detail.component.html',
    styleUrl: './project-detail.component.scss'
})
export class ProjectDetailComponent {
    private route = inject(ActivatedRoute);
    private projectsService = inject(ProjectsService);

    project$ = this.route.paramMap.pipe(
        switchMap(params => this.projectsService.getById(params.get('id')!))
    );

    navLinks = [
        { path: 'board', label: 'Kanban Board' },
        { path: 'tasks', label: 'List' },
        { path: 'sprints', label: 'Sprints' },
        { path: 'members', label: 'Members' },
        { path: 'settings', label: 'Settings' }
    ];
}
