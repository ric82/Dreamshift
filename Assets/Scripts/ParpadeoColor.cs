/*
    parpadeocolor.cs
    
    alterna el color del spriterenderer entre dos colores (colorA y colorB) a una frecuencia que fijo

    se usa cuando una entidad tiene simultaneamente las propiedades rojo y azul

    cambiará de un color al otro según la frecuencia (cada medio periodo)
*/

using UnityEngine;

public class ParpadeoColor : MonoBehaviour
{
    public SpriteRenderer sr;      //spriterenderer objetivo (si esta vacio se busca en awake)
    public Color colorA = Color.red;
    public Color colorB = Color.blue;
    public float period = 0.6f;    // segundos para un ciclo completo A->B->A

    void Awake()
    {
        // si no hay referencia, intenta obtenerla del propio objeto o de un hijo
        if (!sr) sr = GetComponent<SpriteRenderer>();
        if (!sr) sr = GetComponentInChildren<SpriteRenderer>();
    }

    void Update()
    {
        if (!sr) return;

        //cada medio periodo cambia de color
        float medio = Mathf.Max(0.01f, period * 0.5f);              //se evitan periodos muy chicos
        bool usarA = (Mathf.FloorToInt(Time.time / medio) % 2) == 0; // 0->A, 1->B, 0->A, %2 me dice si estoy en el tramo par (A) o impar (B)
        sr.color = usarA ? colorA : colorB; // asigno el color por el tramo calculado.
    }
}
