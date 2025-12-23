import { Injectable, inject } from '@angular/core';
import { MatSnackBar, MatSnackBarConfig } from '@angular/material/snack-bar';

@Injectable({
    providedIn: 'root'
})
export class ToastService {
    private snackBar = inject(MatSnackBar);

    show(message: string, action: string = 'Close', config?: MatSnackBarConfig) {
        this.snackBar.open(message, action, {
            duration: 3000,
            horizontalPosition: 'end',
            verticalPosition: 'top',
            ...config
        });
    }

    success(message: string) {
        this.show(message, 'OK', {
            panelClass: ['success-snackbar']
        });
    }

    error(message: string) {
        this.show(message, 'Close', {
            panelClass: ['error-snackbar'],
            duration: 5000
        });
    }

    info(message: string) {
        this.show(message, 'Info', {
            panelClass: ['info-snackbar']
        });
    }
}
