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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FellowOakDicom;
using FellowOakDicom.Network;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Monai.Deploy.InformaticsGateway.Api;
using Monai.Deploy.InformaticsGateway.Common;
using Monai.Deploy.InformaticsGateway.Configuration;
using Monai.Deploy.InformaticsGateway.Repositories;
using Monai.Deploy.InformaticsGateway.Services.Scp;
using Monai.Deploy.InformaticsGateway.Services.Storage;
using Monai.Deploy.InformaticsGateway.SharedTest;
using Moq;
using xRetry;
using Xunit;

namespace Monai.Deploy.InformaticsGateway.Test.Services.Scp
{
    public class ApplicationEntityManagerTest
    {
        private readonly Mock<IHostApplicationLifetime> _hostApplicationLifetime;
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactory;
        private readonly IMonaiAeChangedNotificationService _monaiAeChangedNotificationService;
        private readonly IOptions<InformaticsGatewayConfiguration> _connfiguration;

        private readonly Mock<ILoggerFactory> _loggerFactory;
        private readonly IDicomToolkit _dicomToolkit;
        private readonly Mock<IApplicationEntityHandler> _applicationEntityHandler;
        private readonly Mock<IServiceScope> _serviceScope;
        private readonly Mock<ILogger<ApplicationEntityManager>> _logger;
        private readonly Mock<ILogger<MonaiAeChangedNotificationService>> _loggerNotificationService;
        private readonly Mock<IStorageInfoProvider> _storageInfoProvider;

        private readonly Mock<IInformaticsGatewayRepository<SourceApplicationEntity>> _sourceEntityRepository;
        private readonly Mock<IInformaticsGatewayRepository<MonaiApplicationEntity>> _applicationEntityRepository;

        private readonly IServiceProvider _serviceProvider;

        public ApplicationEntityManagerTest()
        {
            _hostApplicationLifetime = new Mock<IHostApplicationLifetime>();
            _serviceScopeFactory = new Mock<IServiceScopeFactory>();
            _serviceScope = new Mock<IServiceScope>();
            _loggerFactory = new Mock<ILoggerFactory>();
            _applicationEntityHandler = new Mock<IApplicationEntityHandler>();
            _logger = new Mock<ILogger<ApplicationEntityManager>>();
            _dicomToolkit = new DicomToolkit();

            _loggerNotificationService = new Mock<ILogger<MonaiAeChangedNotificationService>>();
            _monaiAeChangedNotificationService = new MonaiAeChangedNotificationService(_loggerNotificationService.Object);
            _storageInfoProvider = new Mock<IStorageInfoProvider>();
            _sourceEntityRepository = new Mock<IInformaticsGatewayRepository<SourceApplicationEntity>>();
            _applicationEntityRepository = new Mock<IInformaticsGatewayRepository<MonaiApplicationEntity>>();
            _connfiguration = Options.Create(new InformaticsGatewayConfiguration());

            var services = new ServiceCollection();
            services.AddScoped(p => _loggerFactory.Object);
            services.AddScoped(p => _applicationEntityHandler.Object);
            services.AddScoped(p => _sourceEntityRepository.Object);
            services.AddScoped(p => _applicationEntityRepository.Object);
            services.AddScoped(p => _dicomToolkit);
            services.AddScoped(p => _storageInfoProvider.Object);

            _serviceProvider = services.BuildServiceProvider();

            _serviceScopeFactory.Setup(p => p.CreateScope()).Returns(_serviceScope.Object);
            _serviceScope.Setup(p => p.ServiceProvider).Returns(_serviceProvider);

            _loggerFactory.Setup(p => p.CreateLogger(It.IsAny<string>())).Returns((string type) =>
            {
                return _logger.Object;
            });
            _logger.Setup(p => p.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
            _storageInfoProvider.Setup(p => p.HasSpaceAvailableToStore).Returns(true);
        }

        [RetryFact(5, 250, DisplayName = "HandleCStoreRequest - Shall throw if AE Title not configured")]
        public async Task HandleCStoreRequest_ShallThrowIfAENotConfigured()
        {
            var manager = new ApplicationEntityManager(_hostApplicationLifetime.Object,
                                                       _serviceScopeFactory.Object,
                                                       _monaiAeChangedNotificationService,
                                                       _connfiguration);

            var request = GenerateRequest();
            var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await manager.HandleCStoreRequest(request, "BADAET", "CallingAET", Guid.NewGuid());
            });

