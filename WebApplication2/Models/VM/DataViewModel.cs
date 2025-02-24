using WebApplication2.Models;
using WebApplication2.Models.VM;

public class DataViewModel
{
    public IEnumerable<SellerViewModel> Sellers { get; set; }
    public IEnumerable<LotViewModel> Lots { get; set; }
    public IEnumerable<GamesCategory> Games { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPagesSellers { get; set; }
    public int TotalPagesLots { get; set; }
    public int TotalPagesGames { get; set; }
    public string ActiveTab { get; set; } // Добавляем свойство для активной вкладки
}