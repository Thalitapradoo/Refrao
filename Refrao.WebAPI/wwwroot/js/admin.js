// =========================================================
// Painel administrativo — dashboard + gerenciamento completo
// Todas as ações chamam a API; a API bloqueia não-administradores (403).
// =========================================================
const dados = { musicas: [], artistas: [], albuns: [], generos: [], usuarios: [], sugestoes: [] };
let aba = "musicas";
let filtro = "";

document.addEventListener("DOMContentLoaded", async () => {
    if (!exigirAdmin()) return;

    document.querySelectorAll(".abas-painel button").forEach(b => b.addEventListener("click", () => {
        document.querySelectorAll(".abas-painel button").forEach(x => x.classList.toggle("ativo", x === b));
        aba = b.dataset.aba;
        filtro = "";
        desenhar();
    }));
    document.getElementById("cancelarDialogo").addEventListener("click", () => document.getElementById("dialogo").close());

    await recarregar();
});

async function recarregar() {
    const erro = document.getElementById("erroPainel");
    erro.textContent = "";
    try {
        const [painel, musicas, artistas, albuns, generos, usuarios, sugestoes] = await Promise.all([
            api("/Dashboard"), buscarTodasMusicas(), api("/Artistas"), api("/Albuns"), api("/Generos"), api("/Usuarios"), api("/Sugestoes")
        ]);
        Object.assign(dados, { musicas, artistas, albuns, generos, usuarios, sugestoes });
        const contador = document.getElementById("contadorSugestoes");
        contador.textContent = painel.sugestoesPendentes;
        contador.hidden = painel.sugestoesPendentes === 0;
        document.getElementById("totalMusicas").textContent = painel.musicas;
        document.getElementById("totalArtistas").textContent = painel.artistas;
        document.getElementById("totalAlbuns").textContent = painel.albuns;
        document.getElementById("totalUsuarios").textContent = painel.usuarios;
        desenhar();
    } catch (e) {
        if (e.status === 401) { exigirLogin(); return; }
        if (e.status === 403) { location.href = "index.html"; return; }
        erro.textContent = e.message;
        document.getElementById("conteudoPainel").innerHTML = "";
    }
}

// ---------- abas ----------
const ABAS = {
    musicas: { nome: "músicas", botao: "Nova música", tabela: () => tabelaMusicas(), novo: () => formMusica() },
    artistas: { nome: "artistas", botao: "Novo artista", tabela: () => tabelaArtistas(), novo: () => formArtista() },
    albuns: { nome: "álbuns", botao: "Novo álbum", tabela: () => tabelaAlbuns(), novo: () => formAlbum() },
    generos: { nome: "gêneros", botao: "Novo gênero", tabela: () => tabelaGeneros(), novo: () => formGenero() },
    usuarios: { nome: "usuários", botao: "Novo usuário", tabela: () => tabelaUsuarios(), novo: () => formUsuario() },
    sugestoes: { nome: "sugestões", botao: null, tabela: () => tabelaSugestoes(), novo: null }
};

function desenhar() {
    const config = ABAS[aba];
    const alvo = document.getElementById("conteudoPainel");
    alvo.innerHTML = `
        <div class="barra-acoes">
            <input type="search" id="filtroPainel" placeholder="Filtrar ${config.nome}..." value="${esc(filtro)}">
            ${config.botao ? `<button class="botao" type="button" id="botaoNovo">+ ${config.botao}</button>` : `<span style="color:var(--texto-fraco);font-size:.88rem">Aprove para cadastrar a música no catálogo, ou recuse explicando o motivo.</span>`}
        </div>
        <div class="tabela-wrap">${config.tabela()}</div>`;
    const campo = document.getElementById("filtroPainel");
    campo.addEventListener("input", () => {
        filtro = campo.value;
        alvo.querySelector(".tabela-wrap").innerHTML = config.tabela();
        ligarBotoes();
    });
    document.getElementById("botaoNovo")?.addEventListener("click", config.novo);
    ligarBotoes();
}

const combina = (...t) => { const f = filtro.trim().toLowerCase(); return !f || t.some(x => String(x ?? "").toLowerCase().includes(f)); };

