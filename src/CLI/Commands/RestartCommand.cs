﻿// SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
// SPDX-License-Identifier: Apache License 2.0

using System;
using System.CommandLine.NamingConventionBinder;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Monai.Deploy.InformaticsGateway.CLI.Services;

namespace Monai.Deploy.InformaticsGateway.CLI
{
    public class RestartCommand : CommandBase
    {
        public RestartCommand() : base("restart", $"Restart the {Strings.ApplicationName} service")
        {
            AddConfirmationOption();
            Handler = CommandHandler.Create<IHost, bool, bool, CancellationToken>(RestartCommandHandler);
        }

        private async Task<int> RestartCommandHandler(IHost host, bool yes, bool verbose, CancellationToken cancellationToken)
        {
            Guard.Against.Null(host, nameof(host));

            var service = host.Services.GetRequiredService<IControlService>();
            var confirmation = host.Services.GetRequiredService<IConfirmationPrompt>();
            var logger = CreateLogger<RestartCommand>(host);

            Guard.Against.Null(confirmation, nameof(confirmation), "Confirmation prompt is unavailable.");
            Guard.Against.Null(service, nameof(service), "Control service is unavailable.");
            Guard.Against.Null(logger, nameof(logger), "Logger is unavailable.");

            if (!yes && !confirmation.ShowConfirmationPrompt($"Do you want to restart {Strings.ApplicationName}?"))
            {
                logger.ActionCancelled();
                return ExitCodes.Restart_Cancelled;
            }

            try
            {
                await service.RestartService(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.ErrorRestarting(Strings.ApplicationName, ex.Message);
                return ExitCodes.Restart_Error;
            }
            return ExitCodes.Success;
        }
    }
}
