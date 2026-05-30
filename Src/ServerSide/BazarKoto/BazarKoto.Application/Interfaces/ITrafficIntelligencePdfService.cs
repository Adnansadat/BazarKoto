using BazarKoto.Contracts.Admin;

namespace BazarKoto.Application.Interfaces;

public interface ITrafficIntelligencePdfService
{
    byte[] Generate(TrafficIntelligenceReportDto report);
}
