using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace BlazorBrain.Client;

public class CustomAuthorizationMessageHandler : AuthorizationMessageHandler
{
    public CustomAuthorizationMessageHandler(IAccessTokenProvider provider, NavigationManager navigationManager) : base(provider, navigationManager)
    {
        ConfigureHandler(authorizedUrls: new[] { "http://localhost:5150/graphql" });
    }


}