import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface ETLRequest {
    codeType: string;
    codeVersion: string;
    minNumber?: number;
    batchSize?: number;
    medcodeNewFile: File;
    medcodeOldFile?: File;
}

export interface ETLResponse {
    requestId: string;
}

@Injectable({
    providedIn: 'root'
})
export class EtlService {
    private apiUrl = environment.apiUrl;

    constructor(private http: HttpClient) { }

    executeETLBackground(request: ETLRequest): Observable<ETLResponse> {
        const formData = new FormData();
        formData.append('CodeType', request.codeType);
        formData.append('CodeVersion', request.codeVersion);
        formData.append('MedcodeNewFile', request.medcodeNewFile);

        if (request.medcodeOldFile) {
            formData.append('MedcodeOldFile', request.medcodeOldFile);
        }

        if (request.minNumber) {
            formData.append('MinNumber', request.minNumber.toString());
        }

        if (request.batchSize) {
            formData.append('BatchSize', request.batchSize.toString());
        }

        return this.http.post<ETLResponse>(`${this.apiUrl}/ExecuteETLBackground`, formData);
    }
}