function tabela(cab, linhas) {
    return `<table><thead><tr>${cab.map(c => `<th>${c}</th>`).join("")}</tr></thead>
        <tbody>${linhas || `<tr><td colspan="${cab.length}" class="vazio">Nenhum registro encontrado.</td></tr>`}</tbody></table>`;
}

const botoes = (tipo, id, excluir = "Excluir") => `
    <button class="botao secundario pequeno" data-acao="editar" data-tipo="${tipo}" data-id="${id}">Editar</button>
    <button class="botao perigo pequeno" data-acao="excluir" data-tipo="${tipo}" data-id="${id}">${excluir}</button>`;

function tabelaMusicas() {
    return tabela(["", "Título", "Artista", "Álbum", "Gênero", "Duração", ""], dados.musicas
        .filter(m => combina(m.titulo, m.artistaNome, m.albumNome, m.generoNome))
        .map(m => `<tr>
            <td><img class="mini" src="${esc(capaDaMusica(m))}" alt="" onerror="this.src='${CAPA_PADRAO}'"></td>
            <td><strong>${esc(m.titulo)}</strong></td><td>${esc(m.artistaNome)}</td><td>${esc(m.albumNome || "Single")}</td>
            <td><span class="selo">${esc(m.generoNome)}</span></td><td>${formatarDuracao(m.duracao)}</td>
            <td class="acoes">${botoes("musica", m.id)}</td></tr>`).join(""));
}

function tabelaArtistas() {
    return tabela(["", "Nome", "Nacionalidade", "Álbuns", "Músicas", ""], dados.artistas
        .filter(a => combina(a.nome, a.nacionalidade))
        .map(a => `<tr>
            <td><img class="mini redondo" src="${esc(a.imagemUrl || ARTISTA_PADRAO)}" alt="" onerror="this.src='${ARTISTA_PADRAO}'"></td>
            <td><strong>${esc(a.nome)}</strong></td><td>${esc(a.nacionalidade)}</td><td>${a.totalAlbuns}</td><td>${a.totalMusicas}</td>
            <td class="acoes">${botoes("artista", a.id)}</td></tr>`).join(""));
}

function tabelaAlbuns() {
    return tabela(["", "Nome", "Artista", "Lançamento", "Músicas", ""], dados.albuns
        .filter(a => combina(a.nome, a.artistaNome))
        .map(a => `<tr>
            <td><img class="mini" src="${esc(a.capaUrl || CAPA_PADRAO)}" alt="" onerror="this.src='${CAPA_PADRAO}'"></td>
            <td><strong>${esc(a.nome)}</strong></td><td>${esc(a.artistaNome)}</td><td>${formatarData(a.dataLancamento)}</td><td>${a.totalMusicas}</td>
            <td class="acoes">${botoes("album", a.id)}</td></tr>`).join(""));
}

function tabelaGeneros() {
    return tabela(["Nome", "Descrição", "Músicas", ""], dados.generos
        .filter(g => combina(g.nome, g.descricao))
        .map(g => `<tr>
            <td><strong>${esc(g.nome)}</strong></td><td style="color:var(--texto-suave)">${esc(g.descricao || "")}</td>
            <td>${dados.musicas.filter(m => m.generoId === g.id).length}</td>
            <td class="acoes">${botoes("genero", g.id)}</td></tr>`).join(""));
}

function tabelaUsuarios() {
    const meuId = Sessao.usuario?.id;
    return tabela(["Nome", "E-mail", "Perfil", "Cadastro", "Situação", ""], dados.usuarios
        .filter(u => combina(u.nome, u.email, u.perfil))
        .map(u => `<tr>
            <td><strong>${esc(u.nome)}</strong>${u.id === meuId ? ' <span class="selo rosa">você</span>' : ""}</td>
            <td>${esc(u.email)}</td>
            <td><span class="selo ${u.perfil === "Administrador" ? "rosa" : ""}">${u.perfil === "Administrador" ? "Administrador" : "Usuário"}</span></td>
            <td>${formatarData(u.dataCadastro)}</td>
            <td><span class="selo ${u.ativo ? "" : "cinza"}">${u.ativo ? "Ativa" : "Desativada"}</span></td>
            <td class="acoes">
                <button class="botao secundario pequeno" data-acao="editar" data-tipo="usuario" data-id="${u.id}">Editar</button>
                ${u.id === meuId ? "" : u.ativo
                    ? `<button class="botao perigo pequeno" data-acao="excluir" data-tipo="usuario" data-id="${u.id}">Desativar</button>`
                    : `<button class="botao pequeno" data-acao="reativar" data-tipo="usuario" data-id="${u.id}">Reativar</button>`}
            </td></tr>`).join(""));
}

