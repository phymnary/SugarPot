using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Phymnary.SugarPot.AspNetCore.Api;
using Phymnary.SugarPot.AspNetCore.MultiTenancy;
using Phymnary.SugarPot.AspNetCore.Security;

namespace Phymnary.SugarPot.AspNetCore.Extensions;

/// <summary>
/// UseDeveloperExceptionPage is added first when the HostingEnvironment is "Development".<br/>
/// UseRouting is added second if user code didn't already call UseRouting and if there are endpoints configured, for example app.MapGet.<br/>
/// UseEndpoints is added at the end of the middleware pipeline if any endpoints are configured.<br/>
/// UseAuthentication is added immediately after UseRouting if user code didn't already call UseAuthentication and if IAuthenticationSchemeProvider can be detected in the service provider. IAuthenticationSchemeProvider is added by default when using AddAuthentication, and services are detected using IServiceProviderIsService.<br/>
/// UseAuthorization is added next if user code didn't already call UseAuthorization and if IAuthorizationHandlerProvider can be detected in the service provider. IAuthorizationHandlerProvider is added by default when using AddAuthorization, and services are detected using IServiceProviderIsService.<br/>
/// User configured middleware and endpoints are added between UseRouting and UseEndpoints.
/// </summary>
public static class WebApplicationBuilderExtensions
{
    public static WebApplication UseBoilerplateServices(this WebApplication app)
    {
        app.Use(
            (context, next) =>
            {
                if (
                    context.RequestServices.GetService<IAbortedToken>()
                    is HttpContextAbortedProvider provider
                )
                    provider.Set(context.RequestAborted);

                if (
                    context.User.FindFirstValue(AspClaimNames.UserId) is { } subId
                    && Guid.TryParse(subId, out var currentUserId)
                    && context.RequestServices.GetService<ICurrentUser>()
                        is HttpContextCurrentUser currentUser
                )
                    currentUser.Id = currentUserId;

                if (
                    context.User.FindFirstValue(AspClaimNames.TenantId) is { } tenantId
                    && Guid.TryParse(tenantId, out var currentTenantId)
                    && context.RequestServices.GetService<ICurrentTenant>()
                        is HttpContextCurrentTenant currentTenant
                )
                    currentTenant.Id = currentTenantId;

                return next(context);
            }
        );

        return app;
    }
}
