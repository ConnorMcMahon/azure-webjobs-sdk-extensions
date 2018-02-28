using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Protocols;

namespace Microsoft.Azure.WebJobs.Extensions.Http
{
    internal class AuthenticatedUserBindingProvider : IBindingProvider
    {
        public Task<IBinding> TryCreateAsync(BindingProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            if (context.Parameter.ParameterType != typeof(AuthenticatedUser))
            {
                return Task.FromResult<IBinding>(null);
            }

            return Task.FromResult<IBinding>(new AuthenticatedUserBinding(context.Parameter));
        }

        private class AuthenticatedUserBinding : IBinding
        {
            private readonly ParameterInfo _parameter;

            public AuthenticatedUserBinding(ParameterInfo parameter)
            {
                _parameter = parameter;
            }

            public bool FromAttribute => false;

            public Task<IValueProvider> BindAsync(object value, ValueBindingContext context)
            {
                if (context == null)
                {
                    throw new ArgumentNullException("context");
                }

                return BindAsync();
            }

            public Task<IValueProvider> BindAsync(BindingContext context)
            {
                if (context == null)
                {
                    throw new ArgumentNullException("context");
                }

                return BindAsync();
            }

            private Task<IValueProvider> BindAsync()
            {
                return Task.FromResult<IValueProvider>(new AuthenticatedUserValueProvider());
            }

            public ParameterDescriptor ToParameterDescriptor()
            {
                return new ParameterDescriptor
                {
                    Name = _parameter.Name,
                    DisplayHints = new ParameterDisplayHints
                    {
                        Description = "AuthenticatedUser"
                    }
                };
            }

            private class AuthenticatedUserValueProvider : IValueProvider
            {
                private static HttpClient client = new HttpClient();

                public AuthenticatedUserValueProvider()
                {
                }

                public Type Type => typeof(AuthenticatedUser);

                public async Task<object> GetValueAsync()
                {
                    string hostname = Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME");
                    var authUri = "https://" + hostname + "/.auth/me";
                    HttpResponseMessage response = await client.GetAsync(authUri);

                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        throw new InvalidOperationException("There is no authenticated user.");
                    } else if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        throw new InvalidOperationException("Authentication/Authorization is not enabled.");
                    } else if (response.IsSuccessStatusCode)
                    {
                        string authenticatedUserJson = await response.Content.ReadAsStringAsync();
                        return AuthenticatedUser.DeserializeJson(authenticatedUserJson);
                    } else
                    {
                        throw new InvalidOperationException($"Do not know how to handle response status code {response.StatusCode} from {authUri}");
                    }
                }

                public string ToInvokeString()
                {
                    //TODO: what goes here?
                    return string.Empty;
                }
            }
        }
    }
}
