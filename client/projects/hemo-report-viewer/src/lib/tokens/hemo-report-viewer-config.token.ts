import { InjectionToken } from '@angular/core';
import { HemoPdfConfig } from '../models/preview-request.model';

export const HEMO_REPORT_VIEWER_CONFIG = new InjectionToken<HemoPdfConfig>(
  'HEMO_REPORT_VIEWER_CONFIG'
);
