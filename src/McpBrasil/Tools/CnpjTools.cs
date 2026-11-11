using System.ComponentModel;
using McpBrasil.Services;
using ModelContextProtocol.Server;

namespace McpBrasil.Tools;

/// <summary>
/// Ferramentas de CNPJ expostas via MCP.
/// Os textos de [Description] são o que o modelo lê para decidir QUANDO
/// e COMO usar cada ferramenta. Trate-os como documentação para um leitor
/// que só tem acesso a eles.
/// </summary>
[McpServerToolType]
public static class CnpjTools
{
    [McpServerTool(Name = "validar_cnpj", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description(
        "Verifica se um CNPJ é válido pelos dígitos verificadores, sem consultar a Receita Federal. " +
        "É instantâneo e não usa a internet. Aceita CNPJ com ou sem pontuação, " +
        "no formato numérico tradicional ou no novo formato alfanumérico. " +
        "Use antes de consultar_cnpj quando houver dúvida se o número foi digitado corretamente, " +
        "ou para validar listas grandes rapidamente.")]
    public static string ValidarCnpj(
        [Description("CNPJ a validar, com ou sem pontuação. Ex.: 33.000.167/0001-01, 33000167000101 ou 12.ABC.345/01DE-35")]
        string cnpj)
    {
        return CnpjValidator.Validar(cnpj, out var normalizado, out var motivo)
            ? $"Válido: {CnpjValidator.Formatar(normalizado)}" +
              (CnpjValidator.EhAlfanumerico(normalizado) ? " (formato alfanumérico)." : ".")
            : $"Inválido: {motivo}";
    }

    [McpServerTool(Name = "consultar_cnpj", ReadOnly = true, Idempotent = true, OpenWorld = true)]
    [Description(
        "Consulta dados públicos de uma empresa brasileira na base da Receita Federal (via BrasilAPI): " +
        "razão social, nome fantasia, situação cadastral (ativa, baixada, inapta etc.), data de abertura, " +
        "porte, atividade principal (CNAE), endereço completo e quadro de sócios. " +
        "Use quando o usuário quiser saber se uma empresa existe, se está ativa, onde fica ou quem são os sócios. " +
        "Os dados são públicos, mas podem ter alguns dias de atraso em relação à Receita.")]
    public static async Task<string> ConsultarCnpj(
        BrasilApiClient api,
        [Description("CNPJ da empresa, com ou sem pontuação. Ex.: 33.000.167/0001-01")]
        string cnpj,
        CancellationToken cancellationToken)
    {
        // Valida localmente primeiro: evita chamada de rede inútil
        // e dá ao modelo uma mensagem de erro clara para corrigir o pedido.
        if (!CnpjValidator.Validar(cnpj, out var normalizado, out var motivo))
            return $"Não consultei a Receita porque o CNPJ informado é inválido: {motivo}";

        var resultado = await api.ConsultarCnpjAsync(normalizado, cancellationToken);
        return resultado.Conteudo;
    }
}
