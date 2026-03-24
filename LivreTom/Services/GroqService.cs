using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace LivreTom.Services;

public class GroqService(IConfiguration configuration, HttpClient httpClient, ILogger<GroqService> logger)
{
    private const string ApiUrl = "https://api.groq.com/openai/v1/chat/completions";

    private const string SystemPrompt = """
        Você é um especialista em criação de músicas personalizadas no Suno AI.
        A partir das informações fornecidas, crie um script completo e otimizado para gerar uma música no Suno.

        O script deve conter EXATAMENTE nesta ordem e formatação:

        Style of Music: [gênero musical, mood, instrumentos e andamento em inglês, máximo 120 caracteres]

        Title: [título criativo da música em português]

        Lyrics:
        [Verse 1]
        (letra do primeiro verso)

        [Chorus]
        (letra do refrão)

        [Verse 2]
        (letra do segundo verso)

        [Chorus]
        (letra do refrão)

        [Bridge]
        (letra da ponte)

        [Outro]
        (finalização)

        Regras:
        - A letra deve ser em português brasileiro
        - Use as informações fornecidas para personalizar ao máximo
        - O Style of Music deve ser em inglês e otimizado para o Suno
        - Retorne APENAS o script, sem explicações ou comentários adicionais
        """;

    public async Task<string?> GenerateSunoScriptAsync(string category, IEnumerable<KeyValuePair<string, string>> answers)
    {
        try
        {
            var apiKey = configuration["Groq:ApiKey"];
            var model = configuration["Groq:Model"] ?? "llama-3.3-70b-versatile";

            if (string.IsNullOrEmpty(apiKey))
            {
                logger.LogWarning("[GroqService] ApiKey não configurada em Groq:ApiKey.");
                return null;
            }

            var answerLines = answers
                .Where(a => !string.IsNullOrWhiteSpace(a.Value))
                .Select(a => $"- {a.Key}: {a.Value}");

            var userContent = $"""
                Categoria da música: {category}

                Informações fornecidas pelo usuário:
                {string.Join("\n", answerLines)}
                """;

            var requestBody = new
            {
                model,
                max_tokens = 2048,
                messages = new[]
                {
                    new { role = "system", content = SystemPrompt },
                    new { role = "user",   content = userContent  }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            logger.LogInformation("[GroqService] Enviando requisição para o modelo {Model}.", model);

            var response = await httpClient.SendAsync(request);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("[GroqService] API retornou {StatusCode}. Resposta: {Body}", (int)response.StatusCode, responseJson);
                return null;
            }

            using var doc = JsonDocument.Parse(responseJson);
            var result = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            logger.LogInformation("[GroqService] Script gerado com sucesso ({Length} caracteres).", result?.Length ?? 0);

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[GroqService] Exceção ao chamar a API do Groq.");
            return null;
        }
    }
}
