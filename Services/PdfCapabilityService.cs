using AMS.Services.Interfaces;

namespace AMS.Services
{
#if WINDOWS || MACCATALYST || LINUX
    public class PdfCapabilityService : IPdfCapabilityService
    {
        public bool IsSupported => true;
    }
#else
    public class PdfCapabilityService : IPdfCapabilityService
    {
        public bool IsSupported => false;
    }
#endif
}