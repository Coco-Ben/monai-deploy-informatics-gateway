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
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Ardalis.GuardClauses;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Api.Rest;
using Monai.Deploy.InformaticsGateway.Api.Storage;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Logging;
using Monai.Deploy.InformaticsGateway.Repositories;
using Monai.Deploy.InformaticsGateway.Services.Common;
using Monai.Deploy.Storage.API;
using Polly;

namespace Monai.Deploy.InformaticsGateway.Services.Storage
{
    internal class ObjectUploadService : IHostedService, IMonaiService, IDisposable
    {
        private readonly ILogger<ObjectUploadService> _logger;
        private readonly IObjectUploadQueue _uplaodQueue;
        private readonly IStorageService _storageService;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly IOptions<InformaticsGatewayConfiguration> _configuration;
        private readonly IServiceScope _scope;
        private ActionBlock<FileStorageMetadata> _worker;
        private bool _disposedValue;

        public ServiceStatus Status { get; set; } = ServiceStatus.Unknown;
        public string ServiceName => "Object Upload Service";

        public ObjectUploadService(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<ObjectUploadService> logger,
            IOptions<InformaticsGatewayConfiguration> configuration)
        {
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _cancellationTokenSource = new CancellationTokenSource();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _scope = _serviceScopeFactory.CreateScope();
            _uplaodQueue = _scope.ServiceProvider.GetService<IObjectUploadQueue>() ?? throw new ServiceNotFoundException(nameof(IObjectUploadQueue));
            _storageService = _scope.ServiceProvider.GetService<IStorageService>() ?? throw new ServiceNotFoundException(nameof(IStorageService));

            RemovePendingUploadObjects();
        }

        /// <summary>
        /// Removes all uploading pending objects from the database at startup since objects are lost upon service restart (crash).
        /// </summary>
        private void RemovePendingUploadObjects()
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetService<IStorageMetadataWrapperRepository>() ?? throw new ServiceNotFoundException(nameof(IStorageMetadataWrapperRepository));
                repository.DeletePendingUploadsAsync();
            }
            catch (Exception ex)
            {
                _logger.ErrorRemovingPendingUploadObjects(ex);
            }
        }

        private void BackgroundProcessing(CancellationToken cancellationToken)
        {
            _logger.ServiceRunning(ServiceName);
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _worker.Post(_uplaodQueue.Dequeue(cancellationToken));
                }
                catch (ObjectDisposedException ex)
                {
                    _logger.ServiceDisposed(ServiceName, ex);
                }
                catch (Exception ex)
                {
                    if (ex is InvalidOperationException || ex is OperationCanceledException)
                    {
                        _logger.ServiceInvalidOrCancelled(ServiceName, ex);
                    }
                }
            }
            Status = ServiceStatus.Cancelled;
            _logger.ServiceCancelled(ServiceName);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var task = Task.Run(() =>
            {
                _worker = new ActionBlock<FileStorageMetadata>(ProcessObject, new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = _configuration.Value.Storage.ConcurrentUploads,
                    CancellationToken = cancellationToken,
                });

                BackgroundProcessing(cancellationToken);
            }, CancellationToken.None);

            Status = ServiceStatus.Running;
            _logger.ServiceRunning(ServiceName);
            if (task.IsCompleted)
                return task;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.ServiceStopping(ServiceName);
            _cancellationTokenSource.Cancel();
            _worker.Complete();
            Status = ServiceStatus.Stopped;
            return Task.CompletedTask;
        }

        private async Task ProcessObject(FileStorageMetadata blob)
        {
            Guard.Against.Null(blob, nameof(blob));

            using var loggerScope = _logger.BeginScope(new LoggingDataDictionary<string, object> { { "File ID", blob.Id }, { "Correlation ID", blob.CorrelationId } });
            var stopwatch = new Stopwatch();
            try
            {
                stopwatch.Start();

                switch (blob)
                {
                    case DicomFileStorageMetadata dicom:
                        if (!string.IsNullOrWhiteSpace(dicom.JsonFile.TemporaryPath))
                        {
                            await UploadData(dicom.Id, dicom.JsonFile, dicom.Source, dicom.Workflows, _cancellationTokenSource.Token).ConfigureAwait(false);
                        }
                        break;
                }

                await UploadData(blob.Id, blob.File, blob.Source, blob.Workflows, _cancellationTokenSource.Token).ConfigureAwait(false);
                await UpdateBlob(blob);
            }
            catch (Exception ex)
            {
                _logger.FailedToUploadFile(blob.Id, ex);
                blob.SetFailed();
                await UpdateBlob(blob);
            }
            finally
            {
                stopwatch.Stop();
                _logger.UploadStats(_configuration.Value.Storage.ConcurrentUploads, stopwatch.Elapsed.TotalSeconds);
            }
        }

        private async Task UpdateBlob(FileStorageMetadata blob)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetService<IStorageMetadataWrapperRepository>() ?? throw new ServiceNotFoundException(nameof(IStorageMetadataWrapperRepository));
            await repository.AddOrUpdateAsync(blob).ConfigureAwait(false);
        }

        private async Task UploadData(string identifier, StorageObjectMetadata storageObjectMetadata, string source, List<string> workflows, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(identifier, nameof(identifier));
            Guard.Against.Null(storageObjectMetadata, nameof(storageObjectMetadata));
            Guard.Against.NullOrWhiteSpace(source, nameof(source));
            Guard.Against.Null(workflows, nameof(workflows));

            if (storageObjectMetadata.IsUploaded)
            {
                return;
            }

            _logger.UploadingFileToTemporaryStore(storageObjectMetadata.TemporaryPath);
            var metadata = new Dictionary<string, string>
                {
                    { FileMetadataKeys.Source, source },
                    { FileMetadataKeys.Workflows, workflows.IsNullOrEmpty() ? string.Empty : string.Join(',', workflows) }
                };

            await Policy
               .Handle<Exception>()
               .WaitAndRetryAsync(
                   _configuration.Value.Storage.Retries.RetryDelays,
                   (exception, timeSpan, retryCount, context) =>
                   {
                       _logger.ErrorUploadingFileToTemporaryStore(timeSpan, retryCount, exception);
                   })
               .ExecuteAsync(async () =>
               {
                   storageObjectMetadata.Data.Seek(0, System.IO.SeekOrigin.Begin);
                   await _storageService.PutObjectAsync(
                       _configuration.Value.Storage.TemporaryStorageBucket,
                       storageObjectMetadata.GetTempStoragPath(_configuration.Value.Storage.RemoteTemporaryStoragePath),
                       storageObjectMetadata.Data,
                       storageObjectMetadata.Data.Length,
                       storageObjectMetadata.ContentType,
                       metadata,
                       cancellationToken).ConfigureAwait(false);
                   storageObjectMetadata.SetUploaded(_configuration.Value.Storage.TemporaryStorageBucket);
               })
               .ConfigureAwait(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _scope.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
