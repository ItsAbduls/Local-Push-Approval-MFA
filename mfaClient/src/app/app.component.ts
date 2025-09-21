import { Component, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { MfaService } from './mfa.service';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-root',
  imports: [FormsModule, ReactiveFormsModule, CommonModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit {
  email = 'abc@gmail.com';
  password = '123';
  challenges: any[] = [];
  approvedId: string | null = null;
  token: string | null = null;
  lastChallengeId: string | null = null;

  constructor(private mfa: MfaService) {}

  ngOnInit() {
    this.mfa.startConnection();

    this.mfa.challenges$.subscribe((c) => (this.challenges = c));
    this.mfa.approved$.subscribe((id) => (this.approvedId = id));
    this.mfa.token$.subscribe((t) => (this.token = t));
  }

  doLogin() {
    this.mfa.login(this.email, this.password).subscribe((res: any) => {
      console.log('Login response', res);
      this.lastChallengeId = res.challengeId;
    });
  }

  doExchange() {
    if (!this.lastChallengeId) {
      alert('No challenge available yet');
      return;
    }
    this.mfa.exchange(this.lastChallengeId).subscribe((res: any) => {
      console.log('Exchange response', res);
      this.token = res.token;
    });
  }
}
