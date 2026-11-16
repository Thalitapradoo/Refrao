// Sugestões: o usuário envia e acompanha o status (Pendente, Aprovada, Recusada).
document.addEventListener("DOMContentLoaded", async () => {
    if (!exigirLogin()) return;

    api("/Artistas").then(artistas => {
        document.getElementById("listaArtistas").innerHTML = artistas.map(a => `<option value="${esc(a.nome)}">`).join("");
    }).catch(() => {});

    document.getElementById("formSugestao").addEventListener("submit", enviar);
    await carregar();
});

function seloStatus(status) {
    const classe = { Pendente: "pendente", Aprovada: "aprovada", Recusada: "recusada" }[status] || "";
    const texto = { Pendente: "Em análise", Aprovada: "Aprovada", Recusada: "Recusada" }[status] || status;
    return `<span class="selo ${classe}">${texto}</span>`;
}

async function carregar() {
    const lista = document.getElementById("minhasSugestoes");
    try {
        const sugestoes = await api("/Sugestoes/minhas");
        lista.innerHTML = sugestoes.length ? sugestoes.map(s => `
            <article class="sugestao painel">
                <div class="topo-sugestao">
                    <h3>${esc(s.titulo)}</h3>
                    ${seloStatus(s.status)}
                </div>
                <span style="color:var(--rosa);font-weight:600">${esc(s.artistaNome)}${s.albumNome ? ` · ${esc(s.albumNome)}` : ""}</span>
                <span style="color:var(--texto-fraco);font-size:.82rem">Enviada em ${formatarData(s.dataCriacao)}</span>
                ${s.observacao ? `<p style="margin:.2rem 0;color:var(--texto-suave)">${esc(s.observacao)}</p>` : ""}
                ${s.respostaAdmin ? `<div class="resposta"><strong>Resposta:</strong> ${esc(s.respostaAdmin)}</div>` : ""}
                <div style="display:flex;gap:.5rem;margin-top:.4rem">
                    ${s.musicaId ? `<a class="botao pequeno" href="musica.html?id=${s.musicaId}">Ver no catálogo</a>` : ""}
                    ${s.status === "Pendente" ? `<button class="botao perigo pequeno" data-cancelar="${s.id}">Cancelar</button>` : ""}
                </div>
            </article>`).join("") : `<div class="vazio painel">Você ainda não enviou nenhuma sugestão.</div>`;

        lista.querySelectorAll("[data-cancelar]").forEach(b => b.addEventListener("click", async () => {
            if (!confirm("Cancelar esta sugestão?")) return;
            try {
                await api(`/Sugestoes/${b.dataset.cancelar}`, { metodo: "DELETE" });
                toast("Sugestão cancelada.", "sucesso");
                await carregar();
            } catch (e) { toast(e.message, "erro"); }
        }));
    } catch (e) {
        lista.innerHTML = "";
        document.getElementById("erroLista").textContent = e.message;
    }
}

async function enviar(e) {
    e.preventDefault();
    const form = e.target;
    limparErros(form);
    const msg = document.getElementById("mensagem");
    msg.className = "aviso"; msg.textContent = "";

    const titulo = document.getElementById("titulo");
    const artista = document.getElementById("artista");
    if (![validarObrigatorio(titulo, "o título"), validarObrigatorio(artista, "o artista")].every(Boolean)) return;

    const botao = form.querySelector("button[type=submit]");
    botao.disabled = true;
    try {
        await api("/Sugestoes", {
            metodo: "POST",
            corpo: {
                titulo: titulo.value.trim(),
                artistaNome: artista.value.trim(),
                albumNome: document.getElementById("album").value.trim() || null,
                observacao: document.getElementById("observacao").value.trim() || null
            }
        });
        form.reset();
        msg.className = "aviso sucesso";
        msg.textContent = "Sugestão enviada! Acompanhe o status ao lado.";
        await carregar();
    } catch (erro) {
        msg.className = "aviso erro";
        msg.textContent = erro.message;
    } finally {
        botao.disabled = false;
    }
}
