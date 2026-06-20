using Ombor.Contracts.Requests.Wallet;
using Ombor.Contracts.Responses.Wallet;

namespace Ombor.Application.Interfaces;

public interface IWalletService
{
    Task<WalletDto[]> GetAsync(GetWalletsRequest request);
    Task<WalletDto> GetByIdAsync(GetWalletByIdRequest request);
    Task<WalletDto> CreateAsync(CreateWalletRequest request);
    Task<WalletDto> UpdateAsync(UpdateWalletRequest request);
    Task ArchiveAsync(int id);
    Task RestoreAsync(int id);

    Task<WalletTransferDto> CreateTransferAsync(CreateWalletTransferRequest request);
    Task<WalletOperationDto[]> GetOperationsAsync(int walletId);
    Task<WalletTransferDto[]> GetTransfersAsync(int walletId);
}
