import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { SprintsService, Sprint } from '../sprints.service';
import { map } from 'rxjs/operators';
import { Observable } from 'rxjs';

@Component({
    selector: 'app-sprint-list',
    standalone: true,
    imports: [
        CommonModule,
        MatListModule,
        MatIconModule,
        MatButtonModule
    ],
    templateUrl: './sprint-list.component.html'
})
export class SprintListComponent implements OnInit {
    private route = inject(ActivatedRoute);
    private sprintsService = inject(SprintsService);

    projectId = '';
    activeSprints$?: Observable<Sprint[]>;
    futureSprints$?: Observable<Sprint[]>;

    ngOnInit() {
        // ProjectDetail is parent route, so use parent.snapshot
        this.projectId = this.route.parent?.snapshot.paramMap.get('id') || '';

        if (this.projectId) {
            const allSprints$ = this.sprintsService.getAll(this.projectId);

            this.activeSprints$ = allSprints$.pipe(
                map(sprints => sprints.filter(s => s.active))
            );

            this.futureSprints$ = allSprints$.pipe(
                map(sprints => sprints.filter(s => !s.active))
            );
        }
    }
}
