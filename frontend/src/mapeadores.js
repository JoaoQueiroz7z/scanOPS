// Converte as respostas do back (Api/Dtos/ProcedimentoDtos.cs) para o formato usado nas telas.

// ItemProcedimentoResposta
export const normalizarItem = i => ({
  id: i.id,
  nome: i.instrumento,              // nome do instrumento (texto)
  previsto: i.quantidadePrevista,
  entrada: i.quantidadeInicial,     // contagem inicial: unidades que entraram em campo
  saida: i.quantidadeFinal,         // contagem final: unidades que voltaram
  diferenca: i.diferenca,           // entrada - saída
  pesoMedido: i.pesoMedido,         // peso gravado no back
  pesoReferencia: i.pesoReferencia, // peso de uma unidade
  pesoEsperado: i.pesoEsperado,     // referência × entrada
  toleranciaPeso: i.toleranciaPeso,
  pesoConfere: i.pesoConfere,             // RN4, calculado pelo back a qualquer momento
  possuiDivergencia: i.possuiDivergencia, // contagem OU peso não confere
  identificadoPorCamera: i.identificadoPorCamera,
  status: i.status,                 // Pendente | Conferido | Divergente (só muda ao finalizar)
})

// ProcedimentoResposta
// ProcedimentoResposta e ProcedimentoDetalheResposta (o detalhe traz os campos a mais do histórico)
export const normalizarProcedimento = r => ({
  id: r.id,
  status: r.status,                 // Planejado | EmAndamento | Finalizado | Cancelado
  tipo: r.tipo ?? '',
  sala: r.sala ?? '',               // já vem padronizada do back: "Sala 2"
  descricao: r.descricao ?? '',
  paciente: r.paciente || '—',
  cirurgiao: r.cirurgiao || '—',
  instrumentista: r.instrumentista || '—',
  inicio: r.dataHoraInicio ?? null,
  fim: r.dataHoraFim ?? null,
  duracaoMinutos: r.duracaoMinutos ?? null,
  justificativa: r.justificativa ?? null,
  motivoCancelamento: r.motivoCancelamento ?? null,
  itens: (r.itens ?? []).map(normalizarItem),
})

export const ROTULO_STATUS = { Planejado: 'Planejado', EmAndamento: 'Em andamento', Finalizado: 'Finalizado', Cancelado: 'Cancelado' }

// Datas do back -> "17/11/2026 14:05"
export const dataHora = iso => !iso ? '—'
  : new Date(iso).toLocaleString('pt-BR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' })

export const duracao = minutos => minutos === null || minutos === undefined ? '—'
  : minutos < 60 ? `${minutos} min` : `${Math.floor(minutos / 60)} h ${String(minutos % 60).padStart(2, '0')} min`

// A sala chega como foi cadastrada: "Sala 1" (dados de demonstração) ou só "03" (tela Novo procedimento).
// Só acrescenta "Sala" quando o valor ainda não começa com essa palavra.
export const rotuloSala = sala => !sala ? '' : /^sala\b/i.test(sala.trim()) ? sala.trim() : `Sala ${sala.trim()}`

// Valores do enum StatusProcedimento do back -> classe usada nas telas
const STATUS = { Planejado: 'planejado', EmAndamento: 'andamento', Finalizado: 'finalizado', Cancelado: 'cancelado' }
export const statusDe = s => STATUS[s] ?? 'outro'
