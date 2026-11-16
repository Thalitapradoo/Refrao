// Lista de artistas.
document.addEventListener("DOMContentLoaded", async () => {
    const lista = document.getElementById("listaArtistas");
    try {
        const artistas = await api("/Artistas");
        lista.innerHTML = artistas.length
            ? artistas.map(cardArtista).join("")
            : `<div class="vazio">Nenhum artista cadastrado.</div>`;
    } catch (e) {
        lista.innerHTML = "";
        document.getElementById("erroArtistas").textContent = e.message;
    }
});
