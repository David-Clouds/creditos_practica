using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace CreditosPlataforma.Web.Hubs
{
    [Authorize]
    public class SolicitudesHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var usuarioId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(usuarioId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, usuarioId);
            }

            await base.OnConnectedAsync();
        }
    }
}