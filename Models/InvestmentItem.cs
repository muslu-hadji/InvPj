namespace InvPj.Models // Убедитесь, что namespace совпадает с вашим проектом
{
    public class InvestmentItem
    {
        public int Id { get; set; } // Первичный ключ
        public required string InvestorName { get; set; } // Поле "Имя"
        public decimal Amount { get; set; } // Поле "Сумма (Р)"
    }
}

