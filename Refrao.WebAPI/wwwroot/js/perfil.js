// Perfil: ver e editar os próprios dados; troca de senha exige a senha atual.
let perfil = null;

document.addEventListener("DOMContentLoaded", () => {
    if (!exigirLogin()) return;
    carregar();
    document.getElementById("formPerfil").addEventListener("submit", salvar);
});

async function carregar() {
    try {
        perfil = await api("/Auth/perfil");
        Sessao.atualizarUsuario(perfil);
        document.getElementById("cartaoPerfil").innerHTML = `
            <div class="avatar">${esc(perfil.nome.trim().charAt(0).toUpperCase())}</div>
            <h2 style="margin-bottom:.3rem">${esc(perfil.nome)}</h2>
            <span class="selo ${perfil.perfil === "Administrador" ? "rosa" : ""}">${perfil.perfil === "Administrador" ? "Administrador" : "Usuário comum"}</span>
            <dl>
                <div><dt>E-mail</dt><dd>${esc(perfil.email)}</dd></div>
                <div><dt>Membro desde</dt><dd>${formatarDataLonga(perfil.dataCadastro)}</dd></div>
                <div><dt>Situação</dt><dd>${perfil.ativo ? "Conta ativa" : "Conta desativada"}</dd></div>
            </dl>`;
        document.getElementById("nomePerfil").value = perfil.nome;
        document.getElementById("emailPerfil").value = perfil.email;
    } catch (e) {
        if (e.status === 401) { exigirLogin(); return; }
        document.getElementById("erroPerfil").textContent = e.message;
        document.getElementById("cartaoPerfil").innerHTML = "";
    }
}

async function salvar(e) {
    e.preventDefault();
    const form = e.target;
    limparErros(form);
    const msg = document.getElementById("mensagemPerfil");
    msg.className = "aviso"; msg.textContent = "";

    const nome = document.getElementById("nomePerfil");
    const email = document.getElementById("emailPerfil");
    const atual = document.getElementById("senhaAtual");
    const nova = document.getElementById("novaSenha");
    const confirma = document.getElementById("confirmaSenha");

    let ok = [validarObrigatorio(nome, "o nome"), validarEmail(email), validarSenha(nova, false)].every(Boolean);
    if (nova.value) {
        if (!atual.value) ok = marcarErro(atual, "Informe a senha atual para trocar a senha.") && false;
        if (nova.value !== confirma.value) ok = marcarErro(confirma, "As senhas não conferem.") && false;
    }
    if (!ok) return;

    const botao = form.querySelector("button[type=submit]");
    botao.disabled = true;
    try {
        await api(`/Usuarios/${perfil.id}`, {
            metodo: "PUT",
            corpo: { nome: nome.value.trim(), email: email.value.trim(), senhaAtual: atual.value || null, novaSenha: nova.value || null }
        });
        atual.value = nova.value = confirma.value = "";
        msg.className = "aviso sucesso";
        msg.textContent = "Dados atualizados com sucesso!";
        await carregar();
    } catch (erro) {
        msg.className = "aviso erro";
        msg.textContent = erro.message;
    } finally {
        botao.disabled = false;
    }
}
