import { TestBed } from '@angular/core/testing';
import { SignalrService } from './signalr.service';
import * as signalR from '@microsoft/signalr';

describe('SignalrService', () => {
  let service: SignalrService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [SignalrService]
    });
    service = TestBed.inject(SignalrService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should expose connectionError$ observable', () => {
    expect(service.connectionError$).toBeDefined();
  });

  describe('Connection Error Handling', () => {
    it('should emit error when SignalR connection closes', (done) => {
      // Mock HubConnection
      const mockConnection = {
        start: jasmine.createSpy('start').and.returnValue(Promise.resolve()),
        on: jasmine.createSpy('on'),
        onclose: jasmine.createSpy('onclose')
      } as unknown as signalR.HubConnection;

      spyOn(signalR.HubConnectionBuilder.prototype, 'build').and.returnValue(mockConnection);
      spyOn(signalR.HubConnectionBuilder.prototype, 'withUrl').and.returnValue(new signalR.HubConnectionBuilder());

      // Start connection
      service.startConnection();

      // Get the onclose callback
      const oncloseCallback = (mockConnection.onclose as jasmine.Spy).calls.argsFor(0)[0];

      // Subscribe to connection errors
      service.connectionError$.subscribe(error => {
        expect(error).toBeDefined();
        expect(error.message).toContain('Connection to server lost');
        done();
      });

      // Simulate connection close
      oncloseCallback(undefined);
    });

    it('should emit actual error when connection closes with error', (done) => {
      const testError = new Error('Network failure');

      const mockConnection = {
        start: jasmine.createSpy('start').and.returnValue(Promise.resolve()),
        on: jasmine.createSpy('on'),
        onclose: jasmine.createSpy('onclose')
      } as unknown as signalR.HubConnection;

      spyOn(signalR.HubConnectionBuilder.prototype, 'build').and.returnValue(mockConnection);
      spyOn(signalR.HubConnectionBuilder.prototype, 'withUrl').and.returnValue(new signalR.HubConnectionBuilder());

      service.startConnection();
      const oncloseCallback = (mockConnection.onclose as jasmine.Spy).calls.argsFor(0)[0];

      service.connectionError$.subscribe(error => {
        expect(error).toBe(testError);
        done();
      });

      oncloseCallback(testError);
    });
  });
});
