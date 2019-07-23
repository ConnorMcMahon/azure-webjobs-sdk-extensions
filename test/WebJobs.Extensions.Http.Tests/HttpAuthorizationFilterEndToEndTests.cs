using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.Tests.Common;
using Microsoft.Azure.WebJobs.Extensions.Tests.Extensions.Http;
using Microsoft.Azure.WebJobs.Host.TestCommon;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Tests.Extensions.Http
{
    public class HttpAuthorizationFilterEndToEndTests
    {
        private const string EasyAuthEnabledAppSetting = "WEBSITE_AUTH_ENABLED";
        private const string UserNameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
        private const string UserRoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
        private const string UserAuthType = "aad";
        private const string UserNameClaimValue = "Connor McMahon";
        private const string UserRoleClaimValue = "Software Engineer";

        private JobHost GetJobHost(INameResolver resolver = null)
        {
            if (resolver == null)
            {
                var mockNameResolver = new Mock<INameResolver>();
                mockNameResolver.Setup(nameResolver => nameResolver.Resolve(EasyAuthEnabledAppSetting)).Returns("TRUE");
                resolver = mockNameResolver.Object;
            }

            var host = new HostBuilder()
                .ConfigureServices(configuration =>
                {
                    configuration.AddSingleton<INameResolver>(resolver);
                })
                .ConfigureDefaultTestHost(builder =>
                {
                    builder.AddHttp(o =>
                    {
                        o.SetAuthResponse = SetResultHook;
                    })
                    .AddAzureStorageCoreServices()
                    .AddTimers()
                    .AddAzureStorage();
                }, typeof(TestFunctions))
                .Build();

            return host.GetJobHost();
        }

        [Fact]
        public async Task HttpAuthorizationFilter_FailsAuth_Fails()
        {
            var request = HttpTestHelpers.CreateHttpRequest("GET", "http://functions.com/api/TestIdentityBindings");
            request.HttpContext.User = GetSamplePrincipal();

            var method = typeof(TestFunctions).GetMethod(nameof(TestFunctions.FailsAuth));
            var jobHost = GetJobHost();
            await jobHost.StartAsync();
            await jobHost.CallAsync(method, new { principal = request });

            bool authResult = GetResult(request);

            Assert.False(authResult);
        }

        [Fact]
        public async Task HttpAuthorizationFilter_PassesAuth_Passes()
        {
            var request = HttpTestHelpers.CreateHttpRequest("GET", "http://functions.com/api/TestIdentityBindings");
            request.HttpContext.User = GetSamplePrincipal();

            var method = typeof(TestFunctions).GetMethod(nameof(TestFunctions.PassesAuth));
            await GetJobHost().CallAsync(method, new { principal = request });

            bool authResult = GetResult(request);

            Assert.True(authResult);
        }

        private void SetResultHook(HttpRequest request, bool result)
        {
            request.HttpContext.Items["$auth_result"] = result;
        }

        private bool GetResult(HttpRequest request)
        {
            return (bool)request.HttpContext.Items["$auth_result"];
        }

        private ClaimsPrincipal GetSamplePrincipal()
        {
            ClaimsIdentity identity = new ClaimsIdentity(authenticationType: UserAuthType, nameType: UserNameClaimType, roleType: UserRoleClaimType);
            identity.AddClaim(new Claim(UserNameClaimType, UserNameClaimValue));
            identity.AddClaim(new Claim(UserRoleClaimType, UserRoleClaimValue));
            return new ClaimsPrincipal(identity);
        }

        public static class TestFunctions
        {
            internal static int InvocationCount { get; set; } = 0;

            public static bool FailsAuth(
                [HttpAuthorizationTrigger] ClaimsPrincipal principal)
            {
                return false;
            }

            public static bool PassesAuth(
                [HttpAuthorizationTrigger] ClaimsPrincipal principal)
            {
                return true;
            }
        }
    }
}
