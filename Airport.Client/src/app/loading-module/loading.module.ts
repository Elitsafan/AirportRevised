import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { EllipsisComponent } from './components/ellipsis/ellipsis.component';
import { SpinnerComponent } from './components/spinner/spinner.component';
import { LoadingDisplayComponent } from './components/loading-display/loading-display.component';

@NgModule({
  declarations: [
    SpinnerComponent,
    EllipsisComponent,
    LoadingDisplayComponent
  ],
  imports: [
    CommonModule
  ],
  exports: [
    EllipsisComponent,
    SpinnerComponent,
    LoadingDisplayComponent
  ]
})
export class LoadingModule { }
