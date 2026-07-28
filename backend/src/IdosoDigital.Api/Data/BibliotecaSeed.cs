using IdosoDigital.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace IdosoDigital.Api.Data;

public static class BibliotecaSeed
{
    public static async Task EnsureSeedAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Categorias.AnyAsync(cancellationToken))
        {
            return;
        }

        var categorias = CriarCategorias();
        var conteudos = CriarConteudos(categorias);

        db.Categorias.AddRange(categorias.Values);
        db.Conteudos.AddRange(conteudos);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static Dictionary<string, Categoria> CriarCategorias()
    {
        var items = new[]
        {
            ("pix", "PIX", "Aprenda a enviar e receber dinheiro com segurança.", 1),
            ("whatsapp", "WhatsApp", "Mensagens, bloqueio e segurança no aplicativo.", 2),
            ("bancos", "Aplicativos Bancários", "Como usar o app do banco com calma e segurança.", 3),
            ("internet", "Internet", "Navegar, senhas e cuidados básicos.", 4),
            ("redes-sociais", "Redes Sociais", "Facebook, Instagram e privacidade.", 5),
            ("golpes", "Golpes Virtuais", "Reconheça e evite fraudes por mensagem e telefone.", 6),
            ("governo", "Governo Digital", "Gov.br e serviços públicos online.", 7),
            ("sus", "SUS", "Consultas e serviços de saúde digitais.", 8)
        };

        return items.ToDictionary(
            x => x.Item1,
            x => new Categoria
            {
                Id = Guid.NewGuid(),
                Slug = x.Item1,
                Nome = x.Item2,
                Descricao = x.Item3,
                Ordem = x.Item4
            });
    }

    private static List<Conteudo> CriarConteudos(Dictionary<string, Categoria> cats)
    {
        var list = new List<Conteudo>();

        void Add(string slug, string titulo, TipoConteudo tipo, string corpo, int ordem, string? url = null)
        {
            list.Add(new Conteudo
            {
                Id = Guid.NewGuid(),
                CategoriaId = cats[slug].Id,
                Titulo = titulo,
                Tipo = tipo,
                Corpo = corpo.Trim(),
                UrlMidia = url,
                Ordem = ordem
            });
        }

        // PIX (3)
        Add("pix", "Como fazer um PIX", TipoConteudo.Artigo, """
            Para fazer um PIX, use sempre o aplicativo oficial do seu banco.

            1. Abra o app do banco.
            2. Toque em PIX.
            3. Escolha Transferir ou Pagar.
            4. Digite a chave PIX ou leia o QR Code.
            5. Confira o nome da pessoa e o valor.
            6. Confirme com a senha do aplicativo.

            Nunca faça PIX porque alguém pediu por mensagem ou telefone.
            """, 1);

        Add("pix", "O que é chave PIX?", TipoConteudo.Faq, """
            Pergunta: O que é uma chave PIX?

            Resposta: É um “apelido” para a sua conta. Pode ser CPF, e-mail, telefone ou uma chave aleatória.
            Com a chave, a outra pessoa consegue te enviar dinheiro sem precisar do número do banco.

            Dica: não compartilhe sua chave em grupos desconhecidos.
            """, 2);

        Add("pix", "PIX seguro: o que nunca fazer", TipoConteudo.Artigo, """
            1. Não informe senha a ninguém.
            2. Não clique em links de “desbloqueio de conta”.
            3. Se alguém pedir PIX urgente, desligue e confirme por outro caminho.
            4. Confira sempre o nome que aparece antes de confirmar.
            """, 3);

        // WhatsApp (3)
        Add("whatsapp", "Como bloquear um número no WhatsApp", TipoConteudo.Artigo, """
            1. Abra a conversa.
            2. Toque no nome da pessoa no topo.
            3. Role até Bloquear.
            4. Confirme o bloqueio.

            Depois disso, a pessoa não consegue mais te enviar mensagens.
            """, 1);

        Add("whatsapp", "Mensagens suspeitas no WhatsApp", TipoConteudo.Faq, """
            Pergunta: Recebi uma mensagem pedindo código. O que faço?

            Resposta: Não envie. Códigos do WhatsApp são só seus.
            Feche a conversa, bloqueie o número e avise um familiar de confiança.
            """, 2);

        Add("whatsapp", "Privacidade básica no WhatsApp", TipoConteudo.Artigo, """
            1. Abra Configurações.
            2. Toque em Privacidade.
            3. Ajuste quem vê sua foto, status e última vez.
            4. Prefira “Meus contatos” ou “Ninguém” se quiser mais proteção.
            """, 3);

        // Bancos (2)
        Add("bancos", "Abrir o app do banco com segurança", TipoConteudo.Artigo, """
            1. Baixe o app só na loja oficial (Play Store ou App Store).
            2. Use a senha do aplicativo, nunca a senha do cartão em sites estranhos.
            3. Ative o bloqueio da tela do celular.
            4. Se o app pedir atualização, faça pela loja oficial.
            """, 1);

        Add("bancos", "O banco pediu minha senha por telefone?", TipoConteudo.Faq, """
            Pergunta: Posso passar a senha se a pessoa disser que é do banco?

            Resposta: Não. Banco de verdade não pede senha por telefone, SMS ou WhatsApp.
            Desligue e abra o aplicativo oficial para conferir.
            """, 2);

        // Internet (2)
        Add("internet", "Senhas mais seguras", TipoConteudo.Artigo, """
            1. Use senhas diferentes para banco e redes sociais.
            2. Prefira frases fáceis de lembrar e difíceis de adivinhar.
            3. Não anote a senha em papel colado no celular.
            4. Peça ajuda a um familiar para criar um caderno seguro em casa, se precisar.
            """, 1);

        Add("internet", "Links seguros", TipoConteudo.Faq, """
            Pergunta: Como sei se um link é seguro?

            Resposta: Desconfie de links em SMS e WhatsApp pedindo urgência.
            Prefira abrir o aplicativo oficial ou digitar o endereço que você já conhece.
            """, 2);

        // Redes sociais (2)
        Add("redes-sociais", "Cuidados no Facebook e Instagram", TipoConteudo.Artigo, """
            1. Não aceite amizade de desconhecidos que peçam dinheiro.
            2. Não compartilhe fotos de documentos.
            3. Se alguém “clonar” seu perfil, avise familiares e denuncie na rede.
            """, 1);

        Add("redes-sociais", "Alguém pediu dinheiro na rede social", TipoConteudo.Faq, """
            Pergunta: Um amigo pediu PIX no Facebook. Pode ser golpe?

            Resposta: Pode. Confirme por ligação ou WhatsApp direto com a pessoa.
            Contas clonadas são comuns.
            """, 2);

        // Golpes (3)
        Add("golpes", "Golpe do falso banco", TipoConteudo.Artigo, """
            Mensagem típica: “Sua conta será bloqueada. Clique aqui.”

            O que fazer:
            1. Não clique.
            2. Não informe senha ou código SMS.
            3. Abra o app oficial do banco.
            4. Se precisar, ligue para o número do verso do cartão.
            """, 1);

        Add("golpes", "Golpe do falso suporte", TipoConteudo.Faq, """
            Pergunta: Ligaram dizendo que meu WhatsApp foi invadido. E agora?

            Resposta: Não passe código. Encerre a ligação.
            O WhatsApp nunca pede código por telefone.
            """, 2);

        Add("golpes", "Checklist rápido anti-golpe", TipoConteudo.Artigo, """
            Antes de pagar ou clicar, pergunte:
            1. Isso parece urgente demais?
            2. Estão pedindo senha?
            3. Posso confirmar no aplicativo oficial?

            Se uma resposta for “sim” para perigo, pare e peça ajuda.
            """, 3);

        // Governo (2)
        Add("governo", "O que é a conta Gov.br", TipoConteudo.Artigo, """
            A conta Gov.br serve para acessar serviços do governo pela internet.

            1. Acesse o site ou app oficial Gov.br.
            2. Crie ou entre na sua conta.
            3. Use só em sites oficiais (.gov.br).
            """, 1);

        Add("governo", "Cuidado com sites falsos do governo", TipoConteudo.Faq, """
            Pergunta: Recebi link de “atualização cadastral do governo”. É seguro?

            Resposta: Desconfie. Entre pelo site oficial digitando gov.br ou pelo app.
            Não use links de mensagem.
            """, 2);

        // SUS (3)
        Add("sus", "Como marcar consulta pelo celular", TipoConteudo.Artigo, """
            O caminho mais comum:

            1. Abra o app ou site de saúde da sua cidade / Conecte SUS.
            2. Entre com a conta Gov.br, se pedir.
            3. Procure consultas ou agendamento.
            4. Escolha especialidade e horário.
            5. Anote o dia e o local.

            Se preferir, peça ajuda no posto de saúde.
            """, 1);

        Add("sus", "Preciso da carteirinha do SUS?", TipoConteudo.Faq, """
            Pergunta: Ainda preciso da cartinha física?

            Resposta: Em muitos lugares o CPF já identifica você.
            Leve documento com foto e, se tiver, a cartinha.
            Confirme na unidade de saúde da sua cidade.
            """, 2);

        Add("sus", "Não consigo agendar online", TipoConteudo.Artigo, """
            1. Confira se o celular está na internet.
            2. Tente de novo mais tarde.
            3. Peça ajuda a um familiar.
            4. Vá ao posto de saúde e peça o agendamento no balcão.
            """, 3);

        return list;
    }
}
