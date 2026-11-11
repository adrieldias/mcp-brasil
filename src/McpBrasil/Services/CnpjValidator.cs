namespace McpBrasil.Services;

/// <summary>
/// Validação de CNPJ pelos dígitos verificadores, sem acesso à rede.
/// Aceita o formato numérico tradicional e o novo CNPJ alfanumérico
/// (12 primeiros caracteres podem ser letras A-Z; os 2 últimos são sempre dígitos).
/// </summary>
public static class CnpjValidator
{
    private static readonly int[] Pesos1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
    private static readonly int[] Pesos2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

    /// <summary>Remove pontuação e espaços e converte para maiúsculas.</summary>
    public static string Normalizar(string? cnpj) =>
        new((cnpj ?? string.Empty)
            .Where(char.IsLetterOrDigit)
            .Select(char.ToUpperInvariant)
            .ToArray());

    public static bool EhAlfanumerico(string cnpjNormalizado) =>
        cnpjNormalizado.Take(12).Any(char.IsLetter);

    public static bool Validar(string? cnpj, out string normalizado, out string? motivo)
    {
        normalizado = Normalizar(cnpj);
        motivo = null;

        if (normalizado.Length != 14)
        {
            motivo = $"O CNPJ deve ter 14 caracteres (recebido: {normalizado.Length}).";
            return false;
        }

        for (var i = 0; i < 12; i++)
        {
            var c = normalizado[i];
            if (!(char.IsAsciiDigit(c) || (c >= 'A' && c <= 'Z')))
            {
                motivo = $"Caractere inválido na posição {i + 1}: '{c}'.";
                return false;
            }
        }

        if (!char.IsAsciiDigit(normalizado[12]) || !char.IsAsciiDigit(normalizado[13]))
        {
            motivo = "Os dois últimos caracteres (dígitos verificadores) devem ser números.";
            return false;
        }

        if (normalizado.Distinct().Count() == 1)
        {
            motivo = "CNPJ com todos os caracteres iguais é inválido.";
            return false;
        }

        var dv1 = CalcularDigito(normalizado.AsSpan(0, 12), Pesos1);
        var dv2 = CalcularDigito(normalizado.AsSpan(0, 13), Pesos2);

        if (normalizado[12] - '0' != dv1 || normalizado[13] - '0' != dv2)
        {
            motivo = "Os dígitos verificadores não conferem.";
            return false;
        }

        return true;
    }

    private static int CalcularDigito(ReadOnlySpan<char> base_, int[] pesos)
    {
        var soma = 0;
        for (var i = 0; i < base_.Length; i++)
        {
            // Regra oficial: valor do caractere = código ASCII - 48
            // ('0'..'9' => 0..9, 'A' => 17, 'B' => 18, ...)
            soma += (base_[i] - 48) * pesos[i];
        }

        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }

    public static string Formatar(string normalizado) =>
        normalizado.Length == 14
            ? $"{normalizado[..2]}.{normalizado[2..5]}.{normalizado[5..8]}/{normalizado[8..12]}-{normalizado[12..]}"
            : normalizado;
}
