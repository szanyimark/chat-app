import { Component, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-profile-section',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './profile-section.component.html',
  styleUrl: './profile-section.component.scss'
})
export class ProfileSectionComponent {
  private authService = inject(AuthService);
  private router = inject(Router);
  
  @Input() isExpanded: boolean = false;
  showMenu = false;

  get currentUser() {
    return this.authService.currentUser();
  }

  getInitials(name: string): string {
    return name
      .split(' ')
      .map(word => word[0])
      .join('')
      .toUpperCase()
      .slice(0, 2);
  }

  toggleMenu(event: Event) {
    event.stopPropagation();
    this.showMenu = !this.showMenu;
  }

  closeMenu() {
    this.showMenu = false;
  }

  logout() {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}