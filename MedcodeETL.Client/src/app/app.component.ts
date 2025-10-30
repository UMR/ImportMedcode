import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { provideHttpClient } from '@angular/common/http';
import { EtlService } from './services/etl.service';
import { SignalrService, ProgressUpdate } from './services/signalr.service';
import { Subscription } from 'rxjs';

interface Step {
  id: string;
  label: string;
  status: 'pending' | 'active' | 'completed' | 'error';
  message: string;
  data?: any;
}

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit, OnDestroy {
  title = 'Medcode ETL Process';

  codeType = '';
  codeVersion = '';
  newFile: File | null = null;
  oldFile: File | null = null;
  batchSize = 1000;

  isProcessing = false;
  requestId = '';

  steps: Step[] = [
    { id: 'init', label: 'Initialize', status: 'pending', message: '' },
    { id: 'new', label: 'Process New Codes', status: 'pending', message: '' },
    { id: 'old', label: 'Process Old Codes', status: 'pending', message: '' },
    { id: 'complete', label: 'Complete', status: 'pending', message: '' }
  ];

  private progressSubscription: Subscription | null = null;

  constructor(
    private etlService: EtlService,
    private signalrService: SignalrService
  ) { }

  async ngOnInit() {
    await this.signalrService.connect();

    this.progressSubscription = this.signalrService.progressUpdates.subscribe(
      (update: ProgressUpdate) => this.handleProgressUpdate(update)
    );
  }

  ngOnDestroy() {
    if (this.progressSubscription) {
      this.progressSubscription.unsubscribe();
    }
    this.signalrService.disconnect();
  }

  onNewFileSelected(event: any) {
    const file = event.target.files[0];
    if (file) {
      this.newFile = file;
    }
  }

  onOldFileSelected(event: any) {
    const file = event.target.files[0];
    if (file) {
      this.oldFile = file;
    }
  }

  startETL() {
    if (!this.codeType || !this.codeVersion || !this.newFile) {
      alert('Please fill in all required fields');
      return;
    }

    this.isProcessing = true;
    this.resetSteps();

    try {
      const response = this.etlService.executeETLBackground({
        codeType: this.codeType,
        codeVersion: this.codeVersion,
        medcodeNewFile: this.newFile,
        medcodeOldFile: this.oldFile || undefined,
        batchSize: this.batchSize
      }).subscribe({
        next: (response) => {
          if (response && response.requestId) {
            this.requestId = response.requestId;
            this.updateStepStatus('init', 'active', 'ETL process started');
            this.signalrService.joinRequest(response.requestId);
          }
        },
        error: (error) => {
          console.error('Error starting ETL:', error);
          this.updateStepStatus('init', 'error', 'Failed to start ETL process');
          this.isProcessing = false;
        }
      });


    } catch (error) {
      console.error('Error starting ETL:', error);
      this.updateStepStatus('init', 'error', 'Failed to start ETL process');
      this.isProcessing = false;
    }
  }

  private handleProgressUpdate(update: ProgressUpdate) {
    console.log('Progress update:', update);

    if (update.stage.startsWith('phase:new:start')) {
      this.updateStepStatus('init', 'completed', 'Initialized');
      this.updateStepStatus('new', 'active', update.message);
    } else if (update.stage.startsWith('extract:') || update.stage.startsWith('transform:') || update.stage.startsWith('load:')) {
      const currentPhase = this.getCurrentPhase();
      if (currentPhase) {
        this.updateStepMessage(currentPhase, update.message, update.data);
      }
    } else if (update.stage.startsWith('phase:new:done')) {
      this.updateStepStatus('new', 'completed', update.message, update.data);
    } else if (update.stage.startsWith('phase:old:start')) {
      this.updateStepStatus('old', 'active', update.message);
    } else if (update.stage.startsWith('phase:old:done')) {
      this.updateStepStatus('old', 'completed', update.message, update.data);
    } else if (update.stage.startsWith('etl:done')) {
      this.updateStepStatus('complete', 'completed', 'ETL process completed successfully');
      this.isProcessing = false;
    }
  }

  private getCurrentPhase(): string | null {
    const activeStep = this.steps.find(s => s.status === 'active');
    return activeStep ? activeStep.id : null;
  }

  private updateStepStatus(stepId: string, status: Step['status'], message: string, data?: any) {
    const step = this.steps.find(s => s.id === stepId);
    if (step) {
      step.status = status;
      step.message = message;
      step.data = data;
    }
  }

  private updateStepMessage(stepId: string, message: string, data?: any) {
    const step = this.steps.find(s => s.id === stepId);
    if (step) {
      step.message = message;
      if (data) {
        step.data = data;
      }
    }
  }

  private resetSteps() {
    this.steps.forEach(step => {
      step.status = 'pending';
      step.message = '';
      step.data = null;
    });
  }

  reset() {
    this.isProcessing = false;
    this.requestId = '';
    this.codeType = '';
    this.codeVersion = '';
    this.newFile = null;
    this.oldFile = null;
    this.batchSize = 1000;
    this.resetSteps();
  }
}
