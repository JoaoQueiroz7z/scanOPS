import { useEffect, useState } from "react";

const API = "http://localhost:5000/api/procedimento-atual";

export default function ContagemFinal() {
  const [procedimento, setProcedimento] = useState(null);
  const [resultado, setResultado] = useState(null);
  const [justificativa, setJustificativa] = useState("");

  function carregar() {
    fetch(API).then((r) => r.json()).then(setProcedimento);
  }

  useEffect(() => {
    carregar();
  }, []);

  async function registrarSaida(itemId, quantidade, pesoMedido) {
    await fetch(`${API}/itens/${itemId}/saida`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ quantidade, pesoMedido, identificadoPorCamera: true }),
    });
    carregar();
  }

  async function finalizar() {
    const resposta = await fetch(`${API}/finalizar`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ justificativa: justificativa || null }),
    });
    const dados = await resposta.json();
    setResultado({ ok: resposta.ok, ...dados });
  }

  if (!procedimento) return <p>Carregando...</p>;

  return (
    <div>
      <h1 className="text-2xl font-bold text-slate-900">Contagem Final</h1>
      <p className="text-slate-500 mt-1">Comparar o que entrou com o que saiu.</p>

      <div className="mt-6 grid gap-2 max-w-xl">
        {procedimento.itens.map((item) => (
          <div key={item.id} className="bg-white border border-slate-200 rounded-lg p-4">
            <p className="font-medium">{item.instrumento}</p>
            <p className="text-xs text-slate-400">
              Entrou: {item.quantidadeInicial} · Saiu: {item.quantidadeFinal} · Diferença: {item.diferenca}
            </p>
            <div className="mt-2 flex gap-2">
              <button
                onClick={() => registrarSaida(item.id, item.quantidadeInicial, item.quantidadeInicial * 50)}
                className="bg-slate-700 hover:bg-slate-800 text-white text-sm px-3 py-1.5 rounded-lg"
              >
                Simular saída completa (peso OK)
              </button>
              <button
                onClick={() => registrarSaida(item.id, item.quantidadeInicial - 1, 0)}
                className="border border-red-300 text-red-600 text-sm px-3 py-1.5 rounded-lg"
              >
                Simular item faltando
              </button>
            </div>
          </div>
        ))}
      </div>

      <div className="mt-6 max-w-xl">
        <input
          placeholder="Justificativa (se houver divergência)"
          value={justificativa}
          onChange={(e) => setJustificativa(e.target.value)}
          className="w-full border border-slate-300 rounded-lg px-3 py-2"
        />
        <button
          onClick={finalizar}
          className="mt-3 bg-green-600 hover:bg-green-700 text-white px-4 py-2 rounded-lg"
        >
          Finalizar procedimento
        </button>
      </div>

      {resultado && (
        <div className={`mt-4 max-w-xl p-4 rounded-lg ${resultado.ok ? "bg-green-50 text-green-800" : "bg-red-50 text-red-800"}`}>
          {resultado.ok ? `Status: ${resultado.status}` : resultado.erro}
        </div>
      )}
    </div>
  );
}