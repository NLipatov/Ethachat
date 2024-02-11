using Ethachat.Client.ClientOnlyModels;
using EthachatShared.Models.Message.DataTransfer;

namespace Ethachat.Client.Services.HubServices.HubServices.Implementations.MessageService.Implementation.Handlers.BinaryReceiving;

public interface IBinaryReceivingManager
{
    void StoreMetadata(Metadata metadata);
    bool StoreFile(Guid fileId, ClientPackage clientPackage);
    ClientPackage[] PopData(Guid fileId);
    Metadata PopMetadata(Guid fileId);
}