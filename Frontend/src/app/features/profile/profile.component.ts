import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';

@Component({
    selector: 'app-profile',
    standalone: true,
    imports: [
        CommonModule,
        ReactiveFormsModule,
        MatCardModule,
        MatFormFieldModule,
        MatInputModule,
        MatButtonModule
    ],
    templateUrl: './profile.component.html',
    styleUrl: './profile.component.scss'
})
export class ProfileComponent {
    private fb = inject(FormBuilder);
    private authService = inject(AuthService);
    private toastService = inject(ToastService);

    userSignal = this.authService.currentUser;
    isLoading = false;

    profileForm = this.fb.group({
        currentPassword: ['', [Validators.required]],
        newPassword: ['', [Validators.required, Validators.minLength(6)]]
    });

    onSubmit() {
        if (this.profileForm.valid) {
            this.isLoading = true;
            const { currentPassword, newPassword } = this.profileForm.value;

            this.authService.changePassword({
                oldPassword: currentPassword!,
                newPassword: newPassword!
            }).subscribe({
                next: () => {
                    this.toastService.success('Password updated successfully');
                    this.profileForm.reset();
                    this.isLoading = false;
                },
                error: (err) => {
                    console.error(err);
                    this.isLoading = false;
                }
            });
        }
    }
}
