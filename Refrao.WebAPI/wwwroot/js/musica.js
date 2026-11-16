// Detalhes da música + refrão (minuto e linha do tempo), letra e comentários "por que esse refrão gruda".
let musica = null;

document.addEventListener("DOMContentLoaded", async () => {
    const alvo = document.getElementById("detalheMusica");
    const erro = document.getElementById("erroMusica");
    const id = Number(parametro("id"));
    if (!Number.isInteger(id) || id < 1) { alvo.innerHTML = ""; erro.textContent = "Música não informada."; return; }

    try {
        musica = await api(`/Musicas/${id}`);
        document.title = `${musica.titulo} · Refrão!`;
        alvo.innerHTML = detalheHtml(musica);
        await carregarComentarios();
    } catch (e) {
        alvo.innerHTML = e.status === 404 ? `<div class="vazio">Essa música não existe no catálogo.</div>` : "";
        if (e.status !== 404) erro.textContent = e.message;
    }
});

function detalheHtml(m) {
    const buscaLetra = `https://www.google.com/search?q=${encodeURIComponent(`letra ${m.titulo} ${m.artistaNome}`)}`;
    return `
        <div class="detalhe painel">
            <img class="capa-grande" src="${esc(capaDaMusica(m))}" alt="Capa de ${esc(m.albumNome || m.titulo)}"
                 onerror="this.onerror=null;this.src='${CAPA_PADRAO}'">
            <div>
                <span class="selo">${esc(m.generoNome)}</span>
                <h1>${esc(m.titulo)}</h1>
                <a class="artista-link" href="artista.html?id=${m.artistaId}">${esc(m.artistaNome)}</a>
                <dl class="ficha">
                    <div><dt>Artista</dt><dd>${esc(m.artistaNome)}</dd></div>
                    <div><dt>Álbum</dt><dd>${esc(m.albumNome || "Single avulso")}</dd></div>
                    <div><dt>Gênero</dt><dd>${esc(m.generoNome)}</dd></div>
                    <div><dt>Duração</dt><dd>${formatarDuracao(m.duracao)}</dd></div>
                    <div><dt>Lançamento</dt><dd>${formatarData(m.dataLancamento)}</dd></div>
                </dl>
                <p class="descricao">${esc(m.descricao || "Sem descrição para esta música.")}</p>

                <div class="bloco-refrao">
                    <h3><span class="equalizador" aria-hidden="true"><span></span><span></span><span></span><span></span></span> O refrão</h3>
                    ${m.inicioRefrao != null ? linhaDoTempo(m) : `<p style="color:var(--texto-fraco);margin:0">O minuto do refrão desta música ainda não foi cadastrado.</p>`}
                    <div style="display:flex;gap:.6rem;flex-wrap:wrap;margin-top:1rem">
                        ${m.linkLetra
                            ? `<a class="botao pequeno" href="${esc(m.linkLetra)}" target="_blank" rel="noopener">Ver letra oficial ↗</a>`
                            : `<a class="botao secundario pequeno" href="${buscaLetra}" target="_blank" rel="noopener">Procurar a letra ↗</a>`}
                        <a class="botao secundario pequeno" href="#comentarios">${m.totalComentarios} ${m.totalComentarios === 1 ? "comentário" : "comentários"}</a>
                    </div>
                </div>

                <div style="display:flex;gap:.6rem;flex-wrap:wrap">
                    <a class="botao" href="artista.html?id=${m.artistaId}">Ver ${esc(m.artistaNome)}</a>
                    <a class="botao secundario" href="catalogo.html?generoId=${m.generoId}">Mais ${esc(m.generoNome)}</a>
                </div>
            </div>
        </div>

        <section class="comentarios painel" id="comentarios">
            <h2>Por que esse refrão gruda? <span class="selo rosa" id="totalComentarios"></span></h2>
            <div id="areaFormulario"></div>
            <div id="listaComentarios"><div class="carregando"></div></div>
        </section>`;
}

function linhaDoTempo(m) {
    const pct = Math.min(100, Math.max(0, (m.inicioRefrao / m.duracao) * 100));
    return `
        <p style="margin:0;color:var(--texto-suave)">O refrão começa em <strong style="color:#fff">${formatarDuracao(m.inicioRefrao)}</strong>. Pule direto para ele!</p>
        <div class="linha-tempo" role="img" aria-label="Refrão começa em ${formatarDuracao(m.inicioRefrao)} de ${formatarDuracao(m.duracao)}">
            <div class="antes" style="width:${pct}%"></div>
            <div class="refrao" style="left:${pct}%"></div>
            <span class="marcador" style="left:${pct}%">refrão ${formatarDuracao(m.inicioRefrao)}</span>
        </div>
        <div class="tempos"><span>0:00</span><span>${formatarDuracao(m.duracao)}</span></div>`;
}