function tabelaSugestoes() {
    const status = { Pendente: "pendente", Aprovada: "aprovada", Recusada: "recusada" };
    return tabela(["Música", "Artista", "Enviada por", "Data", "Status", ""], dados.sugestoes
        .filter(s => combina(s.titulo, s.artistaNome, s.usuarioNome, s.status))
        .map(s => `<tr>
            <td><strong>${esc(s.titulo)}</strong>${s.albumNome ? `<br><small style="color:var(--texto-fraco)">${esc(s.albumNome)}</small>` : ""}
                ${s.observacao ? `<br><small style="color:var(--texto-suave)">“${esc(s.observacao)}”</small>` : ""}</td>
            <td>${esc(s.artistaNome)}</td><td>${esc(s.usuarioNome)}</td><td>${formatarData(s.dataCriacao)}</td>
            <td><span class="selo ${status[s.status] || ""}">${esc(s.status)}</span></td>
            <td class="acoes">
                ${s.status === "Pendente" ? `
                    <button class="botao pequeno" data-acao="aprovar" data-tipo="sugestao" data-id="${s.id}">Aprovar</button>
                    <button class="botao secundario pequeno" data-acao="recusar" data-tipo="sugestao" data-id="${s.id}">Recusar</button>` : ""}
                ${s.musicaId ? `<a class="botao secundario pequeno" href="musica.html?id=${s.musicaId}">Ver música</a>` : ""}
                <button class="botao perigo pequeno" data-acao="excluir" data-tipo="sugestao" data-id="${s.id}">Excluir</button>
            </td></tr>`).join(""));
}

// ---------- ações ----------
const ROTAS = { musica: "/Musicas", artista: "/Artistas", album: "/Albuns", genero: "/Generos", usuario: "/Usuarios", sugestao: "/Sugestoes" };
const PERGUNTAS = {
    musica: "Excluir esta música do catálogo?",
    artista: "Excluir este artista? Só é possível se ele não tiver álbuns nem músicas.",
    album: "Excluir este álbum? Só é possível se ele não tiver músicas.",
    genero: "Excluir este gênero? Só é possível se nenhuma música usar ele.",
    usuario: "Desativar esta conta? O usuário não conseguirá mais entrar.",
    sugestao: "Excluir esta sugestão? (a música aprovada, se houver, continua no catálogo)"
};

function ligarBotoes() {
    document.querySelectorAll("#conteudoPainel [data-acao]").forEach(b => b.addEventListener("click", () => acao(b.dataset.acao, b.dataset.tipo, Number(b.dataset.id))));
}

async function acao(tipo_acao, tipo, id) {
    const lista = { musica: dados.musicas, artista: dados.artistas, album: dados.albuns, genero: dados.generos, usuario: dados.usuarios, sugestao: dados.sugestoes }[tipo];
    const item = lista.find(x => x.id === id);

    if (tipo_acao === "aprovar") { formMusica(null, item); return; }
    if (tipo_acao === "recusar") { formRecusa(item); return; }

    if (tipo_acao === "editar") {
        ({ musica: formMusica, artista: formArtista, album: formAlbum, genero: formGenero, usuario: formUsuario })[tipo](item);
        return;
    }
    if (tipo_acao === "reativar") {
        try {
            await api(`/Usuarios/${id}`, { metodo: "PUT", corpo: { nome: item.nome, email: item.email, perfil: item.perfil, ativo: true } });
            toast("Usuário reativado.", "sucesso");
            await recarregar();
        } catch (e) { toast(e.message, "erro"); }
        return;
    }
    if (!(await confirmar(PERGUNTAS[tipo]))) return;
    try {
        await api(`${ROTAS[tipo]}/${id}`, { metodo: "DELETE" });
        toast(tipo === "usuario" ? "Usuário desativado." : "Registro excluído.", "sucesso");
        await recarregar();
    } catch (e) {
        toast(e.message, "erro");
    }
}

