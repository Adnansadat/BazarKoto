using BazarKoto.Contracts.Admin;
using BazarKoto.Contracts.Common;

namespace BazarKoto.Application.Interfaces;

public interface IAdminDashboardService
{
    Task<ApiResponse<AdminDashboardResponse>> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<TrafficIntelligenceReportDto> GetTrafficIntelligenceReportAsync(CancellationToken cancellationToken = default);
}
