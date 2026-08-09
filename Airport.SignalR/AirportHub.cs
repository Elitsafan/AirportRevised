using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Airport.SignalR
{
    [Authorize]
    public class AirportHub : Hub
    {
    }
}