function confirmar(texto) {
    const dialogo = document.getElementById("confirmacao");
    document.getElementById("textoConfirmacao").textContent = texto;
    dialogo.returnValue = "";
    dialogo.showModal();
    return new Promise(resolver => dialogo.addEventListener("close", () => resolver(dialogo.returnValue === "sim"), { once: true }));
}

// ---------- formulários ----------
function abrirDialogo(titulo, html, montarEnvio, aoAbrir) {
    const dialogo = document.getElementById("dialogo");
    const form = document.getElementById("formDialogo");
    const erro = document.getElementById("erroDialogo");
    document.getElementById("tituloDialogo").textContent = titulo;
    document.getElementById("camposDialogo").innerHTML = html;
    erro.textContent = "";

    form.onsubmit = async e => {
        e.preventDefault();
        limparErros(form);
        erro.textContent = "";
        const envio = montarEnvio(form);
        if (!envio) return;
        const botao = form.querySelector("button[type=submit]");
        botao.disabled = true;
        try {
            await envio.enviar();
            dialogo.close();
            toast(envio.mensagem, "sucesso");
            await recarregar();
        } catch (falha) {
            erro.textContent = falha.message;
        } finally {
            botao.disabled = false;
        }
    };
    dialogo.showModal();
    aoAbrir?.(form);
    form.querySelector("input, select, textarea")?.focus();
}

const campo = (id, rotulo, html, extra = "") => `<div class="campo ${extra}"><label for="${id}">${rotulo}</label>${html}<div class="erro-campo"></div></div>`;
const opcoes = (lista, sel, vazio = "Selecione...") => `<option value="">${vazio}</option>` +
    lista.map(i => `<option value="${i.id}" ${i.id === sel ? "selected" : ""}>${esc(i.nome)}</option>`).join("");
const salvarEm = (rota, item) => (corpo, nome) => ({
    enviar: () => api(item ? `${rota}/${item.id}` : rota, { metodo: item ? "PUT" : "POST", corpo }),
    mensagem: item ? `${nome} atualizado(a).` : `${nome} cadastrado(a).`
});

function formRecusa(s) {
    const html = `<p style="color:var(--texto-suave);margin-top:0">Sugestão: <strong style="color:#fff">${esc(s.titulo)}</strong> — ${esc(s.artistaNome)} (de ${esc(s.usuarioNome)})</p>
        ${campo("fMotivo", "Motivo da recusa *", `<textarea id="fMotivo" maxlength="300" placeholder="Ex.: essa música já está no catálogo / não é do gênero pop..."></textarea>`)}`;
    abrirDialogo("Recusar sugestão", html, form => {
        const motivo = form.querySelector("#fMotivo");
        if (!marcarErro(motivo, motivo.value.trim().length >= 3 ? "" : "Explique o motivo (mínimo 3 caracteres).")) return null;
        return {
            enviar: () => api(`/Sugestoes/${s.id}/recusar`, { metodo: "POST", corpo: { resposta: motivo.value.trim() } }),
            mensagem: "Sugestão recusada."
        };
    });
}

const normalizar = t => String(t ?? "").normalize("NFD").replace(/[\u0300-\u036f]/g, "").trim().toLowerCase();

