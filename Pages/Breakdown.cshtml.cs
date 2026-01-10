using InvPj.Services;
using InvPj.ViewModels;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InvPj.Pages
{
    public class BreakdownModel : PageModel
    {
        private readonly InvestmentService _investmentService;

        // Внедряем сервис через конструктор (DI работает автоматически)
        public BreakdownModel(InvestmentService investmentService)
        {
            _investmentService = investmentService;
        }

        // Это свойство будет доступно в HTML-представлении
        public List<InvestorTotal>? InvestorBreakdown { get; set; }

        // Метод, который вызывается при переходе на страницу (GET запрос)
        public async Task OnGetAsync()
        {
            InvestorBreakdown = await _investmentService.GetInvestmentBreakdownAsync();
        }
    }
}
