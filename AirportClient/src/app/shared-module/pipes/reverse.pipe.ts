import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'reverse'
})
export class ReversePipe implements PipeTransform {
  transform<T>(value: T[] | null | undefined): T[] {
    return value ? [...value].reverse() : [];
  }
}
