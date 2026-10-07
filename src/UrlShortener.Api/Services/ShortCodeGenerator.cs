using System.Text;

namespace UrlShortener.Api.Services;

public static class ShortCodeGenerator
{
    private const string Chars =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public static string FromId(long id)
    {
        if (id <= 0)
            throw new ArgumentOutOfRangeException(nameof(id), "ID must be positive");

        var sb = new StringBuilder();
        while (id > 0)
        {
            sb.Insert(0, Chars[(int)(id % 62)]);
            id /= 62;
        }

        return sb.ToString().PadLeft(6, 'a');
    }
}