async function carregarComentarios() {
    const lista = document.getElementById("listaComentarios");
    const area = document.getElementById("areaFormulario");
    try {
        const comentarios = await api(`/Comentarios?musicaId=${musica.id}`);
        document.getElementById("totalComentarios").textContent = comentarios.length;

        const meuId = Sessao.usuario?.id;
        const meu = comentarios.find(c => c.usuarioId === meuId);

        if (!Sessao.logado) {
            area.innerHTML = `<p style="color:var(--texto-suave)"><a href="login.html?voltar=${encodeURIComponent("musica.html?id=" + musica.id)}">Entre</a> ou <a href="cadastro.html">crie uma conta</a> para contar por que esse refrão gruda.</p>`;
        } else if (meu) {
            area.innerHTML = `<p style="color:var(--texto-fraco);font-size:.9rem">Você já comentou esta música. Dá para editar ou apagar o seu comentário abaixo.</p>`;
        } else {
            area.innerHTML = formComentario("", "Publicar comentário");
            ligarFormulario(area, texto => api("/Comentarios", { metodo: "POST", corpo: { musicaId: musica.id, texto } }), "Comentário publicado!");
        }

        lista.innerHTML = comentarios.length ? comentarios.map(c => `
            <article class="comentario" data-id="${c.id}">
                <div class="mini-avatar">${esc(c.usuarioNome.trim().charAt(0).toUpperCase())}</div>
                <div>
                    <span class="autor">${esc(c.usuarioNome)}</span><span class="data">${formatarData(c.dataCriacao)}</span>
                    <p>${esc(c.texto)}</p>
                    ${c.usuarioId === meuId || Sessao.admin ? `<div class="acoes-comentario">
                        ${c.usuarioId === meuId ? `<button class="botao secundario pequeno" data-editar="${c.id}">Editar</button>` : ""}
                        <button class="botao perigo pequeno" data-apagar="${c.id}">Apagar</button>
                    </div>` : ""}
                </div>
            </article>`).join("") : `<p class="vazio" style="padding:1.5rem">Ninguém comentou ainda. Seja a primeira pessoa!</p>`;

        lista.querySelectorAll("[data-apagar]").forEach(b => b.addEventListener("click", async () => {
            if (!confirm("Apagar este comentário?")) return;
            try {
                await api(`/Comentarios/${b.dataset.apagar}`, { metodo: "DELETE" });
                toast("Comentário apagado.", "sucesso");
                await atualizarTudo();
            } catch (e) { toast(e.message, "erro"); }
        }));

        lista.querySelectorAll("[data-editar]").forEach(b => b.addEventListener("click", () => {
            const c = comentarios.find(x => x.id === Number(b.dataset.editar));
            const artigo = b.closest(".comentario").querySelector("div:last-child");
            artigo.innerHTML = formComentario(c.texto, "Salvar");
            ligarFormulario(artigo, texto => api(`/Comentarios/${c.id}`, { metodo: "PUT", corpo: { texto } }), "Comentário atualizado!");
        }));
    } catch (e) {
        lista.innerHTML = `<div class="aviso erro">${esc(e.message)}</div>`;
    }
}

function formComentario(texto, rotulo) {
    return `
        <form class="form-comentario" novalidate>
            <label class="sr-only" for="textoComentario">Comentário</label>
            <textarea id="textoComentario" maxlength="300" placeholder="Ex.: a batida muda e todo mundo canta junto...">${esc(texto)}</textarea>
            <div class="contador"><span>${texto.length}</span>/300</div>
            <div class="aviso erro"></div>
            <div><button class="botao pequeno" type="submit">${rotulo}</button></div>
        </form>`;
}

function ligarFormulario(container, enviar, sucesso) {
    const form = container.querySelector("form");
    const campo = form.querySelector("textarea");
    const contador = form.querySelector(".contador span");
    const erro = form.querySelector(".aviso");
    campo.addEventListener("input", () => contador.textContent = campo.value.length);
    form.addEventListener("submit", async e => {
        e.preventDefault();
        erro.textContent = "";
        const texto = campo.value.trim();
        if (texto.length < 3) { erro.textContent = "Escreva pelo menos 3 caracteres."; return; }
        const botao = form.querySelector("button[type=submit]");
        botao.disabled = true;
        try {
            await enviar(texto);
            toast(sucesso, "sucesso");
            await atualizarTudo();
        } catch (falha) {
            erro.textContent = falha.message;
            if (falha.status === 401) exigirLogin();
        } finally {
            botao.disabled = false;
        }
    });
}

async function atualizarTudo() {
    musica = await api(`/Musicas/${musica.id}`);
    document.getElementById("detalheMusica").innerHTML = detalheHtml(musica);
    await carregarComentarios();
}
