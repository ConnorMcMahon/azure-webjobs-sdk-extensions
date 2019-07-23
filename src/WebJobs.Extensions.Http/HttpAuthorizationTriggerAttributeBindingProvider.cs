// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Triggers;
using static Microsoft.Azure.WebJobs.Extensions.Http.HttpTriggerAttributeBindingProvider.HttpTriggerBinding;

namespace Microsoft.Azure.WebJobs.Extensions.Http
{
    internal class HttpAuthorizationTriggerAttributeBindingProvider : ITriggerBindingProvider
    {
        private readonly Action<HttpRequest, bool> _responseHook;

        public HttpAuthorizationTriggerAttributeBindingProvider(Action<HttpRequest, bool> responseHook)
        {
            this._responseHook = responseHook;
        }

        public Task<ITriggerBinding> TryCreateAsync(TriggerBindingProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            ParameterInfo parameter = context.Parameter;
            HttpAuthorizationTriggerAttribute attribute = parameter.GetCustomAttribute<HttpAuthorizationTriggerAttribute>(inherit: false);
            if (attribute == null)
            {
                return Task.FromResult<ITriggerBinding>(null);
            }

            bool isSupportedTypeBinding = parameter.ParameterType == typeof(string) || parameter.ParameterType == typeof(ClaimsPrincipal);
            if (!isSupportedTypeBinding)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                    "Can't bind HttpAuthorizationFilter to type '{0}'.", parameter.ParameterType));
            }

            return Task.FromResult<ITriggerBinding>(new HttpAuthorziationFilterBinding(_responseHook, context.Parameter));
        }        
    }

    internal class HttpAuthorziationFilterBinding : ITriggerBinding
    {
        private readonly Action<HttpRequest, bool> _responseHook;
        private readonly ParameterInfo _parameterInfo;
        private readonly Dictionary<string, Type> _dataContract = new Dictionary<string, Type>();

        public HttpAuthorziationFilterBinding(Action<HttpRequest, bool> responseHook, ParameterInfo parameter)
        {
            _parameterInfo = parameter;
            _responseHook = responseHook;
            _dataContract["$return"] = typeof(bool).MakeByRefType();
        }

        public Type TriggerValueType
        {
            get { return typeof(HttpRequestMessage); }
        }

        public IReadOnlyDictionary<string, Type> BindingDataContract => _dataContract;

        public Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
        {
            HttpRequest request = value as HttpRequest;
            if (request == null)
            {
                throw new NotSupportedException("An HttpRequest is required");
            }

            ClaimsPrincipal principal = request.HttpContext.User;

            var valueProvider = new SimpleValueProvider(typeof(ClaimsPrincipal), principal, principal?.Identity?.Name);
            string invokeString = ToInvokeString(request);

            IValueBinder returnProvider = new AuthHandler(request, _responseHook);
            ITriggerData trigger = new TriggerData(valueProvider, new Dictionary<string, object>()) { ReturnValueProvider = returnProvider };
            return Task.FromResult(trigger);
        }

        public Task<IListener> CreateListenerAsync(ListenerFactoryContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            return Task.FromResult<IListener>(new NullListener());
        }

        public ParameterDescriptor ToParameterDescriptor()
        {
            return new TriggerParameterDescriptor
            {
                Name = _parameterInfo.Name
            };
        }

        private class AuthHandler : IValueBinder
        {
            private readonly HttpRequest _request;
            private readonly Action<HttpRequest, bool> _responseHook;

            public AuthHandler(HttpRequest request, Action<HttpRequest, bool> responseHook)
            {
                _request = request;
                _responseHook = responseHook;
            }

            public Type Type => typeof(object).MakeByRefType();

            public Task<object> GetValueAsync()
            {
                return null;
            }

            public Task SetValueAsync(object result, CancellationToken cancellationToken)
            {
                if (!(result is bool))
                {
                    throw new InvalidOperationException("Return value of authorization filter must be a boolean");
                }

                if (_responseHook != null)
                {
                    _responseHook(_request, (bool)result);
                }
                return Task.CompletedTask;
            }

            public string ToInvokeString()
            {
                return "auth_response";
            }
        }
    }
}
