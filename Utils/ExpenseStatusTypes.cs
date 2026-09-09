namespace SaaS.Utils
{
    /// <summary>
    /// CMS DB'deki expense_status tablosuyla eşleşir.
    /// Tablodaki id'ler değişirse burası da güncellenmeli.
    /// </summary>
    public enum ExpenseStatusTypes
    {
        Pending = 1,
        Approved = 2,
        Rejected = 3
    }
}