            Assert.Equal("Called AE Title 'BADAET' is not configured", exception.Message);
        }

        [RetryFact(5, 250, DisplayName = "HandleCStoreRequest - Throws when available storage space is low")]
        public async Task HandleCStoreRequest_ThrowWhenOnLowStorageSpace()
        {
            _storageInfoProvider.Setup(p => p.HasSpaceAvailableToStore).Returns(false);
            _storageInfoProvider.Setup(p => p.AvailableFreeSpace).Returns(100);
            var aet = "TESTAET";

            var data = new List<MonaiApplicationEntity>()
            {
                new MonaiApplicationEntity()
                {
                    AeTitle = aet,
                    Name =aet
                }
            };
            _applicationEntityRepository.Setup(p => p.AsQueryable()).Returns(data.AsQueryable());
            var manager = new ApplicationEntityManager(_hostApplicationLifetime.Object,
                                                       _serviceScopeFactory.Object,
                                                       _monaiAeChangedNotificationService,
                                                       _connfiguration);

            var request = GenerateRequest();
            await Assert.ThrowsAsync<InsufficientStorageAvailableException>(async () =>
            {
                await manager.HandleCStoreRequest(request, aet, "CallingAET", Guid.NewGuid());
            });

            _logger.VerifyLogging($"{aet} added to AE Title Manager.", LogLevel.Information, Times.Once());
            _logger.VerifyLoggingMessageBeginsWith($"Preparing to save:", LogLevel.Debug, Times.Never());
            _logger.VerifyLoggingMessageBeginsWith($"Instanced saved", LogLevel.Information, Times.Never());
            _logger.VerifyLoggingMessageBeginsWith($"Instance queued for upload", LogLevel.Information, Times.Never());

            _applicationEntityRepository.Verify(p => p.AsQueryable(), Times.Once());
            _storageInfoProvider.Verify(p => p.HasSpaceAvailableToStore, Times.AtLeastOnce());
            _storageInfoProvider.Verify(p => p.AvailableFreeSpace, Times.AtLeastOnce());
        }

        [RetryFact(5, 250, DisplayName = "GetService - Shall return request service")]
        public void GetService_ShallReturnRequestedServicec()
        {
            var manager = new ApplicationEntityManager(_hostApplicationLifetime.Object,
                                                       _serviceScopeFactory.Object,
                                                       _monaiAeChangedNotificationService,
                                                       _connfiguration);

            Assert.Equal(manager.GetService<ILoggerFactory>(), _loggerFactory.Object);
        }

        [RetryFact(5, 250, DisplayName = "IsValidSource - False when AE is empty or white space")]
        public void IsValidSource_FalseWhenAEIsEmpty()
        {
            var manager = new ApplicationEntityManager(_hostApplicationLifetime.Object,
                                                       _serviceScopeFactory.Object,
                                                       _monaiAeChangedNotificationService,
                                                       _connfiguration);

            Assert.False(manager.IsValidSource("  ", "123"));
            Assert.False(manager.IsValidSource("AAA", ""));
        }

        [RetryFact(5, 250, DisplayName = "IsValidSource - False when no matching source found")]
        public void IsValidSource_FalseWhenNoMatchingSource()
        {
            var manager = new ApplicationEntityManager(_hostApplicationLifetime.Object,
                                                       _serviceScopeFactory.Object,
                                                       _monaiAeChangedNotificationService,
                                                       _connfiguration);

            _sourceEntityRepository.Setup(p => p.FirstOrDefault(It.IsAny<Func<SourceApplicationEntity, bool>>()))
                .Returns(default(SourceApplicationEntity));
            _sourceEntityRepository.Setup(p => p.AsQueryable()).Returns(
                (new List<SourceApplicationEntity>
                    { new SourceApplicationEntity { AeTitle = "SAE", HostIp = "1.2.3.4", Name = "SAE" } }
                ).AsQueryable());

            var sourceAeTitle = "ValidSource";
            Assert.False(manager.IsValidSource(sourceAeTitle, "1.2.3.4"));

            _sourceEntityRepository.Verify(p => p.FirstOrDefault(It.IsAny<Func<SourceApplicationEntity, bool>>()), Times.Once());
            _logger.VerifyLoggingMessageBeginsWith($"Available source AET: SAE @ 1.2.3.4.", LogLevel.Information, Times.Once());
        }

