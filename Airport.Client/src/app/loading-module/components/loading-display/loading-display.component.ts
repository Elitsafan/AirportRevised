import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'loading-display',
  templateUrl: './loading-display.component.html',
  styleUrls: ['./loading-display.component.scss']
})
export class LoadingDisplayComponent {
  @Input() loading: boolean = false;
  @Input() hasData: boolean = false;
  @Input() errorMessage: string | null = null;
  @Output() retry = new EventEmitter<void>();

  onRetry() {
    this.retry.emit();
  }
}
