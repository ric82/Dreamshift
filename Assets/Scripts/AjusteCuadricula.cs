/*
    ajustecriadricula.cs
    
    alinea las posiciones de todos los hijos a una rejilla de tamaño fijo

    funciona en modo edicion por el [ExecuteAlways]

    no hace nada en play (solo valdrá para editar los niveles, esto lo filtra Update)

*/

using UnityEngine;


[ExecuteAlways]
public class AjusteCuadricula : MonoBehaviour
{
    public float tam_celda = 1f;          //tamaño de la celda de la cuadricula a la que se ajusta (en unidades del mundo)
    public bool ajustar_en_edicion = true; // si es true ajusta en modo edicion, si es false no hara nada

    void Update()
    {
        // en play no hace nada
        if (Application.isPlaying) return;
        if (!ajustar_en_edicion) return;

        // recorre todos los hijos
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            if (t == transform) continue; // el raiz lo descarto, solo interesan los hijos
            var p = t.localPosition; //pos actual

            // redondea x e y al multiplo mas cercano de tam_celda, deja z sin modificar
            t.localPosition = new Vector3(
                Mathf.Round(p.x / tam_celda) * tam_celda,
                Mathf.Round(p.y / tam_celda) * tam_celda,
                p.z
            );
        }
    }
}