function formMusica(m, sugestao = null) {
    const artistaSugerido = sugestao ? dados.artistas.find(a => normalizar(a.nome) === normalizar(sugestao.artistaNome)) : null;
    const albumSugerido = sugestao && artistaSugerido && sugestao.albumNome
        ? dados.albuns.find(a => a.artistaId === artistaSugerido.id && normalizar(a.nome) === normalizar(sugestao.albumNome)) : null;
    if (sugestao) m = { titulo: sugestao.titulo, artistaId: artistaSugerido?.id, albumId: albumSugerido?.id ?? null, duracao: 0 };

    const html = `<div class="grade-form">
        ${campo("fTitulo", "Título *", `<input id="fTitulo" maxlength="150" value="${esc(m?.titulo)}">`, "inteiro")}
        ${campo("fArtista", "Artista *", `<select id="fArtista">${opcoes(dados.artistas, m?.artistaId)}</select>`)}
        ${campo("fAlbum", "Álbum", `<select id="fAlbum"></select>`)}
        ${campo("fGenero", "Gênero *", `<select id="fGenero">${opcoes(dados.generos, m?.generoId)}</select>`)}
        ${campo("fDuracao", "Duração (m:ss) *", `<input id="fDuracao" placeholder="3:25" value="${m?.duracao ? formatarDuracao(m.duracao) : ""}">`)}
        ${campo("fData", "Data de lançamento *", `<input id="fData" type="date" value="${m?.dataLancamento ?? ""}">`)}
        ${campo("fCapa", "Capa (URL, opcional)", `<input id="fCapa" maxlength="500" placeholder="usa a capa do álbum se vazio" value="${esc(m?.capaUrl)}">`)}
        ${campo("fInicio", "Início do refrão (m:ss, opcional)", `<input id="fInicio" placeholder="0:45" value="${m?.inicioRefrao != null ? formatarDuracao(m.inicioRefrao) : ""}">`)}
        ${campo("fLetra", "Link da letra (opcional)", `<input id="fLetra" maxlength="500" placeholder="https://www.letras.mus.br/..." value="${esc(m?.linkLetra)}">`, "inteiro")}
        ${campo("fDescricao", "Descrição", `<textarea id="fDescricao" maxlength="1000">${esc(m?.descricao)}</textarea>`, "inteiro")}
        ${sugestao ? campo("fResposta", "Mensagem para quem sugeriu (opcional)", `<input id="fResposta" maxlength="300" placeholder="Obrigado pela sugestão!">`, "inteiro") : ""}
    </div>
    ${sugestao && !artistaSugerido ? `<div class="aviso">O artista <strong>${esc(sugestao.artistaNome)}</strong> ainda não está cadastrado. Cadastre-o na aba <strong>Artistas</strong> (e o álbum, se houver) antes de aprovar, ou escolha outro artista.</div>` : ""}
    ${sugestao?.observacao ? `<div class="aviso">Comentário de ${esc(sugestao.usuarioNome)}: “${esc(sugestao.observacao)}”</div>` : ""}`;

    const titulo = sugestao ? "Aprovar sugestão" : (m ? "Editar música" : "Nova música");
    abrirDialogo(titulo, html, form => {
        const v = id => form.querySelector(id);
        const segundos = duracaoParaSegundos(v("#fDuracao").value);
        const inicioTexto = v("#fInicio").value.trim();
        const inicio = inicioTexto ? duracaoParaSegundos(inicioTexto) : null;
        const letra = v("#fLetra").value.trim();
        const ok = [
            validarObrigatorio(v("#fTitulo"), "o título"),
            marcarErro(v("#fArtista"), v("#fArtista").value ? "" : "Escolha o artista."),
            marcarErro(v("#fGenero"), v("#fGenero").value ? "" : "Escolha o gênero."),
            marcarErro(v("#fDuracao"), segundos > 0 && segundos <= 3600 ? "" : "Use o formato m:ss (ex.: 3:25), até 60 minutos."),
            marcarErro(v("#fData"), v("#fData").value ? "" : "Informe a data."),
            marcarErro(v("#fInicio"), inicio === null || (inicio >= 0 && segundos > 0 && inicio < segundos) ? "" : "Use m:ss e um tempo menor que a duração."),
            marcarErro(v("#fLetra"), !letra || /^https?:\/\/\S+\.\S+/.test(letra) ? "" : "O link precisa começar com http:// ou https://.")
        ].every(Boolean);
        if (!ok) return null;
        const corpo = {
            titulo: v("#fTitulo").value.trim(),
            artistaId: Number(v("#fArtista").value),
            albumId: v("#fAlbum").value ? Number(v("#fAlbum").value) : null,
            generoId: Number(v("#fGenero").value),
            duracao: segundos,
            dataLancamento: v("#fData").value,
            capaUrl: v("#fCapa").value.trim() || null,
            descricao: v("#fDescricao").value.trim() || null,
            inicioRefrao: inicio,
            linkLetra: letra || null
        };
        if (sugestao) {
            return {
                enviar: () => api(`/Sugestoes/${sugestao.id}/aprovar`, { metodo: "POST", corpo: { musica: corpo, resposta: v("#fResposta").value.trim() || null } }),
                mensagem: "Sugestão aprovada! A música entrou no catálogo."
            };
        }
        return salvarEm("/Musicas", m?.id ? m : null)(corpo, "Música");
    }, form => {
        // O álbum só pode ser do artista escolhido.
        const artista = form.querySelector("#fArtista");
        const album = form.querySelector("#fAlbum");
        const atualizarAlbuns = (selecionado) => {
            const doArtista = dados.albuns.filter(a => a.artistaId === Number(artista.value));
            album.innerHTML = opcoes(doArtista, selecionado, "Sem álbum (single)");
        };
        atualizarAlbuns(m?.albumId ?? null);
        artista.addEventListener("change", () => atualizarAlbuns(null));
        album.addEventListener("change", () => {
            const data = form.querySelector("#fData");
            const escolhido = dados.albuns.find(a => a.id === Number(album.value));
            if (escolhido && !data.value) data.value = escolhido.dataLancamento;
        });
    });
}

