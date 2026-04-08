using Bookshelf.Areas.Admin;

namespace Bookshelf.Models;

public class Author : IEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public List<Book> Books { get; set; } = new();
}
