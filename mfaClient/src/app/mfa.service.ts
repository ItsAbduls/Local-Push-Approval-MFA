import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class MfaService {
  private hubConnection!: signalR.HubConnection;
  private apiUrl = 'https://localhost:7244'; // your API base URL

  // Observables for UI
  public challenges$ = new BehaviorSubject<any[]>([]);
  public approved$ = new BehaviorSubject<string | null>(null);
  public token$ = new BehaviorSubject<string | null>(null);

  constructor(private http: HttpClient) {}

  // Start SignalR connection
  startConnection() {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${this.apiUrl}/hubs/auth`)
      .withAutomaticReconnect()
      .build();

    this.hubConnection.start().then(() => console.log('SignalR connected ✅'));

    this.hubConnection.on('ReceiveChallenge', (challenge) => {
      console.log('New challenge', challenge);
      this.challenges$.next([...this.challenges$.value, challenge]);
    });

    this.hubConnection.on('ChallengeApproved', (id) => {
      console.log('Challenge approved', id);
      this.approved$.next(id);
    });
  }

  login(email: string, password: string) {
    return this.http.post<any>(`${this.apiUrl}/api/login`, { email, password });
  }

  exchange(challengeId: string) {
    return this.http.post<any>(`${this.apiUrl}/api/exchange`, { challengeId });
  }
}
