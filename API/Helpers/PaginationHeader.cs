namespace API.Helpers;

public class PaginationHeader
{
    public PaginationHeader(int currentPage, int itemsPerPage, int totalItems, int currentPages)
    {
        CurrentPage = currentPage;
        ItemsPerPage = itemsPerPage;
        TotalItems = totalItems;
        CurrentPages = currentPages;
    }

    public int CurrentPage { get; set; }
    
    public int ItemsPerPage { get; set; }
    
    public int TotalItems { get; set; }
    
    public int CurrentPages { get; set; }
}