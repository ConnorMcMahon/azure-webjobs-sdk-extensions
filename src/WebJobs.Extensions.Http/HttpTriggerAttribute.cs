// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Bindings.Path;

namespace Microsoft.Azure.WebJobs
{
    /// <summary>
    /// Attribute used for http triggered functions.
    /// </summary>
    [Binding(TriggerHandlesReturnValue = true)]
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class HttpTriggerAttribute : Attribute
    {
        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        public HttpTriggerAttribute()
        {
            AuthLevel = AuthorizationLevel.Function;
        }

        /// <summary>
        /// Constructs a new instance.
        /// </summary>        
        /// <param name="methods">The http methods to allow.</param>
        public HttpTriggerAttribute(params string[] methods) : this()
        {
            Methods = methods;
            AllowedRoles = new string[0];
        }

        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        /// <param name="authLevel">The <see cref="AuthorizationLevel"/> to apply.</param>
        public HttpTriggerAttribute(AuthorizationLevel authLevel)
        {
            AuthLevel = authLevel;
        }

        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        /// <param name="authLevel">The <see cref="AuthorizationLevel"/> to apply.</param>
        /// <param name="methods">The http methods to allow.</param>
        public HttpTriggerAttribute(AuthorizationLevel authLevel, params string[] methods)
        {
            AuthLevel = authLevel;
            Methods = methods;
        }

        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        /// <param name="authLevel">The <see cref="AuthorizationLevel"/> to apply.</param>
        /// <param name="methods">The http methods to allow.</param>
        public HttpTriggerAttribute(AuthorizationLevel authLevel, string[] methods, string[] allowedRoles)
        {
            AuthLevel = authLevel;
            Methods = methods;
            AllowedRoles = allowedRoles;
        }

        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        /// <param name="authLevel">The <see cref="AuthorizationLevel"/> to apply.</param>
        /// <param name="methods">The http methods to allow.</param>
        public HttpTriggerAttribute(AuthorizationLevel authLevel, string[] methods, string customFunctionName)
        {
            AuthLevel = authLevel;
            Methods = methods;
            CustomAuthFunction = customFunctionName;
        }

        /// <summary>
        /// Gets or sets the route template for the function. Can include
        /// route parameters using WebApi supported syntax. If not specified,
        /// will default to the function name.
        /// </summary>
        public string Route { get; set; }

        /// <summary>
        /// Gets the http methods that are supported for the function.
        /// </summary>
        public string[] Methods { get; private set; }

        /// <summary>
        /// Gets the authorization level for the function.
        /// </summary>
        public AuthorizationLevel AuthLevel { get; private set; }

        /// <summary>
        /// Gets the authorization level for the function.
        /// </summary>
        [AutoResolve]
        public string[] AllowedRoles { get; private set; } = new string[0];

        /// <summary>
        /// Gets the function name for the custom authorization function.
        /// </summary>
        public string CustomAuthFunction { get; private set; }

        internal void ValidateSchema()
        {
            if ((AllowedRoles?.Length ?? 0) != 0 && AuthLevel != AuthorizationLevel.User)
            {
                throw new InvalidOperationException($"Cannot use {nameof(AllowedRoles)} field without using an auth level of {AuthorizationLevel.User}");
            }

            if (!string.IsNullOrEmpty(CustomAuthFunction) && AuthLevel != AuthorizationLevel.Custom)
            {
                throw new InvalidOperationException($"Cannot use {nameof(CustomAuthFunction)} without using an auth level of {AuthorizationLevel.Custom}");
            }
        }
    }
}
