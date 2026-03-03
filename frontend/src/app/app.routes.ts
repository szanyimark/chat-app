import { Routes } from '@angular/router';
import { LoginComponent } from './features/auth/login/login.component';
import { RegisterComponent } from './features/auth/register/register.component';
import { ChatListComponent } from './features/chat/chat-list/chat-list.component';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'chats', component: ChatListComponent },
  { path: '', redirectTo: '/login', pathMatch: 'full' }
];
