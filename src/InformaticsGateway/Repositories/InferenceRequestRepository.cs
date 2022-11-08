/*
 * Copyright 2021-2022 MONAI Consortium
 * Copyright 2019-2021 NVIDIA Corporation
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

using System;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Database.Api;
using Monai.Deploy.InformaticsGateway.Logging;
using Polly;

namespace Monai.Deploy.InformaticsGateway.Repositories
{
    public class InferenceRequestRepository : IInferenceRequestRepository
    {
        private readonly ILogger<InferenceRequestRepository> _logger;
        private readonly IInformaticsGatewayRepository<InferenceRequest> _inferenceRequestRepository;
        private readonly IOptions<InformaticsGatewayConfiguration> _options;

        public ServiceStatus Status { get; set; } = ServiceStatus.Unknown;

        public InferenceRequestRepository(
            ILogger<InferenceRequestRepository> logger,
            IInformaticsGatewayRepository<InferenceRequest> inferenceRequestRepository,
            IOptions<InformaticsGatewayConfiguration> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _inferenceRequestRepository = inferenceRequestRepository ?? throw new ArgumentNullException(nameof(inferenceRequestRepository));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task Add(InferenceRequest inferenceRequest)
        {
            Guard.Against.Null(inferenceRequest, nameof(inferenceRequest));

            using var loggerScope = _logger.BeginScope(new LoggingDataDictionary<string, object> { { "TransactionId", inferenceRequest.TransactionId } });
            await Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    _options.Value.Database.Retries.RetryDelays,
                    (exception, timeSpan, retryCount, context) =>
                {
                    _logger.ErrorSavingInferenceRequest(timeSpan, retryCount, exception);
                })
                .ExecuteAsync(async () =>
                {
                    await _inferenceRequestRepository.AddAsync(inferenceRequest).ConfigureAwait(false);
                    await _inferenceRequestRepository.SaveChangesAsync().ConfigureAwait(false);
                    _inferenceRequestRepository.Detach(inferenceRequest);
                    _logger.InferenceRequestSaved();
                })
                .ConfigureAwait(false);
        }

        public async Task Update(InferenceRequest inferenceRequest, InferenceRequestStatus status)
        {
            Guard.Against.Null(inferenceRequest, nameof(inferenceRequest));

            using var loggerScope = _logger.BeginScope(new LoggingDataDictionary<string, object> { { "TransactionId", inferenceRequest.TransactionId } });

            if (status == InferenceRequestStatus.Success)
            {
                inferenceRequest.State = InferenceRequestState.Completed;
                inferenceRequest.Status = InferenceRequestStatus.Success;
            }
            else
            {
                if (++inferenceRequest.TryCount > _options.Value.Database.Retries.DelaysMilliseconds.Length)
                {
                    _logger.InferenceRequestUpdateExceededMaximumRetries();
                    inferenceRequest.State = InferenceRequestState.Completed;
                    inferenceRequest.Status = InferenceRequestStatus.Fail;
                }
                else
                {
                    _logger.InferenceRequestUpdateRetryLater();
                    inferenceRequest.State = InferenceRequestState.Queued;
                }
            }

            await Save(inferenceRequest).ConfigureAwait(false);
        }

        public async Task<InferenceRequest> Take(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var inferenceRequest = _inferenceRequestRepository.FirstOrDefault(p => p.State == InferenceRequestState.Queued);

                if (inferenceRequest is not null)
                {
                    using var loggerScope = _logger.BeginScope(new LoggingDataDictionary<string, object> { { "TransactionId", inferenceRequest.TransactionId } });
                    inferenceRequest.State = InferenceRequestState.InProcess;
                    _logger.InferenceRequestSetToInProgress(inferenceRequest.TransactionId);
                    await Save(inferenceRequest).ConfigureAwait(false);
                    return inferenceRequest;
                }
                await Task.Delay(250, cancellationToken).ConfigureAwait(false);
            }

            throw new OperationCanceledException("cancellation requsted");
        }

        public InferenceRequest GetInferenceRequest(string transactionId)
        {
            Guard.Against.NullOrWhiteSpace(transactionId, nameof(transactionId));
            return _inferenceRequestRepository.FirstOrDefault(p => p.TransactionId.Equals(transactionId, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<InferenceRequest> GetInferenceRequest(Guid inferenceRequestId)
        {
            Guard.Against.NullOrEmpty(inferenceRequestId, nameof(inferenceRequestId));
            return await _inferenceRequestRepository.FindAsync(inferenceRequestId).ConfigureAwait(false);
        }

        public bool Exists(string transactionId)
        {
            Guard.Against.NullOrWhiteSpace(transactionId, nameof(transactionId));
            return GetInferenceRequest(transactionId) is not null;
        }

        public async Task<InferenceStatusResponse> GetStatus(string transactionId)
        {
            Guard.Against.NullOrWhiteSpace(transactionId, nameof(transactionId));

            var response = new InferenceStatusResponse();
            var item = GetInferenceRequest(transactionId);
            if (item is null)
            {
                return null;
            }

            response.TransactionId = item.TransactionId;

            return await Task.FromResult(response).ConfigureAwait(false);
        }

        private async Task Save(InferenceRequest inferenceRequest)
        {
            Guard.Against.Null(inferenceRequest, nameof(inferenceRequest));

            await Policy
                 .Handle<Exception>()
                 .WaitAndRetryAsync(
                    _options.Value.Database.Retries.RetryDelays,
                     (exception, timeSpan, retryCount, context) =>
                     {
                         _logger.InferenceRequestUpdateError(timeSpan, retryCount, exception);
                     })
                 .ExecuteAsync(async () =>
                 {
                     _logger.InferenceRequestUpdateState();
                     if (inferenceRequest.State == InferenceRequestState.Completed)
                     {
                         _inferenceRequestRepository.Detach(inferenceRequest);
                     }
                     await _inferenceRequestRepository.SaveChangesAsync().ConfigureAwait(false);
                     _logger.InferenceRequestUpdated();
                 })
                 .ConfigureAwait(false);
        }
    }
}