function formArtista(a) {
    const html = `<div class="grade-form">
        ${campo("fNome", "Nome *", `<input id="fNome" maxlength="100" value="${esc(a?.nome)}">`)}
        ${campo("fNacionalidade", "Nacionalidade *", `<input id="fNacionalidade" maxlength="60" value="${esc(a?.nacionalidade)}">`)}
        ${campo("fImagem", "Imagem (URL ou caminho local)", `<input id="fImagem" maxlength="500" placeholder="https://... ou img/artistas/..." value="${esc(a?.imagemUrl)}">`, "inteiro")}
        ${campo("fBio", "Biografia", `<textarea id="fBio" maxlength="2000">${esc(a?.biografia)}</textarea>`, "inteiro")}
    </div>`;
    abrirDialogo(a ? "Editar artista" : "Novo artista", html, form => {
        const nome = form.querySelector("#fNome"), nac = form.querySelector("#fNacionalidade");
        if (![validarObrigatorio(nome, "o nome"), validarObrigatorio(nac, "a nacionalidade")].every(Boolean)) return null;
        return salvarEm("/Artistas", a)({
            nome: nome.value.trim(), nacionalidade: nac.value.trim(),
            imagemUrl: form.querySelector("#fImagem").value.trim() || null,
            biografia: form.querySelector("#fBio").value.trim() || null
        }, "Artista");
    });
}

function formAlbum(a) {
    const html = `<div class="grade-form">
        ${campo("fNome", "Nome *", `<input id="fNome" maxlength="150" value="${esc(a?.nome)}">`, "inteiro")}
        ${campo("fArtista", "Artista *", `<select id="fArtista">${opcoes(dados.artistas, a?.artistaId)}</select>`)}
        ${campo("fData", "Data de lançamento *", `<input id="fData" type="date" value="${a?.dataLancamento ?? ""}">`)}
        ${campo("fCapa", "Capa (URL ou caminho local)", `<input id="fCapa" maxlength="500" placeholder="https://... ou img/capas/..." value="${esc(a?.capaUrl)}">`, "inteiro")}
        ${campo("fDescricao", "Descrição", `<textarea id="fDescricao" maxlength="1000">${esc(a?.descricao)}</textarea>`, "inteiro")}
    </div>`;
    abrirDialogo(a ? "Editar álbum" : "Novo álbum", html, form => {
        const nome = form.querySelector("#fNome"), artista = form.querySelector("#fArtista"), data = form.querySelector("#fData");
        const ok = [validarObrigatorio(nome, "o nome"),
            marcarErro(artista, artista.value ? "" : "Escolha o artista."),
            marcarErro(data, data.value ? "" : "Informe a data.")].every(Boolean);
        if (!ok) return null;
        return salvarEm("/Albuns", a)({
            nome: nome.value.trim(), artistaId: Number(artista.value), dataLancamento: data.value,
            capaUrl: form.querySelector("#fCapa").value.trim() || null,
            descricao: form.querySelector("#fDescricao").value.trim() || null
        }, "Álbum");
    });
}

