import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { EllipsisComponent } from './components/ellipsis/ellipsis.component';
import { SpinnerComponent } from './components/spinner/spinner.component';

@NgModule({
  declarations: [
    SpinnerComponent,
    EllipsisComponent
  ],
  imports: [
    CommonModule
  ],
  exports: [
    EllipsisComponent,
    SpinnerComponent
  ]
})
export class LoadingModule { }
