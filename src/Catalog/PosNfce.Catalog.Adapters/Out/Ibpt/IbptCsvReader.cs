using System.Globalization;
using System.Text;
using PosNfce.Catalog.Application.Ports;
using PosNfce.Catalog.Domain.Ibpt;
using PosNfce.Catalog.Domain.Products;
using PosNfce.Catalog.Domain.TaxRules;
using PosNfce.SharedKernel;

namespace PosNfce.Catalog.Adapters.Out.Ibpt;

/// <summary>
/// Reads the "Tabela IBPTax" CSV: semicolon-separated, header
/// <c>codigo;ex;tipo;descricao;nacionalfederal;importadosfederal;estadual;municipal;vigenciainicio;vigenciafim;chave;versao;fonte</c>.
/// Columns are found by header name, so a different column order still works.
/// </summary>
public sealed class IbptCsvReader : IIbptFileReader
{
    private const string NcmType = "0";

    private static readonly string[] RequiredColumns =
    [
        "codigo", "ex", "tipo", "descricao", "nacionalfederal", "importadosfederal",
        "estadual", "municipal", "vigenciainicio", "vigenciafim", "versao", "fonte",
    ];

    public async Task<IbptFile> ReadAsync(Stream file, CancellationToken cancellationToken = default)
    {
        var lines = await ReadLinesAsync(file, cancellationToken);
        if (lines.Count == 0)
        {
            throw new DomainException("O arquivo está vazio.");
        }

        var header = Split(lines[0]).Select(h => h.Trim().ToLowerInvariant()).ToList();
        var missing = RequiredColumns.Where(c => !header.Contains(c)).ToList();
        if (missing.Count > 0)
        {
            throw new DomainException($"O arquivo não segue o layout da tabela IBPT. Colunas que faltam: {string.Join(", ", missing)}.");
        }

        var column = RequiredColumns.ToDictionary(c => c, c => header.IndexOf(c));
        var rates = new List<IbptRate>();
        var skipped = 0;

        for (var i = 1; i < lines.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                continue;
            }

            var fields = Split(lines[i]);
            string Field(string name) => column[name] < fields.Count ? fields[column[name]].Trim() : "";

            var code = Field("codigo");
            if (Field("tipo") != NcmType || code.Length != 8 || !code.All(char.IsAsciiDigit))
            {
                skipped++;
                continue;
            }

            try
            {
                rates.Add(new IbptRate(
                    Ncm.Parse(code),
                    Field("ex"),
                    Field("descricao"),
                    Rate(Field("nacionalfederal"), "nacionalfederal"),
                    Rate(Field("importadosfederal"), "importadosfederal"),
                    Rate(Field("estadual"), "estadual"),
                    Rate(Field("municipal"), "municipal"),
                    Date(Field("vigenciainicio")),
                    Date(Field("vigenciafim")),
                    Field("versao"),
                    Field("fonte")));
            }
            catch (DomainException error)
            {
                throw new DomainException($"Linha {i + 1} do arquivo: {error.Message}");
            }
        }

        return new IbptFile(rates, skipped);
    }

    /// <summary>IBPT has published files in UTF-8 and in Latin-1; invalid UTF-8 means Latin-1.</summary>
    private static async Task<List<string>> ReadLinesAsync(Stream file, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();

        string text;
        try
        {
            text = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            text = Encoding.Latin1.GetString(bytes);
        }

        return text.TrimStart('﻿').Split('\n').Select(l => l.TrimEnd('\r')).ToList();
    }

    /// <summary>Semicolon split that respects double quotes, since descriptions may contain ";".</summary>
    private static List<string> Split(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var quoted = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                if (quoted && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    quoted = !quoted;
                }
            }
            else if (c == ';' && !quoted)
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        fields.Add(current.ToString());
        return fields;
    }

    /// <summary>Rates come as "13.45"; "13,45" is accepted too.</summary>
    private static Percentage Rate(string text, string column)
    {
        var normalized = text.Replace(',', '.');
        if (!decimal.TryParse(normalized, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value))
        {
            throw new DomainException($"valor \"{text}\" inválido na coluna {column}.");
        }

        return Percentage.Of(value, column);
    }

    private static DateOnly Date(string text) =>
        DateOnly.TryParseExact(text, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : throw new DomainException($"data \"{text}\" inválida (esperado dd/mm/aaaa).");
}
