// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.WebJobs.Extensions.Http
{
    /// <summary>
    /// Attribute used for http triggered functions.
    /// </summary>
    [Binding(TriggerHandlesReturnValue = true)]
    [AttributeUsage(AttributeTargets.Parameter)]
    public class HttpAuthorizationTriggerAttribute : Attribute
    {
    }
}
