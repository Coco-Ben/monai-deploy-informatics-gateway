/*
 * Copyright 2021-2022 MONAI Consortium
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Monai.Deploy.InformaticsGateway.Services.Http;
using Monai.Deploy.Security.Authentication.Configurations;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Http
{
    public class StartupTest
    {
        [Fact(DisplayName = "Startup - Web host startup builds properly")]
        public async Task Startup_WebHostBuildsProperly()
        {
            var webHost = Microsoft.AspNetCore.WebHost.CreateDefaultBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddOptions<AuthenticationOptions>().Bind(hostContext.Configuration.GetSection("MonaiDeployAuthentication"));
                })
                .UseStartup<Startup>()
                .Build();
            Assert.NotNull(webHost);

            _ = webHost.RunAsync();

            await webHost.StopAsync();
        }
    }
}
