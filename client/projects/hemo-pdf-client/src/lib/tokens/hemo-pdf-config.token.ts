import { InjectionToken } from '@angular/core';
import { HemoPdfConfig } from '../models/generate-pdf-request.model';

export const HEMO_PDF_CONFIG = new InjectionToken<HemoPdfConfig>('HEMO_PDF_CONFIG');
