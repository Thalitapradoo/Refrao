// Catálogo: pesquisa por título ou artista, filtro por artista e por gênero, paginação.
const estado = {
    pagina: 1,
    tamanhoPagina: 15,
    busca: parametro("busca") || "",
    artistaId: parametro("artistaId") || "",
    generoId: parametro("generoId") || ""
};

document.addEventListener("DOMContentLoaded", async () => {
    document.getElementById("busca").value = estado.busca;

    document.getElementById("formBusca").addEventListener("submit", e => {
        e.preventDefault();
        estado.busca = document.getElementById("busca").value.trim();
        estado.pagina = 1;
        carregar();
    });

    document.getElementById("filtroArtista").addEventListener("change", e => {
        estado.artistaId = e.target.value;
        estado.pagina = 1;
        carregar();
    });

    await carregarFiltros();
    carregar();
});

async function carregarFiltros() {
    try {
        const [artistas, generos] = await Promise.all([api("/Artistas"), api("/Generos")]);

        const select = document.getElementById("filtroArtista");
        select.innerHTML += artistas.map(a => `<option value="${a.id}">${esc(a.nome)}</option>`).join("");
        select.value = estado.artistaId;

        const chips = document.getElementById("filtroGenero");
        chips.innerHTML = `<button type="button" class="chip" data-id="">Todos os gêneros</button>` +
            generos.map(g => `<button type="button" class="chip" data-id="${g.id}" title="${esc(g.descricao || "")}">${esc(g.nome)}</button>`).join("");
        (chips.querySelector(`[data-id="${CSS.escape(estado.generoId)}"]`) || chips.querySelector('[data-id=""]')).classList.add("ativo");

        chips.addEventListener("click", e => {
            const chip = e.target.closest(".chip");
            if (!chip) return;
            chips.querySelectorAll(".chip").forEach(c => c.classList.toggle("ativo", c === chip));
            estado.generoId = chip.dataset.id;
            estado.pagina = 1;
            carregar();
        });
    } catch (e) {
        document.getElementById("erroCatalogo").textContent = e.message;
    }
}

async function carregar() {
    const lista = document.getElementById("listaMusicas");
    const erro = document.getElementById("erroCatalogo");
    erro.textContent = "";
    lista.innerHTML = `<div class="carregando"></div>`;

    const params = new URLSearchParams({ pagina: estado.pagina, tamanhoPagina: estado.tamanhoPagina });
    if (estado.busca) params.set("busca", estado.busca);
    if (estado.artistaId) params.set("artistaId", estado.artistaId);
    if (estado.generoId) params.set("generoId", estado.generoId);

    try {
        const { items, total } = await api(`/Musicas?${params}`);
        document.getElementById("resumo").textContent = total === 1 ? "1 música encontrada" : `${total} músicas encontradas`;
        lista.innerHTML = items.length
            ? items.map(cardMusica).join("")
            : `<div class="vazio">Nenhuma música combina com a pesquisa e os filtros.</div>`;
        paginacao(total);
    } catch (e) {
        erro.textContent = e.message;
        lista.innerHTML = "";
        document.getElementById("resumo").textContent = "";
        document.getElementById("paginacao").innerHTML = "";
    }
}

function paginacao(total) {
    const nav = document.getElementById("paginacao");
    const paginas = Math.max(1, Math.ceil(total / estado.tamanhoPagina));
    if (paginas <= 1) { nav.innerHTML = ""; return; }
    nav.innerHTML = `
        <button class="botao secundario pequeno" ${estado.pagina <= 1 ? "disabled" : ""} data-ir="-1">← Anterior</button>
        <span>página ${estado.pagina} de ${paginas}</span>
        <button class="botao secundario pequeno" ${estado.pagina >= paginas ? "disabled" : ""} data-ir="1">Próxima →</button>`;
    nav.querySelectorAll("[data-ir]").forEach(b => b.addEventListener("click", () => {
        estado.pagina += Number(b.dataset.ir);
        carregar();
        window.scrollTo({ top: 0, behavior: "smooth" });
    }));
}
