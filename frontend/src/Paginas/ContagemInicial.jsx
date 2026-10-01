import { useEffect, useState } from "react";

const API = "http://localhost:5000/api/procedimento-atual";

export default function ContagemInicial() {
  const [procedimento, setProcedimento] = useState(null);

  function carregar() {
    fetch(API).then((r) => r.json()).then(setProcedimento);
  }

  useEffect(() => {
    carregar();
  }, []);

  async function registrarEntrada(itemId, quantidade) {
    await fetch(`${API}/itens/${itemId}/entrada`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ quantidade }),
    });
    carregar();
  }

  if (!procedimento) return <p>Carregando...</p>;

  return (
    <div>
      <h1 className="text-2xl font-bold text-slate-900">Contagem Inicial</h1>
      <p className="text-slate-500 mt-1">
        Conferir todos os instrumentos antes do início de: {procedimento.tipo}
      </p>

      <div className="mt-6 grid gap-2 max-w-xl">
        {procedimento.itens.map((item) => (
          <div key={item.id} className="bg-white border border-slate-200 rounded-lg p-4 flex items-center justify-between">
            <div>
              <p className="font-medium">{item.instrumento}</p>
              <p className="text-xs text-slate-400">
                Previsto: {item.quantidadePrevista} · Já contado: {item.quantidadeInicial}
              </p>
            </div>
            <button
              onClick={() => registrarEntrada(item.id, 1)}
              className="bg-blue-700 hover:bg-blue-800 text-white text-sm px-3 py-1.5 rounded-lg"
            >
              + Registrar 1 unidade
            </button>
          </div>
        ))}
      </div>
    </div>
  );
}