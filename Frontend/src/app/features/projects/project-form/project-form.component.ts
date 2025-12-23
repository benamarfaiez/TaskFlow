import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { ProjectsService } from '../projects.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
    selector: 'app-project-form',
    standalone: true,
    imports: [
        CommonModule,
        ReactiveFormsModule,
        RouterLink,
        MatCardModule,
        MatFormFieldModule,
        MatInputModule,
        MatButtonModule
    ],
    templateUrl: './project-form.component.html',
    styleUrl: './project-form.component.scss'
})
export class ProjectFormComponent {
    private fb = inject(FormBuilder);
    private projectsService = inject(ProjectsService);
    private router = inject(Router);
    private toastService = inject(ToastService);

    isLoading = false;

    projectForm = this.fb.group({
        name: ['', [Validators.required]],
        description: ['']
    });

    onSubmit() {
        if (this.projectForm.valid) {
            this.isLoading = true;
            const { name, description } = this.projectForm.value;

            this.projectsService.create({ name: name!, description: description! })
                .subscribe({
                    next: () => {
                        this.toastService.success('Project created successfully');
                        this.router.navigate(['/dashboard']);
                    },
                    error: (err) => {
                        console.error(err);
                        this.isLoading = false;
                    }
                });
        }
    }
}