function formGenero(g) {
    const html = `${campo("fNome", "Nome *", `<input id="fNome" maxlength="50" value="${esc(g?.nome)}">`)}
        ${campo("fDescricao", "Descrição", `<textarea id="fDescricao" maxlength="500">${esc(g?.descricao)}</textarea>`)}`;
    abrirDialogo(g ? "Editar gênero" : "Novo gênero", html, form => {
        const nome = form.querySelector("#fNome");
        if (!validarObrigatorio(nome, "o nome")) return null;
        return salvarEm("/Generos", g)({ nome: nome.value.trim(), descricao: form.querySelector("#fDescricao").value.trim() || null }, "Gênero");
    });
}

function formUsuario(u) {
    const ehVoce = u && u.id === Sessao.usuario?.id;
    const html = `<div class="grade-form">
        ${campo("fNome", "Nome *", `<input id="fNome" maxlength="100" value="${esc(u?.nome)}">`)}
        ${campo("fEmail", "E-mail *", `<input id="fEmail" type="email" maxlength="150" value="${esc(u?.email)}">`)}
        ${campo("fSenha", u ? "Nova senha (opcional)" : "Senha *", `<input id="fSenha" type="password" autocomplete="new-password">`)}
        ${campo("fPerfil", "Perfil *", `<select id="fPerfil" ${ehVoce ? "disabled" : ""}>
            <option value="Usuario" ${u?.perfil === "Usuario" ? "selected" : ""}>Usuário comum</option>
            <option value="Administrador" ${u?.perfil === "Administrador" ? "selected" : ""}>Administrador</option></select>`)}
        ${u && !ehVoce ? `<div class="campo inteiro"><label class="checkbox"><input type="checkbox" id="fAtivo" ${u.ativo ? "checked" : ""}> Conta ativa</label></div>` : ""}
        ${ehVoce ? `<p class="inteiro" style="color:var(--texto-fraco);font-size:.85rem">Para trocar a sua própria senha use a página <a href="perfil.html">Meu perfil</a>.</p>` : ""}
    </div>`;
    abrirDialogo(u ? "Editar usuário" : "Novo usuário", html, form => {
        const nome = form.querySelector("#fNome"), email = form.querySelector("#fEmail"), senha = form.querySelector("#fSenha");
        if (ehVoce && senha.value) { marcarErro(senha, "Troque a sua senha pela página Meu perfil."); return null; }
        if (![validarObrigatorio(nome, "o nome"), validarEmail(email), validarSenha(senha, Boolean(u))].every(Boolean)) return null;
        const perfil = form.querySelector("#fPerfil").value;
        if (!u) {
            return { enviar: () => api("/Usuarios", { metodo: "POST", corpo: { nome: nome.value.trim(), email: email.value.trim(), senha: senha.value, perfil } }), mensagem: "Usuário cadastrado." };
        }
        const corpo = {
            nome: nome.value.trim(), email: email.value.trim(), novaSenha: senha.value || null,
            perfil: ehVoce ? u.perfil : perfil, ativo: ehVoce ? true : form.querySelector("#fAtivo").checked
        };
        return {
            enviar: async () => {
                await api(`/Usuarios/${u.id}`, { metodo: "PUT", corpo });
                if (ehVoce) Sessao.atualizarUsuario({ ...Sessao.usuario, nome: corpo.nome, email: corpo.email });
            },
            mensagem: "Usuário atualizado."
        };
    });
}
