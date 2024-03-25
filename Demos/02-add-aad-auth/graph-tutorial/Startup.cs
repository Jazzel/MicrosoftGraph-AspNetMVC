// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using graph_tutorial.Models;
using Microsoft.Owin;
using Owin;
using System;

[assembly: OwinStartup(typeof(graph_tutorial.Startup))]

namespace graph_tutorial
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);

        }
    }
}
