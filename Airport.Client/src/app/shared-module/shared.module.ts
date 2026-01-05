import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PaginationComponent } from './components/pagination/pagination.component';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ReversePipe } from './pipes/reverse.pipe';

@NgModule({
  declarations: [
    PaginationComponent,
    ReversePipe
  ],
  imports: [
    FormsModule,
    RouterLink,
    CommonModule
  ],
  exports: [
    PaginationComponent,
    ReversePipe
  ]
})
export class SharedModule { }
