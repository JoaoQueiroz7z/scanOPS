import { useEffect, useState } from "react";

export default function App() {
  const [mensagem, setMensagem] = useState("carregando...");

  useEffect(() => {
    fetch("http://localhost:5000/api/teste")
      .then((r) => r.json())
      .then((data) => setMensagem(data.mensagem));
  }, []);

  return <h1>{mensagem}</h1>;
}