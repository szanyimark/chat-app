import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { FriendRequestService } from '../../../core/services/friend-request.service';
import { NavItemComponent } from '../nav-item/nav-item.component';
import { ProfileSectionComponent } from '../profile-section/profile-section.component';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule, NavItemComponent, ProfileSectionComponent],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.scss'
})
export class SidebarComponent implements OnInit {
  private router = inject(Router);
  private authService = inject(AuthService);
  protected friendRequestService: FriendRequestService = inject(FriendRequestService);

  protected readonly isExpanded = signal(false);

  ngOnInit() {
    this.friendRequestService.initialize();
  }

  onToggleSidebar() {
    this.isExpanded.update(v => !v);
  }

  onMenuOpen() {
    this.isExpanded.set(true);
  }

  isChatsPage(): boolean {
    return this.router.url.startsWith('/chats');
  }

  isFriendsPage(): boolean {
    return this.router.url.startsWith('/friends');
  }

  logout() {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}