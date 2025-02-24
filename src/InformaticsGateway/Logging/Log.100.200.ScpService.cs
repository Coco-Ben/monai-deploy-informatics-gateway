/*
 * Copyright 2023 MONAI Consortium
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
using FellowOakDicom;
using FellowOakDicom.Network;
using Microsoft.Extensions.Logging;
using Monai.Deploy.InformaticsGateway.Services.Scp;

namespace Monai.Deploy.InformaticsGateway.Logging
{
    public static partial class Log
    {
        // Application Entity Manager/Handler
        [LoggerMessage(EventId = 100, Level = LogLevel.Information, Message = "ApplicationEntityManager stopping.")]
        public static partial void ApplicationEntityManagerStopping(this ILogger logger);

        [LoggerMessage(EventId = 101, Level = LogLevel.Information, Message = "Study Instance UID: {StudyInstanceUid}. Series Instance UID: {SeriesInstanceUid}.")]
        public static partial void InstanceInformation(this ILogger logger, string studyInstanceUid, string seriesInstanceUid);

        [LoggerMessage(EventId = 102, Level = LogLevel.Error, Message = "AE Title {AETitle} could not be added to CStore Manager.  Already exits: {exists}.")]
        public static partial void AeTitleCannotBeAdded(this ILogger logger, string aeTitle, bool exists);

        [LoggerMessage(EventId = 103, Level = LogLevel.Information, Message = "{AETitle} added to AE Title Manager.")]
        public static partial void AeTitleAdded(this ILogger logger, string aeTitle);

        [LoggerMessage(EventId = 104, Level = LogLevel.Error, Message = "Error notifying observer.")]
        public static partial void ErrorNotifyingObserver(this ILogger logger);

        [LoggerMessage(EventId = 105, Level = LogLevel.Information, Message = "{aeTitle} removed from AE Title Manager.")]
        public static partial void AeTitleRemoved(this ILogger logger, string aeTitle);

        [LoggerMessage(EventId = 106, Level = LogLevel.Information, Message = "Available source AET: {aeTitle} @ {hostIp}.")]
        public static partial void AvailableSource(this ILogger logger, string aeTitle, string hostIp);

        [LoggerMessage(EventId = 107, Level = LogLevel.Information, Message = "Loading MONAI Application Entities from data store.")]
        public static partial void LoadingMonaiAeTitles(this ILogger logger);

        [LoggerMessage(EventId = 108, Level = LogLevel.Information, Message = "Instance ignored due to matching SOP Class UID {uid}.")]
        public static partial void InstanceIgnoredWIthMatchingSopClassUid(this ILogger logger, string uid);

        [LoggerMessage(EventId = 109, Level = LogLevel.Information, Message = "Queuing instance with group {dicomTag}.")]
        public static partial void QueueInstanceUsingDicomTag(this ILogger logger, DicomTag dicomTag);

        [LoggerMessage(EventId = 112, Level = LogLevel.Information, Message = "Notifying {count} observers of MONAI Application Entity {eventType}.")]
        public static partial void NotifyAeChanged(this ILogger logger, int count, ChangedEventType eventType);

        [LoggerMessage(EventId = 113, Level = LogLevel.Error, Message = "Failed to update Application Entity Handler with updated AE Title {aeTitle}.")]
        public static partial void FailedToUpdateAppliationEntityHandlerWithUpdatedAEChange(this ILogger logger, string aeTitle, Exception ex = null);

        // SCP Service
        [LoggerMessage(EventId = 200, Level = LogLevel.Information, Message = "Initializing SCP Service at port {port}...")]
        public static partial void ScpServiceLoading(this ILogger logger, int port);

        [LoggerMessage(EventId = 201, Level = LogLevel.Critical, Message = "Failed to initialize SCP listener.")]
        public static partial void ScpListenerInitializationFailure(this ILogger logger);

        [LoggerMessage(EventId = 202, Level = LogLevel.Information, Message = "SCP listening on port: {port}.")]
        public static partial void ScpListeningOnPort(this ILogger logger, int port);

        [LoggerMessage(EventId = 203, Level = LogLevel.Information, Message = "C-ECHO request received.")]
        public static partial void CEchoReceived(this ILogger logger);

        [LoggerMessage(EventId = 204, Level = LogLevel.Error, Message = "Connection closed with exception.")]
        public static partial void ConnectionClosedWithException(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 205, Level = LogLevel.Information, Message = "Transfer syntax used: {transferSyntax}.")]
        public static partial void TransferSyntaxUsed(this ILogger logger, DicomTransferSyntax transferSyntax);

        [LoggerMessage(EventId = 206, Level = LogLevel.Error, Message = "Failed to process C-STORE request, out of storage space.")]
        public static partial void CStoreFailedWithNoSpace(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 207, Level = LogLevel.Error, Message = "Failed to process C-STORE request.")]
        public static partial void CStoreFailed(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 208, Level = LogLevel.Warning, Message = "Aborted {source} with reason {reason}.")]
        public static partial void CStoreAbort(this ILogger logger, DicomAbortSource source, DicomAbortReason reason);

        [LoggerMessage(EventId = 209, Level = LogLevel.Information, Message = "Association release request received. Connection Time: {elapsedTime}.")]
        public static partial void CStoreAssociationReleaseRequest(this ILogger logger, TimeSpan elapsedTime);

        [LoggerMessage(EventId = 210, Level = LogLevel.Information, Message = "Association received from {host}:{port}.")]
        public static partial void CStoreAssociationReceived(this ILogger logger, string host, int port);

        [LoggerMessage(EventId = 211, Level = LogLevel.Warning, Message = "Verification service is disabled: rejecting association.")]
        public static partial void VerificationServiceDisabled(this ILogger logger);

        [LoggerMessage(EventId = 212, Level = LogLevel.Error, Message = "Failed to process C-STORE request, out of storage space.")]
        public static partial void CStoreFailedDueToLowStorageSpace(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 213, Level = LogLevel.Error, Message = "Error saving DICOM association information. Correlation ID={correlationId}.")]
        public static partial void ErrorSavingDicomAssociationInfo(this ILogger logger, Guid correlationId, Exception ex);
    }
}
