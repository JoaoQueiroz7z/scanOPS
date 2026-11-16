// Converte as respostas do back (Api/Dtos/ProcedimentoDtos.cs) para o formato usado nas telas.

// ItemProcedimentoResposta
export const normalizarItem = i => ({
  id: i.id,
  nome: i.instrumento,              // nome do instrumento (texto)
  previsto: i.quantidadePrevista,
  entrada: i.quantidadeInicial,     // contagem inicial: unidades que entraram em campo
  saida: i.quantidadeFinal,         // contagem final: unidades que voltaram
  diferenca: i.diferenca,
  pesoMedido: i.pesoMedido,
  identificadoPorCamera: i.identificadoPorCamera,
  status: i.status,                 // Pendente | Conferido | Divergente
})

// ProcedimentoResposta
export const normalizarProcedimento = r => ({
  id: r.id,
  status: r.status,                 // Planejado | EmAndamento | Finalizado | Cancelado
  tipo: r.tipo ?? '',
  sala: r.sala ?? '',
  paciente: r.paciente || '—',
  cirurgiao: r.cirurgiao || '—',
  itens: (r.itens ?? []).map(normalizarItem),
})

// Valores do enum StatusProcedimento do back -> classe usada nas telas
const STATUS = { Planejado: 'planejado', EmAndamento: 'andamento', Finalizado: 'finalizado', Cancelado: 'cancelado' }
export const statusDe = s => STATUS[s] ?? 'outro'
