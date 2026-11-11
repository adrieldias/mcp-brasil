using McpBrasil.Services;
using McpBrasil.Tools;
using Xunit;

namespace McpBrasil.Tests;

public class CnpjValidatorTests
{
    [Theory]
    [InlineData("33.000.167/0001-01")]   // formatado
    [InlineData("33000167000101")]       // só números
    [InlineData(" 33.000.167 / 0001-01 ")] // com espaços
    [InlineData("12.ABC.345/01DE-35")]   // alfanumérico (exemplo oficial da Receita)
    [InlineData("12abc34501de35")]       // alfanumérico em minúsculas
    public void Aceita_cnpjs_validos(string cnpj)
    {
        Assert.True(CnpjValidator.Validar(cnpj, out _, out var motivo), motivo);
    }

    [Theory]
    [InlineData("33.000.167/0001-02", "dígitos verificadores")]
    [InlineData("12.ABC.345/01DE-36", "dígitos verificadores")]
    [InlineData("3300016700010", "14 caracteres")]
    [InlineData("", "14 caracteres")]
    [InlineData("11111111111111", "todos os caracteres iguais")]
    [InlineData("12.ABC.345/01DE-3X", "devem ser números")]
    public void Rejeita_cnpjs_invalidos_com_motivo(string cnpj, string trechoDoMotivo)
    {
        Assert.False(CnpjValidator.Validar(cnpj, out _, out var motivo));
        Assert.Contains(trechoDoMotivo, motivo);
    }

    [Fact]
    public void Normaliza_e_formata()
    {
        CnpjValidator.Validar("33000167000101", out var normalizado, out _);
        Assert.Equal("33000167000101", normalizado);
        Assert.Equal("33.000.167/0001-01", CnpjValidator.Formatar(normalizado));
    }

    [Fact]
    public void Identifica_formato_alfanumerico()
    {
        Assert.True(CnpjValidator.EhAlfanumerico("12ABC34501DE35"));
        Assert.False(CnpjValidator.EhAlfanumerico("33000167000101"));
    }

    [Fact]
    public void Ferramenta_validar_cnpj_devolve_texto_legivel_para_o_modelo()
    {
        Assert.StartsWith("Válido", CnpjTools.ValidarCnpj("33.000.167/0001-01"));
        Assert.StartsWith("Inválido", CnpjTools.ValidarCnpj("33.000.167/0001-02"));
    }
}
