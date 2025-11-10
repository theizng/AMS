using System;
using System.Threading.Tasks;
using AMS.Services.Interfaces;

namespace AMS.Services
{
    public class ContractRoomGuard : IContractRoomGuard
    {
        private readonly IContractsRepository _contractsRepo;
        public ContractRoomGuard(IContractsRepository contractsRepo) => _contractsRepo = contractsRepo;

        public async Task<bool> CanEditRoomAsync(string roomCode)
        {
            var active = await _contractsRepo.GetActiveContractByRoomAsync(roomCode, DateTime.Today);
            return active == null; // editable only if no active contract
        }
    }
}