        [RetryFact(5, 250, DisplayName = "IsValidSource - False when IP does not match")]
        public void IsValidSource_FalseWithMismatchIp()
        {
            var manager = new ApplicationEntityManager(_hostApplicationLifetime.Object,
                                                       _serviceScopeFactory.Object,
                                                       _monaiAeChangedNotificationService,
                                                       _connfiguration);

            var aet = "SAE";
            _sourceEntityRepository.Setup(p => p.FirstOrDefault(It.IsAny<Func<SourceApplicationEntity, bool>>()))
                .Returns(default(SourceApplicationEntity));

            Assert.False(manager.IsValidSource(aet, "1.1.1.1"));

            _sourceEntityRepository.Verify(p => p.FirstOrDefault(It.IsAny<Func<SourceApplicationEntity, bool>>()), Times.Once());
        }

        [RetryFact(5, 250, DisplayName = "IsValidSource - True")]
        public void IsValidSource_True()
        {
            var manager = new ApplicationEntityManager(_hostApplicationLifetime.Object,
                                                       _serviceScopeFactory.Object,
                                                       _monaiAeChangedNotificationService,
                                                       _connfiguration);

            var aet = "SAE";
            _sourceEntityRepository.Setup(p => p.FirstOrDefault(It.IsAny<Func<SourceApplicationEntity, bool>>()))
                .Returns(new SourceApplicationEntity { AeTitle = aet, HostIp = "1.2.3.4", Name = "SAE" });

            Assert.True(manager.IsValidSource(aet, "1.2.3.4"));

            _sourceEntityRepository.Verify(p => p.FirstOrDefault(It.IsAny<Func<SourceApplicationEntity, bool>>()), Times.Once());
        }

        [RetryFact(5, 250, DisplayName = "Shall handle AE change events")]
        public void ShallHandleAEChangeEvents()
        {
            var manager = new ApplicationEntityManager(_hostApplicationLifetime.Object,
                                                       _serviceScopeFactory.Object,
                                                       _monaiAeChangedNotificationService,
                                                       _connfiguration);

            _monaiAeChangedNotificationService.Notify(new MonaiApplicationentityChangedEvent(
                new MonaiApplicationEntity
                {
                    AeTitle = "AE1",
                    Name = "AE1"
                }, ChangedEventType.Added));
            Assert.True(manager.IsAeTitleConfigured("AE1"));

            _monaiAeChangedNotificationService.Notify(new MonaiApplicationentityChangedEvent(
                new MonaiApplicationEntity
                {
                    AeTitle = "AE1",
                    Name = "AE1"
                }, ChangedEventType.Deleted));
            Assert.False(manager.IsAeTitleConfigured("AE1"));
        }

        [RetryFact(5, 250, DisplayName = "Shall handle CS Store Request")]
        public async Task ShallHandleCStoreRequest()
        {
            var associationId = Guid.NewGuid();
            var manager = new ApplicationEntityManager(_hostApplicationLifetime.Object,
                                                       _serviceScopeFactory.Object,
                                                       _monaiAeChangedNotificationService,
                                                       _connfiguration);

            _monaiAeChangedNotificationService.Notify(new MonaiApplicationentityChangedEvent(
                new MonaiApplicationEntity
                {
                    AeTitle = "AE1",
                    Name = "AE1"
                }, ChangedEventType.Added));
            Assert.True(manager.IsAeTitleConfigured("AE1"));

            var request = GenerateRequest();
            await manager.HandleCStoreRequest(request, "AE1", "AE", associationId);

            _applicationEntityHandler.Verify(p =>
                p.HandleInstanceAsync(
                    request,
                    "AE1",
                    "AE",
                    associationId,
                    It.Is<StudySerieSopUids>(p =>
                        p.SopClassUid.Equals(request.Dataset.GetSingleValue<string>(DicomTag.SOPClassUID)) &&
                        p.StudyInstanceUid.Equals(request.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID)) &&
                        p.SeriesInstanceUid.Equals(request.Dataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID)) &&
                        p.SopInstanceUid.Equals(request.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID))))
                , Times.Once());
        }

        private static DicomCStoreRequest GenerateRequest()
        {
            var dataset = new DicomDataset
            {
                { DicomTag.PatientID, "PID" },
                { DicomTag.StudyInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID() },
                { DicomTag.SeriesInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID() },
                { DicomTag.SOPInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID() },
                { DicomTag.SOPClassUID, DicomUID.SecondaryCaptureImageStorage.UID }
            };
            var file = new DicomFile(dataset);
            return new DicomCStoreRequest(file);
        }
    }
}
