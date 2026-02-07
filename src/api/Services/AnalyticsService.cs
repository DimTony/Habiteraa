using Habitera.Models;

namespace Habitera.Services
{
    public interface IPriceAnalyticsService
    {
        Task<PriceAnalytics> GetPriceAnalyticsAsync(Guid propertyId);
        Task<MarketTrends> GetMarketTrendsAsync(string city, string state);
    }
    public class PriceAnalyticsService : IPriceAnalyticsService
    {

        public async Task<PriceAnalytics> GetPriceAnalyticsAsync(Guid propertyId)
        {
            return new PriceAnalytics();
        }

        public async Task<MarketTrends> GetMarketTrendsAsync(string city, string state)
        {
            return new MarketTrends();
        }
    }
}
