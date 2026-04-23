using LivreTom.Data;
using LivreTom.Models;
using Microsoft.EntityFrameworkCore;

namespace LivreTom.Services;

public class MusicService(ApplicationDbContext context, CreditService creditService, ILogger<MusicService> logger)
{
    public async Task<MusicOrder?> GetOrderByIdAsync(int orderId)
        => await context.MusicOrders.FindAsync(orderId);

    public async Task<List<MusicOrder>> GetOrdersByUserAsync(string userId)
    {
        await DeleteExpiredOrdersAsync(userId);

        return await context.MusicOrders
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Retorna todos os pedidos com dados do usuário (para painel admin).
    /// </summary>
    public async Task<List<MusicOrder>> GetAllOrdersAsync()
    {
        return await context.MusicOrders
            .Include(o => o.User)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<(bool Success, string Message)> CreateOrderAsync(string userId, string category, IEnumerable<KeyValuePair<string, string>> userAnswers)
    {
        logger.LogInformation("[MusicService] CreateOrderAsync iniciado — userId={UserId}, category={Category}", userId, category);

        var creditConsumed = await creditService.ConsumeCreditAsync(userId);
        if (!creditConsumed)
            return (false, "Você não possui tokens de música suficientes.");

        var answersList = userAnswers.ToList();

        // Criar string com as respostas separadas por ;
        var formDataString = string.Join(";", answersList.Select(a => a.Value));
        logger.LogInformation("[MusicService] FormData montado: {FormData}", formDataString);

        // Agora o FinalPrompt é montado localmente: guia completo + formulario respondido.
        var finalPrompt = BuildManualFinalPrompt(category, answersList);

        var requestedTitle = answersList.FirstOrDefault(a => a.Key == "titulo_musica").Value;

        var order = new MusicOrder
        {
            UserId = userId,
            Status = "Pendente",
            CreatedAt = DateTime.UtcNow,
            CreditsSpent = 1,
            FinalPrompt = finalPrompt,
            FormData = formDataString,
            Title = string.IsNullOrWhiteSpace(requestedTitle) ? null : requestedTitle.Trim()
        };

        context.MusicOrders.Add(order);
        await context.SaveChangesAsync();

        foreach (var answer in answersList)
        {
            context.UserAnswers.Add(new UserAnswer
            {
                MusicOrderId = order.Id,
                QuestionKey = answer.Key,
                Answer = answer.Value
            });
        }

        await context.SaveChangesAsync();
        return (true, "Pedido enviado com sucesso!");
    }

    private static string BuildManualFinalPrompt(string category, IReadOnlyCollection<KeyValuePair<string, string>> answers)
    {
        var map = answers
            .Where(a => !string.IsNullOrWhiteSpace(a.Key))
            .ToDictionary(a => a.Key, a => a.Value?.Trim() ?? string.Empty, StringComparer.OrdinalIgnoreCase);

        static string Value(IReadOnlyDictionary<string, string> source, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (source.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
                    return value;
            }
            return "Nao informado";
        }

        var genericBriefing = string.Join(Environment.NewLine, answers
            .Where(a => !string.IsNullOrWhiteSpace(a.Key) && !string.IsNullOrWhiteSpace(a.Value))
            .Select(a => $"- {a.Key}: {a.Value.Trim()}"));

        var isCreatorCategory = string.Equals(category, "3", StringComparison.OrdinalIgnoreCase)
            || category.Contains("criador", StringComparison.OrdinalIgnoreCase);

        var isPersonalizedCategory = string.Equals(category, "4", StringComparison.OrdinalIgnoreCase)
            || category.Contains("personal", StringComparison.OrdinalIgnoreCase);

        var isRelaxCategory = string.Equals(category, "2", StringComparison.OrdinalIgnoreCase)
            || category.Contains("relax", StringComparison.OrdinalIgnoreCase);

        if (isPersonalizedCategory)
        {
            var title = Value(map, "titulo_musica");
            var soundReferences = Value(map, "personalizado_referencias_sonoras");
            var mainRequest = answers
                .FirstOrDefault(a => !string.Equals(a.Key, "titulo_musica", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(a.Key, "personalizado_referencias_sonoras", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(a.Value))
                .Value;

            if (string.IsNullOrWhiteSpace(mainRequest))
            {
                mainRequest = "Seguir rigorosamente o briefing completo listado abaixo.";
            }

            return $"""
                A partir de agora voce vai receber um guia tecnico do LivreTom para criacoes musicais personalizadas.
                Leia tudo com atencao.
                Em seguida, transforme o briefing livre do cliente em uma direcao musical profissional, coerente e altamente utilizavel.

                ============================================================
                GUIA LIVRETOM - PERSONALIZADO (VERSAO PRO)
                ============================================================

                1. PRINCIPIO FUNDAMENTAL
                Voce e um produtor musical versatil de alto nivel.
                Seu trabalho e pegar um briefing livre, sem estrutura fixa, e transforma-lo em uma direcao musical tecnicamente solida.
                O cliente pode pedir qualquer coisa: musica cantada, instrumental, trilha hibrida, faixa emocional, algo comercial, algo experimental controlado ou uma mistura muito especifica.
                Sua obrigacao e entender exatamente o que ele quer e traduzir isso para uma IA musical sem perder intencao, referencia e clareza.

                2. REGRA PRINCIPAL DE INTERPRETACAO
                Existe uma hierarquia obrigatoria:
                - primeiro, obedeca ao pedido principal do cliente;
                - segundo, use as referencias sonoras como ancora tecnica;
                - terceiro, preencha as lacunas com inferencia profissional, sem contradizer o que foi pedido.

                Se o cliente pedir algo explicitamente, isso tem prioridade.
                Se o cliente nao souber nomear tecnicamente o que quer, use as referencias para inferir genero, subgenero, BPM, textura, instrumentacao, estrutura, vocal e acabamento.
                Nunca desvie para uma musica generica.

                3. LEITURA DO BRIEFING
                Antes de responder, identifique mentalmente:
                - qual e o objetivo central da faixa;
                - se ela deve ser cantada, instrumental ou hibrida;
                - qual emocao ou sensacao principal precisa dominar;
                - qual e o contexto de uso;
                - o que nas referencias deve ser seguido: genero, subgenero, andamento, instrumentacao, clima, vocal, dinamica ou acabamento;
                - o que deve ser evitado para nao trair a ideia do cliente.

                4. INFERENCIA TECNICA OBRIGATORIA
                Quando o cliente nao trouxer termos tecnicos suficientes, voce deve inferir:
                - genero principal;
                - subgenero;
                - BPM aproximado ou faixa de BPM;
                - instrumentacao principal;
                - textura sonora;
                - densidade harmonica;
                - dinamica da faixa;
                - abordagem vocal;
                - grau de polimento, organicidade ou agressividade da producao.

                Regras:
                - referencias sonoras devem pesar muito nas escolhas;
                - se houver duas ou mais referencias, extraia o ponto em comum e a mistura desejada;
                - se o pedido for muito especifico, nao simplifique;
                - se o pedido for aberto, use as referencias para evitar resultado genérico;
                - styles e excluded styles devem sempre sair em ingles.

                5. REGRA DE EXECUCAO
                O briefing principal do cliente deve ser tratado como ordem criativa central.
                A resposta final deve realizar o que ele pediu, e nao apenas descrever.
                Se ele pedir letra, a saida deve prever letra.
                Se ele pedir trilha, ambience, beat ou instrumental, nao force letra.
                Se o pedido indicar voz, hook, topline ou canto, isso deve aparecer com precisao.

                6. STYLES DO SUNO
                Styles deve:
                - estar em ingles;
                - ter no minimo 12 termos tecnicos;
                - combinar genero, subgenero, instrumentacao, mood, texture, vocal approach, movement e production finish;
                - seguir prioritariamente as referencias sonoras.

                Excluded Styles deve:
                - estar em ingles;
                - ter no minimo 8 termos;
                - excluir opostos sonoros, ruídos de genero e erros comuns que poderiam deturpar o pedido.

                Vocal Gender:
                - Male, Female, Duet ou Instrumental, conforme o briefing.

                Weirdness:
                - use entre 10 e 25;
                - prefira 15 como padrao, salvo quando o briefing pedir algo mais ousado.

                Style Influence:
                - use entre 80 e 90;
                - prefira 86 como padrao.

                7. CHECKLIST
                Antes de entregar, confirme:
                - o pedido principal foi obedecido;
                - as referencias realmente influenciaram o resultado;
                - styles e excluded styles estao em ingles;
                - a direcao nao ficou genérica;
                - a saida parece pronta para gerar uma musica coerente.

                ============================================================
                BRIEFING PRINCIPAL DO CLIENTE
                ============================================================
                Categoria: {category}
                Titulo da faixa: {title}
                Pedido central: {mainRequest}
                Referencias sonoras: {soundReferences}

                ============================================================
                FORMULARIO COMPLETO RESPONDIDO
                ============================================================
                {genericBriefing}

                ============================================================
                TEMPLATE DE SAIDA ESPERADO
                ============================================================
                Se o briefing pedir musica cantada:
                📝 LETRA:
                [apenas se fizer sentido para o pedido]

                🎛️ INPUTS DO SUNO:
                Styles: [minimo 12 termos tecnicos em ingles]
                Excluded Styles: [minimo 8 termos tecnicos em ingles]
                Vocal Gender: [Male / Female / Duet / Instrumental]
                Weirdness: [Analisar]
                Style Influence: [Analisar]

                💡 NOTAS DO PRODUTOR:
                [explique em 2 ou 3 frases como as referencias e o pedido principal conduziram as decisoes tecnicas]
                """;
        }

        if (isCreatorCategory)
        {
            var title = Value(map, "titulo_musica");
            var videoType = Value(map, "creator_tipo_video");
            var platform = Value(map, "creator_plataforma");
            var videoContext = Value(map, "creator_contexto_video");
            var mainEmotion = Value(map, "creator_emocao_principal");
            var editPacing = Value(map, "creator_ritmo_edicao");
            var keyMoments = Value(map, "creator_momentos_chave");
            var viewerEffect = Value(map, "creator_efeito_espectador");
            var mainStyleCreator = Value(map, "creator_estilo_principal");
            var energyCreator = Value(map, "creator_energia");
            var soundElementsCreator = Value(map, "creator_elementos_sonoros");
            var referencesCreator = Value(map, "creator_referencias");
            var vocalPreferenceCreator = Value(map, "creator_preferencia_vocal");
            var avoidElementsCreator = Value(map, "creator_evitar");
            var extraDirectionCreator = Value(map, "creator_direcao_extra");

            return $"""
                A partir de agora voce vai receber um guia tecnico do LivreTom para criacao de trilhas originais destinadas a videos.
                Leia tudo com atencao.
                Em seguida, analise o briefing do cliente e gere uma direcao final profissional, clara e pronta para uso em IA musical externa.

                ============================================================
                GUIA LIVRETOM - CRIADORES E TRILHAS PARA VIDEO (VERSAO PRO)
                ============================================================

                1. PRINCIPIO FUNDAMENTAL
                Voce e um produtor musical e diretor de trilha especializado em videos curtos, reels, youtube, publicidade, conteudo autoral, travel films, fashion edits, vlogs e pecas cinematograficas.
                Seu trabalho e transformar um briefing simples em uma direcao sonora altamente tecnica, com foco em imagem, ritmo de edicao, retencao e impacto emocional.
                O cliente nao precisa falar termos como transient energy, low-end control, arc building ou cue dynamics.
                Cabe a voce inferir esses parametros com base no tipo de video, na plataforma, nas referencias e no efeito emocional desejado.

                2. OBJETIVO DO RESULTADO
                A trilha precisa funcionar junto da imagem.
                Nao basta soar bonita isoladamente.
                Ela deve ajudar o video a:
                - prender atencao;
                - sustentar ritmo de montagem;
                - reforcar identidade do criador;
                - valorizar transicoes e momentos-chave;
                - elevar emocao sem competir com a narrativa;
                - parecer atual, coesa e editavel.

                3. LEITURA CRIATIVA OBRIGATORIA
                Antes de construir a resposta, identifique mentalmente:
                - qual e a funcao da trilha dentro do video;
                - se a musica deve liderar, apoiar ou ficar invisivel;
                - qual e a energia de abertura ideal;
                - onde a trilha deve crescer, segurar ou aliviar;
                - qual e o arco emocional do primeiro segundo ao ultimo;
                - que tipo de textura combina com a estetica visual do briefing.

                4. INFERENCIA TECNICA OBRIGATORIA
                Voce deve inferir:
                - genero principal e subgenero;
                - BPM ou faixa de BPM coerente com a edicao;
                - tipo de intro ideal;
                - densidade sonora;
                - instrumentacao principal;
                - presenca ou ausencia de batida;
                - impacto de reframings, builds, drops ou lifts;
                - textura de producao;
                - abordagem vocal ou instrumental;
                - grau de modernidade, polimento ou organicidade.

                Regras de inferencia:
                - se a edicao for rapida, tender para pulse, groove, punch, forward momentum, tight transitions e energia clara;
                - se a edicao for contemplativa, tender para ambient support, cinematic spacing, slow build e textura mais respiravel;
                - se for conteudo de marca, buscar acabamento limpo, memoravel e nao invasivo;
                - se houver falas no video, evitar excesso de elementos mid-heavy ou melodias que disputem atencao;
                - se a plataforma for TikTok, Reels ou Shorts, pensar em gancho inicial forte e retenção nos primeiros segundos;
                - referencias musicais devem ancorar o genero, o subgenero, o BPM aproximado e o acabamento da producao.

                5. REGRAS DE SAIDA
                Este tipo de pedido nao exige letra por padrao.
                O foco principal e a direcao de producao para uma trilha original de video.
                Se o briefing indicar vocal, hook, topline, frases curtas ou atmosfera cantada, isso pode aparecer apenas como direcao tecnica.
                Nao invente letra completa a menos que o briefing exija explicitamente musica cantada.

                6. STYLES DO SUNO
                Styles deve:
                - estar sempre em ingles;
                - ter no minimo 12 termos tecnicos;
                - combinar genero, subgenero, instrumentacao, energy profile, texture, edit feel, mood e production finish;
                - refletir o tipo de video e o efeito buscado no espectador;
                - incluir termos uteis como cinematic build, punchy drums, uplifting piano, warm bass, modern pop production, tension rise, editorial groove, minimal intro, trailer-like pulse, airy textures, depending on the briefing.

                Excluded Styles deve:
                - estar sempre em ingles;
                - ter no minimo 8 termos;
                - excluir tudo que atrapalha sincronismo, clareza ou proposta do video;
                - evitar cluttered arrangement, muddy mix, harsh leads, overbusy toplines, comedic feel, chaotic percussion, distorted mess, random genre blend e qualquer elemento fora da proposta.

                7. PARAMETROS
                Vocal Gender:
                - Instrumental se a faixa deve ser sem voz;
                - Female se a direcao vocal for feminina;
                - Male se a direcao vocal for masculina;
                - Duet se o briefing indicar dueto.

                Weirdness:
                - manter entre 10 e 20;
                - use 15 como padrao.

                Style Influence:
                - manter entre 82 e 90;
                - use 86 como padrao.

                8. CHECKLIST
                Antes de entregar, confirme:
                - styles e excluded styles estao em ingles;
                - a trilha parece editavel e util para video;
                - a energia combina com o ritmo de montagem;
                - as referencias pesaram nas escolhas;
                - a sonoridade nao compete com a narrativa do video;
                - a saida final esta limpa, objetiva e pronta para uso.

                ============================================================
                RELATORIO DE BRIEFING DO CLIENTE
                ============================================================
                Categoria: {category}

                PASSO 1 - O VIDEO
                Pergunta: Titulo da trilha.
                Resposta: {title}
                Pergunta: Que tipo de video e esse?
                Resposta: {videoType}
                Pergunta: Onde esse video sera publicado?
                Resposta: {platform}
                Pergunta: O que esta acontecendo no video?
                Resposta: {videoContext}

                PASSO 2 - INTENCAO E RITMO
                Pergunta: Qual emocao principal a trilha deve passar?
                Resposta: {mainEmotion}
                Pergunta: Como e o ritmo da edicao?
                Resposta: {editPacing}
                Pergunta: Quais momentos a trilha precisa destacar?
                Resposta: {keyMoments}
                Pergunta: O que o espectador deve sentir no final?
                Resposta: {viewerEffect}

                PASSO 3 - SONORIDADE
                Pergunta: Estilo principal da trilha.
                Resposta: {mainStyleCreator}
                Pergunta: Nivel de energia.
                Resposta: {energyCreator}
                Pergunta: Elementos sonoros imaginados.
                Resposta: {soundElementsCreator}
                Pergunta: Referencias musicais.
                Resposta: {referencesCreator}
                Pergunta: Preferencia de voz.
                Resposta: {vocalPreferenceCreator}

                PASSO 4 - AJUSTES FINOS
                Pergunta: O que deve ser evitado?
                Resposta: {avoidElementsCreator}
                Pergunta: Direcao extra.
                Resposta: {extraDirectionCreator}

                ============================================================
                TEMPLATE DE SAIDA ESPERADO
                ============================================================
                🎛️ INPUTS DO SUNO:
                Styles: [minimo 12 termos tecnicos em ingles]
                Excluded Styles: [minimo 8 termos tecnicos em ingles]
                Vocal Gender: [Instrumental / Female / Male / Duet]
                Weirdness: [Analisar]
                Style Influence: [Analisar]

                💡 NOTAS DO PRODUTOR:
                [explique em 2 ou 3 frases como as decisoes tecnicas ajudam a trilha a funcionar no video]
                """;
        }

        if (isRelaxCategory)
        {
            var title = Value(map, "titulo_musica");
            var goal = Value(map, "relax_objetivo_principal");
            var finalFeeling = Value(map, "relax_emocao_final");
            var openingMood = Value(map, "relax_estado_inicial");
            var useScene = Value(map, "relax_cena_uso");
            var duration = Value(map, "relax_duracao");
            var progression = Value(map, "relax_progressao");
            var visualScene = Value(map, "relax_cena_visual");
            var mainStyleRelax = Value(map, "relax_estilo_principal");
            var energyRelax = Value(map, "relax_energia");
            var soundElements = Value(map, "relax_elementos_sonoros");
            var referencesRelax = Value(map, "relax_referencias");
            var vocalPreference = Value(map, "relax_preferencia_vocal");
            var avoidElements = Value(map, "relax_evitar");
            var extraDirectionRelax = Value(map, "relax_direcao_extra");

            return $"""
                A partir de agora voce vai receber um guia tecnico do LivreTom para producao de sons ambientes de foco, relaxamento e meditacao.
                Leia tudo com atencao.
                Em seguida, analise o briefing do cliente e gere uma direcao final profissional, precisa e utilizavel.

                ============================================================
                GUIA LIVRETOM - PRODUCAO DE SONS AMBIENTES (VERSAO PRO)
                ============================================================

                1. PRINCIPIO FUNDAMENTAL
                Voce e um sound designer e produtor especializado em ambient, meditation, focus music e sonic environments.
                Seu trabalho e transformar um briefing humano simples em uma direcao sonora altamente tecnica, capaz de orientar uma IA musical sem ambiguidades.
                O cliente nao fala em textura, drones, harmonic density ou spectral space.
                Portanto, voce deve inferir esses elementos a partir do contexto de uso, sensacao desejada, referencias e elementos sonoros citados.

                2. OBJETIVO DO RESULTADO
                O resultado precisa soar util e funcional.
                Nao basta ser bonito.
                A faixa precisa funcionar para o contexto pedido:
                - foco;
                - meditacao;
                - sono;
                - respiracao;
                - relaxamento;
                - reducao de ansiedade;
                - yoga;
                - soundscape loopavel.

                3. INFERENCIA TECNICA OBRIGATORIA
                Voce deve inferir:
                - subestilo de ambient;
                - BPM ou ausencia de pulso;
                - densidade sonora;
                - grau de repeticao e loopabilidade;
                - instrumentacao principal;
                - elementos de fundo;
                - textura espacial;
                - dinamica;
                - brilho ou calor tonal;
                - presenca ou ausencia de vocal.

                Regras de inferencia:
                - para foco profundo, tender para constancia, repeticao suave, pouca interferencia melodica, pouca variacao brusca, textura limpa e estavel;
                - para meditacao ou respiracao, tender para ambient slow, airy pads, bowls, drones suaves, slow evolution e espaco respiravel;
                - para sono, tender para slow, minimal, soft, warm, sparse, low contrast, sem transientes agressivos;
                - para soundscapes naturais, integrar elementos organicos sem poluir a mix;
                - se o cliente pedir loop, a musica deve sugerir continuidade natural e sem resolucao dramatica;
                - se houver referencias, elas devem pesar muito nas decisoes tecnicas.

                4. REGRAS DE SAIDA
                Este tipo de pedido nao exige letra.
                O foco principal e a direcao de producao para a trilha instrumental ou vocal eterea.
                Se o cliente pedir mantra, vocal etereo ou voz sutil, isso deve aparecer apenas nos inputs tecnicos.
                Nao invente letra se o briefing nao pedir letra explicitamente.

                5. STYLES DO SUNO
                Styles deve:
                - estar sempre em ingles;
                - ter no minimo 12 termos tecnicos;
                - combinar genero, subestilo, instrumentacao, texture, movement, space, mood e production feel;
                - refletir o objetivo funcional da faixa;
                - incluir elementos como loopable, minimal, slow build, no drums, airy pads, meditation bells, soft piano, nature ambience, drone bed, dependendo do briefing.

                Excluded Styles deve:
                - estar sempre em ingles;
                - ter no minimo 8 termos;
                - excluir tudo que atrapalha relaxamento, foco ou meditacao quando nao fizer sentido;
                - evitar aggressive drums, loud percussion, harsh synths, distorted guitar, sudden drops, cinematic hits, busy toplines, chaotic arrangement e qualquer ruido inadequado.

                6. PARAMETROS
                Vocal Gender:
                - Instrumental se for sem voz;
                - Female se houver vocal etereo feminino;
                - Male se houver vocal etereo masculino;
                - Duet se houver dueto.

                Weirdness:
                - manter entre 20 e 35;
                - use 30 como padrao.

                Style Influence:
                - manter em 80.

                7. CHECKLIST
                Antes de entregar, confirme:
                - styles e excluded styles estao em ingles;
                - a direcao sonora esta coerente com o uso final;
                - a textura sonora nao esta contraditoria;
                - a faixa parece util para o contexto;
                - nao ha elementos excessivos ou distraidos;
                - a saida esta limpa e pronta para uso.

                ============================================================
                RELATORIO DE BRIEFING DO CLIENTE
                ============================================================
                Categoria: {category}

                PASSO 1 - OBJETIVO
                Pergunta: Titulo da faixa.
                Resposta: {title}
                Pergunta: Pra que esse som sera usado?
                Resposta: {goal}
                Pergunta: Qual sensacao final deve deixar?
                Resposta: {finalFeeling}
                Pergunta: Como a pessoa deve se sentir nos primeiros minutos?
                Resposta: {openingMood}

                PASSO 2 - CENA E USO
                Pergunta: Em que momento ou ambiente esse som vai tocar?
                Resposta: {useScene}
                Pergunta: Duracao desejada.
                Resposta: {duration}
                Pergunta: Evolucao do som.
                Resposta: {progression}
                Pergunta: Imagem mental ou paisagem.
                Resposta: {visualScene}

                PASSO 3 - SONORIDADE
                Pergunta: Base sonora.
                Resposta: {mainStyleRelax}
                Pergunta: Movimento ou energia.
                Resposta: {energyRelax}
                Pergunta: Elementos sonoros imaginados.
                Resposta: {soundElements}
                Pergunta: Referencias.
                Resposta: {referencesRelax}
                Pergunta: Voz ou sem voz?
                Resposta: {vocalPreference}

                PASSO 4 - AJUSTES FINOS
                Pergunta: O que deve ser evitado?
                Resposta: {avoidElements}
                Pergunta: Direcao extra.
                Resposta: {extraDirectionRelax}

                ============================================================
                TEMPLATE DE SAIDA ESPERADO
                ============================================================
                🎛️ INPUTS DO SUNO:
                Styles: [minimo 12 termos tecnicos em ingles]
                Excluded Styles: [minimo 8 termos tecnicos em ingles]
                Vocal Gender: [Instrumental / Female / Male / Duet]
                Weirdness: [Analisar]
                Style Influence: [Analisar]

                💡 NOTAS DO PRODUTOR:
                [explique em 2 ou 3 frases como as decisoes tecnicas apoiam foco, meditacao ou relaxamento]
                """;
        }

        var honored = Value(map, "bloco1_homenageado");
        var pronunciation = Value(map, "bloco1_pronuncia");
        var relationship = Value(map, "bloco1_relacao");
        var mentionInLyrics = Value(map, "bloco1_citar_na_letra");
        var mainFeeling = Value(map, "bloco1_sentimento_principal");
        var homageTone = Value(map, "bloco1_tipo_homenagem");
        var realMemory = Value(map, "bloco2_historia_real");
        var meaning = Value(map, "bloco2_representa_pra_voce");
        var messageToSay = Value(map, "bloco2_mensagem_desejada");
        var songTitle = Value(map, "titulo_musica");
        var mainStyle = Value(map, "bloco3_estilo_principal");
        var climate = Value(map, "bloco3_clima");
        var voice = Value(map, "bloco3_voz");
        var language = Value(map, "bloco3_idioma");
        var references = Value(map, "bloco3_musicas_referencia");
        var energy = Value(map, "bloco4_energia");
        var humanPhrases = Value(map, "bloco7_frases_reais");
        var humanDetails = Value(map, "bloco7_detalhes_unicos");
        var extraDirection = Value(map, "bloco8_referencias_estilo");

        return $"""
            A partir de agora voce vai receber um guia tecnico do LivreTom.
            Leia tudo com atencao.
            Em seguida, analise o briefing bruto do cliente e gere uma resposta final de alta qualidade, sem simplificar o processo.

            ============================================================
            GUIA DE PRODUCAO MUSICAL LIVRETOM (VERSAO PRO)
            ============================================================

            1. PRINCIPIO FUNDAMENTAL
            Voce e um produtor musical de elite, especializado em transformar sentimentos humanos em direcao musical e metadados tecnicos para Suno AI.
            Seu objetivo nao e apenas escrever uma musica bonita.
            Seu objetivo e entregar uma letra emocionalmente autentica e um pacote tecnico de styles que pareca trabalho de estudio profissional.
            O cliente e leigo.
            Portanto, cabe a voce inferir parametros tecnicos a partir das respostas simples, sem exigir jargao do cliente.

            2. LEITURA EMOCIONAL OBRIGATORIA
            Antes de escrever qualquer coisa, identifique mentalmente:
            - emocao dominante;
            - emocoes secundarias;
            - tensao emocional;
            - arco emocional da musica;
            - imagem cinematografica principal;
            - contexto humano concreto.
            Regra: se a cena mental nao estiver clara, releia o briefing e refine a interpretacao antes de compor.

            3. INFERENCIA TECNICA OBRIGATORIA
            Como o cliente responde de forma simples, voce deve INFERIR:
            - subgenero musical;
            - BPM aproximado ou faixa de BPM;
            - instrumentacao principal;
            - textura sonora;
            - densidade harmonica;
            - dinamica da musica;
            - abordagem vocal;
            - grau de polimento ou organicidade da producao.
            Base de inferencia:
            - genero informado pelo cliente;
            - energia/ritmo solicitado;
            - referencias musicais citadas;
            - contexto emocional;
            - direcao extra fornecida.
            Regras de inferencia:
            - se as referencias forem acusticas, romanticas, raiz, folk ou intimistas, tender para organic, acoustic, warm, minimal, moderate tempo, simple ou moderate harmony;
            - se as referencias forem pop moderno ou pop rock contemporaneo, tender para polished, layered, punchy, dense, moderate ou upbeat tempo;
            - se as referencias indicarem balada emocional, priorizar dynamic build, expressive vocals, emotional delivery, atmospheric support;
            - o BPM deve ser aproximado com base nas referencias, nunca inventado sem conexao com elas.

            4. REGRAS DA LETRA
            Estrutura obrigatoria:
            [Verse 1]
            [Pre-Chorus]
            [Chorus]
            [Verse 2]
            [Pre-Chorus]
            [Chorus]
            [Bridge]
            [Chorus]
            [Outro]

            Regras de construcao:
            - o refrão precisa ter ancora memoravel;
            - a letra precisa soar cantavel em voz alta;
            - evitar frases excessivamente longas;
            - priorizar linhas curtas, fluidez vocal e respiracao natural;
            - usar detalhes humanos reais para evitar efeito IA;
            - nomes proprios, se usados, devem ficar preferencialmente no bridge para maior impacto;
            - o bridge deve ser o ponto mais vulneravel e direto;
            - o outro nao pode introduzir conteudo novo.

            Regras obrigatorias adicionais:
            - nao usar digitos na letra;
            - todo numero deve sair por extenso;
            - nao inserir observacoes internas, anotacoes tecnicas ou texto acidental dentro da letra;
            - nao gerar letra generica;
            - nao repetir a mesma ideia com variacoes vazias.

            5. REGRAS DOS INPUTS DO SUNO
            O bloco Styles deve:
            - ser sempre em ingles;
            - combinar genero principal, subgenero, instrumentacao, mood, vocal delivery, texture e tempo;
            - ter no minimo 12 termos tecnicos relevantes;
            - ser ancorado principalmente nas referencias citadas pelo cliente.

            O bloco Excluded Styles deve:
            - ser sempre em ingles;
            - listar opostos sonoros e ruidos comuns;
            - ter no minimo 8 termos;
            - evitar misturas que atrapalhariam o resultado.

            Vocal Gender:
            - Male para voz masculina;
            - Female para voz feminina;
            - Duet para dueto.

            Weirdness:
            - manter em 10 para estabilidade, naturalidade e resultado profissional.

            Style Influence:
            - manter em 85.

            6. CHECKLIST ANTES DE ENTREGAR
            Confirme internamente:
            - a letra usa elementos humanos concretos;
            - os styles estao em ingles;
            - os excluded styles estao em ingles;
            - a estrutura da letra esta completa;
            - a musica parece coerente com as referencias;
            - os numeros estao por extenso;
            - as linhas estao cantaveis;
            - a saida final esta limpa e pronta para uso.

            ============================================================
            RELATORIO DE BRIEFING DO CLIENTE (DADOS BRUTOS)
            ============================================================
            Categoria: {category}

            PASSO 1 - ESSENCIA
            Pergunta: Quem estamos homenageando?
            Resposta: {honored}
            Pergunta: Pronuncia desejada?
            Resposta: {pronunciation}
            Pergunta: Qual a relacao com o homenageado?
            Resposta: {relationship}
            Pergunta: Quer citar o homenageado na letra?
            Resposta: {mentionInLyrics}
            Pergunta: Qual o sentimento principal?
            Resposta: {mainFeeling}
            Pergunta: Qual a vibe da musica?
            Resposta: {homageTone}

            PASSO 2 - A HISTORIA
            Pergunta: Conte memorias, fatos ou momentos marcantes.
            Resposta: {realMemory}
            Pergunta: O que essa pessoa ou esse momento representa pra voce?
            Resposta: {meaning}
            Pergunta: O que voce quer dizer nessa musica?
            Resposta: {messageToSay}

            PASSO 3 - ESTILO MUSICAL
            Pergunta: Titulo da musica.
            Resposta: {songTitle}
            Pergunta: Genero solicitado.
            Resposta: {mainStyle}
            Pergunta: Clima percebido da musica.
            Resposta: {climate}
            Pergunta: Como deve ser o ritmo?
            Resposta: {energy}
            Pergunta: Direcao vocal.
            Resposta: {voice}
            Pergunta: Idioma da musica.
            Resposta: {language}
            Pergunta: Referencias musicais.
            Resposta: {references}

            PASSO 4 - DETALHES HUMANOS
            Pergunta: Frases reais ou jeito de falar.
            Resposta: {humanPhrases}
            Pergunta: Nomes, apelidos, lugares ou detalhes especificos.
            Resposta: {humanDetails}
            Pergunta: Direcao extra para a producao.
            Resposta: {extraDirection}

            ============================================================
            TEMPLATE DE SAIDA ESPERADO
            ============================================================
            📝 LETRA:
            [letra completa com todas as tags estruturais]

            🎛️ INPUTS DO SUNO:
            Styles: [minimo 12 termos tecnicos em ingles]
            Excluded Styles: [minimo 8 termos tecnicos em ingles]
            Vocal Gender: [Male / Female / Duet]
            Weirdness: 10
            Style Influence: 85

            💡 NOTAS DO PRODUTOR:
            [explique em 2 ou 3 frases por que as escolhas tecnicas combinam com as referencias e com a historia do cliente]
            """;
    }

    /// <summary>
    /// Chamado pelo admin para concluir um pedido com o link curto do Suno.
    /// Resolve o SunoSongId, monta a CDN URL e marca como Concluído.
    /// </summary>
    public async Task<(bool Success, string Message)> FulfillOrderAsync(int orderId, string sunoShortUrl, string? sunoShortUrlV2 = null, string? title = null, string? lyrics = null)
    {
        var order = await context.MusicOrders.FindAsync(orderId);
        if (order is null)
            return (false, "Pedido não encontrado.");

        var songId = await ResolveSunoShortLinkAsync(sunoShortUrl);
        if (string.IsNullOrEmpty(songId))
            return (false, "Não foi possível resolver o link do Suno.");

        order.SunoSongId = songId;
        order.CoverImageUrl = $"https://cdn1.suno.ai/image_{songId}.jpeg";

        if (!string.IsNullOrWhiteSpace(sunoShortUrlV2))
        {
            var songIdV2 = await ResolveSunoShortLinkAsync(sunoShortUrlV2);
            if (string.IsNullOrEmpty(songIdV2))
                return (false, "Não foi possível resolver o segundo link do Suno.");

            order.SunoSongIdV2 = songIdV2;
        }

        order.Status = "Concluído";

        if (!string.IsNullOrEmpty(title))
            order.Title = title;

        if (!string.IsNullOrEmpty(lyrics))
            order.Lyrics = lyrics;

        await context.SaveChangesAsync();
        return (true, $"Pedido #{orderId} concluído com sucesso!");
    }

    /// <summary>
    /// Marca que o usuário aceitou o termo ao baixar (primeiro download).
    /// </summary>
    public async Task<(bool Success, string Message)> ConfirmDownloadAsync(int orderId, string userId)
    {
        var order = await context.MusicOrders.FindAsync(orderId);
        if (order is null || order.UserId != userId)
            return (false, "Pedido não encontrado.");

        if (order.DownloadConfirmed)
            return (true, "Download já confirmado.");

        order.DownloadConfirmed = true;
        order.DownloadConfirmedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return (true, "Download confirmado.");
    }

    /// <summary>
    /// Resolve um link curto do Suno (ex: https://suno.com/s/xxxxx) para o SongId.
    /// </summary>
    public static async Task<string?> ResolveSunoShortLinkAsync(string shortUrl)
    {
        try
        {
            using var handler = new HttpClientHandler { AllowAutoRedirect = false };
            using var client = new HttpClient(handler);

            var response = await client.GetAsync(shortUrl);
            var location = response.Headers.Location?.ToString();

            if (!string.IsNullOrEmpty(location) && location.Contains("/song/"))
                return location.Split("/song/").Last().Split("?").First();

            return null;
        }
        catch
        {
            return null;
        }
    }

    private async Task DeleteExpiredOrdersAsync(string userId)
    {
        var expiredOrders = await context.MusicOrders
            .Where(o => o.UserId == userId && o.CreatedAt < DateTime.UtcNow.AddDays(-30))
            .ToListAsync();

        if (expiredOrders.Count != 0)
        {
            context.MusicOrders.RemoveRange(expiredOrders);
            await context.SaveChangesAsync();
        }
    }
}
