using System;
using System.Collections.Generic;

namespace AMS.Models
{
    public class ContractSnapshot
    {
        public string ContractNumber { get; set; } = "";
        public string RoomCode { get; set; } = "";
        public string HouseAddress { get; set; } = "";

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int DueDay { get; set; }

        public decimal RentAmount { get; set; }
        public decimal SecurityDeposit { get; set; }
        public int DepositReturnDays { get; set; }

        public int MaxOccupants { get; set; }
        public int MaxBikeAllowance { get; set; }

        public string PaymentMethods { get; set; } = "";
        public string LateFeePolicy { get; set; } = "";
        public string PropertyDescription { get; set; } = "";

        public List<ContractTenant> Tenants { get; set; } = new();
        public string? PdfUrl { get; set; }
    }
}