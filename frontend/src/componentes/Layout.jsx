import { Outlet, Link, useLocation } from "react-router-dom";

const MENU = [
  { label: "Visão Geral", path: "/visao-geral" },
  { label: "Paciente", path: "/paciente" },
  { label: "Novo Procedimento", path: "/novo-procedimento" },
  { label: "Instrumentos", path: "/instrumentos" },
  { label: "Contagem Inicial", path: "/contagem-inicial" },
  { label: "Procedimento em Andamento", path: "/procedimento-andamento" },
  { label: "Contagem Final", path: "/contagem-final" },
  { label: "Relatório Final", path: "/relatorio-final" },
];

export default function Layout() {
  const location = useLocation();

  return (
    <div className="min-h-screen flex bg-slate-50">
      <aside className="w-64 bg-slate-900 text-white flex flex-col p-4">
        <div className="text-lg font-bold mb-8">Device Tech</div>
        <nav className="flex flex-col gap-1">
          {MENU.map((item) => (
            <Link
              key={item.path}
              to={item.path}
              className={`px-3 py-2 rounded-lg text-sm ${
                location.pathname === item.path
                  ? "bg-blue-700"
                  : "hover:bg-slate-800 text-slate-300"
              }`}
            >
              {item.label}
            </Link>
          ))}
        </nav>
      </aside>

      <main className="flex-1 p-8">
        <Outlet />
      </main>
    </div>
  );
}