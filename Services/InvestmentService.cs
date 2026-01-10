using InvPj.ViewModels;
using Microsoft.EntityFrameworkCore;
using InvPj.Data;

namespace InvPj.Services
{
    public class InvestmentService
    {
        private readonly ApplicationDbContext _context;

        public InvestmentService(ApplicationDbContext context)
        {
            _context = context;
        }

        // НОВЫЙ МЕТОД: Заменяет вашу JS функцию load()
        public async Task<decimal> GetTotalBalanceAsync()
        {
            // Суммируем баланс всех инвесторов из таблицы Investors
            return await _context.Investors.SumAsync(i => i.AvailableBalance);
        }

        // Метод группировки для страницы Breakdown
        public async Task<List<InvestorTotal>> GetInvestmentBreakdownAsync()
        {
            return await _context.Investments
                .GroupBy(i => i.InvestorName)
                .Select(group => new InvestorTotal
                {
                    InvestorName = group.Key,
                    TotalAmountInvested = group.Sum(i => i.Amount),
                    NumberOfInvestments = group.Count()
                })
                .OrderByDescending(result => result.TotalAmountInvested)
                .ToListAsync();
        }
    }
}
