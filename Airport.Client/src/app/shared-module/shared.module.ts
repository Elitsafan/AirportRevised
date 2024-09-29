import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PaginationComponent } from './components/pagination/pagination.component';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

@NgModule({
  declarations: [
    PaginationComponent,
  ],
  imports: [
    FormsModule,
    RouterLink,
    CommonModule
  ],
  exports: [PaginationComponent]
})
export class SharedModule { }
