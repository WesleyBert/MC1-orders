using System.Globalization;
using System.Text;

namespace Orders.Infrastructure.Orders;

/// <summary>
/// Normaliza texto para busca: minúsculas e sem acentos ("João" → "joao").
/// Aplicado tanto ao termo buscado quanto à chave pré-calculada de cada pedido.
/// </summary>
public static class SearchNormalizer
{
    public static string Normalize(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        // FormD separa "ã" em "a" + til combinante; os combinantes são descartados.
        var decomposed = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.ToLowerInvariant(c));
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
