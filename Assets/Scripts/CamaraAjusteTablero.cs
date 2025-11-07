/*
    camaraajustetablero.cs
    
    ajusta una camara ortografica para que encuadre el tablero completo con un padding opcional

    recalcula automaticamente cuando cambian las dimensiones del tablero (aspect ratio/padding)

    funciona en modo edicion y en play, requiere un componente Camera en el mismo objeto
*/

using UnityEngine;


[RequireComponent(typeof(Camera))]
[ExecuteAlways]
public class CamaraAjusteTablero : MonoBehaviour
{
    public Tablero tablero;          // referencia al tablero a encuadrar
    public float padding = 0.5f;     // margen en unidades del mundo alrededor del tablero
    public bool auto_asignar_tablero = true;

    Camera cam;
    float ultimo_aspecto = -1f;
    int ultimo_ancho = -1, ultimo_alto = -1;
    float ultimo_padding = -1f;

    void OnEnable()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true; // camara ortografica para vista 2d

        tablero = FindFirstObjectByType<Tablero>();
        AjustarAhora();
    }

    void LateUpdate()
    {
        if (!tablero || !cam) return;

        //calculo del aspect ratio basado en el viewport de la camara
        float aspecto = cam.pixelHeight > 0 ? (float)cam.pixelWidth / cam.pixelHeight : 1f;

        // si cambian dimensiones, aspect o padding, recalcular
        if (tablero.ancho != ultimo_ancho || tablero.alto != ultimo_alto ||
            Mathf.Abs(aspecto - ultimo_aspecto) > 1e-4f ||
            Mathf.Abs(padding - ultimo_padding) > 1e-4f)
        {
            AjustarAhora(); //reajusto encuadre
        }
    }

    // realiza el ajuste de orthosize y centra la camara sobre el tablero
    public void AjustarAhora()
    {
        if (!tablero || !cam) return;

        float aspecto = cam.pixelHeight > 0 ? (float)cam.pixelWidth / cam.pixelHeight : 1f;

        int w = Mathf.Max(1, tablero.ancho);
        int h = Mathf.Max(1, tablero.alto);

        // orthosize es la mitad de la altura visible, tambien compruebo el limite por ancho
        float mitad_altura = h * 0.5f + padding;
        float mitad_altura_por_ancho = w * 0.5f / aspecto + padding;
        cam.orthographicSize = Mathf.Max(mitad_altura, mitad_altura_por_ancho);

        //centro la camara en el tablero asumiendo que el origen del tablero esta en (0,0)
        // y que las celdas son de tamano 1 unidad
        var pos = transform.position;
        float cx = (w - 1) * 0.5f;
        float cy = (h - 1) * 0.5f;
        transform.position = new Vector3(cx, cy, pos.z);

        // guarda (para saber si hace falta recalcular)
        ultimo_aspecto = aspecto;
        ultimo_ancho = w;
        ultimo_alto = h;
        ultimo_padding = padding;
    }

    //fuerza un recalculo en el siguiente LateUpdate
    public void Refrescar()
    {
        // la idea es obligar a recalcular
        ultimo_ancho = ultimo_alto = -1;
        ultimo_padding = -1f;
        ultimo_aspecto = -1f;
        AjustarAhora();
    }
}
