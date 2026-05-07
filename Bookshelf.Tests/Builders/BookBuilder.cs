using Bookshelf.Models;

namespace Bookshelf.Tests.Builders;

public class BookBuilder
{
    private int _id;
    private string _title = "A Wizard of Earthsea";
    private string? _isbn = "978-0-547-72207-0";
    private int? _year = 1968;
    private string? _coverImagePath;
    private int _authorId = 1;
    private Author? _author;

    public BookBuilder WithId(int id)
    {
        _id = id;
        return this;
    }

    public BookBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public BookBuilder WithIsbn(string? isbn)
    {
        _isbn = isbn;
        return this;
    }

    public BookBuilder WithYear(int? year)
    {
        _year = year;
        return this;
    }

    public BookBuilder WithCoverImagePath(string? path)
    {
        _coverImagePath = path;
        return this;
    }

    public BookBuilder WithAuthorId(int authorId)
    {
        _authorId = authorId;
        return this;
    }

    public BookBuilder WithAuthor(Author author)
    {
        _author = author;
        _authorId = author.Id;
        return this;
    }

    public Book Build()
    {
        var book = new Book
        {
            Id = _id,
            Title = _title,
            Isbn = _isbn,
            Year = _year,
            CoverImagePath = _coverImagePath,
            AuthorId = _authorId
        };

        if (_author is not null)
        {
            book.Author = _author;
        }

        return book;
    }
}
