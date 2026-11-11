using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace McpBrasil.Services;

/// <summary>
/// Cliente da BrasilAPI (https://brasilapi.com.br), gratuita e sem chave.
/// Devolve um resumo enxuto dos dados: o modelo não precisa de 80 campos,
/// e respostas menores significam menos tokens, menos custo e menos confusão.
/// </summary>
public sealed class BrasilApiClient(HttpClient http)
{
    public async Task<ResultadoConsulta> ConsultarCnpjAsync(string cnpjNormalizado, CancellationToken ct)
    {
        using var resposta = await http.GetAsync($"cnpj/v1/{cnpjNormalizado}", ct);

        if (resposta.StatusCode == HttpStatusCode.NotFound)
            return ResultadoConsulta.Falha("CNPJ não encontrado na base da Receita Federal.");

        if (resposta.StatusCode == HttpStatusCode.TooManyRequests)
            return ResultadoConsulta.Falha("Limite de consultas da BrasilAPI atingido. Aguarde alguns segundos e tente de novo.");

        if (!resposta.IsSuccessStatusCode)
            return ResultadoConsulta.Falha($"A BrasilAPI respondeu com erro {(int)resposta.StatusCode}. Tente novamente mais tarde.");

        await using var stream = await resposta.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var raiz = doc.RootElement;

        var resumo = new JsonObject
        {
            ["cnpj"] = CnpjValidator.Formatar(cnpjNormalizado),
            ["razao_social"] = Texto(raiz, "razao_social"),
            ["nome_fantasia"] = Texto(raiz, "nome_fantasia"),
            ["situacao_cadastral"] = Texto(raiz, "descricao_situacao_cadastral"),
            ["data_situacao_cadastral"] = Texto(raiz, "data_situacao_cadastral"),
            ["data_inicio_atividade"] = Texto(raiz, "data_inicio_atividade"),
            ["natureza_juridica"] = Texto(raiz, "natureza_juridica"),
            ["porte"] = Texto(raiz, "porte"),
            ["atividade_principal"] = Texto(raiz, "cnae_fiscal_descricao"),
            ["endereco"] = new JsonObject
            {
                ["logradouro"] = Juntar(Texto(raiz, "descricao_tipo_de_logradouro"), Texto(raiz, "logradouro")),
                ["numero"] = Texto(raiz, "numero"),
                ["complemento"] = Texto(raiz, "complemento"),
                ["bairro"] = Texto(raiz, "bairro"),
                ["municipio"] = Texto(raiz, "municipio"),
                ["uf"] = Texto(raiz, "uf"),
                ["cep"] = Texto(raiz, "cep"),
            },
            ["socios"] = Socios(raiz),
        };

        return ResultadoConsulta.Ok(resumo.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    private static string? Texto(JsonElement e, string campo)
    {
        if (!e.TryGetProperty(campo, out var v) || v.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return null;

        var texto = v.ValueKind == JsonValueKind.String ? v.GetString() : v.ToString();
        texto = texto?.Trim();
        return string.IsNullOrEmpty(texto) ? null : texto;
    }

    private static string? Juntar(params string?[] partes)
    {
        var texto = string.Join(' ', partes.Where(p => !string.IsNullOrWhiteSpace(p)));
        return texto.Length > 0 ? texto : null;
    }

    private static JsonArray Socios(JsonElement raiz)
    {
        var lista = new JsonArray();
        if (raiz.TryGetProperty("qsa", out var qsa) && qsa.ValueKind == JsonValueKind.Array)
        {
            foreach (var s in qsa.EnumerateArray())
            {
                lista.Add(new JsonObject
                {
                    ["nome"] = Texto(s, "nome_socio"),
                    ["qualificacao"] = Texto(s, "qualificacao_socio"),
                });
            }
        }
        return lista;
    }
}

public sealed record ResultadoConsulta(bool Sucesso, string Conteudo)
{
    public static ResultadoConsulta Ok(string json) => new(true, json);
    public static ResultadoConsulta Falha(string mensagem) => new(false, mensagem);
}
