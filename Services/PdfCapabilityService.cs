using System;
using System.Threading;
using System.Threading.Tasks;
using AMS.Services.Interfaces;
using Microsoft.Maui.Devices;


namespace AMS.Services
{
    // Desktop-capable implementation of IPdfCapabilityService
    public class PdfCapabilityService : IPdfCapabilityService
    {
        public bool CanGeneratePdf =>
            DeviceInfo.Platform == DevicePlatform.WinUI
            || DeviceInfo.Platform == DevicePlatform.MacCatalyst;

    }
}