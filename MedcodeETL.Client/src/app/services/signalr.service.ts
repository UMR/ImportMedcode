import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { environment } from '../../environments/environment';

export interface ProgressUpdate {
    requestId: string;
    stage: string;
    message: string;
    data?: any;
    timestamp: string;
}

@Injectable({
    providedIn: 'root'
})
export class SignalrService {
    private hubConnection: signalR.HubConnection | null = null;
    public progressUpdates = new Subject<ProgressUpdate>();
    private isConnected = false;

    constructor() { }

    connect(): void {
        if (this.isConnected) {
            return;
        }

        this.hubConnection = new signalR.HubConnectionBuilder()
            .withUrl(environment.signalrHubUrl)
            .withAutomaticReconnect()
            .build();

        this.hubConnection.on('progress', (update: ProgressUpdate) => {
            this.progressUpdates.next(update);
        });

        try {
            this.hubConnection.start();
            this.isConnected = true;
            console.log('SignalR connected');
        } catch (err) {
            console.error('Error connecting to SignalR:', err);
            throw err;
        }
    }

    joinRequest(requestId: string) {
        if (!this.hubConnection) {
            throw new Error('SignalR not connected');
        }
        return this.hubConnection.invoke('JoinRequest', requestId);
    }

    disconnect() {
        if (this.hubConnection) {
            this.hubConnection.stop();
            this.isConnected = false;
            console.log('SignalR disconnected');
        }
    }
}
