import { useEffect, useRef, useState } from 'react'

// ATENÇÃO: este componente é só um TESTE VISUAL. A webcam do navegador não participa da conferência:
// nenhuma imagem é enviada ao back e nada aqui identifica instrumentos. O hardware real (câmera e
// balança) é acessado apenas pelo back-end, atrás das interfaces ICamera/IBalanca.
// Se VITE_CAMERA_STREAM existir, mostramos esse stream; senão, a webcam do navegador.
const STREAM = import.meta.env.VITE_CAMERA_STREAM

export default function Camera() {
  const video = useRef(null)
  const [erro, setErro] = useState('')
  useEffect(() => {
    if (STREAM) return
    let stream
    navigator.mediaDevices?.getUserMedia({ video: { facingMode: 'environment' } })
      .then(s => { stream = s; if (video.current) video.current.srcObject = s })
      .catch(() => setErro('Câmera indisponível. Permita o acesso no navegador ou defina VITE_CAMERA_STREAM.'))
    return () => stream?.getTracks().forEach(t => t.stop())
  }, [])
  // A mensagem fica fora do quadro do vídeo (que corta o que passa da borda), para aparecer inteira.
  return (
    <>
      <div className="camera">
        {STREAM ? <img src={STREAM} alt="Imagem da câmera" /> : <video ref={video} autoPlay playsInline muted />}
      </div>
      {erro && <p className="camera-erro" role="status">{erro}</p>}
    </>
  )
}
