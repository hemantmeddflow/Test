using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Text;
using System.Text.Json;

public class IndexModel : PageModel
{
    [BindProperty]
    public string ConnectionString { get; set; }

    [BindProperty]
    public string TableName { get; set; }

    [BindProperty]
    public string Format { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            var cmd = new SqlCommand($"SELECT * FROM [{TableName}]", conn);
            cmd.CommandTimeout = 0;

            var reader = await cmd.ExecuteReaderAsync();

            if (Format == "csv")
                return await ExportCsv(reader);

            if (Format == "json")
                return await ExportJson(reader);

            return Content("Invalid format");
        }
        catch (Exception ex)
        {
            return Content(ex.ToString());
        }
    }

    private async Task<IActionResult> ExportCsv(SqlDataReader reader)
    {
        Response.ContentType = "text/csv";
        Response.Headers.Add("Content-Disposition", $"attachment; filename=data.csv");

        using var writer = new StreamWriter(Response.Body, Encoding.UTF8);

        // Headers
        for (int i = 0; i < reader.FieldCount; i++)
        {
            await writer.WriteAsync(reader.GetName(i));

            if (i < reader.FieldCount - 1)
                await writer.WriteAsync(",");
        }

        await writer.WriteLineAsync();

        // Rows
        while (await reader.ReadAsync())
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var value = reader[i]?.ToString()?.Replace(",", " ");
                await writer.WriteAsync(value);

                if (i < reader.FieldCount - 1)
                    await writer.WriteAsync(",");
            }

            await writer.WriteLineAsync();
        }

        await writer.FlushAsync();

        return new EmptyResult();
    }

    private async Task<IActionResult> ExportJson(SqlDataReader reader)
    {
        Response.ContentType = "application/json";
        Response.Headers.Add("Content-Disposition", $"attachment; filename=data.json");

        await using var jsonWriter = new Utf8JsonWriter(Response.Body);

        jsonWriter.WriteStartArray();

        while (await reader.ReadAsync())
        {
            jsonWriter.WriteStartObject();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                jsonWriter.WriteString(reader.GetName(i), reader[i]?.ToString());
            }

            jsonWriter.WriteEndObject();
        }

        jsonWriter.WriteEndArray();

        await jsonWriter.FlushAsync();

        return new EmptyResult();
    }
}