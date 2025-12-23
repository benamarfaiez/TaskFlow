import { Component, ViewChild, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { MatSidenavModule, MatSidenav } from '@angular/material/sidenav';
import { NavbarComponent } from '../navbar/navbar.component';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';

@Component({
    selector: 'app-main-layout',
    standalone: true,
    imports: [
        CommonModule,
        RouterOutlet,
        MatSidenavModule,
        NavbarComponent,
        SidebarComponent
    ],
    templateUrl: './main-layout.component.html',
    styleUrl: './main-layout.component.scss'
})
export class MainLayoutComponent implements OnInit {
    @ViewChild('sidenav') sidenav!: MatSidenav;

    private breakpointObserver = inject(BreakpointObserver);
    isMobile = false;

    ngOnInit() {
        this.breakpointObserver.observe([Breakpoints.Handset])
            .subscribe(result => {
                this.isMobile = result.matches;
            });
    }

    onSidebarClose() {
        if (this.isMobile) {
            this.sidenav.close();
        }
    }
}
