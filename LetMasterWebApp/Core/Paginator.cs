using Microsoft.EntityFrameworkCore;
using System.Drawing.Printing;

namespace LetMasterWebApp.Core;
public class PaginatedList<T>
{
    public List<T> Items { get; private set; }
    public int TotalCount {  get; private set; }
    public int CurrentPage {  get; private set; }
    public int PageSize { get; private set; }
    public int PageCount { get; private set; }
    public PaginatedList(List<T> items, int totalCount, int currentPage,int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        CurrentPage = currentPage;
        PageSize = pageSize;
        PageCount = (int)Math.Ceiling(totalCount / (double)pageSize);
    }
    public bool HasPrev => CurrentPage > 1;
    public bool HasNext => CurrentPage < PageCount;
    public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int currentPage, int pageSize)
    {
        var count = await source.CountAsync();
        var items = await source.Skip(pageSize * (currentPage - 1)).Take(pageSize).ToListAsync();
        return new PaginatedList<T>(items, count, currentPage, pageSize);
    }
}
public static class PaginationHelper
{
    public static async Task<PaginatedList<T>> PaginateAsync<T>(IQueryable<T> query, int page, int pageSize)
    { 
        return await PaginatedList<T>.CreateAsync(query, page, pageSize);
    }
}