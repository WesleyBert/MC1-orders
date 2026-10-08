using Orders.Infrastructure.Persistence;

namespace Orders.UnitTests.Infrastructure;

public sealed class SearchNormalizerTests
{
    [Theory]
    [InlineData("João Conceição", "joao conceicao")]
    [InlineData("  ÁRVORE  ", "arvore")]
    [InlineData("Pão de Açúcar", "pao de acucar")]
    [InlineData("Ñandú Über", "nandu uber")]
    [InlineData("10234", "10234")]
    [InlineData("", "")]
    public void Normalize_RemovesAccentsCaseAndOuterSpaces(string input, string expected)
    {
        SearchNormalizer.Normalize(input).Should().Be(expected);
    }
}
