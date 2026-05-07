using Bookshelf.Models;

namespace Bookshelf.Tests.Builders;

public class AuthorBuilder
{
    private int _id;
    private string _name = "Ursula K. Le Guin";
    private string _userId = "test-user-id";
    private List<Book> _books = new();

    public AuthorBuilder WithId(int id)
    {
        _id = id;
        return this;
    }

    public AuthorBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public AuthorBuilder WithUserId(string userId)
    {
        _userId = userId;
        return this;
    }

    public AuthorBuilder WithBooks(params Book[] books)
    {
        _books = books.ToList();
        return this;
    }

    public Author Build() => new()
    {
        Id = _id,
        Name = _name,
        UserId = _userId,
        Books = _books
    };
}
