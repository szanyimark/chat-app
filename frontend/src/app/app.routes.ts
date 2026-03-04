import { Routes } from '@angular/router';
import { LoginComponent } from './features/auth/login/login.component';
import { RegisterComponent } from './features/auth/register/register.component';
import { LayoutComponent } from './shared/components/layout/layout.component';
import { ChatsComponent } from './features/chat/chats/chats.component';
import { FriendsComponent } from './features/friends/friends/friends.component';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  {
    path: '',
    component: LayoutComponent,
    children: [
      { path: 'chats', component: ChatsComponent },
      { path: 'friends', component: FriendsComponent },
      { path: '', redirectTo: 'chats', pathMatch: 'full' }
    ]
  },
  { path: '', redirectTo: '/login', pathMatch: 'full' }
];
