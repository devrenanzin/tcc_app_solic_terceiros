// Formulário da demanda (seção 8.1): listas em cascata do QQP, campos condicionais, prévia do custo
// e rascunho guardado só neste navegador por 3 dias desde o último salvamento (UC02, UC17).
// As regras de negócio ficam no servidor; aqui só há apoio ao preenchimento.
(() => {
    "use strict";

    const form = document.querySelector("form[data-formulario-demanda]");
    if (!form) {
        return;
    }

    const moeda = new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" });
    const itens = JSON.parse(document.getElementById("itens-qqp").textContent);

    // ---------- Listas em cascata do QQP ----------

    // SUPOSIÇÃO (S9): o item sem classificação ("-" na planilha) aparece como "Sem classificação".
    const SEM_CLASSIFICACAO = "__sem__";
    const niveis = ["regiao", "funcao", "classificacao", "nivel", "carga"];
    const valorDe = {
        regiao: (x) => x.r,
        funcao: (x) => x.f,
        classificacao: (x) => x.k ?? SEM_CLASSIFICACAO,
        nivel: (x) => x.n,
        carga: (x) => String(x.h),
    };
    const textoDe = {
        regiao: (v) => v.replace(/^QQP\s+/i, ""),
        funcao: (v) => v,
        classificacao: (v) => (v === SEM_CLASSIFICACAO ? "Sem classificação" : v),
        nivel: (v) => v,
        carga: (v) => `${v} horas`,
    };
    const ordemDe = {
        nivel: (v) => itens.find((x) => x.n === v)?.o ?? 0,
        carga: (v) => Number(v),
    };

    const listas = niveis.map((n) => form.querySelector(`[data-qqp="${n}"]`));
    const listaCodigo = form.querySelector('[data-qqp="item"]');
    const caixaCodigo = form.querySelector("[data-qqp-codigo]");
    const campoItem = form.querySelector('[name="Entrada.ItemQqpId"]');
    const resumo = form.querySelector("[data-qqp-resumo]");

    const compativeis = (ate) =>
        itens.filter((x) => listas.slice(0, ate).every((lista, i) => lista.value !== "" && valorDe[niveis[i]](x) === lista.value));

    function opcoes(lista, valores, rotulo, vazio) {
        lista.replaceChildren(new Option(vazio, ""), ...valores.map((v) => new Option(rotulo(v), v)));
    }

    function preencher(indice) {
        const nivel = niveis[indice];
        const lista = listas[indice];
        const anterior = lista.value;
        const habilitada = indice === 0 || listas[indice - 1].value !== "";
        const valores = habilitada ? [...new Set(compativeis(indice).map(valorDe[nivel]))] : [];
        const ordem = ordemDe[nivel];
        valores.sort(ordem ? (a, b) => ordem(a) - ordem(b) : (a, b) => textoDe[nivel](a).localeCompare(textoDe[nivel](b), "pt-BR"));

        opcoes(lista, valores, textoDe[nivel], habilitada ? "Selecione" : "—");
        lista.disabled = !habilitada;
        lista.value = valores.includes(anterior) ? anterior : valores.length === 1 ? valores[0] : "";
    }

    function escolherItem() {
        const completos = listas.every((l) => l.value !== "");
        const candidatos = completos ? compativeis(listas.length) : [];

        // Mesma combinação com mais de um item (códigos 466 e 467): o Solicitante escolhe pelo código.
        if (candidatos.length > 1) {
            const atual = listaCodigo.value || campoItem.value;
            opcoes(listaCodigo, candidatos.map((x) => x.i), (id) => `Código ${candidatos.find((x) => x.i === id).c}`, "Selecione");
            listaCodigo.value = candidatos.some((x) => x.i === atual) ? atual : "";
            caixaCodigo.classList.remove("d-none");
        } else {
            listaCodigo.replaceChildren();
            caixaCodigo.classList.add("d-none");
        }

        const escolhido = candidatos.length === 1 ? candidatos[0] : candidatos.find((x) => x.i === listaCodigo.value);
        campoItem.value = escolhido?.i ?? "";
        resumo.hidden = !escolhido;
        if (escolhido) {
            resumo.querySelector('[data-qqp-campo="codigo"]').textContent = escolhido.c;
            resumo.querySelector('[data-qqp-campo="piso"]').textContent = moeda.format(escolhido.p);
            resumo.querySelector('[data-qqp-campo="preco"]').textContent = moeda.format(escolhido.u);
        }
        avisarCusto();
    }

    function atualizarDesde(indice) {
        for (let i = indice; i < listas.length; i++) {
            preencher(i);
        }
        escolherItem();
    }

    /** Recoloca as escolhas da cascata, uma lista por vez; serve ao item gravado e ao rascunho. */
    function aplicarCascata(valores, codigo) {
        listas.forEach((lista, i) => {
            preencher(i);
            if (valores[i] && [...lista.options].some((o) => o.value === valores[i])) {
                lista.value = valores[i];
            }
        });
        escolherItem();
        if (codigo && !caixaCodigo.classList.contains("d-none")) {
            listaCodigo.value = codigo;
            escolherItem();
        }
    }

    listas.forEach((lista, i) => lista.addEventListener("change", () => atualizarDesde(i + 1)));
    listaCodigo.addEventListener("change", escolherItem);

    const itemInicial = itens.find((x) => x.i === campoItem.value);
    aplicarCascata(itemInicial ? niveis.map((n) => valorDe[n](itemInicial)) : [], itemInicial?.i);

    // ---------- Campos condicionais e contrato do corredor ----------

    function atualizarCondicionais() {
        form.querySelectorAll("[data-depende-de]").forEach((bloco) => {
            const marcado = document.getElementById(bloco.dataset.dependeDe)?.checked ?? false;
            bloco.hidden = !marcado;
        });

        const corredor = form.querySelector('[name="Entrada.CorredorId"]');
        const alvo = document.querySelector(corredor.dataset.contratoAlvo);
        const contrato = corredor.selectedOptions[0]?.dataset.contrato;
        alvo.textContent = contrato ?? "Escolha o corredor";
        alvo.classList.toggle("preenchido", Boolean(contrato));
    }

    form.addEventListener("change", atualizarCondicionais);
    atualizarCondicionais();

    // ---------- Prévia do custo (calculada no servidor, pelo domínio) ----------

    function avisarCusto() {
        document.body.dispatchEvent(new Event("custo-mudou"));
    }

    form.addEventListener("input", (evento) => {
        if (evento.target.matches("[data-afeta-custo]")) {
            avisarCusto();
        }
    });
    form.addEventListener("change", (evento) => {
        if (evento.target.matches("[data-afeta-custo]")) {
            avisarCusto();
        }
    });

    // ---------- Rascunho só no navegador (UC02, UC17) ----------

    const chave = form.dataset.rascunho;
    if (!chave) {
        return;
    }

    const TRES_DIAS = 3 * 24 * 60 * 60 * 1000;
    const aviso = document.querySelector("[data-aviso-rascunho]");
    const textoAviso = document.querySelector("[data-texto-rascunho]");
    const salvoEm = document.querySelector("[data-salvo-em]");
    const hora = new Intl.DateTimeFormat("pt-BR", { hour: "2-digit", minute: "2-digit" });
    const diaHora = new Intl.DateTimeFormat("pt-BR", { day: "2-digit", month: "2-digit", hour: "2-digit", minute: "2-digit" });

    function ler() {
        try {
            const rascunho = JSON.parse(localStorage.getItem(chave));
            if (!rascunho) {
                return null;
            }
            if (Date.now() - rascunho.salvoEm > TRES_DIAS) {
                localStorage.removeItem(chave);
                return null;
            }
            return rascunho;
        } catch {
            return null;
        }
    }

    function campos() {
        const valores = {};
        for (const campo of form.elements) {
            if (!campo.name?.startsWith("Entrada.") || campo.type === "hidden" || campo.type === "file") {
                continue;
            }
            if (campo.type === "checkbox") {
                if (campo.name === "Entrada.Racs") {
                    valores[campo.name] ??= [];
                    if (campo.checked) {
                        valores[campo.name].push(campo.value);
                    }
                } else {
                    valores[campo.name] = campo.checked;
                }
            } else {
                valores[campo.name] = campo.value;
            }
        }
        return valores;
    }

    function mostrarAviso(rascunho, restaurado) {
        const momento = new Date(rascunho.salvoEm);
        const expira = new Date(rascunho.salvoEm + TRES_DIAS);
        textoAviso.textContent = restaurado
            ? `Rascunho de ${diaHora.format(momento)} recuperado. Ele fica só neste navegador até ${diaHora.format(expira)}.`
            : `Rascunho salvo neste navegador às ${hora.format(momento)}. Fica guardado até ${diaHora.format(expira)}.`;
        aviso.hidden = false;
        salvoEm.textContent = `Rascunho salvo às ${hora.format(momento)}.`;
    }

    let espera;
    function salvar() {
        clearTimeout(espera);
        espera = setTimeout(() => {
            const rascunho = {
                salvoEm: Date.now(),
                campos: campos(),
                qqp: listas.map((l) => l.value),
                codigo: listaCodigo.value,
            };
            try {
                localStorage.setItem(chave, JSON.stringify(rascunho));
                mostrarAviso(rascunho, false);
            } catch {
                salvoEm.textContent = "Este navegador não permitiu salvar o rascunho.";
            }
        }, 400);
    }

    function restaurar(rascunho) {
        for (const [nome, valor] of Object.entries(rascunho.campos ?? {})) {
            const elementos = form.querySelectorAll(`[name="${nome}"]`);
            elementos.forEach((campo) => {
                if (campo.type === "checkbox") {
                    campo.checked = Array.isArray(valor) ? valor.includes(campo.value) : Boolean(valor);
                } else {
                    campo.value = valor;
                }
            });
        }
        aplicarCascata(rascunho.qqp ?? [], rascunho.codigo);
        atualizarCondicionais();
        avisarCusto();
        mostrarAviso(rascunho, true);
    }

    if (form.dataset.restaurar === "sim") {
        const rascunho = ler();
        if (rascunho) {
            restaurar(rascunho);
        }
    }

    form.addEventListener("input", salvar);
    form.addEventListener("change", salvar);

    document.querySelector("[data-descartar-rascunho]")?.addEventListener("click", () => {
        try {
            localStorage.removeItem(chave);
        } finally {
            window.location.replace(window.location.pathname);
        }
    });
})();
