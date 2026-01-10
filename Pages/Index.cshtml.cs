using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using InvPj.Models;
using InvPj.Services;
using InvPj.Data;
using System; // Добавьте этот using
using System.Threading.Tasks;

namespace InvPj.Pages
{
    public class IndexModel : PageModel
    {
        private readonly InvestmentService _investmentService;
        private readonly ApplicationDbContext _context;

        public IndexModel(InvestmentService investmentService, ApplicationDbContext context)
        {
            _investmentService = investmentService;
            _context = context;
        }

        [BindProperty]
        public InvestmentItem? NewInvestment { get; set; } // Добавлен '?'

        public decimal TotalBalance { get; set; }

        public async Task OnGetAsync()
        {
            TotalBalance = await _investmentService.GetTotalBalanceAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                // Временно для отладки, чтобы увидеть, что не так:
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        Console.WriteLine($"Model Error: {error.ErrorMessage}");
                    }
                }
                return Page(); // <-- Возвращает при ошибке валидации
            }
       
            // !!! КОД УСПЕШНОГО ВЫПОЛНЕНИЯ !!!
            // Если валидация прошла успешно, мы должны сохранить данные и перенаправить
            _context.Investments.Add(NewInvestment);
            await _context.SaveChangesAsync();
            return RedirectToPage("./Index"); // <-- Возвращает при успехе
        }
    }
}
